# Atelier 09 - Durcissement AuthN/AuthZ (.NET Framework 4.8)

## Objectif

Comparer les mecanismes `vuln` et `secure` sur:
- emission de token
- controle de scopes
- autorisation objet (owner/admin)
- publication protegee

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `09-NET48/Atelier09.slnx`
- `09-NET48/AuthzHardeningLab/AuthzHardeningLab.csproj:1`

```powershell
dotnet restore .\09-NET48\Atelier09.slnx
dotnet build .\09-NET48\Atelier09.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `09-NET48/AuthzHardeningLab/Program.cs:38`
- `09-NET48/AuthzHardeningLab/Program.cs:82`

```powershell
$BaseUrl = 'http://localhost:5109'
dotnet run --project .\09-NET48\AuthzHardeningLab\AuthzHardeningLab.csproj --urls=$BaseUrl
```

## Etape 3 - Emettre token vuln et token secure

Code source a verifier (etape):
- `09-NET48/AuthzHardeningLab/Program.cs:101`
- `09-NET48/AuthzHardeningLab/Program.cs:111`
- `09-NET48/AuthzHardeningLab/Program.cs:198`

```powershell
$BaseUrl = 'http://localhost:5109'

$vulnReq = @{ username = 'alice'; scope = 'docs.read' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/vuln/auth/token" -Method Post -ContentType 'application/json' -Body $vulnReq

$secureReq = @{ username = 'alice'; scope = 'docs.read docs.publish' } | ConvertTo-Json
$secureToken = Invoke-RestMethod -Uri "$BaseUrl/secure/auth/token" -Method Post -ContentType 'application/json' -Body $secureReq
$Bearer = $secureToken.token
```

## Etape 4 - Verifier l'acces lecture document

Code source a verifier (etape):
- `09-NET48/AuthzHardeningLab/Program.cs:121`
- `09-NET48/AuthzHardeningLab/Program.cs:144`
- `09-NET48/AuthzHardeningLab/Program.cs:154`
- `09-NET48/AuthzHardeningLab/Program.cs:161`

```powershell
$BaseUrl = 'http://localhost:5109'
$headers = @{ Authorization = "Bearer $Bearer" }
Invoke-RestMethod -Uri "$BaseUrl/secure/docs/1" -Method Get -Headers $headers

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/docs/2" -Method Get -Headers $headers -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}
```

## Etape 5 - Publication avec scope

Code source a verifier (etape):
- `09-NET48/AuthzHardeningLab/Program.cs:173`
- `09-NET48/AuthzHardeningLab/Program.cs:181`
- `09-NET48/AuthzHardeningLab/Program.cs:187`

```powershell
$BaseUrl = 'http://localhost:5109'
$headers = @{ Authorization = "Bearer $Bearer" }
Invoke-RestMethod -Uri "$BaseUrl/secure/docs/1/publish" -Method Post -Headers $headers

Invoke-RestMethod -Uri "$BaseUrl/vuln/docs/2?username=alice" -Method Get
```

## Etape 6 - Executer les tests atelier

Code source a verifier (etape):
- `09-NET48/AuthzHardeningLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\09-NET48\Atelier09.slnx
```

## Etape 7 - Scripts stagiaires

Code source a verifier (etape):
- `09-NET48/scripts/calls.ps1:1`
- `09-NET48/scripts/run-authz-checks.ps1:1`

```powershell
.\09-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5109'
.\09-NET48\scripts\run-authz-checks.ps1 -BaseUrl 'http://localhost:5109'
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5109/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\09-NET48\Atelier09.slnx
```
