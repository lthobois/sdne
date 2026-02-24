# Atelier 03 - Session, Deserialisation, IDOR (.NET 10)

## Objectif

Comparer les mecanismes vulnerables et durcis sur:
- session theft
- deserialisation insecure
- IDOR

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03/Program.cs:6`
- `03-NET10/AppSecWorkshop03/Program.cs:8`
- `03-NET10/AppSecWorkshop03/Program.cs:10`

```powershell
dotnet restore .\03-NET10\Atelier03.slnx
dotnet build .\03-NET10\Atelier03.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03/Properties/launchSettings.json:8`

```powershell
$BaseUrl = 'http://localhost:5103'
dotnet run --project .\03-NET10\AppSecWorkshop03\AppSecWorkshop03.csproj --urls=$BaseUrl
```

## Etape 3 - Session theft

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03/Program.cs:26`
- `03-NET10/AppSecWorkshop03/Program.cs:47`
- `03-NET10/AppSecWorkshop03/Program.cs:54`
- `03-NET10/AppSecWorkshop03/Security/VulnerableSessionService.cs:7`
- `03-NET10/AppSecWorkshop03/Security/SecureSessionService.cs:22`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/vuln/session/profile?token=YWxpY2U6d29ya3Nob3Atc2Vzc2lvbg==" -Method Get
$login = Invoke-RestMethod -Uri "$BaseUrl/secure/session/login" -Method Post -ContentType 'application/json' -Headers @{ 'User-Agent'='WorkshopAgent/1.0' } -Body (@{ username='alice' } | ConvertTo-Json)
$token = $login.token
Invoke-RestMethod -Uri "$BaseUrl/secure/session/profile" -Method Get -Headers @{ 'X-Session-Token'=$token; 'User-Agent'='WorkshopAgent/1.0' }
```

## Etape 4 - Deserialisation

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03/Program.cs:75`
- `03-NET10/AppSecWorkshop03/Program.cs:82`
- `03-NET10/AppSecWorkshop03/Program.cs:94`
- `03-NET10/AppSecWorkshop03/Serialization/WorkshopActions.cs:17`
- `03-NET10/AppSecWorkshop03/Serialization/WorkshopActions.cs:22`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body (@{ action='echo'; message='hello' } | ConvertTo-Json)
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body (@{ action='delete-all'; message='x' } | ConvertTo-Json) -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Etape 5 - IDOR

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03/Program.cs:108`
- `03-NET10/AppSecWorkshop03/Program.cs:124`
- `03-NET10/AppSecWorkshop03/Program.cs:138`
- `03-NET10/AppSecWorkshop03/Data/OrderRepository.cs:5`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/vuln/idor/orders/1002?username=alice" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=alice" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=bob" -Method Get
```

## Etape 6 - Executer les tests

Code source a verifier (etape):
- `03-NET10/AppSecWorkshop03.Tests/AppSecWorkshop03Tests.cs:18`
- `03-NET10/AppSecWorkshop03.Tests/AppSecWorkshop03Tests.cs:52`
- `03-NET10/AppSecWorkshop03.Tests/AppSecWorkshop03Tests.cs:62`
- `03-NET10/AppSecWorkshop03.Tests/AppSecWorkshop03Tests.cs:72`

```powershell
dotnet test .\03-NET10\Atelier03.slnx
```

## Scripts stagiaires (support)

```powershell
.\03-NET10\scripts\run-session-checks.ps1
.\03-NET10\scripts\calls.ps1
```

## Fichiers utiles

- `03-NET10/AppSecWorkshop03/AppSecWorkshop03.http`
- `03-NET10/scripts/calls.ps1`
- `03-NET10/scripts/run-session-checks.ps1`
- `03-NET10/pipeline/session-ci.yml`

## Nettoyage

```powershell
dotnet clean .\03-NET10\Atelier03.slnx
```