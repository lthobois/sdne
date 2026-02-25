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

### Etape 6 - Ajouter une BasePage anti-CSRF (ViewStateUserKey lie a la session)
Ce qui a ete change:
- Ajout d'une classe `BasePage` qui herite de `System.Web.UI.Page`.
- Ajout de la surcharge `OnInit(EventArgs e)` pour definir `ViewStateUserKey = Session?.SessionID` avant `base.OnInit(e)`.
- Mise a jour des pages WebForms (`Default`, `EmployeeList`, `EmployeeForm`) pour heriter de `BasePage`.
Pourquoi:
- Lier le ViewState a l'identite de session de l'utilisateur reduit le risque de reutilisation cross-user du ViewState et participe a la mitigation CSRF en WebForms.
- Cette affectation doit etre faite en `OnInit`/`Page_Init`, avant le cycle de chargement du ViewState.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/BasePage.cs -Pattern 'protected override void OnInit|ViewStateUserKey = Session\\?\\.SessionID'
Select-String -Path ./A-NET48/WebFormsNet48Basics/Default.aspx.cs -Pattern 'class Default : BasePage'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs -Pattern 'class EmployeeList : BasePage'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.cs -Pattern 'class EmployeeForm : BasePage'
```
Resultat attendu:
- Au demarrage de chaque page qui derive de `BasePage`, `ViewStateUserKey` est initialise avec l'ID de session courant.
- Les pages CRUD continuent de fonctionner avec le meme comportement fonctionnel, avec un durcissement anti-CSRF cote WebForms.
- Dependance d'etat: la protection s'applique uniquement aux pages qui heritent explicitement de `BasePage`.
References code/tests:
- `A-NET48/WebFormsNet48Basics/BasePage.cs:5`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:7`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:10`
- `A-NET48/WebFormsNet48Basics/Default.aspx.cs:5`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs:7`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.cs:10`

### Etape 7 - Ajouter l'en-tete anti-clickjacking X-Frame-Options
Ce qui a ete change:
- Ajout de l'en-tete HTTP `X-Frame-Options: DENY` dans `system.webServer/httpProtocol/customHeaders`.
Pourquoi:
- Empêcher l'affichage de l'application dans une iframe limite les attaques de clickjacking.
- Cet en-tete complete la defense existante `frame-ancestors 'none'` presente dans la CSP report-only.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'X-Frame-Options|customHeaders'
```
Resultat attendu:
- Les reponses IIS retournent l'en-tete `X-Frame-Options: DENY`.
- Un navigateur ne doit pas autoriser l'integration de l'application dans une iframe.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:45`

### Etape 8 - Ajouter l'en-tete X-Content-Type-Options (nosniff)
Ce qui a ete change:
- Ajout de l'en-tete HTTP `X-Content-Type-Options: nosniff` dans `system.webServer/httpProtocol/customHeaders`.
Pourquoi:
- `nosniff` demande au navigateur de respecter le `Content-Type` retourne et d'eviter le MIME sniffing.
- Cela limite certains contournements ou interpretations non prevues de ressources (script/style).
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'X-Content-Type-Options|customHeaders'
```
Resultat attendu:
- Les reponses IIS incluent `X-Content-Type-Options: nosniff`.
- Le navigateur n'essaie pas de deviner un type MIME different de celui fourni par le serveur.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:46`

### Etape 9 - Revoir les directives Cache-Control de maniere systemique (EndRequest)
Ce qui a ete change:
- Ajout d'un hook `Application_EndRequest()` dans `Global.asax.cs`.
- Application systematique des directives anti-cache sur chaque reponse:
  `Cache-Control: no-cache` + `no-store`, `revalidation all caches`, `Expires` dans le passe, et `Pragma: no-cache`.
