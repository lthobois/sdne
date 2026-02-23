param(
    [string]$TargetUrl = "http://host.docker.internal:5000",
    [string]$ReportFile = "zap-report.html"
)

$ErrorActionPreference = "Stop"

Write-Host "[DAST] Running OWASP ZAP baseline scan on $TargetUrl"

docker run --rm `
    -t `
    -v "${PWD}:/zap/wrk/:rw" `
    ghcr.io/zaproxy/zaproxy:stable `
    zap-baseline.py `
    -t $TargetUrl `
    -r $ReportFile `
    -I

Write-Host "[DAST] Report generated: $ReportFile"
