# AGENDA-IIS - Atelier Securite Infrastructure (IIS)

## Etape 1 - Installer IIS en mode minimal pour WebForms .NET 4.8
Objectif pedagogique: Deployer uniquement les composants IIS necessaires a ASP.NET WebForms.
Actions concretes:
- Installer le role IIS et les fonctionnalites minimales (`Web-Server`, `Web-Asp-Net45`, `Web-Windows-Auth`, `Web-Request-Monitor`, `Web-Http-Logging`).
- Eviter l'installation de modules non utiles au contexte atelier.
- Verifier la presence du runtime .NET Framework 4.8.
Resultat attendu / verification:
- Le serveur IIS repond et peut executer une application ASP.NET 4.8.
- Les fonctionnalites installees correspondent au minimum attendu.
References explicites:
- `Server Manager > Add Roles and Features`
- `Get-WindowsFeature Web-*`
- `IIS Manager > Server node`

## Etape 2 - Creer un site IIS dedie a l'application WebForms
Objectif pedagogique: Isoler le perimetre de l'application dans une configuration explicite.
Actions concretes:
- Creer un dossier de deploiement (ex: `C:\inetpub\A-NET48\WebFormsNet48Basics`).
- Creer un site IIS avec binding HTTP temporaire pour validation initiale.
- Pointer le site sur le repertoire de publication.
Resultat attendu / verification:
- Le site est accessible localement et sert la page `Default.aspx`.
- Le chemin physique correspond au dossier de deploiement attendu.
References explicites:
- `IIS Manager > Sites > Add Website`
- `C:\inetpub\A-NET48\WebFormsNet48Basics`

## Etape 3 - Configurer un pool applicatif isole
Objectif pedagogique: Limiter l'impact d'un incident a une application.
Actions concretes:
- Creer un Application Pool dedie en `.NET CLR v4.0` et pipeline `Integrated`.
- Associer le site au pool dedie.
- Desactiver l'option de chargement de profil si non necessaire.
Resultat attendu / verification:
- Le site s'execute dans son propre process `w3wp.exe`.
- Aucun partage de pool avec d'autres applications sensibles.
References explicites:
- `IIS Manager > Application Pools`
- `IIS Manager > Sites > [Site] > Basic Settings`

## Etape 4 - Definir l'identite d'execution et les droits NTFS minimaux
Objectif pedagogique: Appliquer le moindre privilege au compte d'execution IIS.
Actions concretes:
- Utiliser `ApplicationPoolIdentity` ou compte de service dedie.
- Accorder uniquement `Read/Execute` au code applicatif, et droits cibles sur les repertoires necessaires (logs/upload).
- Retirer les permissions excessives (`Users`, `Everyone` en ecriture).
Resultat attendu / verification:
- L'application fonctionne sans droits administrateur local.
- Les acces en ecriture sont strictement limites aux repertoires prevus.
References explicites:
- `IIS Manager > Application Pools > [Pool] > Advanced Settings > Identity`
- `C:\inetpub\A-NET48\WebFormsNet48Basics` (ACL NTFS)
- `icacls` (verification ACL)

## Etape 5 - Activer HTTPS, TLS moderne et certificat serveur
Objectif pedagogique: Chiffrer les flux et assurer l'authenticite du serveur.
Actions concretes:
- Importer/creer un certificat serveur valide.
- Ajouter binding HTTPS (443) et desactiver HTTP si politique stricte.
- Restreindre TLS aux versions/cipher suites conformes a la politique de securite.
Resultat attendu / verification:
- Le site est accessible en HTTPS sans avertissement certificat en environnement cible.
- Les scans TLS confirment le niveau de securite attendu.
References explicites:
- `IIS Manager > Server Certificates`
- `IIS Manager > Sites > [Site] > Bindings`
- `IISCrypto` ou parametres Schannel (`HKLM\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL`)

