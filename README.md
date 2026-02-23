# Formation Securite des applications .NET - Guide participant

Ce depot contient 10 ateliers pratiques a executer localement.
Chaque atelier est autonome et fournit:

- une application de demonstration
- des scenarios `vuln/*` et `secure/*`
- un fichier de requetes HTTP
- des tests automatises

## Pre-requis

- .NET SDK 9.0+
- PowerShell
- (optionnel) Docker Desktop pour les ateliers avec outils proxy/DAST

Verifier l'environnement:

```powershell
dotnet --version
```

## Structure du depot

- `01` a `10`: un dossier par atelier
- chaque dossier contient un `README.md` local
- certains ateliers incluent `scripts/`, `pipeline/` et `infra/`

## Parcours recommande

Suivre les ateliers dans l'ordre:

1. `01` - Authentification HTTP Basic et controles d'acces
2. `02` - SQLi, XSS, CSRF, SSRF
3. `03` - Vol de session, deserialisation, IDOR
4. `04` - Secure code: validation, traversal, redirect, erreurs, headers
5. `05` - Tests de securite automatises (regression, SAST/DAST)
6. `06` - Securite supply chain: dependances, secrets, SBOM
7. `07` - Limitation de surface d'exposition (filtrage, rate limit, upload)
8. `08` - Monitoring securite: correlation, audit, alerting
9. `09` - AuthN/AuthZ avancees: token signe, scopes, BOLA/IDOR
10. `10` - Validation perimetrique: header injection, proxy, DMZ

## Methode de travail (par atelier)

Depuis la racine du depot:

```powershell
cd .\0X
dotnet build .\Atelier0X.slnx
dotnet test .\Atelier0X.slnx
```

Puis lancer l'API:

```powershell
dotnet run --project .\NomDuProjet\NomDuProjet.csproj
```

Executer ensuite les requetes du fichier `*.http` de l'atelier pour comparer les comportements `vuln` et `secure`.

## Resultat attendu

A l'issue du parcours, vous disposez d'une base reutilisable pour:

- identifier et reproduire les failles applicatives courantes
- implementer les contre-mesures dans ASP.NET Core
- valider les protections via tests automatises
- integrer des controles de securite dans la CI
- appliquer des regles de durcissement perimetrique et de monitoring

## Commandes utiles

Executer un atelier complet:

```powershell
cd .\07
.\scripts\run-defense-checks.ps1
```

Executer tous les tests d'un atelier:

```powershell
cd .\10
dotnet test .\Atelier10.slnx
```
