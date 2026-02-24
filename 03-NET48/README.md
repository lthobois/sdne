# Atelier 03 - Session, Deserialisation, IDOR (.NET Framework 4.8)

## Objectif

Comparer les approches `vuln` et `secure` pour session theft, deserialisation et IDOR.

## Pre-requis

- Windows + .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `03-NET48/Atelier03.slnx`
- `03-NET48/AppSecWorkshop03/AppSecWorkshop03.csproj`

```powershell
dotnet restore .\03-NET48\Atelier03.slnx
dotnet build .\03-NET48\Atelier03.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `03-NET48/AppSecWorkshop03/Program.cs:41`
- `03-NET48/AppSecWorkshop03/Program.cs:85`

```powershell
$BaseUrl = 'http://localhost:5103'
dotnet run --project .\03-NET48\AppSecWorkshop03\AppSecWorkshop03.csproj --urls=$BaseUrl
```

## Etape 3 - Session theft

Code source a verifier (etape):
- `03-NET48/AppSecWorkshop03/Program.cs:15`
- `03-NET48/AppSecWorkshop03/Program.cs:47`
- `03-NET48/AppSecWorkshop03/Program.cs:54`
- `03-NET48/AppSecWorkshop03/Program.cs:62`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/vuln/session/profile?token=YWxpY2U6d29ya3Nob3Atc2Vzc2lvbg==" -Method Get
$login = Invoke-RestMethod -Uri "$BaseUrl/secure/session/login" -Method Post -Headers @{ 'User-Agent'='WorkshopAgent/1.0' } -ContentType 'application/json' -Body (@{ username='alice' } | ConvertTo-Json)
$token = $login.token
Invoke-RestMethod -Uri "$BaseUrl/secure/session/profile" -Method Get -Headers @{ 'X-Session-Token'=$token; 'User-Agent'='WorkshopAgent/1.0' }
```

## Etape 4 - Deserialisation

Code source a verifier (etape):
- `03-NET48/AppSecWorkshop03/Program.cs:75`
- `03-NET48/AppSecWorkshop03/Program.cs:94`
- `03-NET48/AppSecWorkshop03/Program.cs:96`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body (@{ action='echo'; message='hello' } | ConvertTo-Json)
try { Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body (@{ action='delete-all'; message='x' } | ConvertTo-Json) -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
```

## Etape 5 - IDOR

Code source a verifier (etape):
- `03-NET48/AppSecWorkshop03/Program.cs:108`
- `03-NET48/AppSecWorkshop03/Program.cs:124`
- `03-NET48/AppSecWorkshop03/Program.cs:138`

```powershell
$BaseUrl = 'http://localhost:5103'
Invoke-RestMethod -Uri "$BaseUrl/vuln/idor/orders/1002?username=alice" -Method Get
try { Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=alice" -Method Get -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=bob" -Method Get
```

## Etape 6 - Tests atelier

Code source a verifier (etape):
- `03-NET48/AppSecWorkshop03.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\03-NET48\Atelier03.slnx
```

## Scripts stagiaires (support)

```powershell
.\03-NET48\scripts\calls.ps1
```

## Nettoyage

```powershell
dotnet clean .\03-NET48\Atelier03.slnx
```