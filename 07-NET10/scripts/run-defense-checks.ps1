param(
    [string]$SolutionPath = ".\Atelier07.slnx"
)

$ErrorActionPreference = "Stop"

Write-Host "[Defense] Build"
dotnet build $SolutionPath

Write-Host "[Defense] Tests"
dotnet test $SolutionPath

Write-Host "[Defense] Basic scan for dangerous patterns"
if (Get-ChildItem -Recurse -File .\ExposureDefenseLab | Select-String -Pattern 'password\s*=\s*"|apikey\s*=\s*"' -CaseSensitive) {
    Write-Host "Potential hardcoded secret pattern found."
    exit 1
}

Write-Host "[Defense] Completed"
