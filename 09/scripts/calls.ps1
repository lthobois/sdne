param([string]$BaseUrl = "http://localhost:5128")

function Api($Method, $Path, $Body = $null, $Headers = @{}) {
  Write-Host "`n=== $Method $Path ==="
  if ($Body -ne $null) { Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json) }
  else { Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -Headers $Headers }
}

Api POST "/vuln/auth/token" @{ username="alice"; scope="docs.read" }
$alice = Api POST "/secure/auth/token" @{ username="alice"; scope="docs.read" }
Api GET "/vuln/docs/2?username=alice"
try { Api GET "/secure/docs/2" $null @{ Authorization = "Bearer $($alice.token)" } } catch { Write-Host $_.Exception.Message }
$bob = Api POST "/secure/auth/token" @{ username="bob"; scope="docs.read docs.publish" }
Api POST "/secure/docs/2/publish" $null @{ Authorization = "Bearer $($bob.token)" }
