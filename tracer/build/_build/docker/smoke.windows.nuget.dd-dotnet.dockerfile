﻿ARG DOTNETSDK_VERSION
ARG RUNTIME_IMAGE

# Build the ASP.NET Core app using the latest SDK
FROM mcr.microsoft.com/dotnet/sdk:$DOTNETSDK_VERSION-windowsservercore-ltsc2022 as builder

# Without this, the container doesn't have permission to add the new package 
USER ContainerAdministrator

# Build the smoke test app
WORKDIR /src
COPY ./test/test-applications/regression/AspNetCoreSmokeTest/ .

ARG PUBLISH_FRAMEWORK
ARG TOOL_VERSION
RUN dotnet restore "AspNetCoreSmokeTest.csproj" \
    && dotnet nuget add source "c:\src\artifacts" \
    && dotnet add package "Datadog.Trace.Bundle" --version %TOOL_VERSION% \
    && dotnet publish "AspNetCoreSmokeTest.csproj" -c Release --framework %PUBLISH_FRAMEWORK% -o "c:\src\publish"

FROM $RUNTIME_IMAGE AS publish-msi
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

WORKDIR /app

ARG CHANNEL_32_BIT
RUN if($env:CHANNEL_32_BIT){ \
    echo 'Installing x86 dotnet runtime ' + $env:CHANNEL_32_BIT; \
    curl 'https://raw.githubusercontent.com/dotnet/install-scripts/2bdc7f2c6e00d60be57f552b8a8aab71512dbcb2/src/dotnet-install.ps1' -o dotnet-install.ps1; \
    ./dotnet-install.ps1 -Architecture x86 -Runtime aspnetcore -Channel $env:CHANNEL_32_BIT -InstallDir c:\cli; \
    [Environment]::SetEnvironmentVariable('Path',  'c:\cli;' + $env:Path, [EnvironmentVariableTarget]::Machine); \
    rm ./dotnet-install.ps1; }

RUN mkdir /logs

# Set the required env vars
ENV DD_TRACE_LOG_DIRECTORY="C:\logs" \
    DD_REMOTE_CONFIGURATION_ENABLED=0 \
    ASPNETCORE_URLS=http://localhost:5000

# Set a random env var we should ignore
ENV SUPER_SECRET_CANARY=MySuperSecretCanary

# see https://github.com/DataDog/dd-trace-dotnet/pull/3579
ENV DD_INTERNAL_WORKAROUND_77973_ENABLED=1

# We need to be able to override this for the crash tracking tests,
# so we set it globally here instead of forcing it in the entrypoint
ENV DD_PROFILING_ENABLED=1


# Copy the app across
COPY --from=builder /src/publish /app/.

ENTRYPOINT ["/app/datadog/dd-dotnet.cmd", "run", "--set-env", "DD_APPSEC_ENABLED=1","--set-env", "DD_TRACE_DEBUG=1", "--",  "dotnet", "/app/AspNetCoreSmokeTest.dll"]