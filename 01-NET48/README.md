# Atelier 01 - HTTP Basic Auth (.NET Framework 4.8)

## Objectif

Comparer l'acces public, l'authentification Basic et l'autorisation par role.

## Pre-requis

- Windows + .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `01-NET48/Atelier01.slnx`
- `01-NET48/BasicAuthWorkshop/BasicAuthWorkshop.csproj`

```powershell
dotnet restore .\01-NET48\Atelier01.slnx
dotnet build .\01-NET48\Atelier01.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `01-NET48/BasicAuthWorkshop/Program.cs:23`
- `01-NET48/BasicAuthWorkshop/Program.cs:67`
- `01-NET48/BasicAuthWorkshop/Properties/launchSettings.json:8`

```powershell
$BaseUrl = 'http://localhost:5101'
dotnet run --project .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj --urls=$BaseUrl
```

## Etape 3 - Endpoint public

Code source a verifier (etape):
- `01-NET48/BasicAuthWorkshop/Program.cs:85`

```powershell
$BaseUrl = 'http://localhost:5101'
Invoke-RestMethod -Uri "$BaseUrl/public" -Method Get
```

## Etape 4 - Authentification Basic

Code source a verifier (etape):
- `01-NET48/BasicAuthWorkshop/Program.cs:91`
- `01-NET48/BasicAuthWorkshop/Program.cs:115`
- `01-NET48/BasicAuthWorkshop/Program.cs:145`

```powershell
$BaseUrl = 'http://localhost:5101'
$analyst = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('analyst:Passw0rd!'))
$headers = @{ Authorization = "Basic $analyst" }
Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Headers $headers -Method Get
```

## Etape 5 - Autorisation role Admin

Code source a verifier (etape):
- `01-NET48/BasicAuthWorkshop/Program.cs:106`
- `01-NET48/BasicAuthWorkshop/Program.cs:112`

```powershell
$BaseUrl = 'http://localhost:5101'
$analyst = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('analyst:Passw0rd!'))
$admin = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('admin:Adm1nPass!'))
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers @{ Authorization = "Basic $analyst" } -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers @{ Authorization = "Basic $admin" } -Method Get
```

## Etape 6 - Tests atelier

Code source a verifier (etape):
- `01-NET48/BasicAuthWorkshop.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\01-NET48\Atelier01.slnx
```

## Scripts stagiaires (support)

```powershell
.\01-NET48\scripts\run-auth-checks.ps1
.\01-NET48\scripts\calls.ps1
```

## Nettoyage

```powershell
dotnet clean .\01-NET48\Atelier01.slnx
```