param(
    [string]$SolutionPath = ".\Atelier08.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Monitoring] Build"
dotnet build $SolutionPath

Write-Host "[Monitoring] Tests"
dotnet test $SolutionPath

Write-Host "[Monitoring] Check dangerous logging patterns"
if (Get-ChildItem -Recurse -File .\SecurityMonitoringLab | Select-String -Pattern 'password=\{Password\}|password=' -CaseSensitive) {
    Write-Host "Potential sensitive logging pattern found."
    exit 1
}

Write-Host "[Monitoring] Completed"
