# Playbook Proxy + Capture reseau

## 1) Demarrage API

```powershell
dotnet run --project .\PerimeterValidationLab\PerimeterValidationLab.csproj
```

## 2) Interception proxy (Burp ou OWASP ZAP)

1. Configurer le navigateur sur le proxy local (`127.0.0.1:8080`).
2. Appeler:
   - `GET /vuln/links/reset-password?user=alice`
3. Modifier les headers:
   - `X-Forwarded-Host: evil.example`
   - `X-Forwarded-Proto: http`
4. Observer la reponse: lien reset empoisonne.
5. Rejouer sur `/secure/links/reset-password?user=alice` et observer le blocage.

## 3) Capture Wireshark / tcpdump

Filtres Wireshark utiles:

- `http.host contains "contoso"`
- `http.request.header.name == "X-Forwarded-Host"`
- `ip.addr == 127.0.0.1 and tcp.port == 5110`

Exemple tcpdump (Linux):

```bash
sudo tcpdump -i any -A 'tcp port 5110'
```

## 4) Attendus pedagogiques

- Un header non fiable peut modifier liens, tenant, cache et routage.
- Les headers `X-Forwarded-*` ne doivent etre pris en compte que depuis un proxy de confiance.
- L'origin externe doit etre canonique (host allowlist + https).
