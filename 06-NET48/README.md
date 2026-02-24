# Atelier 06 - Securite du code externe (.NET Framework 4.8)

## Objectif

Comparer les pratiques `vuln` et `secure` autour de:
- gestion des secrets
- appels sortants
- approbation de dependances
- verification de hash

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `06-NET48/Atelier06.slnx`
- `06-NET48/SupplyChainSecurityLab/SupplyChainSecurityLab.csproj:1`

```powershell
dotnet restore .\06-NET48\Atelier06.slnx
dotnet build .\06-NET48\Atelier06.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `06-NET48/SupplyChainSecurityLab/Program.cs:45`
- `06-NET48/SupplyChainSecurityLab/Program.cs:89`

```powershell
$BaseUrl = 'http://localhost:5106'
dotnet run --project .\06-NET48\SupplyChainSecurityLab\SupplyChainSecurityLab.csproj --urls=$BaseUrl
```

## Etape 3 - Secrets: hardcode vs environment

Code source a verifier (etape):
- `06-NET48/SupplyChainSecurityLab/Program.cs:108`
- `06-NET48/SupplyChainSecurityLab/Program.cs:114`

```powershell
$BaseUrl = 'http://localhost:5106'
Invoke-RestMethod -Uri "$BaseUrl/vuln/config/secret" -Method Get

$env:UPSTREAM_API_KEY = 'local-workshop-key'
Invoke-RestMethod -Uri "$BaseUrl/secure/config/secret" -Method Get
```

## Etape 4 - Appels sortants controles

Code source a verifier (etape):
- `06-NET48/SupplyChainSecurityLab/Program.cs:123`
- `06-NET48/SupplyChainSecurityLab/Program.cs:141`

```powershell
$BaseUrl = 'http://localhost:5106'
Invoke-RestMethod -Uri "$BaseUrl/vuln/outbound/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/outbound/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/outbound/fetch?url=$([uri]::EscapeDataString('http://127.0.0.1:80'))" -Method Get -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

## Etape 5 - Approbation de dependance

Code source a verifier (etape):
- `06-NET48/SupplyChainSecurityLab/Program.cs:175`
- `06-NET48/SupplyChainSecurityLab/Program.cs:186`
- `06-NET48/SupplyChainSecurityLab/Program.cs:228`

```powershell
$BaseUrl = 'http://localhost:5106'

$bad = @{ packageId = 'unknown.pkg'; sourceUrl = 'https://evil.local/pkg'; sha256 = '123' } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/dependency/approve" -Method Post -ContentType 'application/json' -Body $bad -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

$shaBody = @{ payload = 'package-content-v1' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/dependency/sha256" -Method Post -ContentType 'application/json' -Body $shaBody
```

## Etape 6 - Executer les tests atelier

Code source a verifier (etape):
- `06-NET48/SupplyChainSecurityLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\06-NET48\Atelier06.slnx
```

## Etape 7 - Scripts stagiaires

Code source a verifier (etape):
- `06-NET48/scripts/calls.ps1:1`
- `06-NET48/scripts/run-sca.ps1:1`
- `06-NET48/scripts/generate-sbom.ps1:1`

```powershell
.\06-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5106'
.\06-NET48\scripts\run-sca.ps1
.\06-NET48\scripts\generate-sbom.ps1
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5106/ user=$env:USERNAME
```

## Nettoyage

```powershell
Remove-Item Env:\UPSTREAM_API_KEY -ErrorAction SilentlyContinue
dotnet clean .\06-NET48\Atelier06.slnx
```
