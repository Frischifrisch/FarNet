
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using FarNet;

namespace PowerShellFar.Commands
{
	sealed class FindFarFileCommand : BaseCmdlet
	{
		[Parameter(Position = 0, Mandatory = true, ParameterSetName = "Name")]
		public string Name { get; set; }
		[Parameter(Position = 0, Mandatory = true, ParameterSetName = "Where")]
		public ScriptBlock Where { get; set; }
		[Parameter(ParameterSetName = "Where")]
		public SwitchParameter Up { get; set; }
		protected override void BeginProcessing()
		{
			if (Name != null)
			{
				bool found = Far.Api.Panel.GoToName(Name, false);
				if (!found)
					WriteError(new ErrorRecord(
						new FileNotFoundException("File is not found: '" + Name + "'."),
						"FileNotFound",
						ErrorCategory.ObjectNotFound,
						Name));
			}
			else
			{
				var files = Far.Api.Panel.Files;
				int current = Far.Api.Panel.CurrentIndex;
				int count = files.Count;

				int step;
				int[] beg;
				int[] end;
				if (Up)
				{
					step = -1;
					beg = new int[] { current - 1, count - 1 };
					end = new int[] { -1, current - 1 };
				}
				else
				{
					step = 1;
					beg = new int[] { current + 1, 0 };
					end = new int[] { count, current + 1 };
				}

				for (int pass = 0; pass < 2; ++pass)
				{
					for (int index = beg[pass]; index != end[pass]; index += step)
					{
						var result = PS2.InvokeWithContext(Where, files[index]);
						if (result.Count == 0)
							continue;

						if (result.Count > 1 || LanguagePrimitives.IsTrue(result[0]))
						{
							Far.Api.Panel.Redraw(index, -1);
							return;
						}
					}
				}

				WriteError(new ErrorRecord(
					new FileNotFoundException("File is not found: {" + Where + "}."),
					"FileNotFound",
					ErrorCategory.ObjectNotFound,
					Where));
			}
		}
	}
}
