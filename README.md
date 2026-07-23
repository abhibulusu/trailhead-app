# Trailhead

**Title:** Trailhead — Gym Workout Builder
**Start & End Date:** July 2026 – Present
**Project URL:** https://github.com/abhibulusu/trailhead-app

## Description

Trailhead is a mobile-first web app that takes the guesswork out of walking into an unfamiliar gym. Users tap the equipment they see on the floor (or snap a photo and let the camera scan identify it), pick a focus for the day (full body, upper body, lower body, or just getting moving), and Trailhead builds a guided workout route with step-by-step instructions and form tips for each exercise. A visual trail indicator tracks progress through the flow, and a "My Gym" feature saves a user's usual equipment locally so they can jump straight to building a workout on their next visit.

## Tech

The frontend ([trailhead-fitness.html](trailhead-fitness.html)) is a single-file HTML/CSS/JavaScript progressive web app (PWA) — no build step required. Equipment and workout data are defined client-side, with workout selection logic that mixes exercises by movement pattern (squat, hinge, push, pull, core, cardio) based on the user's goal and available equipment.

The optional camera-scan feature ("Scan equipment with camera" on step 1) is backed by a small ASP.NET Core minimal API — [TrailheadApi](TrailheadApi) — that proxies the photo to the Claude vision API to identify the equipment's brand and machine type, then maps it to one of Trailhead's known equipment ids. This exists because sending photos straight to a vision API from client-side JS would expose the API key; manual equipment tapping still works with zero backend if you don't run the API.

### Running the camera-scan backend

```
cd TrailheadApi
dotnet user-secrets set "Anthropic:ApiKey" "sk-ant-..."
dotnet run --launch-profile https
```

The API listens on `https://localhost:7171` by default (see [TrailheadApi/Properties/launchSettings.json](TrailheadApi/Properties/launchSettings.json)). If you change the port or deploy it elsewhere, update `API_BASE` near the top of the `<script>` block in [trailhead-fitness.html](trailhead-fitness.html).
