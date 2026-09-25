#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Generate TypeScript types from packages/api-client/openapi.json into src/generated/.

.DESCRIPTION
  Uses openapi-typescript (CONTRACT section 9.1). Does not require a running API —
  regenerate openapi.json first via scripts/export-openapi.ps1 when the API surface changes.
#>
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root

$openapi = Join-Path $root "packages/api-client/openapi.json"
if (-not (Test-Path $openapi)) {
  throw "Missing $openapi — run scripts/export-openapi.ps1 first."
}

$outDir = Join-Path $root "packages/api-client/src/generated"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$outFile = Join-Path $outDir "schema.d.ts"

pnpm --filter @fee/api-client exec openapi-typescript "../../packages/api-client/openapi.json" -o "./src/generated/schema.d.ts"
if ($LASTEXITCODE -ne 0) {
  # Fallback: npx from repo root
  pnpm dlx openapi-typescript $openapi -o $outFile
}

Write-Host "Generated $outFile"
