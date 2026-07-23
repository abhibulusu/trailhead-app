# TrailheadApi

Small ASP.NET Core minimal API that backs the camera-scan feature in [trailhead-fitness.html](../trailhead-fitness.html). It exists solely to keep the Anthropic API key off the client — the frontend stays a static, buildless PWA.

## Setup

```
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
dotnet run --launch-profile https
```

Runs on `https://localhost:7171` / `http://localhost:5218` in dev (see `Properties/launchSettings.json`).

Never put the API key in `appsettings.json` — use `dotnet user-secrets` locally, and an environment variable (`ANTHROPIC_API_KEY`) or your host's secret store in production.

## Endpoint

`POST /api/identify-equipment` — `multipart/form-data` with a `photo` field (JPEG/PNG/WebP, max 8 MB).

Response body:

```json
{
  "equipmentId": "legPress",
  "brand": "Life Fitness",
  "machineName": "Seated Leg Press",
  "confidence": "high",
  "note": "Looks like a standard plate-loaded leg press."
}
```

`equipmentId` is always one of the ids in `EquipmentCatalog.KnownIds` (kept in sync with the `EQUIPMENT` array in `trailhead-fitness.html`) or `"unknown"` if the model can't place it.

## CORS

With no `AllowedOrigins` configured, the API allows any origin — fine for local development where the frontend may be opened as a `file://` page. Before deploying publicly, set `AllowedOrigins` in `appsettings.json` (or an environment-specific override) to the real origin(s) serving `trailhead-fitness.html`.
