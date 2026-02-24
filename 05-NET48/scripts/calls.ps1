param([string]$BaseUrl = "http://localhost:5105")

function Api($Path) {
  Write-Host "`n=== GET $Path ==="
  Invoke-RestMethod -Method Get -Uri "$BaseUrl$Path"
}

Api "/vuln/xss?input=<script>alert(1)</script>"
Api "/secure/xss?input=<script>alert(1)</script>"
try { Api "/secure/open-redirect?returnUrl=https://evil.example" } catch { Write-Host $_.Exception.Message }
Api "/secure/open-redirect?returnUrl=/ok"
Invoke-RestMethod -Method Post -Uri "$BaseUrl/secure/register" -ContentType "application/json" -Body '{"username":"alice","password":"weak"}'
Invoke-RestMethod -Method Post -Uri "$BaseUrl/secure/register" -ContentType "application/json" -Body '{"username":"alice.secure","password":"S3cure!Password"}'
