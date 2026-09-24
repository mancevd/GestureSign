# Restores NuGet packages and local .NET Framework reference assemblies for MSBuild.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$nuget = Join-Path $root ".tools\nuget.exe"
New-Item -ItemType Directory -Force -Path (Split-Path $nuget) | Out-Null
if (-not (Test-Path $nuget)) {
    Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nuget
}

& $nuget restore (Join-Path $root "GestureSign.sln") -NonInteractive
foreach ($id in @(
    "Microsoft.NETFramework.ReferenceAssemblies.net45",
    "Microsoft.NETFramework.ReferenceAssemblies.net452",
    "Microsoft.NETFramework.ReferenceAssemblies.net46",
    "Microsoft.NETFramework.ReferenceAssemblies.net461",
    "Microsoft.Windows.SDK.Contracts",
    "System.Runtime.WindowsRuntime"
)) {
    & $nuget install $id -OutputDirectory (Join-Path $root "packages") -NonInteractive
}

$netfx = Join-Path $root ".ref-assemblies\.NETFramework"
New-Item -ItemType Directory -Force -Path $netfx | Out-Null
$map = @{
    "v4.5"   = "packages\Microsoft.NETFramework.ReferenceAssemblies.net45.1.0.3\build\.NETFramework\v4.5"
    "v4.5.2" = "packages\Microsoft.NETFramework.ReferenceAssemblies.net452.1.0.3\build\.NETFramework\v4.5.2"
    "v4.6"   = "packages\Microsoft.NETFramework.ReferenceAssemblies.net46.1.0.3\build\.NETFramework\v4.6"
    "v4.6.1" = "packages\Microsoft.NETFramework.ReferenceAssemblies.net461.1.0.3\build\.NETFramework\v4.6.1"
}
foreach ($name in $map.Keys) {
    $link = Join-Path $netfx $name
    $target = Join-Path $root $map[$name]
    if (-not (Test-Path $target)) { throw "Missing $target. Re-run setup.ps1 after nuget install." }
    if (Test-Path $link) { cmd /c "rmdir `"$link`"" | Out-Null }
    cmd /c "mklink /J `"$link`" `"$target`"" | Out-Null
}

Write-Host "Setup complete. Build with:"
Write-Host '  msbuild GestureSign.sln /p:Configuration=Debug /p:Platform="Any CPU"'
