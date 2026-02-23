# Atelier 10 - Validation perimetrique (headers, proxy, DMZ)

Objectif: couvrir les derniers points du programme:

- injection d'en-tetes HTTP (`Host`, `X-Forwarded-*`)
- validation par proxy + capture reseau
- durcissement perimetrique (reverse proxy / DMZ)

## Structure

- `Atelier10.slnx`
- `PerimeterValidationLab/`: API demo (`vuln/*` et `secure/*`)
- `PerimeterValidationLab.Tests/`: tests d'integration
- `scripts/run-perimeter-checks.ps1`
- `scripts/proxy-capture-playbook.md`
- `infra/docker-compose.yml`
- `infra/nginx.conf`
- `pipeline/perimeter-ci.yml`

## Lancer

```powershell
cd .\10
dotnet build .\Atelier10.slnx
dotnet test .\Atelier10.slnx
dotnet run --project .\PerimeterValidationLab\PerimeterValidationLab.csproj
```

## Scenarios atelier (90 min)

1. Header injection sur lien de reset:
   - `GET /vuln/links/reset-password?user=alice` avec `X-Forwarded-Host: evil.example`
   - constater un lien malveillant genere
2. Remediation:
   - `GET /secure/links/reset-password?user=alice`
   - verifier allowlist host + scheme `https`
3. Resolution tenant:
   - `GET /vuln/tenant/home` avec host injecte
   - `GET /secure/tenant/home` (host inconnu bloque)
4. Diagnostic forwarding:
   - `GET /secure/diagnostics/request-meta`
5. Proxy/capture:
   - suivre `scripts/proxy-capture-playbook.md`

## Mapping programme

- Jour 2: injection d'en-tete HTTP
- Jour 3: capture via proxy / tcpdump / Wireshark
- Jour 3: role reverse proxy / DMZ
