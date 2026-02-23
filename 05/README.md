# Atelier 05 - Tests de securite automatises

Objectif: industrialiser la validation securite avec des tests de regression,
un job SAST simple et un job DAST de base.

## Structure

- `Atelier05.slnx`: solution atelier.
- `SecurityValidationLab/`: API de demonstration (`vuln/*` et `secure/*`).
- `SecurityValidationLab.Tests/`: tests d'integration securite.
- `scripts/run-sast.ps1`: controle statique et dependances.
- `scripts/run-dast.ps1`: scan DAST baseline avec OWASP ZAP (Docker).
- `pipeline/security-ci.yml`: exemple de pipeline CI.

## Lancer localement

```powershell
cd .\05
dotnet build .\Atelier05.slnx
dotnet test .\Atelier05.slnx
```

## Ateliers proposes (90 min)

1. Executer les tests et analyser les controles couverts.
2. Introduire volontairement une regression (ex: supprimer l'encodage XSS) et verifier l'echec des tests.
3. Executer `scripts/run-sast.ps1` et corriger les alertes.
4. Lancer l'API puis `scripts/run-dast.ps1` pour obtenir un rapport baseline.
5. Integrer `pipeline/security-ci.yml` dans un projet GitHub.

## Commandes utiles

```powershell
dotnet run --project .\SecurityValidationLab\SecurityValidationLab.csproj
.\scripts\run-sast.ps1
.\scripts\run-dast.ps1 -TargetUrl http://host.docker.internal:5000
```