Pourquoi:
- Eviter la mise en cache des pages/reponses potentiellement sensibles sur le navigateur, des proxys intermediaires ou caches partages.
- Uniformiser la politique cache au niveau application plutot que page par page.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Global.asax.cs -Pattern 'Application_EndRequest|SetCacheability|SetNoStore|SetRevalidation|SetExpires|Pragma'
```
Resultat attendu:
- Les reponses HTTP emises par l'application incluent des directives no-cache/no-store.
- Les navigateurs et caches intermediaires doivent eviter de conserver le contenu applicatif.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:24`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:27`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:28`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:29`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:30`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:31`

### Etape 10 - Corriger la reference projet pour BasePage (compilation)
Ce qui a ete change:
- Ajout de `BasePage.cs` dans les elements `Compile` du fichier projet WebForms.
Pourquoi:
- Le projet `.csproj` liste explicitement les fichiers C# a compiler.
- Sans cette entree, les pages `Default`, `EmployeeList` et `EmployeeForm` qui heritent de `BasePage` echouent avec l'erreur `The type or namespace name 'BasePage' could not be found`.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj -Pattern 'Compile Include=\"BasePage.cs\"'
```
Resultat attendu:
- `BasePage.cs` est compile avec le reste du projet.
- Les classes code-behind qui derivent de `BasePage` sont resolues correctement par le compilateur.
References code/tests:
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:57`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:5`
- `A-NET48/WebFormsNet48Basics/Default.aspx.cs:5`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx.cs:7`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx.cs:10`

### Etape 11 - Ajouter un jeton anti-CSRF explicite (champ cache + validation serveur)
Ce qui a ete change:
- Renforcement de `BasePage` avec un token anti-CSRF de session (`__AntiCsrfToken`).
- Injection d'un champ cache `__RequestVerificationToken` dans le formulaire en `OnPreRender`.
- Validation du token sur chaque POST en `OnLoad` avant traitement des actions (save/delete/cancel).
- Conservation de `ViewStateUserKey = Session?.SessionID` en `OnInit`.
Pourquoi:
- Certains scanners (ex: ZAP) signalent encore "Absence de jetons Anti-CSRF" quand seul `ViewStateUserKey` est present.
- Le token explicite dans le HTML + validation serveur apporte une preuve detectable et une protection robuste par double verification.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/BasePage.cs -Pattern '__RequestVerificationToken|ValidateAntiCsrfToken|RegisterHiddenField|ViewStateUserKey'
```
Resultat attendu:
- Les pages derivees de `BasePage` rendent un champ cache `__RequestVerificationToken`.
- Toute requete POST avec token absent ou invalide est rejetee en HTTP 400.
- Les operations POST legitimes (save/delete) continuent de fonctionner avec un token valide issu de la session.
References code/tests:
- `A-NET48/WebFormsNet48Basics/BasePage.cs:9`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:14`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:18`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:31`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:52`

### Etape 12 - Activer un en-tete CSP effectif (enforcement)
Ce qui a ete change:
- Ajout de l'en-tete HTTP `Content-Security-Policy` dans `system.webServer/httpProtocol/customHeaders`.
- Conservation de `Content-Security-Policy-Report-Only` pour comparer/observer si besoin de mesure.
Pourquoi:
- `Content-Security-Policy-Report-Only` seul n'applique aucun blocage et peut laisser l'alerte scanner "CSP Header Not Set".
- L'en-tete `Content-Security-Policy` en mode enforcement active reellement la politique CSP cote navigateur.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'Content-Security-Policy\"|Content-Security-Policy-Report-Only'
```
Resultat attendu:
- Les reponses HTTP retournent un en-tete `Content-Security-Policy`.
- Le scanner ne doit plus remonter "Content Security Policy (CSP) Header Not Set" sur les pages couvertes.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:47`
- `A-NET48/WebFormsNet48Basics/Web.config:49`

### Etape 13 - Retirer `unsafe-inline` de la CSP (script/style) et aligner les pages
Ce qui a ete change:
- Durcissement des en-tetes `Content-Security-Policy` et `Content-Security-Policy-Report-Only` en supprimant `unsafe-inline` de `script-src` et `style-src`.
- Suppression du JavaScript inline de suppression dans la grille (remplacement `LinkButton` + `OnClientClick` par `Button` standard).
- Suppression des styles inline generes par `ForeColor`/`Width` et externalisation dans un fichier CSS `Content/site.css`.
- Ajout du lien CSS dans les pages formulaire/liste.
Pourquoi:
- ZAP signale `CSP: script-src unsafe-inline` et `CSP: style-src unsafe-inline` tant que la policy contient explicitement `unsafe-inline`.
- Une CSP stricte doit s'accompagner d'un HTML sans JS/CSS inline pour eviter les blocages fonctionnels.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern \"script-src 'self'; style-src 'self'|unsafe-inline\"
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeList.aspx -Pattern 'OnClientClick|LinkButton|asp:Button ID=\"btnDelete\"'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeeForm.aspx -Pattern 'ForeColor|Width=|CssClass=\"text-error|CssClass=\"w-'
```
Resultat attendu:
- Les en-tetes CSP ne contiennent plus `unsafe-inline` pour scripts et styles.
- Les pages CRUD restent fonctionnelles avec soumission/POST serveur sans JavaScript inline.
- Les alertes ZAP liees a `CSP: script-src unsafe-inline` et `CSP: style-src unsafe-inline` ne doivent plus apparaitre apres rescan.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:48`
- `A-NET48/WebFormsNet48Basics/Web.config:50`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx:7`
- `A-NET48/WebFormsNet48Basics/EmployeeList.aspx:34`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx:7`
- `A-NET48/WebFormsNet48Basics/EmployeeForm.aspx:15`
- `A-NET48/WebFormsNet48Basics/Content/site.css:1`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:86`

