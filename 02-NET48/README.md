# Atelier 02 - SQLi, XSS, CSRF, SSRF (.NET Framework 4.8)

## Objectif

Comparer les routes `vuln` et `secure` sur SQLi, XSS, CSRF et SSRF.

## Pre-requis

- Windows + .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `02-NET48/Atelier02.slnx`
- `02-NET48/AppSecWorkshop02/AppSecWorkshop02.csproj`

```powershell
dotnet restore .\02-NET48\Atelier02.slnx
dotnet build .\02-NET48\Atelier02.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:34`
- `02-NET48/AppSecWorkshop02/Program.cs:85`

```powershell
$BaseUrl = 'http://localhost:5102'
dotnet run --project .\02-NET48\AppSecWorkshop02\AppSecWorkshop02.csproj --urls=$BaseUrl
```

## Etape 3 - SQL Injection

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:104`
- `02-NET48/AppSecWorkshop02/Program.cs:110`
- `02-NET48/AppSecWorkshop02/Program.cs:223`

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = "alice' OR 1=1 --"
Invoke-RestMethod -Uri "$BaseUrl/vuln/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
```

## Etape 4 - XSS

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:116`
- `02-NET48/AppSecWorkshop02/Program.cs:124`

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = '<script>alert("xss")</script>'
Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($payload))" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($payload))" | Select-Object -ExpandProperty Content
```

## Etape 5 - CSRF

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:135`
- `02-NET48/AppSecWorkshop02/Program.cs:149`
- `02-NET48/AppSecWorkshop02/Program.cs:164`
- `02-NET48/AppSecWorkshop02/Program.cs:173`

```powershell
$BaseUrl = 'http://localhost:5102'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -WebSession $session -ContentType 'application/json' -Body (@{ username='alice' } | ConvertTo-Json)
$csrf = $login.csrfToken
$body = @{ to='bob'; amount=150 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/vuln/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $body
try { Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $body -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -Headers @{ 'X-CSRF-Token'=$csrf } -ContentType 'application/json' -Body $body
```

## Etape 6 - SSRF

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:186`
- `02-NET48/AppSecWorkshop02/Program.cs:194`
- `02-NET48/AppSecWorkshop02/Program.cs:211`

```powershell
$BaseUrl = 'http://localhost:5102'
Invoke-RestMethod -Uri "$BaseUrl/vuln/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
try { Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('http://127.0.0.1:80'))" -Method Get -ErrorAction Stop } catch { $_.Exception.Response.StatusCode.value__ }
```

## Etape 7 - Validation atelier

Code source a verifier (etape):
- `02-NET48/AppSecWorkshop02/Program.cs:186`
- `02-NET48/Atelier02.slnx:1`

```powershell
dotnet build .\02-NET48\Atelier02.slnx
```

## Scripts stagiaires (support)

```powershell
.\02-NET48\scripts\calls.ps1
```

## Nettoyage

```powershell
dotnet clean .\02-NET48\Atelier02.slnx
```
