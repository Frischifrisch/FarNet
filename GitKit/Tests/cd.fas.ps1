﻿
job {
	$Far.Panel.CurrentDirectory = $PSScriptRoot
}

job {
	$Far.InvokeCommand('gk:cd')
}
job {
	Assert-Far $Far.Panel.CurrentDirectory -eq $env:FarNetCode
}

job {
	$Far.InvokeCommand('gk:cd path=.git')
}
job {
	Assert-Far $Far.Panel.CurrentDirectory -eq $env:FarNetCode\.git
}

job {
	$Far.InvokeCommand('gk:cd path=.git\/objects')
}
job {
	Assert-Far $Far.Panel.CurrentDirectory -eq $env:FarNetCode\.git\objects
}

job {
	$Far.InvokeCommand('gk:cd path=GitKit\Tests')
}
job {
	Assert-Far $Far.Panel.CurrentDirectory -eq $PSScriptRoot
}
