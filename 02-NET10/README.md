# Atelier 02 - SQLi, XSS, CSRF, SSRF (.NET 10)

## Objectif

Comparer des implementations vulnerables (`/vuln/*`) et durcies (`/secure/*`) sur 4 themes:
- SQL injection
- XSS reflechi
- CSRF
- SSRF

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Lignes de code a verifier (pedagogie)

Configuration atelier:
- `02-NET10/AppSecWorkshop02/Program.cs:8`
- `02-NET10/AppSecWorkshop02/Program.cs:20`

SQL injection:
- `02-NET10/AppSecWorkshop02/Program.cs:29`
- `02-NET10/AppSecWorkshop02/Program.cs:35`
- `02-NET10/AppSecWorkshop02/Program.cs:53`
- `02-NET10/AppSecWorkshop02/Program.cs:60`

XSS:
- `02-NET10/AppSecWorkshop02/Program.cs:77`
- `02-NET10/AppSecWorkshop02/Program.cs:83`
- `02-NET10/AppSecWorkshop02/Program.cs:90`
- `02-NET10/AppSecWorkshop02/Program.cs:92`

CSRF:
- `02-NET10/AppSecWorkshop02/Program.cs:104`
- `02-NET10/AppSecWorkshop02/Program.cs:123`
- `02-NET10/AppSecWorkshop02/Program.cs:145`
- `02-NET10/AppSecWorkshop02/Program.cs:157`
- `02-NET10/AppSecWorkshop02/Security/SessionStore.cs:7`

SSRF:
- `02-NET10/AppSecWorkshop02/Program.cs:172`
- `02-NET10/AppSecWorkshop02/Program.cs:180`
- `02-NET10/AppSecWorkshop02/Security/SsrfGuard.cs:14`
- `02-NET10/AppSecWorkshop02/Security/SsrfGuard.cs:21`
- `02-NET10/AppSecWorkshop02/Security/SsrfGuard.cs:32`

Initialisation SQL lite:
- `02-NET10/AppSecWorkshop02/Data/DbInitializer.cs:7`
- `02-NET10/AppSecWorkshop02/Data/DbInitializer.cs:23`

Tests d'integration:
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:21`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:34`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:46`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:71`

## Build et tests

```powershell
dotnet restore .\02-NET10\Atelier02.slnx
dotnet build .\02-NET10\Atelier02.slnx
dotnet test .\02-NET10\Atelier02.slnx
```

Option script:

```powershell
.\02-NET10\scripts\run-workshop-checks.ps1
```

## Lancement API

```powershell
$BaseUrl = 'http://localhost:5102'
dotnet run --project .\02-NET10\AppSecWorkshop02\AppSecWorkshop02.csproj --urls=$BaseUrl
```

## Verification manuelle rapide

Dans un second terminal:

```powershell
$BaseUrl = 'http://localhost:5102'

# SQLi
$payload = "alice' OR 1=1 --"
Invoke-RestMethod -Uri "$BaseUrl/vuln/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get

# XSS
$input = '<script>alert("xss")</script>'
Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($input))" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($input))" | Select-Object -ExpandProperty Content

# CSRF
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$loginBody = @{ username = 'alice' } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -WebSession $session -ContentType 'application/json' -Body $loginBody
$csrf = $login.csrfToken
$transfer = @{ to = 'bob'; amount = 150 } | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/vuln/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $transfer
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $transfer -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -Headers @{ 'X-CSRF-Token' = $csrf } -ContentType 'application/json' -Body $transfer

# SSRF
Invoke-RestMethod -Uri "$BaseUrl/vuln/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('http://localhost:5102'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Attendus

- SQLi visible sur endpoint `vuln`, neutralisee sur endpoint `secure`
- XSS non encode sur `vuln`, encode sur `secure`
- CSRF `secure` refuse sans header `X-CSRF-Token`
- SSRF `secure` bloque localhost et hosts non allowlist

## Fichiers utiles

- `02-NET10/AppSecWorkshop02/AppSecWorkshop02.http`
- `02-NET10/scripts/calls.ps1`
- `02-NET10/scripts/run-workshop-checks.ps1`
- `02-NET10/pipeline/workshop-ci.yml`

## Nettoyage

```powershell
Remove-Item .\workshop.db -ErrorAction SilentlyContinue
dotnet clean .\02-NET10\Atelier02.slnx
```