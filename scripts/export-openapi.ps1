#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Export OpenAPI document from the API host (InMemory DB — no Postgres/Redis required).

.DESCRIPTION
  Runs the OpenApi export test with EXPORT_OPENAPI=1 and writes
  packages/api-client/openapi.json
#>
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$env:EXPORT_OPENAPI = "1"
dotnet test "$root/backend/tests/FeePlatform.Api.Tests/FeePlatform.Api.Tests.csproj" `
  -c Release `
  --filter "FullyQualifiedName~OpenApi_export_to_api_client_when_flag_set"

$out = Join-Path $root "packages/api-client/openapi.json"
if (-not (Test-Path $out)) {
  throw "Expected openapi.json at $out"
}
Write-Host "Wrote $out"
