# Atelier 05 - Validation continue (tests, SAST, DAST) (.NET Framework 4.8)

## Objectif

Verifier une chaine de validation securite continue sur l'atelier NET48:
- checks manuels `vuln` vs `secure`
- tests automatises
- controles SAST/SCA locaux
- checks DAST scripts

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+
- Etre positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

Code source a verifier (etape):
- `05-NET48/Atelier05.slnx`
- `05-NET48/SecurityValidationLab/SecurityValidationLab.csproj:1`

```powershell
dotnet restore .\05-NET48\Atelier05.slnx
dotnet build .\05-NET48\Atelier05.slnx
```

## Etape 2 - Lancer l'API

Code source a verifier (etape):
- `05-NET48/SecurityValidationLab/Program.cs:28`
- `05-NET48/SecurityValidationLab/Program.cs:73`

```powershell
$BaseUrl = 'http://localhost:5105'
dotnet run --project .\05-NET48\SecurityValidationLab\SecurityValidationLab.csproj --urls=$BaseUrl
```

## Etape 3 - Verifier XSS (vuln vs secure)

Code source a verifier (etape):
- `05-NET48/SecurityValidationLab/Program.cs:92`
- `05-NET48/SecurityValidationLab/Program.cs:100`

```powershell
$BaseUrl = 'http://localhost:5105'
$xss = '<script>alert(1)</script>'

Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($xss))" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($xss))" | Select-Object -ExpandProperty Content
```

## Etape 4 - Verifier open redirect (vuln vs secure)

Code source a verifier (etape):
- `05-NET48/SecurityValidationLab/Program.cs:109`
- `05-NET48/SecurityValidationLab/Program.cs:118`
- `05-NET48/SecurityValidationLab/Program.cs:163`

```powershell
$BaseUrl = 'http://localhost:5105'
Invoke-WebRequest -Uri "$BaseUrl/vuln/open-redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/open-redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -ErrorAction Stop
} catch {
    $_.Exception.Response.StatusCode.value__
}

Invoke-RestMethod -Uri "$BaseUrl/secure/open-redirect?returnUrl=$([uri]::EscapeDataString('/ok'))"
```

## Etape 5 - Executer les tests atelier

Code source a verifier (etape):
- `05-NET48/SecurityValidationLab.Tests/SmokeTests.cs:5`

```powershell
dotnet test .\05-NET48\Atelier05.slnx
```

## Etape 6 - Controles scripts (SAST / DAST)

Code source a verifier (etape):
- `05-NET48/scripts/run-sast.ps1:1`
- `05-NET48/scripts/run-dast.ps1:1`
- `05-NET48/scripts/calls.ps1:1`

```powershell
.\05-NET48\scripts\run-sast.ps1
.\05-NET48\scripts\run-dast.ps1 -TargetUrl 'http://host.docker.internal:5105'
.\05-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5105'
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5105/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\05-NET48\Atelier05.slnx
```
