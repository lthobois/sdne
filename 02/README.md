# Atelier 02 - Exploitation de failles web (.NET)

Ce dossier contient une solution de reference pour l'atelier 02:
SQL injection, XSS, CSRF et SSRF.

## Structure

- `AppSecWorkshop02/`: API ASP.NET Core (net9.0).
- Endpoints `vuln/*`: version vulnerable pedagogique.
- Endpoints `secure/*`: version corrigee.

## Lancer l'atelier

```powershell
cd .\02\AppSecWorkshop02
dotnet run
```

## Parcours formateur (90-120 min)

1. SQLi: exploiter `/vuln/sql/users` avec `username=' OR 1=1 --`.
2. SQLi corrige: tester `/secure/sql/users` avec le meme payload.
3. XSS: injecter `<script>alert('xss')</script>` sur `/vuln/xss`.
4. XSS corrige: comparer avec `/secure/xss`.
5. CSRF: creer une session via `/auth/login`, puis appeler `/vuln/csrf/transfer`.
6. CSRF corrige: appeler `/secure/csrf/transfer` sans puis avec `X-CSRF-Token`.
7. SSRF: tester `/vuln/ssrf/fetch?url=http://example.com`.
8. SSRF corrige: verifier le blocage de `/secure/ssrf/fetch?url=http://localhost:5142`.

## Points de debrief

- SQLi: requetes concatenees vs parametrees.
- XSS: sortie HTML encodee, jamais brute.
- CSRF: cookie de session insuffisant sans token anti-CSRF.
- SSRF: validation stricte URL + allowlist + blocage IP internes.

## Fichiers cles

- `AppSecWorkshop02/Program.cs`
- `AppSecWorkshop02/Data/DbInitializer.cs`
- `AppSecWorkshop02/Security/SessionStore.cs`
- `AppSecWorkshop02/Security/SsrfGuard.cs`
- `AppSecWorkshop02/AppSecWorkshop02.http`
