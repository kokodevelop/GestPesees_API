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
| `src/GestPesees.Agent.Core` | Bibliothèque `netstandard2.0` commune aux agents : lecture BD/CSV, curseur, client API, boucle de synchro |
| `src/GestPesees.Agent` | Agent service Windows **.NET 8** (Windows 10 1607+ / Server 2012+) |
| `src/GestPesees.Agent.Net48` | Agent service Windows **.NET Framework 4.8** (Windows 7 SP1+ / Server 2008 R2 SP1+), mêmes `appsettings.json` et comportement |
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
4. Agent .NET Framework 4.8 (anciens postes) : `dotnet build src/GestPesees.Agent.Net48 -c Release` (ou Visual Studio), copier le dossier `bin\Release\net48` sur le poste (prérequis : .NET Framework 4.8 installé), éditer `appsettings.json` (même format que l'agent .NET 8), tester en console en lançant `GestPesees.Agent.Net48.exe`, puis
   `sc create "GestPesees Agent" binPath= "C:\GestPesees\GestPesees.Agent.Net48.exe" start= auto`. Les journaux sont dans `logs\agent-AAAAMMJJ.log` (30 jours conservés).
5. Tests : `dotnet test`.

## Source alternative : dossier CSV
Pour un éditeur qui refuse l'accès à sa base, le logiciel de pesée (ou sa tâche d'export planifiée) dépose des CSV dans un dossier et l'agent les lit : `"Provider": "Csv"` (exemple `EditeurC` dans `appsettings.json`).
- `ColumnMap` associe les noms standard aux colonnes du CSV (par nom d'en-tête, ou par n° de colonne base 0 si `HasHeader` est faux). `SourceId` et `WeighedAt` sont obligatoires ; les colonnes non mappées partent dans `ExtraJson`.
- Réglages d'export : `Delimiter`, `Encoding` (ex. `windows-1252`), `Culture`, `DateFormat`, `DecimalSeparator`.
- Fonctionne avec un fichier par pesée comme avec un fichier journalier complété au fil de l'eau : l'agent mémorise, par fichier, le nombre de lignes déjà envoyées. Une ligne en cours d'écriture (sans saut de ligne final) est ignorée jusqu'au cycle suivant.
- Un fichier remplacé par un autre de même nom (première ligne différente) est relu en entier ; l'API dédoublonne.
- Une ligne dont la date est illisible est envoyée quand même et rejetée par l'API (compteur `Rejected` dans `SyncLog`), pour ne pas bloquer la source.
- Penser à purger régulièrement les vieux fichiers du dossier d'export.

## Éditeur GestPontPro (MySQL)
Fichiers : `samples/gestpontpro.source.json` (source à ajouter dans `Agent:Sources`), `db/mysql/gestpontpro_requete.sql` (la même requête, lisible, commentée), `db/mysql/gestpontpro_lecture_seule.sql` (compte `SELECT` seul).
- Seules les pesées **terminées** (`Statut = 'TERMINEE'`, seconde pesée faite) sont envoyées. `WeighedAt` est la date de **seconde pesée** (`DatePesee` est la date d'import/création, pas celle de la pesée).
- **Curseur composite** `(DateDeuxiemePesee, IdPesee)` et non `IdPesee` seul : l'ID est attribué à la première pesée, un camion qui termine plus tard qu'un autre serait sinon perdu. Un délai de sécurité de 10 s évite de dépasser une ligne en cours d'écriture (suppose que l'horloge du serveur MySQL est celle du poste de pesée).
- `SourceId` = `IdPesee`. `NumTicket`, mouvement (entrant/sortant), client, fournisseur, chauffeur, transporteur, origine, destination, commande, refaction, prix, etc. partent dans `ExtraJson`.
- Limite : une pesée modifiée ou annulée **après** son envoi n'est pas renvoyée (voir « Limites connues »).

## Anciens postes (agent .NET Framework 4.8)
- Installer un seul des deux agents par poste ; ils lisent le même `appsettings.json` et le même fichier de curseur `state.json`.
- **TLS 1.2** : l'agent le force, mais Windows 7 doit avoir le SP1 et la mise à jour KB3140245 (activation de TLS 1.2 pour WinHTTP/.NET). Sans cela, l'appel HTTPS vers l'API échoue.
- **SQL Server ancien** (2008/2012) : `Microsoft.Data.SqlClient` chiffre par défaut. Si la connexion échoue, ajouter `Encrypt=False` (ou `TrustServerCertificate=true`) à la chaîne de connexion de la source.
- Alternative : un seul agent .NET 8 sur un PC récent du site qui atteint la base du pont via le réseau.

## Limites connues
- Le curseur doit être strictement croissant (ID auto-incrémenté ou horodatage unique). Les lignes modifiées après envoi ne sont pas renvoyées.
