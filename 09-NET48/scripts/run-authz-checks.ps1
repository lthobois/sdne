param(
    [string]$SolutionPath = ".\Atelier09.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[AuthZ] Build"
dotnet build $SolutionPath

Write-Host "[AuthZ] Tests"
dotnet test $SolutionPath

Write-Host "[AuthZ] Done"
