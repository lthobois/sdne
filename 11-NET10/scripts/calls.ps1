param(
    [string]$BaseUrl = 'http://localhost:5111'
)

Write-Host "[11-NET10] Concepts"
Invoke-RestMethod -Uri "$BaseUrl/secure/crypto/concepts" -Method Get

Write-Host "[11-NET10] SHA-256"
Invoke-RestMethod -Uri "$BaseUrl/secure/hash/sha256" -Method Post -ContentType 'application/json' -Body (@{ input = 'atelier-11' } | ConvertTo-Json)

Write-Host "[11-NET10] AES roundtrip"
Invoke-RestMethod -Uri "$BaseUrl/secure/aes/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'message-secret' } | ConvertTo-Json)

Write-Host "[11-NET10] RSA roundtrip"
Invoke-RestMethod -Uri "$BaseUrl/secure/rsa/roundtrip" -Method Post -ContentType 'application/json' -Body (@{ message = 'rsa-secret' } | ConvertTo-Json)

Write-Host "[11-NET10] Cert self-signed"
Invoke-RestMethod -Uri "$BaseUrl/secure/cert/self-signed" -Method Post -ContentType 'application/json' -Body (@{ subject = 'CN=CryptoWorkshop11' } | ConvertTo-Json)
