# Atelier 03 - Session, Deserialisation, IDOR (.NET Framework 4.8)

## Objectif

Atelier NET48 de comparaison `vuln` vs `secure` pour:

- Session theft
- Insecure deserialization
- IDOR

Implementation reelle: `03-NET48/AppSecWorkshop03/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Build et lancement

```powershell
dotnet restore .\03-NET48\AppSecWorkshop03\AppSecWorkshop03.csproj
dotnet build .\03-NET48\AppSecWorkshop03\AppSecWorkshop03.csproj

$BaseUrl = 'http://localhost:5103'
dotnet run --project .\03-NET48\AppSecWorkshop03\AppSecWorkshop03.csproj --urls=$BaseUrl
```

## Verification fonctionnelle

Dans un second terminal:

### 1) Session vuln vs secure

```powershell
$BaseUrl = 'http://localhost:5103'
$loginBody = @{ username = 'alice' } | ConvertTo-Json

$vulnLogin = Invoke-RestMethod -Uri "$BaseUrl/vuln/session/login" -Method Post -ContentType 'application/json' -Body $loginBody
$vulnToken = $vulnLogin.token
Invoke-RestMethod -Uri "$BaseUrl/vuln/session/profile?token=$vulnToken" -Method Get

$headersLogin = @{ 'User-Agent' = 'WorkshopAgent/1.0' }
$secureLogin = Invoke-RestMethod -Uri "$BaseUrl/secure/session/login" -Method Post -Headers $headersLogin -ContentType 'application/json' -Body $loginBody
$secureToken = $secureLogin.token

$headersProfile = @{ 'X-Session-Token' = $secureToken; 'User-Agent' = 'WorkshopAgent/1.0' }
Invoke-RestMethod -Uri "$BaseUrl/secure/session/profile" -Method Get -Headers $headersProfile
```

Attendu:

- mode `vuln`: token previsible/reutilisable
- mode `secure`: token valide seulement avec `X-Session-Token` + meme `User-Agent`

### 2) Deserialisation vuln vs secure

```powershell
$BaseUrl = 'http://localhost:5103'

$danger = @"
{
  "$type": "AppSecWorkshop03.Serialization.DangerousAction, AppSecWorkshop03",
  "FileName": "owned-by-deserialization.txt",
  "Content": "Payload deserialize"
}
"@
Invoke-RestMethod -Uri "$BaseUrl/vuln/deserialization/execute" -Method Post -ContentType 'application/json' -Body $danger

$safeBody = @{ action = 'echo'; message = 'hello' } | ConvertTo-Json
Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body $safeBody

$badBody = @{ action = 'delete-all'; message = 'x' } | ConvertTo-Json
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/deserialization/execute" -Method Post -ContentType 'application/json' -Body $badBody -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Attendu:

- `vuln`: payload type peut declencher une action dangereuse
- `secure`: seule l'action `echo` est acceptee

### 3) IDOR vuln vs secure

```powershell
$BaseUrl = 'http://localhost:5103'

Invoke-RestMethod -Uri "$BaseUrl/vuln/idor/orders/1002?username=alice" -Method Get

try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=alice" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

Invoke-RestMethod -Uri "$BaseUrl/secure/idor/orders/1002?username=bob" -Method Get
```

Attendu:

- `vuln`: acces direct possible
- `secure`: `403` pour utilisateur non proprietaire, acces admin autorise

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5103/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\03-NET48\AppSecWorkshop03\AppSecWorkshop03.csproj
```
