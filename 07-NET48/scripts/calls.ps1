param([string]$BaseUrl = "http://localhost:5107")

function Api($Method, $Path, $Body = $null, $Headers = @{}) {
  Write-Host "`n=== $Method $Path ==="
  if ($Body -ne $null) {
    Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json)
  }
  else {
    Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers
  }
}

Api GET "/vuln/admin/ping"
try { Api GET "/secure/admin/ping" } catch { Write-Host $_.Exception.Message }
Api GET "/secure/admin/ping" $null @{ 'X-Admin-Key'='workshop-admin-key' }
Api GET "/vuln/search?q=<script>alert(1)</script>"
try { Api GET "/secure/search?q=<script>alert(1)</script>" } catch { Write-Host $_.Exception.Message }
Api POST "/vuln/upload/meta" @{ fileName="payload.exe"; contentType="application/x-msdownload"; size=1000 }
try { Api POST "/secure/upload/meta" @{ fileName="payload.exe"; contentType="application/x-msdownload"; size=1000 } } catch { Write-Host $_.Exception.Message }
Api POST "/secure/upload/meta" @{ fileName="document.pdf"; contentType="application/pdf"; size=34567 }
