param([string]$BaseUrl = "http://localhost:5110")

function Api($Method, $Path, $Headers = @{}) {
  Write-Host "`n=== $Method $Path ==="
  Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers
}

Api GET "/vuln/links/reset-password?user=alice" @{ 'X-Forwarded-Host'='evil.example'; 'X-Forwarded-Proto'='http' }
Api GET "/secure/links/reset-password?user=alice" @{ 'X-Forwarded-Host'='app.contoso.local'; 'X-Forwarded-Proto'='https' }
try { Api GET "/secure/links/reset-password?user=alice" @{ 'X-Forwarded-Host'='evil.example'; 'X-Forwarded-Proto'='https' } } catch { Write-Host $_.Exception.Message }
Api GET "/vuln/tenant/home" @{ 'X-Forwarded-Host'='evil.example' }
Api GET "/secure/tenant/home" @{ 'X-Forwarded-Host'='app.contoso.local'; 'X-Forwarded-Proto'='https' }
Api GET "/secure/diagnostics/request-meta" @{ 'X-Forwarded-Host'='app.contoso.local'; 'X-Forwarded-Proto'='https' }
