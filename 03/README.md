# Atelier 03 - Attaques avancees et durcissement

Ce dossier contient la solution de reference pour l'atelier 03:
vol de session, deserialisation non sure et IDOR.

## Structure

- `AppSecWorkshop03/`: API ASP.NET Core (net9.0).
- Endpoints `vuln/*`: version vulnerable pedagogique.
- Endpoints `secure/*`: version corrigee.

## Lancer l'atelier

```powershell
cd .\03\AppSecWorkshop03
dotnet run
```

## Parcours formateur (90-120 min)

1. Session theft:
   - appeler `/vuln/session/login`
   - reutiliser un token previsible sur `/vuln/session/profile`
   - comparer avec `/secure/session/login` + `/secure/session/profile`
2. Deserialisation:
   - envoyer un payload avec `$type` sur `/vuln/deserialization/execute`
   - constater l'effet de bord (creation de fichier)
   - comparer avec `/secure/deserialization/execute`
3. IDOR:
   - `alice` lit la commande `1002` via `/vuln/idor/orders/1002`
   - verifier le refus sur `/secure/idor/orders/1002?username=alice`
   - verifier l'acces `admin` avec `bob`

## Points de debrief

- Session: tokens aleatoires, expiration, liaison du contexte client.
- Deserialisation: ne jamais deserialiser un type arbitraire depuis une entree non fiable.
- IDOR: verification d'autorisation objet par objet.

## Fichiers cles

- `AppSecWorkshop03/Program.cs`
- `AppSecWorkshop03/Security/VulnerableSessionService.cs`
- `AppSecWorkshop03/Security/SecureSessionService.cs`
- `AppSecWorkshop03/Serialization/WorkshopActions.cs`
- `AppSecWorkshop03/Data/OrderRepository.cs`
- `AppSecWorkshop03/AppSecWorkshop03.http`
