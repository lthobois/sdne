param([string]$BaseUrl = "http://localhost:5156")

function Api($Method, $Path, $Body = $null, $Headers = @{}) {
  Write-Host "`n=== $Method $Path ==="
  $uri = "$BaseUrl$Path"
  if ($Body -ne $null) {
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json -Depth 10)
  } else {
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers
  }
}

Api POST "/vuln/session/login" @{ username = "alice" }
Api GET "/vuln/session/profile?token=YWxpY2U6d29ya3Nob3Atc2Vzc2lvbg=="
$secure = Api POST "/secure/session/login" @{ username = "alice" }
Api GET "/secure/session/profile" $null @{ 'X-Session-Token' = $secure.token }

$danger = '{"$type":"AppSecWorkshop03.Serialization.DangerousAction, AppSecWorkshop03","FileName":"owned-by-deserialization.txt","Content":"Payload deserialize"}'
Invoke-RestMethod -Method Post -Uri "$BaseUrl/vuln/deserialization/execute" -ContentType "application/json" -Body $danger
Api POST "/secure/deserialization/execute" @{ action = "echo"; message = "safe payload" }

Api GET "/vuln/idor/orders/1002?username=alice"
try { Api GET "/secure/idor/orders/1002?username=alice" } catch { Write-Host $_.Exception.Message }
Api GET "/secure/idor/orders/1002?username=bob"
