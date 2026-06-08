# Multi-stage Dockerfile for OutingApi (ASP.NET Core)
# Uses .NET 10 SDK/runtime images — adjust tags if needed for your environment

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["API/api.csproj", "API/"]
RUN dotnet restore "API/api.csproj"

# Copy the rest of the sources
COPY . .
WORKDIR /src/API

# Publish the app
RUN dotnet publish "api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Ensure OTP templates are copied into the final image
COPY Repositories/Templates ./Templates

# Install CA certificates, openssl and Kerberos GSSAPI libs so TLS and native
# drivers (libgssapi_krb5) work inside the container.
RUN apt-get update \
	&& apt-get install -y --no-install-recommends \
	   ca-certificates \
	   openssl \
	   libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*

# Don't hardcode the port; use the Render-provided $PORT at runtime.
# We provide a sensible default of 5000 if PORT is not set.
ENV PORT=5000
EXPOSE 5000

# Use a shell entrypoint so the container will substitute $PORT at runtime.
# This runs the app and tells Kestrel to bind to the provided port.
ENTRYPOINT ["sh", "-c", "exec dotnet api.dll --urls http://+:${PORT}"]
