# Atelier 01 - HTTP Basic Auth (.NET Framework 4.8)

## Objectif

Valider un flux d'authentification HTTP Basic sur une API pedagogique NET48:

- `/public`: acces anonyme
- `/secure/profile`: authentification requise
- `/secure/admin`: role `Admin` requis

Implementation reelle: `01-NET48/BasicAuthWorkshop/Program.cs`.

## Pre-requis

- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe (`dotnet --version`)
- PowerShell 5.1+
- Positionne a la racine du depot `sdne`

## Build

```powershell
dotnet restore .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj
dotnet build .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj
```

Resultat attendu: build `net48` sans erreur.

## Execution

```powershell
$BaseUrl = 'http://localhost:5101'
dotnet run --project .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj --urls=$BaseUrl
```

Le process reste actif et expose les routes HTTP.

## Verification fonctionnelle

Dans un second terminal:

### 1) Endpoint public

```powershell
$BaseUrl = 'http://localhost:5101'
Invoke-RestMethod -Uri "$BaseUrl/public" -Method Get
```

Attendu: HTTP 200, JSON avec `resource = "public"`.

### 2) Endpoint protege sans credentials

```powershell
$BaseUrl = 'http://localhost:5101'
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}
```

Attendu: `401`.

### 3) Endpoint protege avec utilisateur standard

```powershell
$BaseUrl = 'http://localhost:5101'
$token = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('analyst:Passw0rd!'))
$headers = @{ Authorization = "Basic $token" }
Invoke-RestMethod -Uri "$BaseUrl/secure/profile" -Headers $headers -Method Get
```

Attendu: HTTP 200, JSON avec `user = "analyst"` et role `User`.

### 4) Controle d'acces admin

```powershell
$BaseUrl = 'http://localhost:5101'

$tokenUser = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('analyst:Passw0rd!'))
$headersUser = @{ Authorization = "Basic $tokenUser" }
try {
    Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersUser -Method Get -ErrorAction Stop
} catch {
    [int]$_.Exception.Response.StatusCode
}

$tokenAdmin = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('admin:Adm1nPass!'))
$headersAdmin = @{ Authorization = "Basic $tokenAdmin" }
Invoke-RestMethod -Uri "$BaseUrl/secure/admin" -Headers $headersAdmin -Method Get
```

Attendu:

- compte `analyst`: `403`
- compte `admin`: HTTP 200

## Comptes de demo

- `analyst / Passw0rd!` -> role `User`
- `admin / Adm1nPass!` -> roles `User`, `Admin`

## Depannage

### Erreur `Access denied` au demarrage (HttpListener)

Executer une seule fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5101/ user=$env:USERNAME
```

### Erreur `prefix is in conflict`

Le prefixe est deja reserve/utilise.

Option 1: utiliser un autre port

```powershell
$BaseUrl = 'http://localhost:5199'
dotnet run --project .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj --urls=$BaseUrl
```

Option 2: nettoyer une reservation URL ACL obsolete (admin)

```powershell
netsh http show urlacl
netsh http delete urlacl url=http://localhost:5101/
```

## Arret et nettoyage

- arreter l'API: `Ctrl+C`
- nettoyage:

```powershell
dotnet clean .\01-NET48\BasicAuthWorkshop\BasicAuthWorkshop.csproj
```
