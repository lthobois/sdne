# Annexes Techniques - Exemples complementaires

Ce document regroupe des exemples complementaires relies aux ateliers.
Il peut etre utilise comme reference rapide pendant et apres la formation.

## Positionnement

- Les ateliers du depot ciblent principalement `ASP.NET Core / .NET 9`.
- Certains exemples ci-dessous utilisent des API historiques de `.NET Framework`.
- Pour les projets modernes, privilegier les alternatives actuelles quand elles existent.

## 1) Rappels securite applicative

### Injection DLL en .NET (CreateRemoteThread) - cadre defensif

La technique d'injection de DLL via API Windows (`OpenProcess`, `VirtualAllocEx`, `WriteProcessMemory`, `CreateRemoteThread`) est une technique offensive connue.

Utilisation dans ce parcours:

- comprendre le mecanisme pour mieux le detecter
- savoir quels signaux surveiller cote EDR/SIEM
- durcir les droits process et la surface d'execution

Points d'analyse a verifier:

- creation de thread distant inter-processus
- ecriture memoire dans un processus externe
- chargement non attendu de bibliotheques
- enchainement suspect `OpenProcess -> WriteProcessMemory -> CreateRemoteThread`

Controles recommandes:

- execution uniquement sur environnement de lab isole
- principe du moindre privilege
- protection EDR active et journalisation Sysmon/Windows Event
- allowlist applicative (AppLocker / WDAC selon contexte)

Reference atelier:

- `03` (attaques avancees)
- `08` (monitoring et alerting)

## 2) Securite du framework .NET - exemples

### System.Security.Principal

Verifier l'identite Windows courante et l'appartenance au role administrateur.

```csharp
using System;
using System.Security.Principal;

WindowsIdentity identity = WindowsIdentity.GetCurrent();
WindowsPrincipal principal = new WindowsPrincipal(identity);
bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

Console.WriteLine(identity.Name);
Console.WriteLine(identity.IsAuthenticated);
Console.WriteLine(isAdmin ? "Admin" : "Not admin");
```

### System.Security.Permissions (legacy .NET Framework)

Exemple de `FileIOPermission.Demand()` utile pour comprendre l'historique CAS.
Sur pile moderne .NET, ce modele n'est plus le mecanisme principal de securisation applicative.

### System.Security.Policy (legacy .NET Framework)

Concept historique de politique AppDomain.
Conserver comme culture technique pour maintenance legacy, pas comme mecanisme central en .NET moderne.

### System.Security.AccessControl

Definir les ACL d'un fichier via `FileSecurity` / `FileSystemAccessRule`.
Verifier le resultat avec un compte de test non admin.

### System.IdentityModel (JWT)

Pour projets modernes, utiliser la validation JWT standard dans ASP.NET Core.
Reference pratique dans ce depot:

- `09` (`AuthzHardeningLab`) pour token signe, scopes et controle d'acces objet.

## 3) Deploiement conteneur

Reference pratique:

- `10/infra/docker-compose.yml`
- `10/infra/nginx.conf`

Workflow minimal:

```powershell
cd .\10\infra
docker compose up --build
```

Ce scenario permet de tester:

- forwarding headers
- reverse proxy
- positionnement perimetrique type DMZ

## 4) Logging securise

La regle principale: ne jamais journaliser les secrets en clair (password, token, apikey, etc.).

Reference pratique:

- `08` (audit + correlation + alerting)

Exemple de sanitization:

```csharp
private static readonly string[] SensitiveKeys = { "password", "pwd", "token", "secret", "apikey" };
```

Puis remplacement avant ecriture du log.

## 5) Data protection (AES + DPAPI)

Scenario recommande:

1. generer cle AES et IV
2. proteger cle/IV avec `ProtectedData` (`CurrentUser`)
3. chiffrer les donnees metier avec AES
4. deproteger puis dechiffrer pour verification

Artifacts typiques:

- `key.bin` (cle protegee DPAPI)
- `iv.bin` (IV protege DPAPI)
- `data.enc` (payload chiffre)

Reference atelier:

- `04` (secure code)
- `06` (gestion des secrets)

## 6) Chiffrement fichier avec File.Encrypt

`File.Encrypt` / `File.Decrypt` (EFS) depend:

- du systeme de fichiers (NTFS)
- du contexte utilisateur
- des droits effectifs

Verifier:

- comportement en cas d'acces refuse
- restauration de lisibilite apres `Decrypt`

## 7) Authentification Basic - client HTTP

Reference pratique:

- `01` (`BasicAuthWorkshop`)

Rappel:

- `Authorization: Basic base64(username:password)`
- Base64 n'est pas du chiffrement
- HTTPS obligatoire en production

Exemple PowerShell:

```powershell
$raw = "alice:P@ssw0rd!"
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($raw))
Invoke-RestMethod -Uri "http://localhost:5098/secure/profile" -Headers @{ Authorization = "Basic $b64" }
```

## Mapping rapide vers les ateliers

- Auth Basic: `01`
- Failles web OWASP (SQLi/XSS/CSRF/SSRF): `02`
- Session/deserialization/IDOR: `03`
- Secure code et hardening: `04`
- Tests securite automatisee: `05`
- Supply chain et secrets: `06`
- Reduction surface exposition: `07`
- Monitoring securite: `08`
- AuthN/AuthZ avancees: `09`
- Perimetre, proxy, DMZ, header injection: `10`
