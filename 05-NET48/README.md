# Atelier 05 - Validation continue (tests, SAST, DAST) (.NET Framework 4.8)

## Objectif

Atelier NET48 pour automatiser des controles securite sur une API:

- checks manuels `vuln` vs `secure`
- tests automatisees (smoke tests NET48)
- verification SAST/depedencies
- verification DAST scriptable

Implementation reelle: `05-NET48/SecurityValidationLab/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Etape 1 - Restaurer et builder

```powershell
if (Test-Path .\05-NET48) { Set-Location .\05-NET48 }
dotnet restore .\Atelier05.slnx
dotnet build .\Atelier05.slnx
```

## Etape 2 - Lancer l'API

```powershell
$BaseUrl = 'http://localhost:5105'
dotnet run --project .\SecurityValidationLab\SecurityValidationLab.csproj --urls=$BaseUrl
```

Resultat attendu: API active sur `http://localhost:5105`.

## Etape 3 - Checks manuels rapides (XSS + Redirect)

Dans un second terminal:

```powershell
$BaseUrl = 'http://localhost:5105'
$xss = '<script>alert(1)</script>'

Invoke-WebRequest -Uri "$BaseUrl/vuln/xss?input=$([uri]::EscapeDataString($xss))" | Select-Object -ExpandProperty Content
Invoke-WebRequest -Uri "$BaseUrl/secure/xss?input=$([uri]::EscapeDataString($xss))" | Select-Object -ExpandProperty Content

Invoke-WebRequest -Uri "$BaseUrl/vuln/open-redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Select-Object StatusCode

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/open-redirect?returnUrl=$([uri]::EscapeDataString('https://example.com'))" -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

Invoke-RestMethod -Uri "$BaseUrl/secure/open-redirect?returnUrl=$([uri]::EscapeDataString('/ok'))"
```

## Etape 4 - Lancer les tests automatisees

```powershell
if (Test-Path .\05-NET48) { Set-Location .\05-NET48 }
dotnet test .\SecurityValidationLab.Tests\SecurityValidationLab.Tests.csproj
```

Resultat attendu: `Passed`.

Note: sur la piste NET48, le projet de tests fournit des smoke tests d'execution (`05-NET48/SecurityValidationLab.Tests/SmokeTests.cs`).

## Etape 5 - Controle SAST local

```powershell
if (Test-Path .\05-NET48) { Set-Location .\05-NET48 }
dotnet build .\SecurityValidationLab\SecurityValidationLab.csproj -warnaserror
dotnet list .\SecurityValidationLab\SecurityValidationLab.csproj package --vulnerable --include-transitive
dotnet list .\SecurityValidationLab.Tests\SecurityValidationLab.Tests.csproj package --vulnerable --include-transitive
```

## Etape 6 - Controle DAST local

```powershell
if (Test-Path .\05-NET48) { Set-Location .\05-NET48 }
.\scripts\run-dast.ps1 -TargetUrl "http://host.docker.internal:5105"
```

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5105/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\Atelier05.slnx
```
