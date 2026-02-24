# Atelier 08 - Monitoring securite (.NET Framework 4.8)

## Objectif

Atelier NET48 pour mettre en place des fondamentaux de monitoring securite:

- correlation ID
- audit trail
- alerting sur echecs d'auth
- endpoint SOC protege

Implementation reelle: `08-NET48/SecurityMonitoringLab/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et lancer

```powershell
if (Test-Path .\08-NET48) { Set-Location .\08-NET48 }
dotnet restore .\Atelier08.slnx

$BaseUrl = 'http://localhost:5108'
dotnet run --project .\SecurityMonitoringLab\SecurityMonitoringLab.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5108`.

## Etape 2 - Login vuln vs secure

```powershell
$BaseUrl = 'http://localhost:5108'
$badLogin = @{ username = 'alice'; password = 'wrong' } | ConvertTo-Json
$okLogin = @{ username = 'alice'; password = 'Password123!' } | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/vuln/login" -Method Post -ContentType 'application/json' -Body $badLogin
Invoke-RestMethod -Uri "$BaseUrl/secure/login" -Method Post -ContentType 'application/json' -Headers @{ 'X-Correlation-ID' = 'corr-001' } -Body $badLogin
Invoke-RestMethod -Uri "$BaseUrl/secure/login" -Method Post -ContentType 'application/json' -Headers @{ 'X-Correlation-ID' = 'corr-002' } -Body $okLogin
```

Resultat attendu: reponses avec `correlationId` et etat `authenticated`.

## Etape 3 - Consulter audit trail

```powershell
$BaseUrl = 'http://localhost:5108'
Invoke-RestMethod -Uri "$BaseUrl/secure/audit/events" -Method Get
```

Resultat attendu: presence de `auth.failure` et `auth.success`.

## Etape 4 - Consulter alertes

```powershell
$BaseUrl = 'http://localhost:5108'
Invoke-RestMethod -Uri "$BaseUrl/secure/alerts" -Method Get
```

Resultat attendu: alertes apres plusieurs echecs.

## Etape 5 - Reset alertes (admin)

```powershell
$BaseUrl = 'http://localhost:5108'

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin/reset-alerts" -Method Post -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$headers = @{ 'X-SOC-Key' = 'soc-admin-key' }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin/reset-alerts" -Method Post -Headers $headers
Invoke-RestMethod -Uri "$BaseUrl/secure/alerts" -Method Get
```

Resultat attendu: reset autorise uniquement avec `X-SOC-Key` valide.

## Tests automatisees

```powershell
if (Test-Path .\08-NET48) { Set-Location .\08-NET48 }
dotnet test .\SecurityMonitoringLab.Tests\SecurityMonitoringLab.Tests.csproj
```

Note: sur la piste NET48, le projet de tests fournit des smoke tests d'execution (`08-NET48/SecurityMonitoringLab.Tests/SmokeTests.cs`).

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5108/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\Atelier08.slnx
```
