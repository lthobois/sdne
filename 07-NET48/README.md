# Atelier 07 - Limiter l'exposition (.NET Framework 4.8)

## Objectif

Reduire la surface d'attaque via:
- endpoint admin protege
- filtrage de patterns malveillants
- validation metadata upload
- rate limiting

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `07-NET48/Atelier07.slnx`
- `07-NET48/ExposureDefenseLab/ExposureDefenseLab.csproj:1`

```powershell
dotnet restore .\07-NET48\Atelier07.slnx
dotnet build .\07-NET48\Atelier07.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `07-NET48/ExposureDefenseLab/Program.cs:35`
- `07-NET48/ExposureDefenseLab/Program.cs:128`

```powershell
$BaseUrl = 'http://localhost:5107'
dotnet run --project .\07-NET48\ExposureDefenseLab\ExposureDefenseLab.csproj --urls=$BaseUrl
```

## Etape 3 - Endpoint admin (vuln vs secure)

Code source a verifier (etape):
- `07-NET48/ExposureDefenseLab/Program.cs:147`
- `07-NET48/ExposureDefenseLab/Program.cs:154`

```powershell
$BaseUrl = 'http://localhost:5107'
Invoke-RestMethod -Uri "$BaseUrl/vuln/admin/ping" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin/ping" -Method Get -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

$headers = @{ 'X-Admin-Key' = 'workshop-admin-key' }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin/ping" -Method Get -Headers $headers
```

## Etape 4 - Filtrage et upload

Code source a verifier (etape):
- `07-NET48/ExposureDefenseLab/Program.cs:121`
- `07-NET48/ExposureDefenseLab/Program.cs:176`
- `07-NET48/ExposureDefenseLab/Program.cs:202`

```powershell
$BaseUrl = 'http://localhost:5107'
Invoke-RestMethod -Uri "$BaseUrl/secure/search?q=normal-query" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/search?q=$([uri]::EscapeDataString('<script>alert(1)</script>'))" -Method Get -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

$ok = @{ fileName = 'doc.pdf'; contentType = 'application/pdf'; size = 1200 } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/upload/meta" -Method Post -ContentType 'application/json' -Body $ok
```

## Etape 5 - Verifier le rate limiting

Code source a verifier (etape):
- `07-NET48/ExposureDefenseLab/Program.cs:91`

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

## Etape 6 - Executer les tests atelier

Code source a verifier (etape):
- `07-NET48/ExposureDefenseLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\07-NET48\Atelier07.slnx
```

## Etape 7 - Scripts stagiaires

Code source a verifier (etape):
- `07-NET48/scripts/calls.ps1:1`
- `07-NET48/scripts/run-defense-checks.ps1:1`

```powershell
.\07-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5107'
.\07-NET48\scripts\run-defense-checks.ps1 -BaseUrl 'http://localhost:5107'
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5107/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\07-NET48\Atelier07.slnx
```
