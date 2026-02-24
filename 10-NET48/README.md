# Atelier 10 - Validation perimetrique (.NET Framework 4.8)

## Mode compatibilite NET48

Cette piste est executable en .NET Framework 4.8 avec un hote `HttpListener`.
Les cas pedagogiques couverts sont:
- injection `X-Forwarded-*` sur generation de liens
- durcissement de resolution d'origine externe
- resolution multi-tenant par host
- endpoint de diagnostic perimetrique

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK installe (`dotnet`)
- .NET Framework 4.8 Developer Pack
- PowerShell 5.1+
- (Optionnel) Docker Desktop pour `infra/docker-compose.yml`

## Lancement

```powershell
if (Test-Path .\10-NET48) { Set-Location .\10-NET48 }
dotnet restore .\Atelier10.slnx
$BaseUrl = 'http://localhost:5110'
dotnet run --project .\PerimeterValidationLab\PerimeterValidationLab.csproj --urls=$BaseUrl
```

Si Windows retourne `Access denied` sur `HttpListener`, executer une fois en administrateur:

```powershell
netsh http add urlacl url=http://localhost:5110/ user=%USERNAME%
```

Code principal:
- `10-NET48/PerimeterValidationLab/Program.cs`

## Endpoints

- `GET /`
- `GET /vuln/links/reset-password?user=...`
- `GET /secure/links/reset-password?user=...`
- `GET /vuln/tenant/home`
- `GET /secure/tenant/home`
- `GET /secure/diagnostics/request-meta`

## Parcours rapide

```powershell
$BaseUrl = 'http://localhost:5110'

# 1) Vuln: lien forge via forwarded headers
$hBad = @{ 'X-Forwarded-Host'='evil.example'; 'X-Forwarded-Proto'='http' }
Invoke-RestMethod -Uri "$BaseUrl/vuln/links/reset-password?user=alice" -Headers $hBad -Method Get

# 2) Secure: rejet host non allowlist
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/links/reset-password?user=alice" -Headers $hBad -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}

# 3) Secure: acceptation host allowlist + https
$hOk = @{ 'X-Forwarded-Host'='app.contoso.local'; 'X-Forwarded-Proto'='https' }
Invoke-RestMethod -Uri "$BaseUrl/secure/links/reset-password?user=alice" -Headers $hOk -Method Get

# 4) Tenant secure
Invoke-RestMethod -Uri "$BaseUrl/secure/tenant/home" -Headers $hOk -Method Get

# 5) Diagnostics
Invoke-RestMethod -Uri "$BaseUrl/secure/diagnostics/request-meta" -Headers $hOk -Method Get
```

## Tests

Les tests NET48 de cette piste sont des smoke tests (validation d'execution):

```powershell
if (Test-Path .\10-NET48) { Set-Location .\10-NET48 }
dotnet test .\PerimeterValidationLab.Tests\PerimeterValidationLab.Tests.csproj
```

## Verifications attendues

- `vuln/links/reset-password` accepte les headers controles par l'attaquant
- `secure/links/reset-password` impose host allowlist + scheme `https`
- `secure/tenant/home` refuse les tenants hors allowlist
- `secure/diagnostics/request-meta` expose la decision de resolution

## Nettoyage

```powershell
if (Test-Path .\10-NET48) { Set-Location .\10-NET48 }
if (Test-Path .\infra) {
  Set-Location .\infra
  docker compose down
  Set-Location ..
}
dotnet clean .\Atelier10.slnx
```