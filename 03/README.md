# Atelier 03 - Session, Deserialisation, IDOR

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 9.x installe
- PowerShell 5.1+

## Etape 1 - Initialiser et lancer

Objectif: demarrer l'API de l'atelier.

```powershell
if\ \(Test-Path\ \.\03\)\ \{\ Set-Location\ \.\03\ }
dotnet restore .\AppSecWorkshop03\AppSecWorkshop03.csproj
$BaseUrl = 'http://localhost:5103'
dotnet run --project .\AppSecWorkshop03\AppSecWorkshop03.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5103`.

## Etape 2 - Session theft (token)

Objectif: comparer validation faible et validation renforcee.

```powershell
$BaseUrl = 'http://localhost:5103'
$loginBody = @{ username = 'alice' } | ConvertTo-Json

$vulnLogin = Invoke-RestMethod -Uri "$BaseUrl/vuln/session/login" -Method Post -ContentType 'application/json' -Body $loginBody
$vulnToken = $vulnLogin.token
Invoke-RestMethod -Uri "$BaseUrl/vuln/session/profile?token=$vulnToken" -Method Get

$secureLogin = Invoke-RestMethod -Uri "$BaseUrl/secure/session/login" -Method Post -ContentType 'application/json' -Headers @{ 'User-Agent' = 'WorkshopAgent/1.0' } -Body $loginBody
$secureToken = $secureLogin.token
Invoke-RestMethod -Uri "$BaseUrl/secure/session/profile" -Method Get -Headers @{ 'X-Session-Token' = $secureToken; 'User-Agent' = 'WorkshopAgent/1.0' }
```

Resultat attendu: profile secure valide seulement avec token + contexte attendu.

## Etape 3 - Deserialisation

Objectif: tester endpoint vulnerable puis endpoint securise.

```powershell
$BaseUrl = 'http://localhost:5103'

$safeBody = @{ action = 'echo'; message = 'hello' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body $safeBody

$badBody = @{ action = 'delete-all'; message = 'x' } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body $badBody -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

Resultat attendu: seule l'action `echo` est acceptee en mode secure.

## Etape 4 - IDOR

Objectif: verifier qu'un utilisateur ne lit pas une ressource qui ne lui appartient pas.

```powershell
$BaseUrl = 'http://localhost:5103'

Invoke-RestMethod -Uri "$BaseUrl/vuln/idor/orders/2?username=alice" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/2?username=alice" -Method Get -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/2?username=admin" -Method Get
```

Resultat attendu:

- `vuln`: acces direct possible
- `secure`: `403` pour utilisateur non autorise, acces admin autorise

## Verifications

- Token vulnerable reutilisable facilement
- Validation secure impose en-tete token
- Actions de deserialisation whitelistes
- Controle d'acces objet actif sur endpoint secure

## Depannage

- Si `401` sur `/secure/session/profile`, verifier `X-Session-Token` et `User-Agent`.
- Si `404` sur commandes IDOR, utiliser un id existant (ex: `1` ou `2`).

## Nettoyage / Reset

```powershell
# Dans le terminal API
# Ctrl+C

if\ \(Test-Path\ \.\03\)\ \{\ Set-Location\ \.\03\ }
dotnet clean .\AppSecWorkshop03\AppSecWorkshop03.csproj
```

## Diagramme Mermaid

```mermaid
flowchart TD
    A[Client] --> B[Session module]
    A --> C[Deserialization module]
    A --> D[IDOR module]
    B --> E[Secure checks]
    C --> E
    D --> E
```


