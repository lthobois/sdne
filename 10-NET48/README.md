# Atelier 10 - Validation perimetrique (.NET Framework 4.8)

## Objectif

Valider les controles perimetriques NET48:
- resistance a l'injection de headers forwarded
- resolution tenant durcie
- diagnostics request meta
- checks scripts reproductibles

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`
- (Optionnel) Docker Desktop pour la partie `infra`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `10-NET48/Atelier10.slnx`
- `10-NET48/PerimeterValidationLab/PerimeterValidationLab.csproj:1`

```powershell
dotnet restore .\10-NET48\Atelier10.slnx
dotnet build .\10-NET48\Atelier10.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `10-NET48/PerimeterValidationLab/Program.cs:33`
- `10-NET48/PerimeterValidationLab/Program.cs:77`

```powershell
$BaseUrl = 'http://localhost:5110'
dotnet run --project .\10-NET48\PerimeterValidationLab\PerimeterValidationLab.csproj --urls=$BaseUrl
```

## Etape 3 - Verifier reset-link avec headers forwarded

Code source a verifier (etape):
- `10-NET48/PerimeterValidationLab/Program.cs:96`
- `10-NET48/PerimeterValidationLab/Program.cs:108`
- `10-NET48/PerimeterValidationLab/Program.cs:176`

```powershell
$BaseUrl = 'http://localhost:5110'
$headers = @{ 'X-Forwarded-Host' = 'evil.example'; 'X-Forwarded-Proto' = 'http' }

Invoke-RestMethod -Uri "$BaseUrl/vuln/links/reset-password?user=alice" -Method Get -Headers $headers
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/links/reset-password?user=alice" -Method Get -Headers $headers -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

## Etape 4 - Verifier resolution tenant

Code source a verifier (etape):
- `10-NET48/PerimeterValidationLab/Program.cs:124`
- `10-NET48/PerimeterValidationLab/Program.cs:132`
- `10-NET48/PerimeterValidationLab/Program.cs:143`

```powershell
$BaseUrl = 'http://localhost:5110'
$headersBad = @{ 'X-Forwarded-Host' = 'unknown-tenant.local'; 'X-Forwarded-Proto' = 'https' }

Invoke-RestMethod -Uri "$BaseUrl/vuln/tenant/home" -Method Get -Headers $headersBad
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/tenant/home" -Method Get -Headers $headersBad -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

## Etape 5 - Consulter les diagnostics perimetriques

Code source a verifier (etape):
- `10-NET48/PerimeterValidationLab/Program.cs:152`
- `10-NET48/PerimeterValidationLab/Program.cs:155`
- `10-NET48/PerimeterValidationLab/Program.cs:291`

```powershell
$BaseUrl = 'http://localhost:5110'
$headers = @{ 'X-Forwarded-Host' = 'app.example.local'; 'X-Forwarded-Proto' = 'https' }
Invoke-RestMethod -Uri "$BaseUrl/secure/diagnostics/request-meta" -Method Get -Headers $headers
```

## Etape 6 - Executer les tests atelier

Code source a verifier (etape):
- `10-NET48/PerimeterValidationLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\10-NET48\Atelier10.slnx
```

## Etape 7 - Scripts stagiaires

Code source a verifier (etape):
- `10-NET48/scripts/calls.ps1:1`
- `10-NET48/scripts/run-perimeter-checks.ps1:1`
- `10-NET48/scripts/proxy-capture-playbook.md:1`

```powershell
.\10-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5110'
.\10-NET48\scripts\run-perimeter-checks.ps1 -BaseUrl 'http://localhost:5110'
```

## Etape 8 - Scenario docker (optionnel)

Code source a verifier (etape):
- `10-NET48/infra/docker-compose.yml:1`
- `10-NET48/infra/nginx.conf:1`

```powershell
Set-Location .\10-NET48\infra
docker compose up -d
docker compose ps
Set-Location ..
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5110/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\10-NET48\Atelier10.slnx
```
