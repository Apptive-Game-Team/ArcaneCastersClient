param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('win-x64', 'linux-x64', 'osx-x64', 'osx-arm64')]
    [string]$Platform,
    [Parameter(Mandatory = $true)]
    [string]$Output
)
$ErrorActionPreference = 'Stop'
$destination = [IO.Path]::GetFullPath($Output)
if ((Test-Path -LiteralPath $destination) -and
    (Get-ChildItem -LiteralPath $destination -Force | Select-Object -First 1)) {
    throw 'Use an empty output directory for the updater.'
}
dotnet publish (Join-Path $PSScriptRoot 'DesktopUpdater.csproj') -c Release -r $Platform `
    --self-contained true -o $destination
if ($LASTEXITCODE -ne 0) { throw 'Updater publish failed.' }
[IO.File]::WriteAllText((Join-Path $destination 'updater-platform.txt'), $Platform)
