param(
    [string]$ProjectPath = ".\SupplyChainSecurityLab\SupplyChainSecurityLab.csproj",
    [string]$OutputFile = ".\sbom.spdx.json"
)

$ErrorActionPreference = "Stop"

Write-Host "[SBOM] Generating software bill of materials"
dotnet sbom generate `
  --project $ProjectPath `
  --format spdx `
  --output $OutputFile

Write-Host "[SBOM] Generated: $OutputFile"
