# AGENDA-APPS - Atelier Securite Applicative (WebForms .NET 4.8)

## Etape 1 - Creer le socle WebForms .NET Framework 4.8
Objectif pedagogique: Disposer d'une application WebForms minimale executable sur IIS/IIS Express.
Actions concretes:
- Creer une solution Visual Studio avec un projet ASP.NET Web Application (WebForms) cible `.NET Framework 4.8`.
- Ajouter `Default.aspx` et son code-behind.
- Ajouter `Global.asax` et `Web.config` minimaux.
Resultat attendu / verification:
- L'application demarre sans erreur et la page par defaut affiche un message simple.
- Verification manuelle: ouvrir `Default.aspx` via IIS Express.
References explicites:
- `A-NET48/WebFormsNet48Basics/WebFormsNet48Basics.csproj`
- `A-NET48/WebFormsNet48Basics/Default.aspx`
- `A-NET48/WebFormsNet48Basics/Default.aspx.cs`
- `A-NET48/WebFormsNet48Basics/Global.asax`
- `A-NET48/WebFormsNet48Basics/Global.asax.cs`
- `A-NET48/WebFormsNet48Basics/Web.config`

## Etape 4 - Securiser la connexion SQL Server 2019
Objectif pedagogique: Etablir une connexion SQL fiable et controlee en environnement Windows.
Actions concretes:
- Definir une chaine de connexion vers SQL Server 2019 dans `connectionStrings`.
- Privilegier `Integrated Security=SSPI` (ou compte de service dedie) selon l'architecture.
- Restreindre les droits SQL au strict necessaire (lecture/ecriture ciblees).
Resultat attendu / verification:
- L'application execute une requete de sante sans erreur de droits.
- Un acces hors privilege echoue proprement et est journalise.
References explicites:
- `A-NET48/WebFormsNet48Basics/Web.config` (section `connectionStrings`)
- `SQL Server 2019 > Security > Logins > User Mapping`
- `SQL Server 2019 > Databases > [DB] > Security > Users`

## Etape 5 - Eliminer les injections SQL (requetes parametrees)
Objectif pedagogique: Bloquer l'injection SQL dans tous les acces donnees.
Actions concretes:
- Remplacer les concatenations SQL par `SqlCommand` parametre.
- Typage explicite des parametres (`SqlDbType`) et validation de longueur.
- Centraliser les exemples de bonnes/mauvaises pratiques pour comparaison pedagogique.
Resultat attendu / verification:
- Les payloads d'injection ne modifient pas la requete executee.
- Les tests fonctionnels restent passants.
References explicites:
- `A-NET48/WebFormsNet48Basics/*.aspx.cs` (acces ADO.NET)
- `System.Data.SqlClient.SqlCommand` (usage parametre)

## Etape 6 - Reduire le risque XSS dans les pages WebForms
Objectif pedagogique: Eviter l'execution de scripts injectes dans les sorties HTML.
Actions concretes:
- Encoder systematiquement les sorties (`HttpUtility.HtmlEncode` / `<%: %>`).
- Eviter `Response.Write` de donnees utilisateur non encodees.
- Verifier les controles WebForms affichant des donnees dynamiques.
Resultat attendu / verification:
- Un input contenant `<script>` est affiche en texte brut.
- Aucun script arbitraire n'est execute cote navigateur.
References explicites:
- `A-NET48/WebFormsNet48Basics/Default.aspx` (syntaxe d'affichage)
- `A-NET48/WebFormsNet48Basics/*.aspx.cs` (encodage des sorties)

## Etape 7 - Ajouter une protection CSRF adaptee a WebForms
Objectif pedagogique: Proteger les actions POST contre les requetes forgees.
Actions concretes:
- Mettre en place un token anti-CSRF (ViewStateUserKey + verification token serveur).
- Lier le token a la session utilisateur authentifiee Windows.
- Exiger la verification token pour les operations de modification.
Resultat attendu / verification:
- Une requete POST sans token valide est rejetee.
- Les soumissions legitimes continuent de fonctionner.
References explicites:
- `A-NET48/WebFormsNet48Basics/Global.asax.cs` (initialisation contexte)
- `A-NET48/WebFormsNet48Basics/*.aspx.cs` (validation token)

## Etape 8 - Valider strictement toutes les entrees
Objectif pedagogique: Rejeter les donnees mal formees avant traitement metier/SQL.
Actions concretes:
- Ajouter des validateurs WebForms (`RequiredFieldValidator`, `RegularExpressionValidator`).
- Renforcer la validation serveur (`int.TryParse`, whitelist, bornes de longueur).
- Refuser les entrees non conformes avec message utilisateur neutre.
Resultat attendu / verification:
- Les entrees invalides sont bloquees en UI et cote serveur.
- Les donnees valides suivent le flux normal.
References explicites:
- `A-NET48/WebFormsNet48Basics/*.aspx` (validateurs)
- `A-NET48/WebFormsNet48Basics/*.aspx.cs` (validation serveur)

## Etape 9 - Gerer les erreurs sans fuite d'information
Objectif pedagogique: Eviter l'exposition de stack traces et d'informations sensibles.
Actions concretes:
- Configurer `customErrors`/`httpErrors` et pages d'erreur dediees.
- Capter les exceptions globales dans `Application_Error`.
- Retourner des messages generiques cote utilisateur.
Resultat attendu / verification:
- En erreur applicative, l'utilisateur voit une page maitrisee.
- Les details techniques restent accessibles uniquement dans les logs.
References explicites:
- `A-NET48/WebFormsNet48Basics/Web.config` (`customErrors`)
- `A-NET48/WebFormsNet48Basics/Global.asax.cs` (`Application_Error`)
- `IIS Manager > [Site] > Error Pages`

## Etape 10 - Journaliser proprement et proteger les secrets
Objectif pedagogique: Assurer la tracabilite de securite sans exposer d'informations sensibles.
Actions concretes:
- Journaliser les evenements de securite (auth, refus, erreurs applicatives).
- Ne jamais logger mot de passe, chaine de connexion complete, token, cookie.
- Proteger les secrets de configuration (chiffrement section `connectionStrings` si necessaire).
Resultat attendu / verification:
- Les journaux permettent l'analyse d'incident sans fuite de secret.
- Les secrets ne sont pas visibles en clair dans les depots et sorties applicatives.
References explicites:
- `A-NET48/WebFormsNet48Basics/Web.config` (`connectionStrings`, `appSettings`)
- `Windows Event Viewer` ou fichier de logs applicatifs cible
- `aspnet_regiis -pef` (protection configuration .NET Framework)

## Etape 11 - Campagne de verification finale AppSec
Objectif pedagogique: Valider la posture de securite applicative avant mise en service.
Actions concretes:
- Executer des tests fonctionnels (authentification, navigation, acces SQL).
- Executer des tests d'attaque de base (SQLi, XSS, CSRF, elevation de privilege).
- Documenter les ecarts restants et les actions correctives.
Resultat attendu / verification:
- Les controles de securite applicative cibles sont verifies.
- Un rapport de verification final est disponible.
References explicites:
- `A-NET48/WebFormsNet48Basics/*`
- Matrice de tests AppSec de l'atelier (a versionner dans `A-NET48`)