### Etape 14 - Supprimer la fuite d'erreurs applicatives (Application Error Disclosure)
Ce qui a ete change:
- Passage de `customErrors` en mode `On` avec redirection vers une page d'erreur generique.
- Ajout d'une configuration IIS `httpErrors` pour couvrir les codes `400` et `500` avec reponse personnalisee.
- Desactivation du mode debug ASP.NET (`compilation debug="false"`).
- Ajout d'une page `Error.aspx` sans details techniques.
- Durcissement des exceptions anti-CSRF (`BasePage`) pour retourner des messages generiques (`Bad Request.`).
Pourquoi:
- Eviter l'exposition des details d'exception ASP.NET (stack trace, chemins, infos techniques) detectee par ZAP.
- Garantir une reponse utilisateur stable et non verbeuse en cas d'erreur `400/500`.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'compilation debug=\"false\"|customErrors|httpErrors|statusCode=\"400\"|statusCode=\"500\"'
Select-String -Path ./A-NET48/WebFormsNet48Basics/BasePage.cs -Pattern 'HttpException\\(400, \"Bad Request\\.\"\\)'
Test-Path ./A-NET48/WebFormsNet48Basics/Error.aspx
```
Resultat attendu:
- Les erreurs applicatives ne retournent plus la page d'exception detaillee ASP.NET au client.
- Les reponses en erreur sont redirigees vers `Error.aspx` pour les cas `400` et `500`.
- L'alerte ZAP `Application Error Disclosure` doit disparaitre apres rescan.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:15`
- `A-NET48/WebFormsNet48Basics/Web.config:17`
- `A-NET48/WebFormsNet48Basics/Web.config:56`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:39`
- `A-NET48/WebFormsNet48Basics/BasePage.cs:59`
- `A-NET48/WebFormsNet48Basics/Error.aspx:1`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:88`

### Etape 15 - Corriger `Cookie Without Secure Flag` (ASP.NET_SessionId)
Ce qui a ete change:
- Ajout d'une configuration cookie globale dans `Web.config`:
  `httpOnlyCookies="true"`, `requireSSL="true"`, `sameSite="Lax"`.
- Ajout de `sessionState cookieless="UseCookies" cookieSameSite="Lax"` pour expliciter le mode cookie de session.
- Renforcement systematique dans `Application_EndRequest` pour tous les cookies emis:
  `Secure=true`, `HttpOnly=true`, `SameSite=Lax`.
