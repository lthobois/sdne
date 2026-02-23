param([string]$BaseUrl = "http://localhost:5297")

function Api($Method, $Path, $Body = $null, $Headers = @{}) {
  Write-Host "`n=== $Method $Path ==="
  if ($Body -ne $null) { Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json) }
  else { Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers }
}

Api POST "/vuln/login" @{ username="alice"; password="bad-password" }
Api POST "/secure/login" @{ username="alice"; password="bad-password" } @{ 'X-Correlation-ID'='req-12345' }
Api POST "/secure/login" @{ username="alice"; password="Password123!" }
Api GET "/secure/audit/events"
Api GET "/secure/alerts"
try { Api POST "/secure/admin/reset-alerts" } catch { Write-Host $_.Exception.Message }
Api POST "/secure/admin/reset-alerts" $null @{ 'X-SOC-Key'='soc-admin-key' }
