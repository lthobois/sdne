# Atelier 04 - Secure Coding et durcissement (.NET Framework 4.8)

## Objectif

Comparer les endpoints `vuln` et `secure` sur validation d'entree, traversal, redirect et erreurs.

## Pre-requis

- Windows + .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `04-NET48/Atelier04.slnx`
- `04-NET48/AppSecWorkshop04/AppSecWorkshop04.csproj`

```powershell
dotnet restore .\04-NET48\Atelier04.slnx
dotnet build .\04-NET48\Atelier04.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04/Program.cs:3`
- `04-NET48/AppSecWorkshop04/Program.cs:16`

```powershell
$BaseUrl = 'http://localhost:5104'
dotnet run --project .\04-NET48\AppSecWorkshop04\AppSecWorkshop04.csproj --urls=$BaseUrl
```

## Etape 3 - Validation d'entree (register)

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04/Program.cs:37`
- `04-NET48/AppSecWorkshop04/Program.cs:51`
- `04-NET48/AppSecWorkshop04/Program.cs:65`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/vuln/register" -Method Post -ContentType 'application/json' -Body (@{ username='a'; password='123' } | ConvertTo-Json)
try { Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='a'; password='123' } | ConvertTo-Json) -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='alice.secure'; password='Str0ng!Passw0rd' } | ConvertTo-Json)
```

## Etape 4 - Path traversal

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04/Program.cs:83`
- `04-NET48/AppSecWorkshop04/Program.cs:100`
- `04-NET48/AppSecWorkshop04/Program.cs:110`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=public-note.txt" -Method Get
try { Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=$([uri]::EscapeDataString('..\\..\\appsettings.json'))" -Method Get -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
```

## Etape 5 - Open redirect

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04/Program.cs:124`
- `04-NET48/AppSecWorkshop04/Program.cs:129`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-WebRequest -Uri "$BaseUrl/vuln/redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode
try { Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('/home'))" -Method Get
```

## Etape 6 - Gestion d'erreurs

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04/Program.cs:144`
- `04-NET48/AppSecWorkshop04/Program.cs:151`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/secure/errors/divide-by-zero" -Method Get
```

## Etape 7 - Tests atelier

Code source a verifier (etape):
- `04-NET48/AppSecWorkshop04.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\04-NET48\Atelier04.slnx
```

## Scripts stagiaires (support)

```powershell
.\04-NET48\scripts\calls.ps1
```

## Nettoyage

```powershell
dotnet clean .\04-NET48\Atelier04.slnx
```