param(
    [string]$SolutionPath = ".\Atelier05.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[SAST] Build"
dotnet build $SolutionPath

Write-Host "[SAST] Tests"
dotnet test $SolutionPath

Write-Host "[SAST] Vulnerable packages"
dotnet list .\SecurityValidationLab\SecurityValidationLab.csproj package --vulnerable --include-transitive
dotnet list .\SecurityValidationLab.Tests\SecurityValidationLab.Tests.csproj package --vulnerable --include-transitive

Write-Host "[SAST] Done"
