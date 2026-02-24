# Atelier 08 - Monitoring securite (.NET Framework 4.8)

## Objectif

Verifier un flux de monitoring securite:
- correlation id
- audit events
- alertes
- reset admin protege

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `08-NET48/Atelier08.slnx`
- `08-NET48/SecurityMonitoringLab/SecurityMonitoringLab.csproj:1`

```powershell
dotnet restore .\08-NET48\Atelier08.slnx
dotnet build .\08-NET48\Atelier08.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `08-NET48/SecurityMonitoringLab/Program.cs:34`
- `08-NET48/SecurityMonitoringLab/Program.cs:78`

```powershell
$BaseUrl = 'http://localhost:5108'
dotnet run --project .\08-NET48\SecurityMonitoringLab\SecurityMonitoringLab.csproj --urls=$BaseUrl
```

## Etape 3 - Login vuln et secure

Code source a verifier (etape):
- `08-NET48/SecurityMonitoringLab/Program.cs:106`
- `08-NET48/SecurityMonitoringLab/Program.cs:119`
- `08-NET48/SecurityMonitoringLab/Program.cs:80`

```powershell
$BaseUrl = 'http://localhost:5108'
$badLogin = @{ username = 'alice'; password = 'wrong' } | ConvertTo-Json
$okLogin = @{ username = 'alice'; password = 'Password123!' } | ConvertTo-Json

Invoke-RestMethod -Uri "$BaseUrl/vuln/login" -Method Post -ContentType 'application/json' -Body $badLogin
Invoke-RestMethod -Uri "$BaseUrl/secure/login" -Method Post -ContentType 'application/json' -Headers @{ 'X-Correlation-ID' = 'corr-001' } -Body $badLogin
Invoke-RestMethod -Uri "$BaseUrl/secure/login" -Method Post -ContentType 'application/json' -Headers @{ 'X-Correlation-ID' = 'corr-002' } -Body $okLogin
```

## Etape 4 - Consulter audit et alertes

Code source a verifier (etape):
- `08-NET48/SecurityMonitoringLab/Program.cs:157`
- `08-NET48/SecurityMonitoringLab/Program.cs:175`
- `08-NET48/SecurityMonitoringLab/Program.cs:143`

```powershell
$BaseUrl = 'http://localhost:5108'
Invoke-RestMethod -Uri "$BaseUrl/secure/audit/events" -Method Get
Invoke-RestMethod -Uri "$BaseUrl/secure/alerts" -Method Get
```

## Etape 5 - Reset alertes admin

Code source a verifier (etape):
- `08-NET48/SecurityMonitoringLab/Program.cs:187`
- `08-NET48/SecurityMonitoringLab/Program.cs:194`

```powershell
$BaseUrl = 'http://localhost:5108'

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin/reset-alerts" -Method Post -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

$headers = @{ 'X-SOC-Key' = 'soc-admin-key' }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin/reset-alerts" -Method Post -Headers $headers
Invoke-RestMethod -Uri "$BaseUrl/secure/alerts" -Method Get
```

## Etape 6 - Executer les tests atelier

Code source a verifier (etape):
- `08-NET48/SecurityMonitoringLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\08-NET48\Atelier08.slnx
```

## Etape 7 - Scripts stagiaires

Code source a verifier (etape):
- `08-NET48/scripts/calls.ps1:1`
- `08-NET48/scripts/run-monitoring-checks.ps1:1`

```powershell
.\08-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5108'
.\08-NET48\scripts\run-monitoring-checks.ps1 -BaseUrl 'http://localhost:5108'
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5108/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\08-NET48\Atelier08.slnx
```