## Etape 6 - Configurer Windows Authentication et desactiver les modules inutiles
Objectif pedagogique: Reduire la surface d'attaque et imposer l'authentification integree.
Actions concretes:
- Sur le site: `Windows Authentication = Enabled`, `Anonymous Authentication = Disabled`.
- Configurer les providers (`Negotiate`, `NTLM`) selon besoin.
- Retirer/desactiver modules IIS non utilises pour WebForms.
Resultat attendu / verification:
- L'acces anonyme est impossible.
- Les modules actifs sont limites au besoin operationnel.
References explicites:
- `IIS Manager > [Site] > Authentication`
- `IIS Manager > [Site] > Modules`

## Etape 7 - Renforcer Request Filtering et limites de requetes
Objectif pedagogique: Bloquer en amont les requetes malicieuses et abus de taille.
Actions concretes:
- Configurer `Request Filtering` (extensions interdites, verbes autorises, sequences bloquees).
- Definir des limites de taille (`maxAllowedContentLength`, `requestLimits`).
- Interdire le browsing de repertoires si inutile.
Resultat attendu / verification:
- Les requetes non conformes sont bloquees (404.7, 404.11, 404.12, etc.).
- Les uploads/requetes au-dela des limites sont refuses.
References explicites:
- `IIS Manager > [Site] > Request Filtering`
- `A-NET48/WebFormsNet48Basics/Web.config` (`system.webServer/security/requestFiltering`)
- `IIS Manager > [Site] > Directory Browsing`

## Etape 8 - Ajouter les en-tetes de securite HTTP cote IIS
Objectif pedagogique: Renforcer le navigateur contre certains vecteurs d'attaque.
Actions concretes:
- Ajouter au minimum: `X-Content-Type-Options: nosniff`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy` adaptee WebForms.
- Retirer ou masquer les en-tetes de divulgation (`X-Powered-By`, version serveur).
- Harmoniser avec les besoins fonctionnels de l'application.
Resultat attendu / verification:
- Les headers attendus sont presents sur toutes les reponses applicatives.
- Aucun header de divulgation inutile n'est expose.
References explicites:
- `IIS Manager > [Site] > HTTP Response Headers`
- `A-NET48/WebFormsNet48Basics/Web.config` (`system.webServer/httpProtocol/customHeaders`)

## Etape 9 - Configurer les logs IIS et la retention
Objectif pedagogique: Assurer l'auditabilite et l'investigation post-incident.
Actions concretes:
- Activer les logs W3C avec champs utiles (IP, user, URI, status, user-agent, referer).
- Definir emplacement de logs securise et retention.
- Synchroniser l'heure serveur (NTP) pour la correlation.
Resultat attendu / verification:
- Les traces IIS permettent de reconstruire une sequence d'attaque.
- Les logs sont disponibles, lisibles et proteges.
References explicites:
- `IIS Manager > [Site] > Logging`
- `C:\inetpub\logs\LogFiles` (ou chemin dedie)
- `Event Viewer > Windows Logs > Security/System`

## Etape 10 - Verifier l'integration IIS <-> SQL Server 2019
Objectif pedagogique: Confirmer que l'identite applicative et les droits SQL sont conformes.
Actions concretes:
- Verifier l'identite effective du pool applicatif.
- Verifier le mode d'authentification SQL (Windows privilegie).
- Tester une operation SQL autorisee et une operation interdite.
Resultat attendu / verification:
- Les acces SQL respectent le principe de moindre privilege.
- Les tentatives hors droit sont rejetees proprement.
References explicites:
- `IIS Manager > Application Pools > [Pool] > Identity`
- `SQL Server 2019 > Security > Logins`
- `A-NET48/WebFormsNet48Basics/Web.config` (`connectionStrings`)

## Etape 11 - Verification finale de durcissement IIS
Objectif pedagogique: Valider la configuration finale avant ouverture de service.
Actions concretes:
- Executer une checklist de durcissement IIS (auth, TLS, modules, ACL, logs, filtering, headers).
- Realiser des tests de securite basiques (acces anonyme, verbes interdits, chemins sensibles, downgrade TLS).
- Documenter les ecarts et plan de remediation.
Resultat attendu / verification:
- Le serveur IIS est durci selon le cadre atelier.
- Un rapport de validation SecOps est produit.
References explicites:
- `IIS Manager` (configuration finale site + serveur)
- `A-NET48/WebFormsNet48Basics/Web.config`
- Rapport de verification versionne dans `A-NET48`
