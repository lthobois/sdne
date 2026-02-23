param(
    [string]$SolutionPath = ".\Atelier10.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Perimeter] Build"
dotnet build $SolutionPath

Write-Host "[Perimeter] Tests"
dotnet test $SolutionPath

Write-Host "[Perimeter] Done"
