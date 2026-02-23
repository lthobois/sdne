# Atelier 01 - Authentification HTTP Basic

## But

Implementer une authentification HTTP Basic, verifier les controles d'acces, puis analyser les limites de ce schema.

## Pre-requis

- .NET SDK 9+
- Terminal PowerShell

## Demarrage

```powershell
cd .\01\BasicAuthWorkshop
dotnet run
```

Conserver l'URL affichee par l'application (ex: `http://localhost:5098`).

## Comptes de test

- `alice / P@ssw0rd!` (role `User`)
- `bob / Admin123!` (roles `User`, `Admin`)

## Mode operatoire

### Etape 1 - Verifier l'acces public

Action:
- Appeler la route publique.

Requete:
```http
GET /public HTTP/1.1
Host: localhost
```

Resultat attendu:
- `200 OK`
- message indiquant qu'aucune authentification n'est requise.

Point a observer:
- Une route publique doit rester explicite et limitee.

### Etape 2 - Verifier le refus sans authentification

Action:
- Appeler une route protegee sans header `Authorization`.

Requete:
```http
GET /secure/profile HTTP/1.1
Host: localhost
```

Resultat attendu:
- `401 Unauthorized`
- header `WWW-Authenticate: Basic ...`.

Point a observer:
- Le challenge HTTP indique le schema attendu.

### Etape 3 - Appeler avec des identifiants valides

Action:
- Encoder `alice:P@ssw0rd!` en Base64.

Commande:
```powershell
[Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('alice:P@ssw0rd!'))
```

Requete:
```http
GET /secure/profile HTTP/1.1
Host: localhost
Authorization: Basic YWxpY2U6UEBzc3cwcmQh
```

Resultat attendu:
- `200 OK`
- identite `alice` et role `User`.

Point a observer:
- Basic Auth transmet un secret reutilisable a chaque requete.

### Etape 4 - Verifier le controle de role

Action:
- Tester la route admin avec un utilisateur non admin, puis admin.

Requetes:
```http
GET /secure/admin HTTP/1.1
Host: localhost
Authorization: Basic YWxpY2U6UEBzc3cwcmQh
```

```http
GET /secure/admin HTTP/1.1
Host: localhost
Authorization: Basic Ym9iOkFkbWluMTIzIQ==
```

Resultat attendu:
- `alice`: `403 Forbidden`
- `bob`: `200 OK`

Point a observer:
- L'authentification et l'autorisation sont deux controles distincts.

### Etape 5 - Montrer la faiblesse du schema

Action:
- Decoder un token Basic capture.

Commande:
```powershell
[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('YWxpY2U6UEBzc3cwcmQh'))
```

Resultat attendu:
- valeur claire: `alice:P@ssw0rd!`

Point a observer:
- Base64 n'est pas du chiffrement.
- Basic Auth est acceptable uniquement avec HTTPS strict.

## Reexecution rapide

- Utiliser `BasicAuthWorkshop.http` pour rejouer toutes les requetes.
- Relancer `dotnet run` apres modification de code.

## Script PowerShell des appels Web Service

```powershell
cd .\01
.\scripts\calls.ps1
```

## Diagramme Mermaid

```mermaid
graph TD
  N1[Client] --> N2[Public endpoint]
  N1 --> N3[Profile endpoint]
  N3 --> N4{Basic header present}
  N4 -- No --> N5[Unauthorized 401]
  N4 -- Yes --> N6{Credentials valid}
  N6 -- No --> N5
  N6 -- Yes --> N7[Authenticated user]
  N7 --> N8[Admin endpoint]
  N8 --> N9{Admin role}
  N9 -- No --> N10[Forbidden 403]
  N9 -- Yes --> N11[Admin access 200]
```
