# Atelier 11 - Chiffrement avec C# (.NET 10)

## Objectif

Mettre en pratique les bases de cryptographie applicative en .NET:
- hashage (SHA-256, PBKDF2)
- chiffrement symetrique (AES)
- chiffrement asymetrique (RSA)
- APIs Windows (DPAPI, File.Encrypt)
- generation de cles et certificat auto-signe

## Pre-requis

- Etre positionne a la racine du depot `sdne`
- .NET SDK 10.x installe
- PowerShell 5.1+
- Windows recommande pour DPAPI/EFS

## Les etapes de l'atelier

### Etape 1 - Restaurer et builder l'atelier

Code source a verifier (etape):
- `11-NET10/Atelier11.slnx:1`
- `11-NET10/CryptoFoundationLab/CryptoFoundationLab.csproj:1`

```powershell
dotnet restore .\11-NET10\Atelier11.slnx
dotnet build .\11-NET10\Atelier11.slnx
```

### Etape 2 - Demarrer l'API crypto

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:7`
- `11-NET10/CryptoFoundationLab/Program.cs:24`

```powershell
$BaseUrl = 'http://localhost:5111'
dotnet run --project .\11-NET10\CryptoFoundationLab\CryptoFoundationLab.csproj --urls=$BaseUrl
```

### Etape 3 - Hashage: SHA-256 et mot de passe

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:32`
- `11-NET10/CryptoFoundationLab/Program.cs:44`
- `11-NET10/CryptoFoundationLab/Program.cs:48`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/hash/sha256" -Method Post -ContentType 'application/json' -Body (@{ input = 'atelier-11' } | ConvertTo-Json)
Invoke-RestMethod -Uri "$BaseUrl/secure/hash/password" -Method Post -ContentType 'application/json' -Body (@{ password = 'Passw0rd!Demo' } | ConvertTo-Json)
```

### Etape 4 - Chiffrement symetrique AES

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:59`
- `11-NET10/CryptoFoundationLab/Program.cs:62`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/aes/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'secret-aes' } | ConvertTo-Json)
```

### Etape 5 - Chiffrement asymetrique RSA

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:90`
- `11-NET10/CryptoFoundationLab/Program.cs:101`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/rsa/keypair" -Method Post
Invoke-RestMethod -Uri "$BaseUrl/secure/rsa/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'secret-rsa' } | ConvertTo-Json)
```

### Etape 6 - APIs Windows: DPAPI et File.Encrypt

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:116`
- `11-NET10/CryptoFoundationLab/Program.cs:123`
- `11-NET10/CryptoFoundationLab/Program.cs:144`
- `11-NET10/CryptoFoundationLab/Program.cs:151`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/windows/dpapi/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'dpapi-demo' } | ConvertTo-Json)
Invoke-RestMethod -Uri "$BaseUrl/secure/windows/file-encrypt/demo" -Method Post
```

### Etape 7 - Certificat auto-signe et tests

Code source a verifier (etape):
- `11-NET10/CryptoFoundationLab/Program.cs:174`
- `11-NET10/CryptoFoundationLab/Program.cs:179`
- `11-NET10/CryptoFoundationLab.Tests/CryptoFoundationTests.cs:16`

```powershell
$BaseUrl = 'http://localhost:5111'
Invoke-RestMethod -Uri "$BaseUrl/secure/cert/self-signed" -Method Post -ContentType 'application/json' -Body (@{ subject = 'CN=CryptoWorkshop11' } | ConvertTo-Json)

dotnet test .\11-NET10\Atelier11.slnx
```

## Scripts stagiaires (support)

- `11-NET10/scripts/calls.ps1:1`

```powershell
.\11-NET10\scripts\calls.ps1 -BaseUrl 'http://localhost:5111'
```

## Fichiers utiles

- `11-NET10/CryptoFoundationLab/Program.cs`
- `11-NET10/CryptoFoundationLab.Tests/CryptoFoundationTests.cs`
- `11-NET10/Atelier11.slnx`
- `11-NET10/scripts/calls.ps1`
- Documentation .NET: `https://learn.microsoft.com/fr-fr/dotnet/api/system.security.cryptography`

## Nettoyage

```powershell
dotnet clean .\11-NET10\Atelier11.slnx
```
