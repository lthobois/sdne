param(
    [string]$SolutionPath = ".\Atelier03.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Workshop03] Restore"
dotnet restore $SolutionPath

Write-Host "[Workshop03] Build"
dotnet build $SolutionPath --no-restore

Write-Host "[Workshop03] Tests"
dotnet test $SolutionPath --no-build

Write-Host "[Workshop03] Done"