# Atelier 09 - Authentification et autorisation avancees

Objectif: durcir les acces API avec token signe, scopes et controle d'acces
au niveau objet.

## Structure

- `Atelier09.slnx`
- `AuthzHardeningLab/`: API demo (`vuln/*` et `secure/*`)
- `AuthzHardeningLab.Tests/`: tests d'integration des controles AuthN/AuthZ
- `scripts/run-authz-checks.ps1`
- `pipeline/authz-ci.yml`

## Lancer

```powershell
cd .\09
dotnet build .\Atelier09.slnx
dotnet test .\Atelier09.slnx
```

## Scenarios atelier (90 min)

1. Token non signe (vulnerable):
   - `POST /vuln/auth/token`
2. Token signe HMAC (secure):
   - `POST /secure/auth/token`
   - verifier rejet d'un token modifie sur `/secure/docs/{id}`
3. IDOR / BOLA:
   - `/vuln/docs/{id}?username=...` (pas de controle)
   - `/secure/docs/{id}` (proprietaire + scope)
4. Scope d'action sensible:
   - `/secure/docs/{id}/publish` requiert `docs.publish`
