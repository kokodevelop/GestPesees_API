# Essai de bout en bout (source CSV)

1. **Base** : sur un SQL Server de test, créer une base `GestPesees`, puis exécuter dans l'ordre
   `db/001_schema.sql` et `db/002_site_test.sql` (site `S001`, clé `cle-test-S001-changez-moi`).
2. **API** : vérifier `ConnectionStrings:Central` dans `src/GestPesees.Api/appsettings.json`, puis
   `dotnet run --project src/GestPesees.Api --urls http://localhost:5000`
   (en HTTP pour l'essai ; en production, HTTPS obligatoire).
3. **Dossier d'export** : créer `C:\GestPesees\Test\Export` et y copier `samples/pesees_exemple.csv`
   (fichier encodé en Windows-1252, séparateur `;`, décimales à virgule, comme le suppose `EditeurC`).
4. **Agent** : copier `samples/appsettings.test.json` par-dessus `src/GestPesees.Agent/appsettings.json`
   (ou celui de `bin\Debug\net8.0`), puis `dotnet run --project src/GestPesees.Agent`.
   Résultat attendu dans la console : `Source EditeurC : 6/6 pesées acceptées`.
5. **Contrôle** : requêtes commentées à la fin de `db/002_site_test.sql`.

## Scénarios à rejouer
| Action | Résultat attendu |
|---|---|
| Laisser tourner l'agent (cycle de 10 s) | Rien de nouveau : le curseur est mémorisé dans `state.json` |
| Ajouter une ligne `T0007;...` à la fin du CSV | Seule T0007 est envoyée |
| Supprimer `state.json` et relancer l'agent | Tout est renvoyé, mais `dbo.Pesee` reste à 7 lignes (idempotence) |
| Copier `pesees_exemple_erreur.csv` dans le dossier | `1/2` ou `2/2` selon la ligne : `T0102` (date illisible) est rejetée, visible dans `SyncLog.Rejected` |
| Arrêter l'API, ajouter une ligne, relancer l'API | Erreur journalisée tant que l'API est coupée, puis envoi au cycle suivant sans perte |
| Mettre une mauvaise `ApiKey` | Erreur 401 journalisée, curseur inchangé |
