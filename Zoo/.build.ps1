
# Synopsis: Interactive release steps.
task release {
	#! save outside or it gets to zip
	$save = "$HOME\ReleaseFarNet.clixml"
	Build-Checkpoint $save @{Task = '*'; File = 'ReleaseFarNet.build.ps1'} -Resume:(Test-Path $save)
}

# Synopsis: Pack FarNet assets.
task nuget {
	Invoke-Build nuget $env:FarNetCode\.build.ps1
}

# Synopsis: Test FarNet assets.
task testNuGet {
	.\Test-Update-FarNet.ps1
}

# Synopsis: Zip FarDev sources on release.
task zipFarDev {
	. $env:FarNetCode\Get-Version.ps1
	$zip = "FarDev.$FarNetVersion-$PowerShellFarVersion.7z"

	Set-Location ..\..\..
	assert (Test-Path FarDev)

	if (Test-Path $zip) { Remove-Item $zip -Confirm }
	exec { & 7z.exe a $zip FarDev '-xr!.vs' '-xr!bin' '-xr!obj' '-xr!packages' }
}