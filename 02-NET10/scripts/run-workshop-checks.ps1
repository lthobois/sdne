param(
    [string]$SolutionPath = ".\Atelier02.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Workshop02] Restore"
dotnet restore $SolutionPath

Write-Host "[Workshop02] Build"
dotnet build $SolutionPath --no-restore

Write-Host "[Workshop02] Tests"
dotnet test $SolutionPath --no-build

Write-Host "[Workshop02] Done"