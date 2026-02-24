# Atelier 04 - Secure Coding et durcissement (.NET Framework 4.8)

## Objectif

Atelier NET48 de comparaison `vuln` vs `secure` pour:

- validation d'entrees
- path traversal
- open redirect
- gestion d'erreurs

Implementation reelle: `04-NET48/AppSecWorkshop04/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Build et lancement

```powershell
dotnet restore .\04-NET48\AppSecWorkshop04\AppSecWorkshop04.csproj
dotnet build .\04-NET48\AppSecWorkshop04\AppSecWorkshop04.csproj

$BaseUrl = 'http://localhost:5104'
dotnet run --project .\04-NET48\AppSecWorkshop04\AppSecWorkshop04.csproj --urls=$BaseUrl
```

## Verification fonctionnelle

Dans un second terminal:

### 1) Validation des entrees (register)

```powershell
$BaseUrl = 'http://localhost:5104'

$weak = @{ username = 'a'; password = '123' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/vuln/register" -Method Post -ContentType 'application/json' -Body $weak

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body $weak -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$strong = @{ username = 'alice.secure'; password = 'Str0ng!Passw0rd' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/register" -Method Post -ContentType 'application/json' -Body $strong
```

Attendu:

- `vuln/register`: accepte
- `secure/register`: rejette faible puis accepte fort

### 2) Path traversal

```powershell
$BaseUrl = 'http://localhost:5104'

Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=public-note.txt" -Method Get

$traversal = '..\\..\\Windows\\win.ini'
Invoke-RestMethod -Uri "$BaseUrl/vuln/files/read?path=$([uri]::EscapeDataString($traversal))" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/files/read?fileName=$([uri]::EscapeDataString($traversal))" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Attendu: tentative traversal rejetee cote `secure`.

### 3) Open redirect

```powershell
$BaseUrl = 'http://localhost:5104'

Invoke-WebRequest -Uri "$BaseUrl/vuln/redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode,Headers

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

Invoke-RestMethod -Uri "$BaseUrl/secure/redirect?returnUrl=$([uri]::EscapeDataString('/home'))" -Method Get
```

Attendu: URL externe refusee sur endpoint secure.

### 4) Gestion d'erreurs

```powershell
$BaseUrl = 'http://localhost:5104'

try {
    Invoke-RestMethod -Uri "$BaseUrl/vuln/errors/divide-by-zero" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

Invoke-RestMethod -Uri "$BaseUrl/secure/errors/divide-by-zero" -Method Get
```

Attendu:

- `vuln`: erreur technique non maitrisee
- `secure`: erreur controlee (payload type ProblemDetails)

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5104/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\04-NET48\AppSecWorkshop04\AppSecWorkshop04.csproj
```
