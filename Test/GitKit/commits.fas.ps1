﻿
job {
	$Far.InvokeCommand("gk: panel=commits; repo=$PSScriptRoot")
}

job {
	Assert-Far $Far.Panel.GetType().Name -eq CommitsPanel
}

keys Enter

job {
	Assert-Far $Far.Panel.GetType().Name -eq ChangesPanel
}

keys ShiftEsc
