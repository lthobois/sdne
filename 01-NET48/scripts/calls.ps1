param([string]$BaseUrl = "http://localhost:5098")

function Call($Method, $Path, $Headers = @{}, $Body = $null) {
  Write-Host "`n=== $Method $Path ==="
  $uri = "$BaseUrl$Path"
  if ($Body -ne $null) {
    $json = $Body | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers -ContentType "application/json" -Body $json
  } else {
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers
  }
}

$alice = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("alice:P@ssw0rd!"))
$bob = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("bob:Admin123!"))

Call GET "/public"
try { Call GET "/secure/profile" } catch { Write-Host $_.Exception.Message }
Call GET "/secure/profile" @{ Authorization = "Basic $alice" }
try { Call GET "/secure/admin" @{ Authorization = "Basic $alice" } } catch { Write-Host $_.Exception.Message }
Call GET "/secure/admin" @{ Authorization = "Basic $bob" }
