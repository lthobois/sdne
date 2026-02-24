# Atelier 01 - HTTP Basic Auth (.NET 10)

## Objectif

Valider un flux HTTP Basic simple:
- `/public`: acces anonyme
- `/secure/profile`: authentification requise
- `/secure/admin`: role `Admin` requis

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Lignes de code a verifier (pedagogie)

Configuration auth/autorisation:
- `01-NET10/BasicAuthWorkshop/Program.cs:10`
- `01-NET10/BasicAuthWorkshop/Program.cs:16`
- `01-NET10/BasicAuthWorkshop/Program.cs:32`
- `01-NET10/BasicAuthWorkshop/Program.cs:33`

Endpoints proteges:
- `01-NET10/BasicAuthWorkshop/Program.cs:47`
- `01-NET10/BasicAuthWorkshop/Program.cs:61`

Traitement du header Basic:
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:24`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:32`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:44`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:60`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:76`

Comptes de demo en memoire:
- `01-NET10/BasicAuthWorkshop/Auth/InMemoryWorkshopUserStore.cs:8`
- `01-NET10/BasicAuthWorkshop/Auth/InMemoryWorkshopUserStore.cs:9`

Tests d'integration:
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:25`
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:33`
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:43`

## Build et tests

```powershell
dotnet restore .\01-NET10\Atelier01.slnx
dotnet build .\01-NET10\Atelier01.slnx
dotnet test .\01-NET10\Atelier01.slnx
```

Option script:

```powershell
.\01-NET10\scripts\run-auth-checks.ps1
```

## Execution API

```powershell
$BaseUrl = 'http://localhost:5101'
dotnet run --project .\01-NET10\BasicAuthWorkshop\BasicAuthWorkshop.csproj --urls=$BaseUrl
```

## Verification manuelle rapide

Dans un second terminal:

```powershell
$BaseUrl = 'http://localhost:5101'

Invoke-RestMethod -Uri "$BaseUrl/public" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$alice = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('alice:P@ssw0rd!'))
$headersAlice = @{ Authorization = "Basic $alice" }
Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Headers $headersAlice -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersAlice -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$bob = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('bob:Admin123!'))
$headersBob = @{ Authorization = "Basic $bob" }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersBob -Method Get
```

Attendus:
- `/public` -> `200`
- `/secure/profile` sans header -> `401`
- `/secure/profile` avec `alice` -> `200`
- `/secure/admin` avec `alice` -> `403`
- `/secure/admin` avec `bob` -> `200`

## Fichiers utiles

- `01-NET10/BasicAuthWorkshop/BasicAuthWorkshop.http`
- `01-NET10/scripts/calls.ps1`
- `01-NET10/pipeline/basic-auth-ci.yml`

## Nettoyage

```powershell
dotnet clean .\01-NET10\Atelier01.slnx
```