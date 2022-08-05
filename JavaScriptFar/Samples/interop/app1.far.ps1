﻿<#
.Synopsis
	Run JavaScript commands using JavaScriptFar interop.
#>

# step 1: get the function to run commands
$ModuleManager = $Far.GetModuleManager('JavaScriptFar')
$EvaluateCommand = $ModuleManager.Interop('EvaluateCommand', $null)

# step 2: run commands with parameters
$EvaluateCommand.Invoke('args.LiveObject', @{LiveObject = $Host})
