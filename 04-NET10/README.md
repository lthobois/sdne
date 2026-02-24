# Atelier 04 - Secure Coding et durcissement (.NET 10)

## Objectif

Comparer des implementations `vuln` vs `secure` pour:
- validation d'entree
- path traversal
- open redirect
- gestion d'erreurs

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Program.cs:3`
- `04-NET10/AppSecWorkshop04/Program.cs:16`
- `04-NET10/AppSecWorkshop04/Program.cs:25`

```powershell
dotnet restore .\04-NET10\Atelier04.slnx
dotnet build .\04-NET10\Atelier04.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Properties/launchSettings.json:8`

```powershell
$BaseUrl = 'http://localhost:5104'
dotnet run --project .\04-NET10\AppSecWorkshop04\AppSecWorkshop04.csproj --urls=$BaseUrl
```

## Etape 3 - Validation d'entree (register)

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Program.cs:37`
- `04-NET10/AppSecWorkshop04/Program.cs:42`
- `04-NET10/AppSecWorkshop04/Program.cs:51`
- `04-NET10/AppSecWorkshop04/Program.cs:65`
- `04-NET10/AppSecWorkshop04/Program.cs:175`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/vuln/register" -Method Post -ContentType 'application/json' -Body (@{ username='aa'; password='1234' } | ConvertTo-Json)
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='aa'; password='1234' } | ConvertTo-Json) -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body (@{ username='alice.secure'; password='Str0ng!Passw0rd' } | ConvertTo-Json)
```

## Etape 4 - Path traversal

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Program.cs:83`
- `04-NET10/AppSecWorkshop04/Program.cs:85`
- `04-NET10/AppSecWorkshop04/Program.cs:100`
- `04-NET10/AppSecWorkshop04/Program.cs:102`
- `04-NET10/AppSecWorkshop04/Program.cs:110`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=public-note.txt" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/vuln/files/read?path=$([uri]::EscapeDataString('..\\..\\appsettings.json'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=$([uri]::EscapeDataString('..\\..\\appsettings.json'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Etape 5 - Open redirect

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Program.cs:124`
- `04-NET10/AppSecWorkshop04/Program.cs:129`
- `04-NET10/AppSecWorkshop04/Program.cs:131`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-WebRequest -Uri "$BaseUrl/vuln/redirect?returnUrl=$([uri]::EscapeDataString('https://evil.example/phishing'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode
Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('/dashboard'))" -Method Get
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('https://evil.example/phishing'))" -Method Get -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Etape 6 - Gestion d'erreurs

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04/Program.cs:144`
- `04-NET10/AppSecWorkshop04/Program.cs:151`
- `04-NET10/AppSecWorkshop04/Program.cs:161`

```powershell
$BaseUrl = 'http://localhost:5104'
Invoke-RestMethod -Uri "$BaseUrl/secure/errors/divide-by-zero" -Method Get
```

## Etape 7 - Executer les tests

Code source a verifier (etape):
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:20`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:30`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:40`
- `04-NET10/AppSecWorkshop04.Tests/AppSecWorkshop04Tests.cs:50`

```powershell
dotnet test .\04-NET10\Atelier04.slnx
```

## Scripts stagiaires (support)

```powershell
.\04-NET10\scripts\run-crypto-checks.ps1
.\04-NET10\scripts\calls.ps1
```

## Fichiers utiles

- `04-NET10/AppSecWorkshop04/AppSecWorkshop04.http`
- `04-NET10/scripts/calls.ps1`
- `04-NET10/scripts/run-crypto-checks.ps1`
- `04-NET10/pipeline/crypto-ci.yml`

## Nettoyage

```powershell
dotnet clean .\04-NET10\Atelier04.slnx
```