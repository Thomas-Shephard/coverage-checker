param(
    [string] $Configuration = "Release",
    [string] $PackageOutputPath = (Join-Path (Join-Path $PSScriptRoot "..") "nupkg"),
    [string] $SmokeRoot = (Join-Path ([System.IO.Path]::GetTempPath()) "coveragechecker-package-consumption-smoke-$([guid]::NewGuid().ToString('N'))"),
    [switch] $SkipPack
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solutionPath = Join-Path $repoRoot "CoverageChecker.slnx"
$packageOutputFullPath = if ([System.IO.Path]::IsPathRooted($PackageOutputPath)) {
    [System.IO.Path]::GetFullPath($PackageOutputPath)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $PackageOutputPath))
}
$smokeRootFullPath = if ([System.IO.Path]::IsPathRooted($SmokeRoot)) {
    [System.IO.Path]::GetFullPath($SmokeRoot)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $SmokeRoot))
}

function Invoke-DotNet {
    param([Parameter(ValueFromRemainingArguments = $true)] [string[]] $Arguments)

    Write-Output "dotnet $($Arguments -join ' ')"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Get-PackageVersion {
    param([string] $PackageId)

    $packageNamePattern = "^$([regex]::Escape($PackageId))\.(?<version>\d+\.\d+\.\d+(?:[-+][A-Za-z0-9.-]+)?)\.nupkg$"
    $packages = @(Get-ChildItem -Path $packageOutputFullPath -Filter "*.nupkg" |
        Where-Object { $_.Name -match $packageNamePattern } |
        Sort-Object LastWriteTimeUtc -Descending)

    if ($packages.Count -eq 0) {
        throw "Expected package '$PackageId' in '$packageOutputFullPath', but no .nupkg was found."
    }

    $fileName = $packages[0].Name
    if ($fileName -notmatch $packageNamePattern) {
        throw "Could not infer package version from '$fileName'."
    }

    return $Matches["version"]
}

if (-not $SkipPack) {
    New-Item -ItemType Directory -Force -Path $packageOutputFullPath | Out-Null
    Invoke-DotNet pack $solutionPath --configuration $Configuration --output $packageOutputFullPath
}

if (-not (Test-Path $packageOutputFullPath)) {
    throw "Package output path '$packageOutputFullPath' does not exist. Run dotnet pack first or omit -SkipPack."
}

$libraryVersion = Get-PackageVersion "CoverageChecker"
$toolVersion = Get-PackageVersion "CoverageChecker.CommandLine"
Write-Host "Using CoverageChecker $libraryVersion"
Write-Host "Using CoverageChecker.CommandLine $toolVersion"

if (Test-Path $smokeRootFullPath) {
    Remove-Item -LiteralPath $smokeRootFullPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $smokeRootFullPath | Out-Null

$smokeProjectPath = Join-Path $smokeRootFullPath "CoverageChecker.PackageSmoke"
Invoke-DotNet new console --framework net10.0 --output $smokeProjectPath --no-restore

$escapedPackageOutputPath = [System.Security.SecurityElement]::Escape($packageOutputFullPath)
$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-coveragechecker" value="$escapedPackageOutputPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
Set-Content -Path (Join-Path $smokeProjectPath "nuget.config") -Value $nugetConfig -Encoding utf8

$projectFile = Join-Path $smokeProjectPath "CoverageChecker.PackageSmoke.csproj"
[xml] $projectXml = Get-Content $projectFile
$itemGroup = $projectXml.CreateElement("ItemGroup")
$packageReference = $projectXml.CreateElement("PackageReference")
$packageReference.SetAttribute("Include", "CoverageChecker")
$packageReference.SetAttribute("Version", $libraryVersion)
[void] $itemGroup.AppendChild($packageReference)
[void] $projectXml.Project.AppendChild($itemGroup)
$projectXml.Save($projectFile)

$program = @'
using CoverageChecker;

var options = new CoverageAnalyserOptions
{
    GlobPatterns = ["coverage.cobertura.xml"],
};

_ = new CoverageAnalyser(options);

return 0;
'@
Set-Content -Path (Join-Path $smokeProjectPath "Program.cs") -Value $program -Encoding utf8

Invoke-DotNet restore $smokeProjectPath --configfile (Join-Path $smokeProjectPath "nuget.config")
Invoke-DotNet build $smokeProjectPath --configuration $Configuration --no-restore

$toolPath = Join-Path $smokeRootFullPath "tools"
Invoke-DotNet tool install CoverageChecker.CommandLine --tool-path $toolPath --version $toolVersion --add-source $packageOutputFullPath

$coverageFile = Join-Path $smokeRootFullPath "coverage.cobertura.xml"
@'
<?xml version="1.0" encoding="utf-8"?>
<coverage line-rate="1" branch-rate="1">
  <sources>
    <source>.</source>
  </sources>
  <packages>
    <package name="smoke" line-rate="1" branch-rate="1">
      <classes>
        <class name="Smoke" filename="Smoke.cs" line-rate="1" branch-rate="1">
          <lines>
            <line number="1" hits="1" branch="false" />
          </lines>
        </class>
      </classes>
    </package>
  </packages>
</coverage>
'@ | Set-Content -Path $coverageFile -Encoding utf8

$toolExecutable = Join-Path $toolPath "coveragechecker"
if ($IsWindows) {
    $toolExecutable = "$toolExecutable.exe"
}

$previousGitHubActions = $env:GITHUB_ACTIONS
$previousGitHubStepSummary = $env:GITHUB_STEP_SUMMARY
$toolExitCode = 0
try {
    Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue
    Remove-Item Env:GITHUB_STEP_SUMMARY -ErrorAction SilentlyContinue

    & $toolExecutable --format Cobertura --directory $smokeRootFullPath --glob-patterns "coverage.cobertura.xml" --line-threshold 100 --branch-threshold 100
    $toolExitCode = $LASTEXITCODE
}
finally {
    if ($null -eq $previousGitHubActions) {
        Remove-Item Env:GITHUB_ACTIONS -ErrorAction SilentlyContinue
    }
    else {
        $env:GITHUB_ACTIONS = $previousGitHubActions
    }

    if ($null -eq $previousGitHubStepSummary) {
        Remove-Item Env:GITHUB_STEP_SUMMARY -ErrorAction SilentlyContinue
    }
    else {
        $env:GITHUB_STEP_SUMMARY = $previousGitHubStepSummary
    }
}

if ($toolExitCode -ne 0) {
    throw "coveragechecker command failed with exit code $toolExitCode."
}

Write-Host "Package consumption smoke test passed."
