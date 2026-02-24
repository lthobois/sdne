# Atelier 06 - Securite du code externe (.NET Framework 4.8)

## Objectif

Atelier NET48 pour travailler la securite supply-chain:

- gestion des secrets
- controles sur appels sortants
- approbation de dependances (provenance + digest)
- verification SCA/SBOM

Implementation reelle: `06-NET48/SupplyChainSecurityLab/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et lancer

```powershell
if (Test-Path .\06-NET48) { Set-Location .\06-NET48 }
dotnet restore .\Atelier06.slnx

$BaseUrl = 'http://localhost:5106'
dotnet run --project .\SupplyChainSecurityLab\SupplyChainSecurityLab.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5106`.

## Etape 2 - Secrets: hardcode vs env var

```powershell
$BaseUrl = 'http://localhost:5106'
Invoke-RestMethod -Uri "$BaseUrl/vuln/config/secret" -Method Get

$env:UPSTREAM_API_KEY = 'local-workshop-key'
Invoke-RestMethod -Uri "$BaseUrl/secure/config/secret" -Method Get
```

Resultat attendu: endpoint secure indique `keyConfigured = true`.

## Etape 3 - Appels sortants controles

```powershell
$BaseUrl = 'http://localhost:5106'
Invoke-RestMethod -Uri "$BaseUrl/vuln/outbound/fetch?url=$([uri]::EscapeDataString('https://example.com'))" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/outbound/fetch?url=$([uri]::EscapeDataString('http://example.com'))" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

Invoke-RestMethod -Uri "$BaseUrl/secure/outbound/fetch?url=$([uri]::EscapeDataString('https://jsonplaceholder.typicode.com/todos/1'))" -Method Get
```

Resultat attendu:

- mode `vuln`: fetch permissif
- mode `secure`: HTTPS + host allowlist obligatoires

## Etape 4 - Approbation de dependance

```powershell
$BaseUrl = 'http://localhost:5106'

$bad = @{ packageId = 'unknown.pkg'; sourceUrl = 'https://evil.local/pkg'; sha256 = '123' } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/dependency/approve" -Method Post -ContentType 'application/json' -Body $bad -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$good = @{ packageId = 'Polly'; sourceUrl = 'https://api.nuget.org/v3/index.json'; sha256 = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/dependency/approve" -Method Post -ContentType 'application/json' -Body $good

$shaBody = @{ payload = 'package-content-v1' } | ConvertTo-Json
$sha = Invoke-RestMethod -Uri "$BaseUrl/secure/dependency/sha256" -Method Post -ContentType 'application/json' -Body $shaBody
$sha.sha256
```

## Etape 5 - Tests automatiques

```powershell
if (Test-Path .\06-NET48) { Set-Location .\06-NET48 }
dotnet test .\SupplyChainSecurityLab.Tests\SupplyChainSecurityLab.Tests.csproj
```

Note: sur la piste NET48, le projet de tests fournit des smoke tests d'execution (`06-NET48/SupplyChainSecurityLab.Tests/SmokeTests.cs`).

## Etape 6 - SCA / SBOM

```powershell
if (Test-Path .\06-NET48) { Set-Location .\06-NET48 }
.\scripts\run-sca.ps1
.\scripts\generate-sbom.ps1
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5106/ user=$env:USERNAME
```

## Nettoyage

```powershell
Remove-Item Env:\UPSTREAM_API_KEY -ErrorAction SilentlyContinue
dotnet clean .\Atelier06.slnx
```
