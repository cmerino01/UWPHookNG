#!/usr/bin/env pwsh
# Pre-push hook: build (Release, warnings-as-errors via Directory.Build.props) and run tests.
# Refuses the push on failure.

$ErrorActionPreference = 'Stop'

Write-Host "[pre-push] dotnet build (Release)..."
dotnet build UWPHook.sln -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "[pre-push] dotnet test..."
dotnet test tests/UWPHook.Tests/UWPHook.Tests.csproj -c Release --no-build --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "[pre-push] OK"
exit 0
