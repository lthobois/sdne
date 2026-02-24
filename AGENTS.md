# Agent Context - README & Code Policy

Regles minimales pour tout agent IA sur ce depot.

## 1) README d'atelier: role et structure

- Le `README.md` est la reference pedagogique principale.
- Il doit decrire l'atelier de bout en bout, sans etape critique manquante.
- Les commandes doivent etre progressives (pas de bloc "tout-en-un").

Sections obligatoires (ordre impose):
1. `Objectif`
2. `Pre-requis`
3. `Les etapes de l'atelier`
4. `Scripts stagiaires (support)` (si scripts disponibles)
5. `Fichiers utiles`
6. `Nettoyage`

## 2) README: exigences pedagogiques

Pour chaque etape:
- expliquer **quoi faire**, **pourquoi**, et **resultat attendu**;
- preciser les dependances d'etat avant/apres si appels successifs;
- indiquer les effets observables (HTTP, donnees retournees, creation/lecture/modification/suppression/controle).

## 3) README: tracabilite code

Chaque etape/activite doit contenir des references explicites:
- code source: `fichier:ligne`;
- tests lies: `fichier:ligne` (si applicables).

Contraintes:
- une section globale "code a verifier" ne suffit pas;
- les references doivent apparaitre dans chaque etape concernee;
- pas de logique "magique" ni comportement utilise mais non documente.

## 4) Commandes PowerShell dans les README

- Les scripts doivent etre appeles avec des chemins **relatifs a la racine du depot**.
- Exemple valide: `./01-NET10/scripts/calls.ps1`.
- Exemple invalide: appel depuis un sous-dossier atelier sans chemin racine.

## 5) Lisibilite du code (endpoint-first)

Principes:
- priorite a la lisibilite pedagogique;
- noms explicites (variables, fonctions, fichiers);
- eviter implicite/abstraction inutile.

Organisation:
- mettre pres de l'endpoint la validation, le mapping, la logique metier simple et les appels directs;
- n'introduire services/helpers partages que si la comprehension est meilleure;
- toute abstraction ajoutee doit etre pedagogiquement justifiable.

## 6) Objectif global

Garantir un lien clair et verifiable:
**atelier <-> commandes <-> code source <-> resultat**,
avec un code lisible sans expertise avancee.
