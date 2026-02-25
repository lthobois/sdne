# Atelier A-NET48 - WebForms .NET Framework 4.8

## Objectif
Construire un socle WebForms .NET Framework 4.8 minimal, puis documenter une progression de formation securite applicative et securite IIS reproductible.

## Pre-requis
- Windows avec IIS disponible.
- .NET Framework 4.8.
- Visual Studio avec charge de travail ASP.NET.
- SQL Server 2019 (instance locale ou distante de labo).
- Un poste joint domaine pour les scenarios Windows Authentication.

## Les etapes de l'atelier

### Etape 3 - Ajouter le CRUD Employee avec Entity Framework + LINQ
Ce qui a ete change:
- Ajout d'un acces donnees Entity Framework 6 (`AdventureWorksContext`) mappe sur `HumanResources.Employee`.
- Ajout du modele `Employee` (colonnes utiles au CRUD).
- Ajout d'une page Liste (`EmployeeList.aspx`) pour lecture + suppression.
- Ajout d'une page Formulaire (`EmployeeForm.aspx`) pour creation + modification.
- Ajout de la connexion SQL Server de test vers `www.avaedos.com` / `AdventureWorks2014`.
- Ajout de `packages.config` pour restaurer `EntityFramework 6.4.4`.
Pourquoi:
- Fournir un atelier concret WebForms .NET 4.8 avec CRUD en LINQ to Entities, directement sur la table cible `HumanResources.Employee`.
Comment reproduire:
```powershell
# Depuis la racine du depot
nuget restore ./A-NET48/WebFormsNet48Basics.sln

# Ouvrir la solution dans Visual Studio
# puis Build + Run, et naviguer vers la liste CRUD
# URL attendue (IIS Express): /EmployeeList.aspx
```
Resultat attendu:
- Liste: affichage des employees (lecture via LINQ).
- Formulaire: creation / edition d'un employee.
- Liste: suppression d'un employee.
- En cas de violation de contrainte SQL (FK vers `Person.Person` ou checks), un message d'erreur est affiche dans le formulaire.
References code/tests:
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:1`
- `A-NET48/WebFormsNet48Basics/packages.config:1`
- `A-NET48/WebFormsNet48Basics/Web.config:1`
- `A-NET48/WebFormsNet48Basics/Data/AdventureWorksContext.cs:1`
- `A-NET48/WebFormsNet48Basics/Models/Employee.cs:1`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx:1`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.cs:1`
- `A-NET48/WebFormsNet48Basics/Default.aspx:1`

### Etape 4 - Corriger l'erreur UnobtrusiveValidationMode (mapping jquery)
Ce qui a ete change:
- Ajout d'un `ScriptResourceMapping` nomme `jquery` dans `Application_Start`.
- Ajout des fichiers `Scripts/jquery-3.7.1.js` et `Scripts/jquery-3.7.1.min.js` dans le projet WebForms.
- Inclusion des scripts dans le projet Visual Studio (`.csproj`).
Pourquoi:
- Les validateurs WebForms (`RequiredFieldValidator`, `ValidationSummary`) utilisent le mode unobtrusive et exigent un mapping `jquery`.
- Sans ce mapping, l'application plante au rendu de la page formulaire avec l'exception `InvalidOperationException`.
Comment reproduire:
```powershell
# Depuis la racine du depot
Test-Path ./A-NET48/WebFormsNet48Basics/Scripts/jquery-3.7.1.min.js
Test-Path ./A-NET48/WebFormsNet48Basics/Scripts/jquery-3.7.1.js

# Build/Run ensuite dans Visual Studio
# puis ouvrir /EmployeeForm.aspx
```
Resultat attendu:
- La page `EmployeeForm.aspx` se charge sans l'erreur "UnobtrusiveValidationMode requiert un ScriptResourceMapping pour jquery".
- Les controles de validation WebForms s'affichent correctement.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:1`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:1`
- `A-NET48/WebFormsNet48Basics/Scripts/jquery-3.7.1.min.js:1`
- `A-NET48/WebFormsNet48Basics/Scripts/jquery-3.7.1.js:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx:1`

### Etape 5 - Basculer vers une base volontairement vulnerable (point de depart atelier)
Ce qui a ete change:
- Desactivation de la protection ASP.NET request validation globale (`validateRequest="false"`) et affichage d'erreurs detaillees (`customErrors="Off"`).
- Suppression des validateurs WebForms du formulaire employe (`RequiredFieldValidator`, `ValidationSummary`).
- Conservation d'un parsing permissif cote serveur (valeurs par defaut silencieuses si parsing invalide).
- Affichage direct d'un message issu de query string dans la liste (sans encodage HTML).
Pourquoi:
- Fournir un socle pedagogique minimal avec des faiblesses exploitables pour appliquer les contre-mesures progressivement pendant l'atelier AppSec.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'customErrors|validateRequest'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs -Pattern 'lblMessage.Text = message'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeForm.aspx -Pattern 'RequiredFieldValidator|ValidationSummary'
```
Resultat attendu:
- L'application reste fonctionnelle (CRUD utilisable) mais presente des lacunes de securite intentionnelles pour les exercices de durcissement.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.cs:1`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.designer.cs:1`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs:1`