Pourquoi:
- L'alerte ZAP indique que `ASP.NET_SessionId` est emis sans attribut `Secure`.
- Les drapeaux `Secure` + `HttpOnly` + `SameSite` reduisent l'exposition du cookie de session (transport non chiffre, acces script, CSRF cross-site).
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'httpCookies|requireSSL|sessionState|cookieSameSite'
Select-String -Path ./A-NET48/WebFormsNet48Basics/Global.asax.cs -Pattern 'resp.Cookies|cookie.Secure|cookie.HttpOnly|cookie.SameSite'
```
Resultat attendu:
- Le cookie `ASP.NET_SessionId` est emis avec `Secure` (en HTTPS), `HttpOnly` et `SameSite=Lax`.
- L'alerte ZAP `Cookie Without Secure Flag` ne doit plus apparaitre apres rescan HTTPS.
- Dependance d'etat: si l'application est testee en HTTP non chiffre, le flag `Secure` peut empecher l'utilisation du cookie (comportement normal de securite).
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:17`
- `A-NET48/WebFormsNet48Basics/Web.config:18`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:33`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:41`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:42`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs:43`

### Etape 16 - Ajouter un CRUD `Clients` pour tester le chiffrement SQL (EncryptByKey/DecryptByKey)
Ce qui a ete change:
- Ajout d'une page liste `ClientsList.aspx` (lecture dechiffree + suppression).
- Ajout d'une page formulaire `ClientForm.aspx` (creation + edition avec valeur chiffree).
- Ajout des code-behind ADO.NET avec ouverture/fermeture de la cle symetrique SQL pour les operations qui chiffrent/dechiffrent.
- Ajout des fichiers dans le projet `.csproj` et lien d'acces depuis `Default.aspx`.
Pourquoi:
- Fournir un atelier CRUD specifique pour valider concretement votre configuration SQL:
  `OPEN SYMMETRIC KEY`, `EncryptByKey`, `DecryptByKey`.
- Permettre de verifier visuellement qu'une valeur est stockee chiffre (`VARBINARY`) et relue dechiffree dans l'application.
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/ClientsList.aspx.cs -Pattern 'OPEN SYMMETRIC KEY|DecryptByKey|DELETE FROM Clients'
Select-String -Path ./A-NET48/WebFormsNet48Basics/ClientForm.aspx.cs -Pattern 'EncryptByKey|DecryptByKey|OPEN SYMMETRIC KEY'
Select-String -Path ./A-NET48/WebFormsNet48Basics/Default.aspx -Pattern 'ClientsList.aspx'

