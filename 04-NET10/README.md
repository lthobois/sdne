# Atelier 04 - Secure Coding et durcissement (.NET 10)

## Objectif

Comparer des implementations vulnerables (`/vuln/*`) et durcies (`/secure/*`) sur:
- validation d'entree
- path traversal
- open redirect
- gestion d'erreurs

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Lignes de code a verifier (pedagogie)

Durcissement global (headers):
- `04-NET10/AppSecWorkshop04/Program.cs:16`
- `04-NET10/AppSecWorkshop04/Program.cs:18`
- `04-NET10/AppSecWorkshop04/Program.cs:21`

Validation d'entree / register:
- `04-NET10/AppSecWorkshop04/Program.cs:37`
- `04-NET10/AppSecWorkshop04/Program.cs:42`
- `04-NET10/AppSecWorkshop04/Program.cs:51`
- `04-NET10/AppSecWorkshop04/Program.cs:60`
- `04-NET10/AppSecWorkshop04/Program.cs:65`
- `04-NET10/AppSecWorkshop04/Program.cs:175`

Path traversal:
- `04-NET10/AppSecWorkshop04/Program.cs:83`
- `04-NET10/AppSecWorkshop04/Program.cs:85`
- `04-NET10/AppSecWorkshop04/Program.cs:100`
- `04-NET10/AppSecWorkshop04/Program.cs:102`
- `04-NET10/AppSecWorkshop04/Program.cs:110`

Open redirect:
- `04-NET10/AppSecWorkshop04/Program.cs:124`
- `04-NET10/AppSecWorkshop04/Program.cs:129`
- `04-NET10/AppSecWorkshop04/Program.cs:131`

Gestion d'erreurs:
- `04-NET10/AppSecWorkshop04/Program.cs:144`
- `04-NET10/AppSecWorkshop04/Program.cs:151`
- `04-NET10/AppSecWorkshop04/Program.cs:161`

Tests d'integration:
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:20`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:30`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:40`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:50`

## Build et tests

```powershell
dotnet restore .\04-NET10\Atelier04.slnx
dotnet build .\04-NET10\Atelier04.slnx
dotnet test .\04-NET10\Atelier04.slnx
```

Option script:

```powershell
.\04-NET10\scripts\run-crypto-checks.ps1
```

## Lancement API

```powershell
$BaseUrl = 'http://localhost:5104'
dotnet run --project .\04-NET10\AppSecWorkshop04\AppSecWorkshop04.csproj --urls=$BaseUrl
```

## Verification manuelle rapide

Dans un second terminal:

```powershell
$BaseUrl = 'http://localhost:5104'

# Register: vuln vs secure
Invoke-RestMethod -Uri "$BaseUrl/vuln/register" -Method Post -ContentType 'application/json' -Body (@{ username='aa'; password='1234' } | ConvertTo-Json)
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='aa'; password='1234' } | ConvertTo-Json) -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='alice.secure'; password='Str0ng!Passw0rd' } | ConvertTo-Json)

# Path traversal
Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=public-note.txt" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/vuln/files/read?path=$([uri]::EscapeDataString('..\\..\\appsettings.json'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=$([uri]::EscapeDataString('..\\..\\appsettings.json'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}

# Redirect
Invoke-WebRequest -Uri "$BaseUrl/vuln/redirect?returnUrl=$([uri]::EscapeDataString('https://evil.example/phishing'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode
Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('/dashboard'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('https://evil.example/phishing'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}

# Error handling
Invoke-RestMethod -Uri "$BaseUrl/secure/errors/divide-by-zero" -Method Get
```

## Attendus

- `secure/register` rejette les mots de passe faibles
- `secure/files/read` bloque les chemins non conformes
- `secure/redirect` refuse les URL absolues externes
- `secure/errors/divide-by-zero` renvoie une erreur controlee (`application/problem+json`)

## Fichiers utiles

- `04-NET10/AppSecWorkshop04/AppSecWorkshop04.http`
- `04-NET10/scripts/calls.ps1`
- `04-NET10/scripts/run-crypto-checks.ps1`
- `04-NET10/pipeline/crypto-ci.yml`

## Nettoyage

```powershell
dotnet clean .\04-NET10\Atelier04.slnx
```