# Atelier 01 - HTTP Basic Auth (.NET 10)

## Objectif

Mettre en pratique un flux HTTP Basic avec comparaison entre:
- endpoint public (`/public`)
- endpoint authentifie (`/secure/profile`)
- endpoint autorise par role (`/secure/admin`)

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Etape 1 - Restaurer la solution atelier

Code source a verifier (etape):
- `01-NET10/Atelier01.slnx`

```powershell
dotnet restore .\01-NET10\Atelier01.slnx
```

Resultat attendu: restauration sans erreur.

## Etape 2 - Builder la solution atelier

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/BasicAuthWorkshop.csproj`
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthWorkshop.Tests.csproj`

```powershell
dotnet build .\01-NET10\Atelier01.slnx
```

Resultat attendu: build `net10.0` sans erreur.

## Etape 3 - Lancer l'API

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/Program.cs:10`
- `01-NET10/BasicAuthWorkshop/Program.cs:16`
- `01-NET10/BasicAuthWorkshop/Program.cs:32`
- `01-NET10/BasicAuthWorkshop/Program.cs:33`
- `01-NET10/BasicAuthWorkshop/Properties/launchSettings.json:8`

```powershell
$BaseUrl = 'http://localhost:5101'
dotnet run --project .\01-NET10\BasicAuthWorkshop\BasicAuthWorkshop.csproj --urls=$BaseUrl
```

Resultat attendu: API en ecoute sur `http://localhost:5101`.

## Etape 4 - Verifier l'endpoint public

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/Program.cs:41`

Dans un second terminal:

```powershell
$BaseUrl = 'http://localhost:5101'
Invoke-RestMethod -Uri "$BaseUrl/public" -Method Get
```

Resultat attendu: `200` avec `resource = public`.

## Etape 5 - Verifier le refus sans credentials

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/Program.cs:47`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:24`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:76`

```powershell
$BaseUrl = 'http://localhost:5101'
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Resultat attendu: `401`.

## Etape 6 - Verifier l'authentification utilisateur

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:32`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:44`
- `01-NET10/BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs:60`
- `01-NET10/BasicAuthWorkshop/Auth/InMemoryWorkshopUserStore.cs:8`

```powershell
$BaseUrl = 'http://localhost:5101'
$alice = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('alice:P@ssw0rd!'))
$headersAlice = @{ Authorization = "Basic $alice" }
Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Headers $headersAlice -Method Get
```

Resultat attendu: `200`, utilisateur `alice`.

## Etape 7 - Verifier l'autorisation par role

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop/Program.cs:61`
- `01-NET10/BasicAuthWorkshop/Program.cs:64`
- `01-NET10/BasicAuthWorkshop/Auth/InMemoryWorkshopUserStore.cs:9`

```powershell
$BaseUrl = 'http://localhost:5101'

$alice = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('alice:P@ssw0rd!'))
$headersAlice = @{ Authorization = "Basic $alice" }
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersAlice -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$bob = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('bob:Admin123!'))
$headersBob = @{ Authorization = "Basic $bob" }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersBob -Method Get
```

Resultats attendus:
- `alice` -> `403`
- `bob` -> `200`

## Etape 8 - Executer les tests d'integration

Code source a verifier (etape):
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:25`
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:33`
- `01-NET10/BasicAuthWorkshop.Tests/BasicAuthTests.cs:43`

```powershell
dotnet test .\01-NET10\Atelier01.slnx
```

Resultat attendu: tests `Passed`.

## Scripts PowerShell (support stagiaire)

Script de checks (build + tests):

```powershell
.\01-NET10\scripts\run-auth-checks.ps1
```

Script de demonstration des appels HTTP (API deja lancee):

```powershell
.\01-NET10\scripts\calls.ps1
```

## Fichiers utiles

- `01-NET10/BasicAuthWorkshop/BasicAuthWorkshop.http`
- `01-NET10/scripts/calls.ps1`
- `01-NET10/scripts/run-auth-checks.ps1`
- `01-NET10/pipeline/basic-auth-ci.yml`

## Nettoyage

```powershell
dotnet clean .\01-NET10\Atelier01.slnx
```