# Ensuite lancer l'application et tester:
# 1) /ClientsList.aspx
# 2) Create Client -> saisir Nom + Valeur a chiffrer -> Save
# 3) verifier la valeur dechiffree dans la liste
```
Resultat attendu:
- `Create`/`Update`: la colonne `NumeroCB` est ecrite via `EncryptByKey(...)`.
- `Read`: la liste/formulaire lit `CONVERT(NVARCHAR(100), DecryptByKey(NumeroCB))`.
- `Delete`: suppression par `Id` dans `Clients`.
- Dependance d'etat: la table `Clients`, le certificat `Cert_Chiffrement` et la cle `Cle_Champs` doivent exister dans la base cible de `AdventureWorks2014Connection`.
References code/tests:
- `A-NET48/WebFormsNet48Basics/ClientsList.aspx:1`
- `A-NET48/WebFormsNet48Basics/ClientsList.aspx:20`
- `A-NET48/WebFormsNet48Basics/ClientsList.aspx.cs:8`
- `A-NET48/WebFormsNet48Basics/ClientsList.aspx.cs:66`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx:1`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx:30`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx.cs:8`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx.cs:68`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx.cs:77`
- `A-NET48/WebFormsNet48Basics/ClientForm.aspx.cs:114`
- `A-NET48/WebFormsNet48Basics/Default.aspx:19`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:60`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:67`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:100`

### Etape 17 - Ajouter un CRUD `Employes` sur une autre base avec Always Encrypted
Ce qui a ete change:
- Ajout d'une nouvelle connexion SQL `AlwaysEncryptedConnection` vers `bob.dtc-apps.com,14519` avec `Column Encryption Setting=Enabled`.
- Ajout d'une liste `EmployesAeList.aspx` (lecture + suppression) et d'un formulaire `EmployeAeForm.aspx` (creation + edition).
- Ajout d'un lien d'acces depuis `Default.aspx`.
- Ajout des nouveaux fichiers dans le projet `.csproj`.
Pourquoi:
- Fournir un CRUD dedie pour tester une colonne `Always Encrypted` (`Prenom`) avec chiffrement/dechiffrement transparent cote client ADO.NET.
- Valider le schema cible:
  table `Employes` avec `Prenom` chiffre (deterministic, CEK `CEK_AE_Test`).
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'AlwaysEncryptedConnection|Column Encryption Setting=Enabled|bob.dtc-apps.com,14519|fxp-avadelis\\sqlae'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs -Pattern 'SELECT Id, Nom, Prenom FROM Employes|DELETE FROM Employes'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs -Pattern 'INSERT INTO Employes|UPDATE Employes|SELECT TOP 1 Id, Nom, Prenom'

# Tester ensuite dans l'application:
# 1) /EmployesAeList.aspx
# 2) Create Employe (Nom, Prenom) -> Save
# 3) Edit/Delete depuis la liste
```
Resultat attendu:
- Les operations CRUD fonctionnent sur la table `Employes` de la base distante.
- La colonne `Prenom` est traitee via Always Encrypted grace a `Column Encryption Setting=Enabled`.
- Dependances d'etat:
  - renseigner un mot de passe valide a la place de `CHANGE_ME`;
  - ajuster `Initial Catalog=AE_Test` si votre base porte un autre nom;
  - verifier que les cles Always Encrypted (CMK/CEK) sont accessibles par le compte applicatif.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:13`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx:1`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx:20`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs:8`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs:66`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx:1`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx:30`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs:8`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs:65`
- `A-NET48/WebFormsNet48Basics/Default.aspx:24`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:74`
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj:116`

### Etape 18 - Faciliter le diagnostic des erreurs Always Encrypted (load/save)
Ce qui a ete change:
- Assouplissement TLS de labo sur la connexion AE avec `TrustServerCertificate=True` (en conservant `Encrypt=True`).
- Ajout d'un retour de diagnostic SQL minimal cote UI pour le CRUD AE (`SQL error code: <Number>`), sans stack trace.
Pourquoi:
- Le message generique "Load failed..." ne permettait pas d'identifier rapidement la cause reelle.
- En environnement de labo, les erreurs les plus frequentes sont:
  - `18456` (authentification/login),
  - `4060` (base inaccessible),
  - `208` (table absente),
  - `33299` / erreurs AE (cles/certificats Always Encrypted indisponibles).
Comment reproduire:
```powershell
# Depuis la racine du depot
Select-String -Path ./A-NET48/WebFormsNet48Basics/Web.config -Pattern 'AlwaysEncryptedConnection|TrustServerCertificate=True|Column Encryption Setting=Enabled'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs -Pattern 'SQL error code'
Select-String -Path ./A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs -Pattern 'SQL error code'
```
Resultat attendu:
- En cas d'echec de connexion/CRUD AE, la page affiche un code d'erreur SQL exploitable pour depannage.
- Le flux fonctionnel reste identique quand la connexion et les cles AE sont correctement configurees.
References code/tests:
- `A-NET48/WebFormsNet48Basics/Web.config:13`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs:47`
- `A-NET48/WebFormsNet48Basics/EmployesAeList.aspx.cs:76`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs:81`
- `A-NET48/WebFormsNet48Basics/EmployeAeForm.aspx.cs:125`
