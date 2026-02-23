# Formation Securite .NET - Guide participant

Ce depot contient 11 ateliers pratiques. Chaque atelier est autonome, versionne dans son dossier (`00` a `10`) et fournit un mode operatoire pas a pas.

## Pre-requis generaux

- Windows 10/11 ou Windows Server recent
- PowerShell 5.1+
- .NET SDK 10.x
- Visual Studio 2022 ou VS Code (C# Dev Kit)
- Acces Internet pour restauration NuGet
- Port HTTP local libre (chaque atelier propose un port dedie)

Verification rapide:

```powershell
`$PSVersionTable.PSVersion
dotnet --version
```

Resultat attendu:

- `pwsh` retourne une version 7.x ou plus
- `dotnet` retourne `9.x`

## Ateliers et correspondance programme

| Atelier | Dossier | Theme principal | Modules couverts |
|---|---|---|---|
| 00 | `00` | Rappels securite applicative | Stack/Heap, SAST/DAST, hijacking ressources, protections runtime |
| 01 | `01` | Authentification HTTP Basic | AuthN, AuthZ role-based |
| 02 | `02` | Vulns Web OWASP | SQLi, XSS, CSRF, SSRF |
| 03 | `03` | Attaques avancees | Session theft, deserialisation, IDOR |
| 04 | `04` | Secure coding | Validation d'entree, path traversal, open redirect, gestion d'erreurs |
| 05 | `05` | Validation securite continue | Tests de regression, SAST, DAST |
| 06 | `06` | Securite code externe | Secrets, appels sortants, provenance dependances, SCA/SBOM |
| 07 | `07` | Limitation exposition | Filtrage WAF-like, admin hardening, upload validation, rate limiting |
| 08 | `08` | Monitoring securite | Audit trail, correlation id, alerting, logs securises |
| 09 | `09` | Durcissement AuthN/AuthZ | Integrite de token, scopes, autorisation objet |
| 10 | `10` | Validation perimetrique | Headers forwarded, confiance proxy, surface perimetre |

## Convention de travail (commune a tous les ateliers)

1. Ouvrir un terminal PowerShell a la racine du depot.
2. Se placer dans le dossier atelier.
3. Restaurer et lancer le projet API avec `dotnet run --urls=...`.
4. Executer les appels `Invoke-RestMethod` ou `Invoke-WebRequest` des etapes.
5. Comparer endpoint `vuln` vs `secure`.
6. Arreter l'API avec `Ctrl+C`.

## Verifications globales

- Tous les ateliers compilent:

```powershell
dotnet build .\FormationSecuriteDotNet.sln
```

- Tous les tests (si presents) passent:

```powershell
dotnet test .\FormationSecuriteDotNet.sln
```

## Depannage global

- Erreur de restauration NuGet: verifier proxy/reseau puis relancer `dotnet restore`.
- Port deja utilise: changer le port dans la commande `dotnet run --urls=...`.
- Certificats dev HTTPS non configures: utiliser les URLs HTTP proposees dans les READMEs.

## Nettoyage global

```powershell
dotnet clean .\FormationSecuriteDotNet.sln
Get-ChildItem -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force
```




