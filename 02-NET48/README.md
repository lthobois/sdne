# Atelier 02 - SQLi, XSS, CSRF, SSRF (.NET Framework 4.8)

## Objectif

Atelier NET48 de comparaison `vuln` vs `secure` pour:

- SQL Injection
- XSS reflechi
- CSRF
- SSRF

Implementation reelle: `02-NET48/AppSecWorkshop02/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Build et lancement

```powershell
dotnet restore .\02-NET48\AppSecWorkshop02\AppSecWorkshop02.csproj
dotnet build .\02-NET48\AppSecWorkshop02\AppSecWorkshop02.csproj

$BaseUrl = 'http://localhost:5102'
dotnet run --project .\02-NET48\AppSecWorkshop02\AppSecWorkshop02.csproj --urls=$BaseUrl
```

## Verification fonctionnelle

Dans un second terminal:

### 1) SQLi

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = "alice' OR 1=1 --"

Invoke-RestMethod -Uri "$BaseUrl/vuln/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/sql/users?username=$([uri]::EscapeDataString($payload))" -Method Get
```

Attendu:

- `vuln`: renvoie plusieurs utilisateurs
- `secure`: ne contourne pas le filtrage (liste vide pour ce payload)

### 2) XSS

```powershell
$BaseUrl = 'http://localhost:5102'
$payload = '<script>alert("xss")</script>'

Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($payload))" -Method Get | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($payload))" -Method Get | Select-Object -ExpandProperty Content
```

Attendu:

- `vuln`: balise non encodee
- `secure`: balise encodee HTML

### 3) CSRF

```powershell
$BaseUrl = 'http://localhost:5102'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$loginBody = @{ username = 'alice' } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -WebSession $session -ContentType 'application/json' -Body $loginBody
$csrf = $login.csrfToken

$transferBody = @{ to = 'bob'; amount = 150 } | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/vuln/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $transferBody

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -ContentType 'application/json' -Body $transferBody -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$headers = @{ 'X-CSRF-Token' = $csrf }
Invoke-RestMethod -Uri "$BaseUrl/secure/csrf/transfer" -Method Post -WebSession $session -Headers $headers -ContentType 'application/json' -Body $transferBody
```

Attendu:

- `vuln`: accepte sans token
- `secure`: `403` sans token, `200` avec token valide

### 4) SSRF

```powershell
$BaseUrl = 'http://localhost:5102'
Invoke-RestMethod -Uri "$BaseUrl/vuln/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/ssrf/fetch?url=$([uri]::EscapeDataString('http://127.0.0.1:80'))" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Attendu:

- `secure`: bloque localhost/IP sensible (HTTP `400`)

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5102/ user=$env:USERNAME
```

Si conflit de prefixe:

```powershell
netsh http show urlacl
```

## Nettoyage

```powershell
dotnet clean .\02-NET48\AppSecWorkshop02\AppSecWorkshop02.csproj
```
