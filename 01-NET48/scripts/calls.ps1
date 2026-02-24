param([string]$BaseUrl = "http://localhost:5101")

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

$analyst = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("analyst:Passw0rd!"))
$admin = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:Adm1nPass!"))

Call GET "/public"
try { Call GET "/secure/profile" } catch { Write-Host $_.Exception.Message }
Call GET "/secure/profile" @{ Authorization = "Basic $analyst" }
try { Call GET "/secure/admin" @{ Authorization = "Basic $analyst" } } catch { Write-Host $_.Exception.Message }
Call GET "/secure/admin" @{ Authorization = "Basic $admin" }
