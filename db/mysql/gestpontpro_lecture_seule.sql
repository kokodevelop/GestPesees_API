-- Compte MySQL en LECTURE SEULE pour l'agent GestPesees (a executer avec un compte administrateur MySQL).
-- Remplacer NOM_DE_LA_BASE et le mot de passe. 'localhost' si l'agent tourne sur la meme machine que MySQL,
-- sinon l'adresse IP du poste de l'agent.
CREATE USER 'gestpesees_ro'@'localhost' IDENTIFIED BY 'MOT_DE_PASSE_LONG_ET_ALEATOIRE';
GRANT SELECT ON NOM_DE_LA_BASE.pesees     TO 'gestpesees_ro'@'localhost';
GRANT SELECT ON NOM_DE_LA_BASE.vuepesees  TO 'gestpesees_ro'@'localhost';
FLUSH PRIVILEGES;

-- Verification : doit retourner des lignes, et toute ecriture doit etre refusee.
-- SELECT COUNT(*) FROM NOM_DE_LA_BASE.pesees WHERE Statut = 'TERMINEE';
