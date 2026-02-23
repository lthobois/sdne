# Formation Securite .NET - Guide participant

Ce depot contient 11 ateliers pratiques declinés en 2 variantes:
- `.NET 10` dans les dossiers `00-NET10` a `10-NET10`
- `.NET Framework 4.8` dans les dossiers `00-NET48` a `10-NET48`

Chaque atelier reste autonome et fournit un mode operatoire pas a pas via son `README.md`.

## Pre-requis generaux

- Windows 10/11 ou Windows Server recent
- PowerShell 5.1+
- .NET SDK 10.x (pour les ateliers `*-NET10`)
- .NET Framework 4.8 Developer Pack (pour les ateliers `*-NET48`)
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
- `dotnet` retourne une version 10.x

## Ateliers et correspondance programme

| Atelier | Dossier NET10 | Dossier NET48 | Theme principal | Modules couverts |
|---|---|---|---|
| 00 | `00-NET10` | `00-NET48` | Rappels securite applicative | Stack/Heap, SAST/DAST, hijacking ressources, protections runtime |
| 01 | `01-NET10` | `01-NET48` | Authentification HTTP Basic | AuthN, AuthZ role-based |
| 02 | `02-NET10` | `02-NET48` | Vulns Web OWASP | SQLi, XSS, CSRF, SSRF |
| 03 | `03-NET10` | `03-NET48` | Attaques avancees | Session theft, deserialisation, IDOR |
| 04 | `04-NET10` | `04-NET48` | Secure coding | Validation d'entree, path traversal, open redirect, gestion d'erreurs |
| 05 | `05-NET10` | `05-NET48` | Validation securite continue | Tests de regression, SAST, DAST |
| 06 | `06-NET10` | `06-NET48` | Securite code externe | Secrets, appels sortants, provenance dependances, SCA/SBOM |
| 07 | `07-NET10` | `07-NET48` | Limitation exposition | Filtrage WAF-like, admin hardening, upload validation, rate limiting |
| 08 | `08-NET10` | `08-NET48` | Monitoring securite | Audit trail, correlation id, alerting, logs securises |
| 09 | `09-NET10` | `09-NET48` | Durcissement AuthN/AuthZ | Integrite de token, scopes, autorisation objet |
| 10 | `10-NET10` | `10-NET48` | Validation perimetrique | Headers forwarded, confiance proxy, surface perimetre |

## Convention de travail (commune a tous les ateliers)

1. Ouvrir un terminal PowerShell a la racine du depot.
2. Se placer dans le dossier atelier.
3. Restaurer et lancer le projet API avec `dotnet run --urls=...`.
4. Executer les appels `Invoke-RestMethod` ou `Invoke-WebRequest` des etapes.
5. Comparer endpoint `vuln` vs `secure`.
6. Arreter l'API avec `Ctrl+C`.

## Verifications globales

- Tous les ateliers `.NET 10` compilent:

```powershell
dotnet build .\FormationSecuriteDotNet10.sln
```

- Tous les tests `.NET 10` (si presents) passent:

```powershell
dotnet test .\FormationSecuriteDotNet10.sln
```

- Variante `.NET Framework 4.8`:

```powershell
dotnet build .\FormationSecuriteDotNet48.sln
dotnet test .\FormationSecuriteDotNet48.sln
```

Note: les ateliers `*-NET48` sont provisionnes au format cible `net48`, mais le code applicatif actuel base sur les Minimal APIs modernes necessite un portage complementaire pour etre 100% compilable/executable en .NET Framework 4.8.

## Depannage global

- Erreur de restauration NuGet: verifier proxy/reseau puis relancer `dotnet restore`.
- Port deja utilise: changer le port dans la commande `dotnet run --urls=...`.
- Certificats dev HTTPS non configures: utiliser les URLs HTTP proposees dans les READMEs.

## Nettoyage global

```powershell
dotnet clean .\FormationSecuriteDotNet10.sln
dotnet clean .\FormationSecuriteDotNet48.sln
Get-ChildItem -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force
Get-ChildItem -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force
```




