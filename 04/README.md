# Atelier 04 - Secure Code et durcissement

Ce dossier contient une solution de reference pour un atelier "Secure Code"
oriente OWASP ASVS (niveau fondamental).

## Structure

- `AppSecWorkshop04/`: API ASP.NET Core (net9.0)
- Endpoints `vuln/*`: implementation volontairement faible
- Endpoints `secure/*`: implementation corrigee

## Lancer l'atelier

```powershell
cd .\04\AppSecWorkshop04
dotnet run
```

## Parcours formateur (90 min)

1. Validation d'entree:
   - tester `/vuln/register` avec identifiant/mot de passe faibles
   - comparer avec `/secure/register`
2. Path traversal:
   - tenter lecture arbitraire via `/vuln/files/read?path=..\\..\\appsettings.json`
   - comparer avec `/secure/files/read`
3. Open redirect:
   - tester `/vuln/redirect?returnUrl=https://evil.example/phishing`
   - comparer avec `/secure/redirect`
4. Gestion d'erreurs:
   - observer `/secure/errors/divide-by-zero` (message generique)
5. Headers de securite:
   - verifier en reponse `X-Content-Type-Options`, `X-Frame-Options`, `CSP`

## Mapping ASVS (light)

- Validation d'entree: V5
- Gestion des erreurs: V10
- Redirects controles: V4
- Controle des fichiers/chemins: V12
- Headers de securite navigateur: V14

## Fichiers cles

- `AppSecWorkshop04/Program.cs`
- `AppSecWorkshop04/AppSecWorkshop04.http`
