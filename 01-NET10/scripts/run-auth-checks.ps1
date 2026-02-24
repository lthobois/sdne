param(
    [string]$SolutionPath = ".\Atelier01.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[BasicAuth] Restore"
dotnet restore $SolutionPath

Write-Host "[BasicAuth] Build"
dotnet build $SolutionPath --no-restore

Write-Host "[BasicAuth] Tests"
dotnet test $SolutionPath --no-build

Write-Host "[BasicAuth] Done"