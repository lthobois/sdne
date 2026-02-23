param([string]$BaseUrl = "http://localhost:5076")

function Api($Method, $Path, $Body = $null) {
  Write-Host "`n=== $Method $Path ==="
  $uri = "$BaseUrl$Path"
  if ($Body -ne $null) { Invoke-RestMethod -Method $Method -Uri $uri -ContentType "application/json" -Body ($Body | ConvertTo-Json) }
  else { Invoke-RestMethod -Method $Method -Uri $uri }
}

Api POST "/vuln/register" @{ username = "aa"; password = "1234" }
Api POST "/secure/register" @{ username = "aa"; password = "1234" }
Api POST "/secure/register" @{ username = "alice.secure"; password = "S3cure!Password" }
Api GET "/vuln/files/read?path=..\\..\\appsettings.json"
Api GET "/secure/files/read?fileName=public-note.txt"
try { Api GET "/secure/files/read?fileName=..\\..\\appsettings.json" } catch { Write-Host $_.Exception.Message }
Invoke-WebRequest -Uri "$BaseUrl/vuln/redirect?returnUrl=https://evil.example/phishing" -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
Api GET "/secure/redirect?returnUrl=/dashboard"
try { Api GET "/secure/redirect?returnUrl=https://evil.example/phishing" } catch { Write-Host $_.Exception.Message }
Api GET "/secure/errors/divide-by-zero"
