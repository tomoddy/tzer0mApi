# tzer0mApi

[![Deploy](https://github.com/tzer0m/tzer0mApi/actions/workflows/deploy.yml/badge.svg)](https://github.com/tzer0m/tzer0mApi/actions/workflows/deploy.yml)

A backend API that groups together a handful of otherwise-unrelated self-hosted services into a single ASP.NET Core Web API, rather than standing up a separate app for each one.

## Modules

- **Hours** — proxies a Clockify summary report for the current month, used by [`hours.py`](hours.py) (see below) to show hours worked vs. a monthly target.
- **SmarterMeter** — receives a captured meter photo (by filename, read from a shared NAS path), runs it through Google Cloud Vision OCR, extracts the reading, and prices it against configured tariff periods. Backs a companion meter-reading dashboard and its Home Assistant integration.
- **StockWise** — `Items` / `Stock` / `Storage` endpoints, backed by EF Core + Postgres, powering a companion .NET MAUI inventory app.
- **Ting** — sends push notifications via Firebase Cloud Messaging, exposes an endpoint to update the stored FCM token, and receives Uptime Kuma's webhook payload to relay monitor alerts as pushes.
- **Weather** — UV index/forecast lookups via the OpenUV API.

## Security

Requests to configured private paths require an `X-API-Key` header, checked by `ApiKeyMiddleware` against SHA-256 hashes stored in Postgres (only the hash is ever persisted). New keys are minted with the companion `KeyGenerator` console tool, which generates a GUID, hashes it, stores the hash, and prints the raw key once — it can't be retrieved again after that.

```
KeyGenerator <key-name>
```

## Tech Stack

- ASP.NET Core Web API on .NET 10, with Swagger/OpenAPI at the root
- EF Core + Npgsql (StockWise data)
- FirebaseAdmin / Google.Apis.Auth (push notifications, Vision OCR)
- Health checks at `/health` (Postgres connectivity)

## Configuration

Configuration lives in `appsettings.json` (see `appsettingsGit.json` for the shape, values stripped):

```json
{
  "ConnectionStrings": {
    "Robert1": "",
    "SmarterMeter": "",
    "StockWise": ""
  },
  "Authentication": {
    "PrivatePaths": [ "/ting", "/ting/update", "/weather/uv/notify", "/smartermeter/*", "stockwise/*" ]
  },
  "Hours": { "WorkspaceId": "", "ApiKey": "" },
  "Weather": { "OpenUV": { "BaseUrl": "https://api.openuv.io/api/v1/forecast", "ApiKey": "" } },
  "RssApiKey": "",
  "SmarterMeter": {
    "Storage": { "CapturePath": "" },
    "GoogleVision": { "ApiKey": "" },
    "Tariffs": [
      { "StartDate": "", "EndDate": "", "UnitRatePence": 0, "StandingChargePence": 0 }
    ]
  }
}
```

## hours.py

A companion script for a Raspberry Pi: an IR sensor triggers a request to `/Hours`, and the result is rendered on a 16x2 LCD alongside how many days ahead or behind a monthly hours target the tracked total is, with an optional buzzer confirmation. Requires `RPi.GPIO` and a wired IR receiver + LCD + buzzer.

## Deployment

Deployed via GitHub Actions on push to `master`, using a self-hosted runner. The workflow stops the `Api.service` systemd unit, publishes a fresh build, and restarts the service.
