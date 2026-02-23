param(
    [string]$SolutionPath = ".\Atelier06.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[SCA] Build"
dotnet build $SolutionPath

Write-Host "[SCA] Test"
dotnet test $SolutionPath

Write-Host "[SCA] Vulnerable packages"
dotnet list .\SupplyChainSecurityLab\SupplyChainSecurityLab.csproj package --vulnerable --include-transitive
dotnet list .\SupplyChainSecurityLab.Tests\SupplyChainSecurityLab.Tests.csproj package --vulnerable --include-transitive

Write-Host "[SCA] Outdated packages"
dotnet list .\SupplyChainSecurityLab\SupplyChainSecurityLab.csproj package --outdated
dotnet list .\SupplyChainSecurityLab.Tests\SupplyChainSecurityLab.Tests.csproj package --outdated
