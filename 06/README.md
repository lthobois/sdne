# Atelier 06 - Securite du code externe et supply chain

Objectif: traiter les risques lies aux dependances tierces, aux appels API externes
et a la gestion des secrets.

## Structure

- `Atelier06.slnx`: solution atelier
- `SupplyChainSecurityLab/`: API (`vuln/*` et `secure/*`)
- `SupplyChainSecurityLab.Tests/`: tests d'integration de controles supply-chain
- `scripts/run-sca.ps1`: build/test + scan dependances vulnerables et obsoletes
- `scripts/generate-sbom.ps1`: generation SBOM (SPDX)
- `pipeline/supply-chain-ci.yml`: exemple CI

## Lancer

```powershell
cd .\06
dotnet build .\Atelier06.slnx
dotnet test .\Atelier06.slnx
```

## Parcours formateur (90 min)

1. Secrets:
   - `GET /vuln/config/secret`
   - `GET /secure/config/secret`
2. API externes:
   - `GET /vuln/outbound/fetch?url=http://example.com`
   - `GET /secure/outbound/fetch?url=http://example.com` (refus attendu)
   - `GET /secure/outbound/fetch?url=https://jsonplaceholder.typicode.com/todos/1`
3. Provenance dependances:
   - `POST /vuln/dependency/approve` (acceptation aveugle)
   - `POST /secure/dependency/approve` (allowlist package + host + format sha256)
4. Automatisation:
   - `.\scripts\run-sca.ps1`
   - `.\scripts\generate-sbom.ps1`

## Notes pedagogiques

- Le endpoint vulnerable de secret est intentionnellement mauvais pour la demo.
- En production, stocker les secrets dans un coffre (Key Vault, AWS Secrets Manager, etc.).
- Les controles "secure" montrent la base; ajouter ensuite signature, attestations et politique de mise a jour.
