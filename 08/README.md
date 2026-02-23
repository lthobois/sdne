# Atelier 08 - Monitoring et reponse securite

Objectif: rendre les incidents detectables et actionnables via journalisation
structuree, correlation des requetes, piste d'audit et alerting de base.

## Structure

- `Atelier08.slnx`
- `SecurityMonitoringLab/`: API de demo (`vuln/*` et `secure/*`)
- `SecurityMonitoringLab.Tests/`: tests d'integration des controles de monitoring

## Lancer

```powershell
cd .\08
dotnet build .\Atelier08.slnx
dotnet test .\Atelier08.slnx
```

## Scenarios atelier (90 min)

1. Logging non securise:
   - `POST /vuln/login` (mot de passe journalise)
2. Logging securise + correlation:
   - `POST /secure/login` avec/sans `X-Correlation-ID`
   - verifier le header de reponse `X-Correlation-ID`
3. Audit trail:
   - `GET /secure/audit/events`
4. Alerting:
   - 3 echecs de login sur le meme user
   - `GET /secure/alerts`
5. Action admin protegee:
   - `POST /secure/admin/reset-alerts` sans cle (401)
   - avec `X-SOC-Key` (200)
