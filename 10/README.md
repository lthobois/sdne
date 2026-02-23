# Atelier 10 - Validation perimetrique

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 9.x installe
- PowerShell 5.1+
- (Optionnel) Docker Desktop pour scenario compose/nginx

## Etape 1 - Initialiser et lancer

Objectif: demarrer l'API perimetrique locale.

```powershell
if\ \(Test-Path\ \.\10\)\ \{\ Set-Location\ \.\10\ }
dotnet restore .\Atelier10.slnx
$BaseUrl = 'http://localhost:5110'
dotnet run --project .\PerimeterValidationLab\PerimeterValidationLab.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5110`.

## Etape 2 - Header injection sur lien de reset

Objectif: comparer resolution d'origine vulnerable et securisee.

```powershell
$BaseUrl = 'http://localhost:5110'
$headers = @{ 'X-Forwarded-Host' = 'evil.example'; 'X-Forwarded-Proto' = 'http' }

Invoke-RestMethod -Uri "$BaseUrl/vuln/links/reset-password?user=alice" -Method Get -Headers $headers
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/links/reset-password?user=alice" -Method Get -Headers $headers -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

Resultat attendu: endpoint secure rejette origine non fiable.

## Etape 3 - Resolution tenant

Objectif: verifier protection de la resolution multi-tenant par host.

```powershell
$BaseUrl = 'http://localhost:5110'
$headersBad = @{ 'X-Forwarded-Host' = 'unknown-tenant.local'; 'X-Forwarded-Proto' = 'https' }

Invoke-RestMethod -Uri "$BaseUrl/vuln/tenant/home" -Method Get -Headers $headersBad
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/tenant/home" -Method Get -Headers $headersBad -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

Resultat attendu: tenant inconnu refuse en mode secure (`403` ou `400`).

## Etape 4 - Diagnostics des metadonnees de requete

Objectif: verifier ce que l'API conserve des headers forwarded.

```powershell
$BaseUrl = 'http://localhost:5110'
$headers = @{ 'X-Forwarded-Host' = 'app.example.local'; 'X-Forwarded-Proto' = 'https' }
Invoke-RestMethod -Uri "$BaseUrl/secure/diagnostics/request-meta" -Method Get -Headers $headers
```

Resultat attendu: JSON de diagnostic avec `resolved.Valid` et details de resolution.

## Etape 5 - Option Docker compose (perimetre local)

Objectif: executer le scenario proxy + application.

```powershell
if (Test-Path .\10\infra) { if\ \(Test-Path\ \.\10\)\ \{\ Set-Location\ \.\10\ }\infra } elseif (Test-Path .\infra) { if (Test-Path .\infra) { Set-Location .\infra } }
docker compose up -d
```

Check:

```powershell
docker compose ps
```

Resultat attendu: services `Up`.

## Etape 6 - Executer les tests

Objectif: valider automatiquement les regles perimetriques.

```powershell
if\ \(Test-Path\ \.\10\)\ \{\ Set-Location\ \.\10\ }
dotnet test .\PerimeterValidationLab.Tests\PerimeterValidationLab.Tests.csproj
```

Resultat attendu: tests `Passed`.

## Verifications

- Les endpoints `secure/*` n'acceptent pas aveuglement `X-Forwarded-*`
- Tenant resolution durcie
- Diagnostics exploitables pour audit perimetrique

## Depannage

- Si Docker indisponible, ignorer l'etape 5 (optionnelle).
- Si endpoint secure rejette tout, verifier headers et host attendus dans la policy.

## Nettoyage / Reset

```powershell
# Dans le terminal API
# Ctrl+C

if\ \(Test-Path\ \.\10\)\ \{\ Set-Location\ \.\10\ }
if (Test-Path .\infra) { Set-Location .\infra }
docker compose down

Set-Location ..
dotnet clean .\Atelier10.slnx
```

## Diagramme Mermaid

```mermaid
flowchart TD
    A[Client headers] --> B[Trusted proxy policy]
    B --> C[Origin resolution]
    C --> D[Tenant validation]
    D --> E[Secure endpoint response]
```


