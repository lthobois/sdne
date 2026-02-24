param([string]$BaseUrl = "http://localhost:5102")

function Api($Method, $Path, $Body = $null, $Headers = @{}, [switch]$Raw) {
  Write-Host "`n=== $Method $Path ==="
  $uri = "$BaseUrl$Path"
  if ($Raw) {
    (Invoke-WebRequest -Method $Method -Uri $uri -Headers $Headers).Content
  } elseif ($Body -ne $null) {
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers -ContentType "application/json" -Body ($Body | ConvertTo-Json)
  } else {
    Invoke-RestMethod -Method $Method -Uri $uri -Headers $Headers
  }
}

Api GET "/vuln/sql/users?username=' OR 1=1 --"
Api GET "/secure/sql/users?username=' OR 1=1 --"
Api GET "/vuln/xss?input=<script>alert('xss')</script>" -Raw
Api GET "/secure/xss?input=<script>alert('xss')</script>" -Raw

$login = Api POST "/auth/login" @{ username = "alice" }
$session = (Invoke-WebRequest -Method Post -Uri "$BaseUrl/auth/login" -ContentType "application/json" -Body '{"username":"alice"}')
$cookie = ($session.Headers['Set-Cookie'] -split ';')[0]
$csrf = ($session.Content | ConvertFrom-Json).csrfToken

Api POST "/vuln/csrf/transfer" @{ to = "mallory"; amount = 100 } @{ Cookie = $cookie }
try { Api POST "/secure/csrf/transfer" @{ to = "mallory"; amount = 100 } @{ Cookie = $cookie } } catch { Write-Host $_.Exception.Message }
Api POST "/secure/csrf/transfer" @{ to = "mallory"; amount = 100 } @{ Cookie = $cookie; 'X-CSRF-Token' = $csrf }

Api GET "/vuln/ssrf/fetch?url=http://example.com"
try { Api GET "/secure/ssrf/fetch?url=http://localhost:5102" } catch { Write-Host $_.Exception.Message }
Api GET "/secure/ssrf/fetch?url=https://jsonplaceholder.typicode.com/todos/1"
