param(
    [string]$SolutionPath = ".\Atelier01.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[BasicAuth48] Restore"
dotnet restore $SolutionPath

Write-Host "[BasicAuth48] Build"
dotnet build $SolutionPath --no-restore

Write-Host "[BasicAuth48] Tests"
dotnet test $SolutionPath --no-build

Write-Host "[BasicAuth48] Done"