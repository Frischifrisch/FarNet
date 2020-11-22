
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace PowerShellFar
{
	static class A
	{
		/// <summary>PowerShellFar actor.</summary>
		public static Actor Psf => _Psf_;
		static Actor _Psf_;
		public static void Connect(Actor psf)
		{
			_Psf_ = psf;
		}
		/// <summary>
		/// Shows an error.
		/// </summary>
		public static void Msg(Exception error)
		{
			Far.Api.Message(error.Message, "PowerShellFar error", MessageOptions.LeftAligned);
		}
		/// <summary>
		/// Shows a message.
		/// </summary>
		public static void Message(string message)
		{
			Far.Api.Message(message, Res.Me, MessageOptions.LeftAligned);
		}
		/// <summary>
		/// Shows a stop pipeline message.
		/// </summary>
		public static void AskStopPipeline()
		{
			if (0 == Far.Api.Message("Stop running commands? (like Ctrl-C in console)", Res.Me, MessageOptions.YesNo))
				throw new PipelineStoppedException();
		}
		/// <summary>
		/// Creates standard Far viewer ready for opening (F3)
		/// </summary>
		/// <param name="filePath">Existing file to view.</param>
		public static IViewer CreateViewer(string filePath)
		{
			IViewer view = Far.Api.CreateViewer();
			view.FileName = filePath;
			return view;
		}
		/// <summary>
		/// Sets an item property value as it is.
		/// </summary>
		public static void SetPropertyValue(string itemPath, string propertyName, object value)
		{
			//! setting PSPropertyInfo.Value is not working, so do use Set-ItemProperty
			InvokeCode(
				"Set-ItemProperty -LiteralPath $args[0] -Name $args[1] -Value $args[2] -ErrorAction Stop",
				itemPath, propertyName, value);
		}
		/// <summary>
		/// Writes invocation errors.
		/// </summary>
		public static void WriteErrors(TextWriter writer, IEnumerable errors)
		{
			if (errors == null)
				return;

			foreach (object error in errors)
			{
				writer.WriteLine("ERROR:");
				writer.WriteLine(error.ToString());

				ErrorRecord asErrorRecord = Cast<ErrorRecord>.From(error);
				if (asErrorRecord != null && asErrorRecord.InvocationInfo != null)
					writer.WriteLine(Kit.PositionMessage(asErrorRecord.InvocationInfo.PositionMessage));
			}
		}
		/// <summary>
		/// Writes invocation exception.
		/// </summary>
		public static void WriteException(TextWriter writer, Exception ex)
		{
			if (ex == null)
				return;

			writer.WriteLine("ERROR:");
			writer.WriteLine(ex.GetType().Name + ":");
			writer.WriteLine(ex.Message);

			if (ex is RuntimeException asRuntimeException && asRuntimeException.ErrorRecord != null && asRuntimeException.ErrorRecord.InvocationInfo != null)
				writer.WriteLine(Kit.PositionMessage(asRuntimeException.ErrorRecord.InvocationInfo.PositionMessage));
		}
		/// <summary>
		/// Sets an item content, shows errors.
		/// </summary>
		public static bool SetContentUI(string itemPath, string text)
		{
			//! PSF only, can do in console:
			//! as a script: if param name -Value is used, can't set e.g. Alias:\%
			//! as a block: same problem
			//! so, use a command
			using (var ps = Psf.NewPowerShell())
			{
				ps.AddCommand("Set-Content")
					.AddParameter("LiteralPath", itemPath)
					.AddParameter("Value", text)
					.AddParameter(Prm.Force)
					.AddParameter(Prm.ErrorAction, ActionPreference.Continue);

				ps.Invoke();

				if (ShowError(ps))
					return false;
			}
			return true;
		}
		/// <summary>
		/// Outputs to the default formatter.
		/// </summary>
		/// <param name="ps">Pipeline.</param>
		/// <param name="input">Input objects.</param>
		public static void Out(PowerShell ps, IEnumerable input)
		{
			ps.Commands.AddCommand(OutHostCommand);
			ps.Invoke(input);
		}
		/// <summary>
		/// Outputs an exception or its error record.
		/// </summary>
		/// <param name="ps">Pipeline.</param>
		/// <param name="ex">Exception.</param>
		public static void OutReason(PowerShell ps, Exception ex)
		{
			object error;
			if (!(ex is RuntimeException asRuntimeException))
				error = ex;
			else
				error = asRuntimeException.ErrorRecord;

			Out(ps, new object[] { "ERROR: " + ex.GetType().Name + ":", error });
		}
		/// <summary>
		/// Shows errors, if any, in a message box and returns true, else just returns false.
		/// </summary>
		public static bool ShowError(PowerShell ps)
		{
			if (ps.Streams.Error.Count == 0)
				return false;

			StringBuilder sb = new StringBuilder();
			foreach (object o in ps.Streams.Error)
				sb.AppendLine(o.ToString());

			Far.Api.Message(sb.ToString(), "PowerShellFar error(s)", MessageOptions.LeftAligned);
			return true;
		}
		/// <summary>
		/// Sets current directory, shows errors but does not throw.
		/// </summary>
		/// <param name="currentDirectory">null or a path.</param>
		public static void SetCurrentDirectoryFinally(string currentDirectory)
		{
			if (currentDirectory != null)
			{
				try
				{
					Directory.SetCurrentDirectory(currentDirectory);
				}
				catch (IOException ex)
				{
					Far.Api.ShowError(Res.Me, ex);
				}
			}
		}
		/// <summary>
		/// Command for formatted output friendly for apps with interaction and colors.
		/// </summary>
		/// <remarks>
		/// "Out-Host" is not suitable for apps with interaction, e.g. more.com, git.exe.
		/// </remarks>
		public static Command OutDefaultCommand => new Command("Out-Default")
		{
			MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error
		};
		/// <summary>
		/// Command for formatted output of everything.
		/// </summary>
		/// <remarks>
		/// "Out-Default" is not suitable for external apps, output goes to console.
		/// </remarks>
		public static Command OutHostCommand => new Command("Out-Host")
		{
			MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error
		};
		/// <summary>
		/// Finds heuristically a property to be used to display the object.
		/// </summary>
		public static PSPropertyInfo FindDisplayProperty(PSObject value)
		{
			//! Microsoft.PowerShell.Commands.Internal.Format.PSObjectHelper.GetDisplayNameExpression()

			PSPropertyInfo pi;
			pi = value.Properties[Word.Name];
			if (pi != null)
				return pi;
			pi = value.Properties[Word.Id];
			if (pi != null)
				return pi;
			pi = value.Properties[Word.Key];
			if (pi != null)
				return pi;

			ReadOnlyPSMemberInfoCollection<PSPropertyInfo> ppi;
			ppi = value.Properties.Match("*" + Word.Key);
			if (ppi.Count > 0)
				return ppi[0];
			ppi = value.Properties.Match("*" + Word.Name);
			if (ppi.Count > 0)
				return ppi[0];
			ppi = value.Properties.Match("*" + Word.Id);
			if (ppi.Count > 0)
				return ppi[0];

			return null;
		}
		public static Type FindCommonType(Collection<PSObject> values)
		{
			Type result = null;
			object sample = null;
			bool baseMode = false;

			foreach (PSObject value in values)
			{
				// get the sample
				if (sample == null)
				{
					sample = value.BaseObject;
					result = sample.GetType();
				}
				// base mode: compare base types
				else if (baseMode)
				{
					if (GetCommonBaseType(value.BaseObject) != result)
						return null;
				}
				// mono mode: compare directly; works for mono sets
				else if (value.BaseObject.GetType() != result)
				{
					// compare base types
					result = GetCommonBaseType(sample);
					if (GetCommonBaseType(value.BaseObject) != result)
						return null;

					// turn on
					baseMode = true;
				}
			}

			return result;
		}
		/// <summary>
		/// Gets heuristic base type suitable to be common for mixed sets.
		/// </summary>
		/// <remarks>
		/// This method should be consistent with redirection in <see cref="FindTableControl"/>.
		/// </remarks>
		static Type GetCommonBaseType(object value)
		{
			if (value is System.IO.FileSystemInfo) return typeof(System.IO.FileSystemInfo);
			if (value is CommandInfo) return typeof(CommandInfo);
			if (value is Breakpoint) return typeof(Breakpoint);
			if (value is PSVariable) return typeof(PSVariable);
			return value.GetType();
		}
		//! Get-FormatData is very expensive (~50% on search), use cache.
		static Dictionary<string, TableControl> _CacheTableControl;
		/// <summary>
		/// Finds an available table control.
		/// </summary>
		/// <param name="typeName">The type name. To be redirected for some types.</param>
		/// <param name="tableName">Optional table name to find.</param>
		/// <returns>Found table control or null.</returns>
		/// <remarks>
		/// Type name redirection should be consistent with <see cref="GetCommonBaseType"/>.
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static TableControl FindTableControl(string typeName, string tableName)
		{
			// process\redirect special types
			switch (typeName)
			{
				case "System.Management.Automation.PSCustomObject": return null;

				case "System.IO.FileSystemInfo": typeName = "FileSystemTypes"; break;
				case "System.IO.DirectoryInfo": typeName = "FileSystemTypes"; break;
				case "System.IO.FileInfo": typeName = "FileSystemTypes"; break;

				case "System.Management.Automation.Breakpoint": typeName = "BreakpointTypes"; break;
				case "System.Management.Automation.LineBreakpoint": typeName = "BreakpointTypes"; break;
				case "System.Management.Automation.CommandBreakpoint": typeName = "BreakpointTypes"; break;
				case "System.Management.Automation.VariableBreakpoint": typeName = "BreakpointTypes"; break;
			}

			// make/try cache
			string cacheKey = typeName + "|" + tableName;
			if (_CacheTableControl == null)
			{
				_CacheTableControl = new Dictionary<string, TableControl>();
			}
			else
			{
				if (_CacheTableControl.TryGetValue(cacheKey, out TableControl result))
					return result;
			}

			// extended type definitions:
			foreach (var pso in InvokeCode("Get-FormatData -TypeName $args[0]", typeName))
			{
				var typeDef = (ExtendedTypeDefinition)pso.BaseObject;
				foreach (var viewDef in typeDef.FormatViewDefinition)
				{
					if (string.IsNullOrEmpty(tableName) || string.Equals(tableName, viewDef.Name, StringComparison.OrdinalIgnoreCase))
					{
						if (viewDef.Control is TableControl table)
						{
							// cache and return the table
							_CacheTableControl.Add(cacheKey, table);
							return table;
						}
					}
				}
			}

			// nothing, cache anyway, and return null
			_CacheTableControl.Add(cacheKey, null);
			return null;
		}
		/// <summary>
		/// Robust Get-ChildItem.
		/// </summary>
		public static Collection<PSObject> GetChildItems(string literalPath)
		{
			//! If InvokeProvider.ChildItem.Get() fails (e.g. hklm:) then we get nothing at all.
			//! Get-ChildItem gets some items even on errors, that is much better than nothing.
			//! NB: exceptions on getting data: FarNet returns false and Far closes the panel.

			try
			{
				return Psf.Engine.InvokeProvider.ChildItem.Get(new string[] { literalPath }, false, true, true);
			}
			catch (RuntimeException e)
			{
				FarNet.Log.TraceException(e);
			}

			try
			{
				return InvokeCode("Get-ChildItem -LiteralPath $args[0] -Force -ErrorAction 0", literalPath);
			}
			catch (RuntimeException e)
			{
				FarNet.Log.TraceException(e);
			}

			return new Collection<PSObject>();
		}
		/// <summary>
		/// Gets the $FormatEnumerationLimit if it is sane or 4.
		/// </summary>
		public static int FormatEnumerationLimit
		{
			get
			{
				object value = Psf.Engine.SessionState.PSVariable.GetValue("FormatEnumerationLimit") ?? 4;
				if (value.GetType() != typeof(int))
					return 4;
				else
					return (int)value;
			}
		}
		public static void InvokePipelineForEach(IEnumerable<PSObject> input)
		{
			List<PSObject> items = new List<PSObject>(input);
			if (items.Count == 0)
				return;

			// keep $_
			var dash = Psf.Engine.SessionState.PSVariable.GetValue("_");

			// set $_ to a sample for TabExpansion
			Psf.Engine.SessionState.PSVariable.Set("_", items[0]);

			try
			{
				// show the dialog, get the code
				UI.InputDialog ui = new UI.InputDialog()
				{
					Title = Res.Me,
					History = Res.HistoryApply,
					UseLastHistory = true,
					Prompt = new string[] { "For each $_ in " + items.Count + " selected:" }
				};

				var code = ui.Show();
				if (string.IsNullOrEmpty(code))
					return;

				// invoke the pipeline using the input
				Psf.Engine.SessionState.PSVariable.Set("_", items);
				Psf.Act("$_ | .{process{ " + code + " }}", null, false);
			}
			finally
			{
				// restore $_
				Psf.Engine.SessionState.PSVariable.Set("_", dash);
			}
		}
		/// <summary>
		/// Invokes the script text and returns the result collection.
		/// </summary>
		/// <param name="code">Script text.</param>
		/// <param name="args">Script arguments.</param>
		internal static Collection<PSObject> InvokeCode(string code, params object[] args)
		{
			if (Runspace.DefaultRunspace == null)
				Runspace.DefaultRunspace = A.Psf.Runspace;

			return ScriptBlock.Create(code).Invoke(args);
		}
		/// <summary>
		/// Invokes the script block and returns the result collection.
		/// </summary>
		/// <param name="script">The script block to invoke.</param>
		/// <param name="args">Script arguments.</param>
		internal static Collection<PSObject> InvokeScript(ScriptBlock script, params object[] args)
		{
			if (Runspace.DefaultRunspace == null)
				Runspace.DefaultRunspace = Psf.Runspace;

			return script.Invoke(args);
		}
		/// <summary>
		/// Invokes the handler-like script and returns the result as it is.
		/// </summary>
		/// <param name="script">The script block to invoke.</param>
		/// <param name="args">Script arguments.</param>
		internal static object InvokeScriptReturnAsIs(ScriptBlock script, params object[] args)
		{
			if (Runspace.DefaultRunspace == null)
				Runspace.DefaultRunspace = Psf.Runspace;

			return script.InvokeReturnAsIs(args);
		}
		internal static void SetPropertyFromTextUI(object target, PSPropertyInfo pi, string text)
		{
			bool isPSObject;
			object value;
			string type;
			if (pi.Value is PSObject && (pi.Value as PSObject).BaseObject != null)
			{
				isPSObject = true;
				type = (pi.Value as PSObject).BaseObject.GetType().FullName;
			}
			else
			{
				isPSObject = false;
				type = pi.TypeNameOfValue;
			}

			if (type == "System.Collections.ArrayList" || type.EndsWith("]", StringComparison.Ordinal))
			{
				ArrayList lines = new ArrayList();
				foreach (var line in FarNet.Works.Kit.SplitLines(text))
					lines.Add(line);
				value = lines;
			}
			else
			{
				value = text;
			}

			if (isPSObject)
				value = PSObject.AsPSObject(value);

			try
			{
				pi.Value = value;
				MemberPanel.WhenMemberChanged(target);
			}
			catch (RuntimeException ex)
			{
				Far.Api.Message(ex.Message, "Setting property");
			}
		}
		/// <summary>
		/// Collects names of files.
		/// </summary>
		internal static List<string> FileNameList(IList<FarFile> files)
		{
			var r = new List<string>
			{
				Capacity = files.Count
			};
			foreach (FarFile f in files)
				r.Add(f.Name);
			return r;
		}
		/// <summary>
		/// Invokes Format-List with output to string.
		/// </summary>
		internal static string InvokeFormatList(object data, bool full)
		{
			// suitable for DataRow, noisy data are excluded
			const string codeMain = @"
Format-List -InputObject $args[0] -ErrorAction 0 |
Out-String -Width $args[1]
";
			// suitable for Object, gets maximum information
			const string codeFull = @"
Format-List -InputObject $args[0] -Property * -Force -Expand Both -ErrorAction 0 |
Out-String -Width $args[1]
";
			return InvokeCode((full ? codeFull : codeMain), data, int.MaxValue)[0].ToString();
		}
		internal static void SetBreakpoint(string script, int line, ScriptBlock action)
		{
			string code = "Set-PSBreakpoint -Script $args[0] -Line $args[1]";
			if (action != null)
				code += " -Action $args[2]";
			InvokeCode(code, script, line, action);
		}
		internal static void RemoveBreakpoint(object breakpoint)
		{
			InvokeCode("Remove-PSBreakpoint -Breakpoint $args[0]", breakpoint);
		}
		internal static void DisableBreakpoint(object breakpoint)
		{
			InvokeCode("Disable-PSBreakpoint -Breakpoint $args[0]", breakpoint);
		}
		internal static object SafePropertyValue(PSPropertyInfo pi)
		{
			//! exceptions, e.g. exit code of running process
			try
			{
				return pi.Value;
			}
			catch (GetValueException e)
			{
				return string.Format(null, "<ERROR: {0}>", e.Message);
			}
		}
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static string SafeToString(object value)
		{
			if (value == null)
				return string.Empty;

			try
			{
				return value.ToString();
			}
			catch (Exception e)
			{
				return string.Format(null, "<ERROR: {0}>", e.Message);
			}
		}
	}
}
