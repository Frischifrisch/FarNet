
<#
.Synopsis
	Invokes a file from the current editor.
	Author: Roman Kuzmin

.Description
	Saves a file in the editor and invokes it depending on the file type.

	If a file is *-.ps1 it is executed in the current PowerShell session by
	$Psf.InvokeScriptFromEditor() with $ErrorActionPreference = 'Inquire'

	If a file is *.ps1 it is invoked by PowerShell.exe outside of Far. When it
	is done you can watch the console output and close the window by [Enter].
	If it fails the PowerShell is not exited, but stopped, you may work in
	failed PowerShell session to investigate problems just in place.

	Markdown files are opened by Show-Markdown-.ps1

	*.*proj files are processed by Start-MSBuild-.ps1

	If a file is .bat, .cmd, .pl, .mak, makefile, etc. then some typical action
	is executed, mostly as demo, use your own invocation for practical tasks.

	As for the other files, the script simply calls Invoke-Item for them, i.e.
	starts a program associated with a file type.
#>

# Save the file and get the path
$editor = $Psf.Editor()
$path = $editor.FileName

### Invoke by PowerShellFar in the current session
if ($path -like '*-.ps1') {
	$ErrorActionPreference = 'Inquire'
	$Psf.InvokeScriptFromEditor()
	return
}

# Commit
$editor.Save()

# Extension
$ext = [IO.Path]::GetExtension($path)

### PowerShell in external window and return
if ($ext -eq '.ps1') {
	[Diagnostics.Process]::Start('powershell.exe', "-NoExit . '$($path.Replace("'", "''"))'")
	return
}

### MSBuild
if ($ext -like '.*proj') {
	Start-MSBuild- $path
	return
}

$arg = "`"$path`""

### Markdown
if ('.text', '.md', '.markdown' -contains $ext) {
	Show-Markdown-.ps1
}

### Cmd
elseif ('.bat', '.cmd' -contains $ext) {
	cmd /c start cmd /k $arg
}

### Perl
elseif ('.pl' -eq $ext) {
	cmd /c start cmd /k perl $arg
}

### Makefile
elseif ('.mak' -eq $ext -or [IO.Path]::GetFileName($path) -eq 'makefile') {
	cmd /c start cmd /k nmake /f $arg /nologo
}

### Others
else {
	Invoke-Item -LiteralPath $path
}
