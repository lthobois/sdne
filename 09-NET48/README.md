# Atelier 09 - Durcissement AuthN/AuthZ (.NET Framework 4.8)

## Mode compatibilite NET48

Cette variante fournit une piste executable en .NET Framework 4.8 via `HttpListener`.
Le comportement pedagogique couvre:
- token vulnerable non signe
- token secure signe (HMAC)
- verification des scopes
- controle d'acces objet (ownership)

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK installe (`dotnet`)
- .NET Framework 4.8 Developer Pack
- PowerShell 5.1+

## Lancement

```powershell
if (Test-Path .\09-NET48) { Set-Location .\09-NET48 }
dotnet restore .\Atelier09.slnx
$BaseUrl = 'http://localhost:5109'
dotnet run --project .\AuthzHardeningLab\AuthzHardeningLab.csproj --urls=$BaseUrl
```

Si Windows retourne `Access denied` sur `HttpListener`, executer une fois en administrateur:

```powershell
netsh http add urlacl url=http://localhost:5109/ user=%USERNAME%
```

## Endpoints

- `GET /`
- `POST /vuln/auth/token`
- `POST /secure/auth/token`
- `GET /vuln/docs/{id}`
- `GET /secure/docs/{id}`
- `POST /secure/docs/{id}/publish`

Code principal:
- `09-NET48/AuthzHardeningLab/Program.cs`

## Parcours rapide

```powershell
$BaseUrl = 'http://localhost:5109'

# 1) Token vulnerable
$vulnReq = @{ username = 'alice'; scope = 'docs.read' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/vuln/auth/token" -Method Post -ContentType 'application/json' -Body $vulnReq

# 2) Token secure
$secureReq = @{ username = 'alice'; scope = 'docs.read docs.publish' } | ConvertTo-Json
$secureToken = Invoke-RestMethod -Uri "$BaseUrl/secure/auth/token" -Method Post -ContentType 'application/json' -Body $secureReq
$headers = @{ Authorization = "Bearer $($secureToken.token)" }

# 3) Lecture autorisee (owner)
Invoke-RestMethod -Uri "$BaseUrl/secure/docs/1" -Method Get -Headers $headers

# 4) Lecture refusee (hors ownership)
try {
  Invoke-RestMethod -Uri "$BaseUrl/secure/docs/2" -Method Get -Headers $headers -ErrorAction Stop
} catch {
  $_.Exception.Response.StatusCode.value__
}
```

## Tests

Les tests NET48 de cette piste sont des smoke tests (validation d'execution):

```powershell
if (Test-Path .\09-NET48) { Set-Location .\09-NET48 }
dotnet test .\AuthzHardeningLab.Tests\AuthzHardeningLab.Tests.csproj
```

## Verifications attendues

- token absent/invalide -> `401`
- scope manquant ou mauvais ownership -> `403`
- endpoint `vuln` permissif (demonstration IDOR)

## Nettoyage

```powershell
if (Test-Path .\09-NET48) { Set-Location .\09-NET48 }
dotnet clean .\Atelier09.slnx
```