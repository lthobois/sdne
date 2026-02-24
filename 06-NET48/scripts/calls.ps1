param([string]$BaseUrl = "http://localhost:5106")

function Api($Method, $Path, $Body = $null) {
  Write-Host "`n=== $Method $Path ==="
  if ($Body -ne $null) {
    Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path" -ContentType "application/json" -Body ($Body | ConvertTo-Json)
  }
  else {
    Invoke-RestMethod -Method $Method -Uri "$BaseUrl$Path"
  }
}

Api GET "/vuln/config/secret"
Api GET "/secure/config/secret"
Api GET "/vuln/outbound/fetch?url=http://example.com"
try { Api GET "/secure/outbound/fetch?url=http://example.com" } catch { Write-Host $_.Exception.Message }
Api GET "/secure/outbound/fetch?url=https://jsonplaceholder.typicode.com/todos/1"
Api POST "/vuln/dependency/approve" @{ packageId="Evil.Package"; sourceUrl="https://evil.example/feed/pkg.nupkg"; sha256="1234" }
try { Api POST "/secure/dependency/approve" @{ packageId="Evil.Package"; sourceUrl="https://evil.example/feed/pkg.nupkg"; sha256="1234" } } catch { Write-Host $_.Exception.Message }
Api POST "/secure/dependency/approve" @{ packageId="Polly"; sourceUrl="https://api.nuget.org/v3/index.json"; sha256="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
Api POST "/secure/dependency/sha256" @{ payload="package-content-v1" }
