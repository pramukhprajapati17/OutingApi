# Deploying `API` to Render using Docker

This project is set up to deploy to Render using a Dockerfile located at `API/Dockerfile`.

Quick local build/test:

```powershell
# build image from repo root
docker build -f API/Dockerfile -t outingapi:latest .

# run locally, map port 5000
docker run --rm -p 5000:5000 outingapi:latest
```

Render setup (Docker):

- Create a new **Web Service** on Render.
- Choose **Docker** as the environment and set the Dockerfile path to `API/Dockerfile`.
- Set the **Start Command** to empty (the Dockerfile's ENTRYPOINT is used).
- In the Environment settings, set `PORT` (optional) and ensure Render routes to port `5000` (the container exposes `5000`).
- Add any environment variables required by your app (for example `ASPNETCORE_ENVIRONMENT=Production`).

Notes:
- If your project targets a different .NET version in your environment, update the `mcr.microsoft.com/dotnet/sdk:10.0` and `aspnet:10.0` tags in the `Dockerfile` accordingly.
- Render will run `docker build` using the repository root as the build context; the Dockerfile copies the `API` project and restores/publishes from it.
