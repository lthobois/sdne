# Atelier 07 - Limiter l'exposition applicative

Objectif: mettre en place des controles defensifs "runtime" pour reduire
la surface d'attaque d'une API exposee.

## Structure

- `Atelier07.slnx`
- `ExposureDefenseLab/`: API demo (`vuln/*` et `secure/*`)
- `ExposureDefenseLab.Tests/`: tests d'integration de controles defensifs
- `scripts/run-defense-checks.ps1`: build, tests et scan de patterns dangereux
- `pipeline/exposure-defense-ci.yml`: pipeline CI exemple

## Lancer

```powershell
cd .\07
dotnet build .\Atelier07.slnx
dotnet test .\Atelier07.slnx
```

## Scenarios ateliers (90 min)

1. Endpoint admin expose:
   - `/vuln/admin/ping` (ouvert)
   - `/secure/admin/ping` (cle API requise)
2. Filtrage WAF-like:
   - `/vuln/search?q=<script>alert(1)</script>`
   - `/secure/search?q=<script>alert(1)</script>` (blocage)
3. Validation metadonnees upload:
   - `/vuln/upload/meta` (accepte tout)
   - `/secure/upload/meta` (type, taille, nom de fichier controles)
4. Limitation de debit:
   - exceder 5 requetes / 10s depuis le meme client pour observer `429`
