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

## Limites connues
- .NET 8 exige Windows 10 1607+ / Server 2012+. Les postes plus anciens (Windows 7/8.1) nécessiteraient un agent .NET Framework 4.8 ; le projet n'en contient pas encore.
- Le curseur doit être strictement croissant (ID auto-incrémenté ou horodatage unique). Les lignes modifiées après envoi ne sont pas renvoyées.
- Pour les éditeurs qui refusent l'accès à leur base : prévoir une source alternative (dossier d'export CSV/XML surveillé) — non implémentée.
