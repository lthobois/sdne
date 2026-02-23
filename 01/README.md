# Atelier 01 - Outil d'authentification HTTP Basic (.NET)

Ce dossier contient une solution de référence pour l'atelier :
`Créer un outil d'authentification avec HTTP Basic`.

## Objectif pédagogique

1. Implémenter une authentification Basic dans une API .NET.
2. Protéger des routes avec authentification et rôles.
3. Montrer les limites de Basic Auth (credentials réutilisables, encodage Base64 non chiffrant).

## Contenu

- `BasicAuthWorkshop/` : projet ASP.NET Core (net9.0).
- Schéma d'authentification `Basic` personnalisé.
- Endpoints publics et protégés (`/public`, `/secure/profile`, `/secure/admin`).

## Lancer la solution

```powershell
cd .\01\BasicAuthWorkshop
dotnet run
```

Par défaut, l'application écoute sur un port HTTP local (pas de TLS).

## Comptes de démonstration

- `alice / P@ssw0rd!` (rôle `User`)
- `bob / Admin123!` (rôles `User`, `Admin`)

## Scénario d'atelier (animation)

1. Appeler `GET /public` sans header d'authentification.
2. Appeler `GET /secure/profile` sans auth -> `401`.
3. Ajouter header `Authorization: Basic <base64(user:password)>`.
4. Valider l'accès à `GET /secure/profile`.
5. Tester `GET /secure/admin` avec `alice` (interdit), puis `bob` (autorisé).
6. Montrer que Base64 est décodable instantanément.

## Démonstration des limites de Basic Auth

Exemple de décodage local d'un token:

```powershell
[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String("YWxpY2U6UEBzc3cwcmQh"))
```

Résultat attendu: `alice:P@ssw0rd!`

Message clé à transmettre :
- Basic Auth n'est acceptable qu'avec HTTPS strict.
- Les mots de passe ne doivent pas être stockés en clair (ici c'est volontairement simplifié pour l'atelier).
- En production, préférer OAuth2/OIDC ou au minimum des schémas plus robustes avec gestion des secrets.

## Fichiers clés

- `BasicAuthWorkshop/Program.cs`
- `BasicAuthWorkshop/Auth/BasicAuthenticationHandler.cs`
- `BasicAuthWorkshop/Auth/InMemoryWorkshopUserStore.cs`
- `BasicAuthWorkshop/BasicAuthWorkshop.http`
