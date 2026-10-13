# GestPesees API

Collecte des pesées de ponts bascules (logiciels d'éditeurs différents, MySQL ou SQL Server) vers une base centrale SQL Server, **sans modifier les logiciels de pesée**.

```
BD éditeur A (MySQL) ─┐
BD éditeur B (SQL Srv)┼─ Agent (service Windows, 1 par site) ─HTTPS sortant─> API ─> BD centrale
```

## Projets
| Projet | Rôle |
|---|---|
| `src/GestPesees.Contracts` | DTO partagés (pesée, lot, résultat) |
| `src/GestPesees.Api` | API ASP.NET Core 8 : `POST /api/v1/pesees/batch`, `GET /api/v1/pesees/ping` |
| `src/GestPesees.Agent` | Service Windows : lit les BD éditeurs en lecture seule et pousse vers l'API |
| `db/001_schema.sql` | Schéma de la base centrale |
| `tests/` | Tests xUnit de l'agent |

## Principes
- **Lecture seule** : l'agent utilise un compte SQL `SELECT` uniquement sur la base de l'éditeur.
- **Une requête par éditeur, dans `appsettings.json`** : on ajoute un éditeur sans recompiler. La requête mappe les colonnes de l'éditeur vers les alias communs (`Wm`, `SourceId`, `WeighedAt`, `Plate`, `Product`, `ThirdParty`, `GrossKg`, `TareKg`, `NetKg`). Les autres colonnes partent dans `ExtraJson`.
- **Curseur (`Wm`)** : ID croissant ou date. Il n'avance qu'après accusé de réception de l'API ; la base de l'éditeur sert de file d'attente (coupure réseau = reprise au même point).
- **Idempotence** : clé unique `(SiteCode, Source, SourceId)` + `MERGE` côté API. Un renvoi ne crée pas de doublon.
- **Site** : `SiteCode`/`SiteName` sont ajoutés par l'agent ; l'API vérifie que le code correspond à la clé API (X-Api-Key, stockée hachée SHA-256) et reprend le nom de la table `Site`.

## Mise en route
1. Exécuter `db/001_schema.sql`, puis créer chaque site (voir l'exemple en fin de script).
2. API : renseigner `ConnectionStrings:Central`, `dotnet run --project src/GestPesees.Api` (HTTPS requis en production, derrière IIS/Kestrel).
3. Agent, sur chaque site : `dotnet publish src/GestPesees.Agent -c Release -r win-x64 --self-contained -o publish`, éditer `appsettings.json`, puis
   `sc create "GestPesees Agent" binPath= "C:\GestPesees\GestPesees.Agent.exe" start= auto`.
4. Tests : `dotnet test`.

## Source alternative : dossier CSV
Pour un éditeur qui refuse l'accès à sa base, le logiciel de pesée (ou sa tâche d'export planifiée) dépose des CSV dans un dossier et l'agent les lit : `"Provider": "Csv"` (exemple `EditeurC` dans `appsettings.json`).
- `ColumnMap` associe les noms standard aux colonnes du CSV (par nom d'en-tête, ou par n° de colonne base 0 si `HasHeader` est faux). `SourceId` et `WeighedAt` sont obligatoires ; les colonnes non mappées partent dans `ExtraJson`.
- Réglages d'export : `Delimiter`, `Encoding` (ex. `windows-1252`), `Culture`, `DateFormat`, `DecimalSeparator`.
- Fonctionne avec un fichier par pesée comme avec un fichier journalier complété au fil de l'eau : l'agent mémorise, par fichier, le nombre de lignes déjà envoyées. Une ligne en cours d'écriture (sans saut de ligne final) est ignorée jusqu'au cycle suivant.
- Un fichier remplacé par un autre de même nom (première ligne différente) est relu en entier ; l'API dédoublonne.
- Une ligne dont la date est illisible est envoyée quand même et rejetée par l'API (compteur `Rejected` dans `SyncLog`), pour ne pas bloquer la source.
- Penser à purger régulièrement les vieux fichiers du dossier d'export.

## Limites connues
- .NET 8 exige Windows 10 1607+ / Server 2012+. Les postes plus anciens (Windows 7/8.1) nécessiteraient un agent .NET Framework 4.8 ; le projet n'en contient pas encore.
- Le curseur doit être strictement croissant (ID auto-incrémenté ou horodatage unique). Les lignes modifiées après envoi ne sont pas renvoyées.
