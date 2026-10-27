-- Requete de l'agent pour GestPontPro (MySQL). Colle telle quelle (sur une ligne) dans "Query" de la source.
-- Alias obligatoires pour l'agent : Wm, SourceId, WeighedAt, Plate, Product, ThirdParty, GrossKg, TareKg, NetKg.
-- Les autres colonnes partent dans ExtraJson cote base centrale.
SELECT
  -- Curseur = (date de fin de pesee, IdPesee). Il ne depend pas de IdPesee seul : l'ID est attribue a la
  -- 1ere pesee, alors qu'un camion peut finir apres un autre (sinon sa pesee serait perdue).
  TIMESTAMPDIFF(SECOND, '2000-01-01', p.DateDeuxiemePesee) * 100000000 + p.IdPesee AS Wm,
  p.IdPesee                                   AS SourceId,
  p.DateDeuxiemePesee                         AS WeighedAt,
  v.Immatriculation                           AS Plate,
  v.Produit                                   AS Product,
  CASE WHEN p.TypeMouvement = 'SORTANT' THEN v.NomClient ELSE v.NomFournisseur END AS ThirdParty,
  p.PoidsBrut                                 AS GrossKg,
  p.PoidsTare                                 AS TareKg,
  p.PoidsNet                                  AS NetKg,
  -- Informations complementaires (-> ExtraJson)
  p.NumTicket                                 AS NumTicket,
  p.TypeMouvement                             AS TypeMouvement,
  v.NomClient                                 AS Client,
  v.NomFournisseur                            AS Fournisseur,
  v.CodeClient                                AS CodeClient,
  v.CodeProduit                               AS CodeProduit,
  p.NomChauffeur                              AS Chauffeur,
  p.NumPermisConduire                         AS PermisConduire,
  p.ImmatriculationRemorque                   AS Remorque,
  v.NomTransporteur                           AS Transporteur,
  v.Origine                                   AS Origine,
  v.Destination                               AS Destination,
  v.NumCommande                               AS NumCommande,
  v.Peseur                                    AS Peseur,
  p.RefactionManuelleKg                       AS RefactionKg,
  p.PrixUnitaire                              AS PrixUnitaire,
  p.MontantTotal                              AS MontantTotal,
  CAST(p.Annule AS UNSIGNED)                  AS Annule,
  p.MotifsAnnulation                          AS MotifsAnnulation,
  p.Observations                              AS Observations,
  p.DatePremierePesee                         AS DatePremierePesee
FROM pesees p
LEFT JOIN vuepesees v ON v.IdPesee = p.IdPesee
WHERE p.Statut = 'TERMINEE'
  AND p.DateDeuxiemePesee IS NOT NULL
  -- Delai de securite : on ignore les 10 dernieres secondes pour ne jamais depasser une ligne en cours d'ecriture.
  AND p.DateDeuxiemePesee < NOW() - INTERVAL 10 SECOND
