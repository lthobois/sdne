param(
    [string]$SolutionPath = ".\Atelier04.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Workshop04] Restore"
dotnet restore $SolutionPath

Write-Host "[Workshop04] Build"
dotnet build $SolutionPath --no-restore

Write-Host "[Workshop04] Tests"
dotnet test $SolutionPath --no-build

Write-Host "[Workshop04] Done"