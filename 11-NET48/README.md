# Atelier 11 - Chiffrement avec C# (.NET Framework 4.8)

## Objectif

Mettre en pratique les bases de cryptographie applicative en .NET Framework 4.8:
- hashage (SHA-256, PBKDF2)
- chiffrement symetrique (AES)
- chiffrement asymetrique (RSA)
- APIs Windows (DPAPI, File.Encrypt)
- generation de cles et certificat auto-signe

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- Windows avec .NET Framework 4.8 Developer Pack
- .NET SDK installe
- PowerShell 5.1+

## Les etapes de l'atelier

### Etape 1 - Restaurer et builder l'atelier

Code source a verifier (etape):
- `11-NET48/Atelier11.slnx:1`
- `11-NET48/CryptoFoundationLab/CryptoFoundationLab.csproj:1`

```powershell
dotnet restore .\11-NET48\Atelier11.slnx
dotnet build .\11-NET48\Atelier11.slnx
```

### Etape 2 - Demarrer l'API crypto NET48

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:12`
- `11-NET48/CryptoFoundationLab/Program.cs:32`
- `11-NET48/CryptoFoundationLab/Program.cs:75`

```powershell
$BaseUrl = 'http://localhost:5111'
dotnet run --project .\11-NET48\CryptoFoundationLab\CryptoFoundationLab.csproj --urls=$BaseUrl
```

### Etape 3 - Hashage: SHA-256 et mot de passe

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:108`
- `11-NET48/CryptoFoundationLab/Program.cs:124`
- `11-NET48/CryptoFoundationLab/Program.cs:135`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/hash/sha256" -Method Post -ContentType 'application/json' -Body (@{ input = 'atelier-11' } | ConvertTo-Json)
Invoke-RestMethod -Uri "$BaseUrl/secure/hash/password" -Method Post -ContentType 'application/json' -Body (@{ password = 'Passw0rd!Demo' } | ConvertTo-Json)
```

### Etape 4 - Chiffrement symetrique AES

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:150`
- `11-NET48/CryptoFoundationLab/Program.cs:155`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/aes/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'secret-aes' } | ConvertTo-Json)
```

### Etape 5 - Chiffrement asymetrique RSA

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:187`
- `11-NET48/CryptoFoundationLab/Program.cs:202`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/rsa/keypair" -Method Post
Invoke-RestMethod -Uri "$BaseUrl/secure/rsa/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'secret-rsa' } | ConvertTo-Json)
```

### Etape 6 - APIs Windows: DPAPI et File.Encrypt

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:222`
- `11-NET48/CryptoFoundationLab/Program.cs:227`
- `11-NET48/CryptoFoundationLab/Program.cs:239`
- `11-NET48/CryptoFoundationLab/Program.cs:246`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/windows/dpapi/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'dpapi-demo' } | ConvertTo-Json)
Invoke-RestMethod -Uri "$BaseUrl/secure/windows/file-encrypt/demo" -Method Post
```

### Etape 7 - Certificat auto-signe et tests

Code source a verifier (etape):
- `11-NET48/CryptoFoundationLab/Program.cs:271`
- `11-NET48/CryptoFoundationLab/Program.cs:282`
- `11-NET48/CryptoFoundationLab.Tests/SmokeTests.cs:5`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/cert/self-signed" -Method Post -ContentType 'application/json' -Body (@{ subject = 'CN=CryptoWorkshop11' } | ConvertTo-Json)

dotnet test .\11-NET48\Atelier11.slnx
```

## Scripts stagiaires (support)

- `11-NET48/scripts/calls.ps1:1`

```powershell
.\11-NET48\scripts\calls.ps1 -BaseUrl 'http://localhost:5111'
```

## Fichiers utiles

- `11-NET48/CryptoFoundationLab/Program.cs`
- `11-NET48/CryptoFoundationLab.Tests/SmokeTests.cs`
- `11-NET48/Atelier11.slnx`
- `11-NET48/scripts/calls.ps1`
- Documentation .NET: `https://learn.microsoft.com/fr-fr/dotnet/api/system.security.cryptography`

## URL ACL Windows (si besoin)

Si `HttpListener` retourne `Access denied`, executer une fois en PowerShell administrateur:

```powershell
netsh http add urlacl url=http://localhost:5111/ user=$env:USERNAME
```

## Nettoyage

```powershell
dotnet clean .\11-NET48\Atelier11.slnx
```
