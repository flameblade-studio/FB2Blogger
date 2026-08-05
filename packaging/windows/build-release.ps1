[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDir,

    [Parameter(Mandatory = $true)]
    [string]$OutputDir,

    [Parameter(Mandatory = $true)]
    [string]$AppVersion,

    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$publishPath = (Resolve-Path -LiteralPath $PublishDir).Path
$repositoryPath = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$sourceExecutable = Join-Path $publishPath 'FB2Blogger.exe'
$sourceLicense = Join-Path $repositoryPath 'LICENSE'
if (-not (Test-Path -LiteralPath $sourceExecutable -PathType Leaf)) {
    throw "Published Windows executable is missing: $sourceExecutable"
}
if (-not (Test-Path -LiteralPath $sourceLicense -PathType Leaf)) {
    throw "MIT license is missing: $sourceLicense"
}
if ($AppVersion -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "Invalid application version: $AppVersion"
}

$outputPath = [System.IO.Path]::GetFullPath($OutputDir)
[System.IO.Directory]::CreateDirectory($outputPath) | Out-Null
$artifactPath = Join-Path $outputPath 'FB2Blogger.exe'
$licensePath = Join-Path $outputPath 'FB2Blogger-Windows-x64-LICENSE.txt'
Copy-Item -LiteralPath $sourceExecutable -Destination $artifactPath -Force
Copy-Item -LiteralPath $sourceLicense -Destination $licensePath -Force
if (-not (Select-String -LiteralPath $licensePath -SimpleMatch 'Permission is hereby granted' -Quiet)) {
    throw "The packaged Windows license is not a readable MIT license: $licensePath"
}

$runnerTemp = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$workPath = Join-Path $runnerTemp ("fb2blogger-windows-package-" + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($workPath) | Out-Null
try {
    $markerPath = Join-Path $workPath 'launch-smoke.txt'
    $process = Start-Process `
        -FilePath $artifactPath `
        -ArgumentList @('--package-smoke-test', $markerPath) `
        -PassThru `
        -Wait `
        -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        $diagnostic = if (Test-Path -LiteralPath $markerPath) { Get-Content -LiteralPath $markerPath -Raw } else { 'No diagnostic marker was written.' }
        throw "Packaged Windows executable failed its native-window smoke test with exit code $($process.ExitCode): $diagnostic"
    }
    $marker = Get-Content -LiteralPath $markerPath -Raw
    if (-not $marker.StartsWith('FB2BLOGGER_WINDOWS_PACKAGE_SMOKE_OK', [StringComparison]::Ordinal)) {
        throw "Packaged Windows executable did not produce the expected smoke marker: $marker"
    }
    Write-Output $marker

    $hash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText(
        "$artifactPath.sha256",
        "$hash  FB2Blogger.exe`n",
        [System.Text.UTF8Encoding]::new($false)
    )

    $toolPath = Join-Path $workPath 'sbom-tool'
    & dotnet tool install Microsoft.Sbom.DotNetTool --tool-path $toolPath --version 4.1.5
    if ($LASTEXITCODE -ne 0) {
        throw "Microsoft SBOM tool installation failed with exit code $LASTEXITCODE."
    }
    $manifestPath = Join-Path $workPath 'manifest'
    [System.IO.Directory]::CreateDirectory($manifestPath) | Out-Null
    $sbomTool = Join-Path $toolPath 'sbom-tool.exe'
    & $sbomTool generate `
        -b $publishPath `
        -bc $repositoryPath `
        -pn 'FB2Blogger Windows Release Candidate' `
        -pv $AppVersion `
        -ps 'Flameblade Studio' `
        -nsb 'https://github.com/hitoshic1982/FB2Blogger' `
        -m $manifestPath
    if ($LASTEXITCODE -ne 0) {
        throw "Microsoft SBOM generation failed with exit code $LASTEXITCODE."
    }
    $manifests = @(Get-ChildItem -LiteralPath $manifestPath -Recurse -File -Filter 'manifest.spdx.json')
    if ($manifests.Count -ne 1) {
        throw "Expected one SPDX manifest, found $($manifests.Count)."
    }
    $sbomPath = "$artifactPath.spdx.json"
    Copy-Item -LiteralPath $manifests[0].FullName -Destination $sbomPath -Force
    Get-Content -LiteralPath $sbomPath -Raw | ConvertFrom-Json | Out-Null
    if ((Get-Item -LiteralPath $sbomPath).Length -ge 16MB) {
        throw 'SBOM exceeds the GitHub attestation size limit.'
    }
}
finally {
    if (Test-Path -LiteralPath $workPath) {
        Remove-Item -LiteralPath $workPath -Recurse -Force
    }
}

Write-Output "Created $artifactPath with its readable MIT license, SHA256, and SPDX SBOM."
