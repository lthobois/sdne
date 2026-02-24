# Atelier 02 - SQLi, XSS, CSRF, SSRF (.NET 10)

## Objectif

Comparer les endpoints `vuln` et `secure` pour 4 familles de risques:
- SQL injection
- XSS reflechi
- CSRF
- SSRF

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Program.cs:7`
- `02-NET10/AppSecWorkshop02/Program.cs:20`
- `02-NET10/AppSecWorkshop02/Data/DbInitializer.cs:7`

```powershell
dotnet restore .\02-NET10\Atelier02.slnx
dotnet build .\02-NET10\Atelier02.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Properties/launchSettings.json:8`

```powershell
$BaseUrl = 'http://localhost:5102'
dotnet run --project .\02-NET10\AppSecWorkshop02\AppSecWorkshop02.csproj --urls=$BaseUrl
```

## Etape 3 - SQL Injection

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Program.cs:29`
- `02-NET10/AppSecWorkshop02/Program.cs:35`
- `02-NET10/AppSecWorkshop02/Program.cs:53`
- `02-NET10/AppSecWorkshop02/Program.cs:60`

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = "alice' OR 1=1 --"
Invoke-RestMethod -Uri "$BaseUrl/vuln/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
```

## Etape 4 - XSS reflechi

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Program.cs:77`
- `02-NET10/AppSecWorkshop02/Program.cs:83`
- `02-NET10/AppSecWorkshop02/Program.cs:90`
- `02-NET10/AppSecWorkshop02/Program.cs:92`

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = '<script>alert("xss")</script>'
Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($payload))" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($payload))" | Select-Object -ExpandProperty Content
```

## Etape 5 - CSRF

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Program.cs:104`
- `02-NET10/AppSecWorkshop02/Program.cs:123`
- `02-NET10/AppSecWorkshop02/Program.cs:145`
- `02-NET10/AppSecWorkshop02/Program.cs:157`
- `02-NET10/AppSecWorkshop02/Security/SessionStore.cs:7`

```powershell
$BaseUrl = 'http://localhost:5102'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -WebSession $session -ContentType 'application/json' -Body (@{ username='alice' } | ConvertTo-Json)
$csrf = $login.csrfToken
$body = @{ to='bob'; amount=150 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/vuln/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $body
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $body -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -Headers @{ 'X-CSRF-Token'=$csrf } -ContentType 'application/json' -Body $body
```

## Etape 6 - SSRF

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02/Program.cs:172`
- `02-NET10/AppSecWorkshop02/Program.cs:180`
- `02-NET10/AppSecWorkshop02/Security/SsrfGuard.cs:14`
- `02-NET10/AppSecWorkshop02/Security/SsrfGuard.cs:21`

```powershell
$BaseUrl = 'http://localhost:5102'
Invoke-RestMethod -Uri "$BaseUrl/vuln/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('http://localhost:5102'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Etape 7 - Executer les tests

Code source a verifier (etape):
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:21`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:34`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:46`
- `02-NET10/AppSecWorkshop02.Tests/AppSecWorkshop02Tests.cs:71`

```powershell
dotnet test .\02-NET10\Atelier02.slnx
```

## Scripts stagiaires (support)

```powershell
.\02-NET10\scripts\run-workshop-checks.ps1
.\02-NET10\scripts\calls.ps1
```

## Fichiers utiles

- `02-NET10/AppSecWorkshop02/AppSecWorkshop02.http`
- `02-NET10/scripts/calls.ps1`
- `02-NET10/scripts/run-workshop-checks.ps1`
- `02-NET10/pipeline/workshop-ci.yml`

## Nettoyage

```powershell
dotnet clean .\02-NET10\Atelier02.slnx
```