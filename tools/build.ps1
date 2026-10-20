<#
.SYNOPSIS
    Build TerrariaXDK, run the headless self-test and assemble a portable distribution.

.DESCRIPTION
    One command for the whole pipeline, so a release never skips a step:

        tools\build.ps1                 # build + self-test
        tools\build.ps1 -Pack           # ... + dist/TerrariaXDK/ ready to copy anywhere
        tools\build.ps1 -Pack -Zip      # ... + dist/TerrariaXDK-<date>.zip

    The self-test needs no GPU, so it runs everywhere. A graphical smoke test does not -
    that still has to be done by hand on a machine with OpenGL 2.1+.

.PARAMETER Configuration
    Debug or Release. Default: Release.

.PARAMETER SkipTests
    Build only. Use for a quick syntax check; never for a release.

.PARAMETER Pack
    Assemble dist/TerrariaXDK: exe, native DLLs, Content and the Settings.ini template.

.PARAMETER Zip
    Also write dist/TerrariaXDK-<yyyyMMdd>.zip. Implies -Pack.
#>
[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',
	[switch]$SkipTests,
	[switch]$Pack,
	[switch]$Zip
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'TerrariaXDK.csproj'
$output = Join-Path $root "bin\$Configuration\net40"
$nativeNames = @('FAudio.dll', 'FNA3D.dll', 'libtheorafile.dll', 'SDL2.dll')

function Fail([string]$message)
{
	throw "$message (see the output above)"
}

# The FNA native libraries and Content are not versioned, so the build does not produce
# them. Stage them next to the executable: without SDL2.dll even --selftest dies in the
# FNA platform static constructor.
function Get-NativeDirectory
{
	$candidates = @(
		$env:TXDK_NATIVE_DIR,
		(Join-Path $root 'native\win64'),
		(Join-Path $root 'native'),
		(Join-Path $root 'bin\Debug\net40'),
		(Join-Path $root 'bin\Release\net40')
	)
	foreach ($candidate in $candidates)
	{
		if ($candidate -and (Test-Path (Join-Path $candidate 'SDL2.dll')))
		{
			return $candidate
		}
	}
	return $null
}

function Copy-Native([string]$destination)
{
	$native = Get-NativeDirectory
	if (-not $native)
	{
		Write-Warning 'native FNA libraries not found - set TXDK_NATIVE_DIR to the folder with SDL2.dll/FNA3D.dll'
		return
	}
	foreach ($name in $nativeNames)
	{
		$target = Join-Path $destination $name
		$source = Join-Path $native $name
		if (-not (Test-Path $target) -and (Test-Path $source))
		{
			Copy-Item $source $destination
		}
	}
}

function Get-ContentSource
{
	$candidates = @(
		(Join-Path $root 'Content'),
		(Join-Path $root '..\Content'),
		(Join-Path $root 'bin\Debug\net40\Content')
	)
	foreach ($candidate in $candidates)
	{
		if ((Test-Path (Join-Path $candidate 'Fonts')) -and (Test-Path (Join-Path $candidate 'Images')))
		{
			return (Resolve-Path $candidate).Path
		}
	}
	return $null
}

# A junction keeps the 277 MB Content tree in one place instead of copying it per build.
function Link-Content([string]$destination)
{
	$target = Get-ContentSource
	if (-not $target)
	{
		Write-Warning 'Content folder not found - the self-test will report it as missing'
		return
	}
	$link = Join-Path $destination 'Content'
	if (Test-Path $link)
	{
		return
	}
	try
	{
		New-Item -ItemType Junction -Path $link -Target $target | Out-Null
	}
	catch
	{
		Copy-Item $target $link -Recurse
	}
}

Push-Location $root
try
{
	Write-Host "== build ($Configuration)" -ForegroundColor Cyan
	& dotnet build $project -c $Configuration -v q --nologo
	if ($LASTEXITCODE -ne 0)
	{
		Fail 'build failed'
	}

	$exe = Join-Path $output 'TerrariaXDK.exe'
	if (-not (Test-Path $exe))
	{
		Fail "the build produced no executable at $exe"
	}

	Copy-Native $output
	Link-Content $output

	if (-not $SkipTests)
	{
		Write-Host '== self-test' -ForegroundColor Cyan
		$log = Join-Path $env:TEMP 'terrariaxdk-selftest.log'
		$err = Join-Path $env:TEMP 'terrariaxdk-selftest.err'
		# The game is a WinExe, so its stdout only exists when redirected to a file.
		$process = Start-Process -FilePath $exe -ArgumentList '--selftest' -NoNewWindow -Wait -PassThru `
			-RedirectStandardOutput $log -RedirectStandardError $err
		if (Test-Path $log)
		{
			Get-Content $log | Where-Object { $_ -match 'Passed|Failed|not ok|FAIL' }
		}
		if ($process.ExitCode -ne 0)
		{
			if (Test-Path $err)
			{
				Get-Content $err | Select-Object -First 40
			}
			Fail "self-test failed (exit code $($process.ExitCode))"
		}
		Remove-Item $log, $err -ErrorAction SilentlyContinue
	}

	if ($Pack -or $Zip)
	{
		Write-Host '== pack' -ForegroundColor Cyan
		$dist = Join-Path $root 'dist\TerrariaXDK'
		if (Test-Path $dist)
		{
			Remove-Item $dist -Recurse -Force
		}
		New-Item -ItemType Directory -Path $dist -Force | Out-Null

		foreach ($name in (@('TerrariaXDK.exe', 'TerrariaXDK.exe.config') + $nativeNames))
		{
			$source = Join-Path $output $name
			if (Test-Path $source)
			{
				Copy-Item $source $dist
			}
			elseif ($name -like '*.dll')
			{
				Write-Warning "$name is missing from $output - the game will not start without it"
			}
		}

		$content = Get-ContentSource
		if ($content)
		{
			Write-Host "   Content <- $content"
			Copy-Item $content (Join-Path $dist 'Content') -Recurse
		}

		$settings = Join-Path $root 'Settings.ini'
		if (Test-Path $settings)
		{
			Copy-Item $settings $dist
		}

		Write-Host "   $dist" -ForegroundColor Green

		if ($Zip)
		{
			$archive = Join-Path $root ('dist\TerrariaXDK-' + (Get-Date -Format 'yyyyMMdd') + '.zip')
			if (Test-Path $archive)
			{
				Remove-Item $archive -Force
			}
			Write-Host '== zip' -ForegroundColor Cyan
			Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $archive
			Write-Host "   $archive" -ForegroundColor Green
		}
	}

	Write-Host '== ok' -ForegroundColor Green
}
finally
{
	Pop-Location
}
