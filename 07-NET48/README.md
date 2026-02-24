# Atelier 07 - Limiter l'exposition (.NET Framework 4.8)

## Objectif

Atelier NET48 pour reduire la surface d'exposition:

- filtrage WAF-like
- protection endpoint admin
- validation metadata upload
- limitation de debit

Implementation reelle: `07-NET48/ExposureDefenseLab/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et lancer

```powershell
if (Test-Path .\07-NET48) { Set-Location .\07-NET48 }
dotnet restore .\Atelier07.slnx

$BaseUrl = 'http://localhost:5107'
dotnet run --project .\ExposureDefenseLab\ExposureDefenseLab.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5107`.

## Etape 2 - Admin endpoint: vuln vs secure

```powershell
$BaseUrl = 'http://localhost:5107'
Invoke-RestMethod -Uri "$BaseUrl/vuln/admin/ping" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin/ping" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$headers = @{ 'X-Admin-Key' = 'workshop-admin-key' }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin/ping" -Method Get -Headers $headers
```

Resultat attendu: endpoint secure accessible uniquement avec `X-Admin-Key` valide.

## Etape 3 - Filtrage WAF-like

```powershell
$BaseUrl = 'http://localhost:5107'
Invoke-RestMethod -Uri "$BaseUrl/secure/search?q=normal-query" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/search?q=$([uri]::EscapeDataString('<script>alert(1)</script>'))" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Resultat attendu: pattern suspect bloque en `403`.

## Etape 4 - Validation upload metadata

```powershell
$BaseUrl = 'http://localhost:5107'

$ok = @{ fileName = 'doc.pdf'; contentType = 'application/pdf'; size = 1200 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/upload/meta" -Method Post -ContentType 'application/json' -Body $ok

$bad = @{ fileName = '..\\evil.exe'; contentType = 'application/octet-stream'; size = 99999999 } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/upload/meta" -Method Post -ContentType 'application/json' -Body $bad -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Resultat attendu: payload invalide refuse (`400`).

## Etape 5 - Rate limiting

```powershell
$BaseUrl = 'http://localhost:5107'
1..8 | ForEach-Object {
    try {
        $r = Invoke-WebRequest -Uri "$BaseUrl/vuln/search?q=test$_" -Method Get -ErrorAction Stop
        "Req $_ -> $($r.StatusCode)"
    } catch {
        "Req $_ -> $($_.Exception.Response.StatusCode.value__)"
    }
}
```

Resultat attendu: certaines requetes en `429` (fenetre 10s, limite 5).

## Tests automatisees

```powershell
if (Test-Path .\07-NET48) { Set-Location .\07-NET48 }
dotnet test .\ExposureDefenseLab.Tests\ExposureDefenseLab.Tests.csproj
```

Note: sur la piste NET48, le projet de tests fournit des smoke tests d'execution (`07-NET48/ExposureDefenseLab.Tests/SmokeTests.cs`).

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5107/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\Atelier07.slnx
```
