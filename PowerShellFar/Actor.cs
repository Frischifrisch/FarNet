
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using Microsoft.PowerShell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;

namespace PowerShellFar;
#pragma warning disable CA1822

/// <summary>
/// PowerShellFar tools exposed by the global variable <c>$Psf</c>.
/// </summary>
/// <remarks>
/// Global PowerShell variables:
/// <c>$Far</c> is an instance of <see cref="IFar"/>, it exposes FarNet tools.
/// <c>$Psf</c> is the only instance of this class, it exposes PowerShellFar tools.
/// <para>
/// There is no 'Exiting' event because in PS V2 there is a native way using <c>Register-EngineEvent</c>, see examples.
/// Do not use native Far UI in such a handler, it may not work on exiting. GUI dialogs still can be used.
/// This way works for any workspace where <c>Register-EngineEvent</c> is called, so that
/// it can be used by background jobs (PSF and PS), async consoles (local and remote), and etc.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// # Do some job on exiting
/// Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { $Far.Message('See you', 'Exit', 'Gui') }
/// </code>
/// </example>
public sealed partial class Actor
{
	// guard
	internal Actor()
	{
	}

	/// <summary>
	/// Stops the running pipeline.
	/// </summary>
	void CancelKeyPress(object? sender, ConsoleCancelEventArgs e) //_110128_075844
	{
		// ControlBreak?
		if (e.SpecialKey != ConsoleSpecialKey.ControlBreak)
			return;

		//! use copy
		var pipe = Pipeline;
		if (pipe is null || pipe.InvocationStateInfo.State != PSInvocationState.Running)
			return;

		// stop; it still can be bad but chances are low after the above checks
		pipe.BeginStop(AsyncStop, pipe);
	}

	void AsyncStop(IAsyncResult ar) //_110128_075844
	{
		(ar.AsyncState as PowerShell)!.EndStop(ar);
	}

	#region Life
	static Task? OpenRunspaceTask;

	/// <summary>
	/// Called on connection internally.
	/// </summary>
	// This is the entry point. It calls OpenRunspace in the end. OpenRunspace adds cmdlets and opens a runspace async.
	// Concurrent methods:
	// *) OnRunspaceStateEvent() is invoked by PS when the runspace is opened or broken;
	// if it is opened it sets global variables and calls PSF and a user profiles; then is sets the flag for Invoking().
	// *) Invoking() is called by FarNet on a user action; it should wait for opened/broken runspace and continue or die.
	internal void Connect()
	{
		// preload
		OpenRunspaceTask = Task.Run(OpenRunspace);

		//! subscribe only _110301_164313
		Console.CancelKeyPress += CancelKeyPress; //_110128_075844
	}

	/// <summary>
	/// Called on disconnection internally.
	/// If there are background jobs it shows a dialog about them.
	/// </summary>
	internal void Disconnect()
	{
		//! do not unsubscribe _110301_164313
		//Console.CancelKeyPress -= CancelKeyPress; //_110128_075844

		// unsubscribe
		Far.Api.AnyEditor.Opened -= EditorKit.OnEditorOpened;
		Far.Api.AnyEditor.Opened -= EditorKit.OnEditorFirstOpening;

		// release menu
		UI.ActorMenu.Close();

		// kill remaining jobs
		//! after menus, before PS
		Job.StopJobsOnExit();

		// kill host
		if (FarHost != null)
		{
			try
			{
				//! it may be still opening, but Far is already closing, ignore this case,
				//! or we will see for a sec disappearing together with Far error message.
				if (Runspace.RunspaceStateInfo.State == RunspaceState.Opened)
					Runspace.Close();
			}
			finally
			{
				// detach all
				FarUI = null!;
				FarHost = null!;
				Runspace = null!;
				_engine_ = null;
				Pipeline = null;
			}
		}
	}

	void OpenRunspace()
	{
		// host and UI
		FarUI = new FarUI();
		FarHost = new FarHost(FarUI);

		// initial state
		var state = InitialSessionState.CreateDefault();
		state.ApartmentState = ApartmentState.STA;
		state.ExecutionPolicy = ExecutionPolicy.Bypass;

		// add cmdlets
		Commands.BaseCmdlet.AddCmdlets(state);

		// add variables
		state.Variables.Add(new SessionStateVariableEntry[] {
			new SessionStateVariableEntry("Far", Far.Api, "Exposes FarNet.", ScopedItemOptions.AllScope | ScopedItemOptions.Constant),
			new SessionStateVariableEntry("Psf", this, "Exposes PowerShellFar.", ScopedItemOptions.AllScope | ScopedItemOptions.Constant),
		});

		// open runspace
		Runspace = RunspaceFactory.CreateRunspace(FarHost, state);
		Runspace.Open();

		// Add the module path.
		// STOP: [_100127_182335 test]
		// *) Add before the profile, so that it can load modules.
		// *) Add after the opening so that standard paths are added.
		// *) Check for already added, e.g. when starting from another Far.
		var modulePathAdd = $"{AppHome}\\Modules;";
		var modulePathNow = Environment.GetEnvironmentVariable(Word.PSModulePath) ?? string.Empty;
		if (!modulePathNow.Contains(modulePathAdd))
			Environment.SetEnvironmentVariable(Word.PSModulePath, modulePathAdd + modulePathNow);

		//_090315_091325
		// Get engine once to avoid this: "A pipeline is already executing. Concurrent SessionStateProxy method call is not allowed."
		// Looks like a hack, but it works fine. Problem case: run Test-CallStack-.ps1, Esc -> the error above.
		_engine_ = Runspace.SessionStateProxy.PSVariable.GetValue(Word.ExecutionContext) as EngineIntrinsics;

		//? set instead of adding to initial state
		Engine.SessionState.PSVariable.Set("ErrorActionPreference", ActionPreference.Stop);

		// invoke profiles
		using var ps = NewPowerShell();

		// internal profile
		ps.AddCommand($"{A.Psf.AppHome}\\PowerShellFar.ps1", false).Invoke();

		// user profile, run separately for better errors
		var profile = Entry.RoamingData + "\\Profile.ps1";
		if (File.Exists(profile))
		{
			ps.Commands.Clear();
			try
			{
				ps.AddCommand(profile, false).Invoke();
			}
			catch (RuntimeException ex)
			{
				throw new ModuleException($"Profile.ps1 error: {ex.Message}", ex);
			}
		}
	}

	/// <summary>
	/// Completes the runspace opening.
	/// Called by FarNet on command line and by PowerShellFar on its actions.
	/// </summary>
	internal void Invoking()
	{
		// done?
		if (OpenRunspaceTask is null)
			return;

		// null the task
		var task = OpenRunspaceTask;
		OpenRunspaceTask = null;

		try
		{
			// complete the task
			task.GetAwaiter().GetResult();
		}
		finally
		{
			//! set default runspace for handlers
			//! it has to be done in main thread
			Runspace.DefaultRunspace = Runspace;

			// add the debug handler
			Runspace.Debugger.BreakpointUpdated += OnBreakpointUpdated;
		}
	}

	// Sets the current provider location to the user expected.
	// Used in operations which potentially invoke user code.
	internal void SyncPaths()
	{
		// skip running
		if (IsRunning)
			return;

		// skip no panels
		var panel = Far.Api.Panel;
		if (panel is null)
			return;

		// try get the panel location
		string? location = null;
		if (panel is Panel plugin)
		{
			if (plugin is ItemPanel itemPanel)
			{
				location = itemPanel.Explorer.Location;
			}
			else if (plugin is FolderTree)
			{
				location = panel.CurrentDirectory;
				if (location == "*") //_130117_234326
					location = Far.Api.CurrentDirectory;
			}
		}

		// set yet unknown to the far current
		location ??= Far.Api.CurrentDirectory;

		// Set the current location. Note, PS Core works fine with long paths.
		// So do not catch, we should know why it fails and how to handle this.
		//! Parameter is wildcard. Test: enter into a container "[]" and invoke a command.
		Engine.SessionState.Path.SetLocation(Kit.EscapeWildcard(location));
	}
	#endregion

	/// <summary>
	/// Gets the configuration settings and the session settings.
	/// </summary>
	/// <remarks>
	/// Session preferences are usually set in the profile.
	/// Or change them temporary in the panel, command, script.
	/// <para>
	/// See also the manual [Settings].
	/// </para>
	/// </remarks>
	/// <example>
	/// <code>
	/// # Show settings in the panel
	/// Open-FarPanel $Psf.Settings
	/// </code>
	/// </example>
	public Settings Settings => Settings.Default;

	/// <summary>
	/// Gets or sets the active text of active editor or editor line.
	/// </summary>
	/// <remarks>
	/// Gets or sets selected text if selection exists in the current editor or an editor line,
	/// else a line text if any kind of editor line is active.
	/// </remarks>
	public string ActiveText
	{
		get => EditorKit.ActiveText;
		set => EditorKit.ActiveText = value;
	}

	/// <summary>
	/// Gets the active editor or throws an error.
	/// </summary>
	/// <remarks>
	/// This method gets the editor associated with the current window.
	/// If it is not an editor window then an error is thrown.
	/// <para>
	/// The method is used by scripts designed for editors.
	/// </para>
	/// </remarks>
	/// <exception cref="InvalidOperationException">The current window must be an editor.</exception>
	public IEditor Editor()
	{
		if (Far.Api.Window.Kind != WindowKind.Editor)
			throw new InvalidOperationException(Res.NeedsEditor);

		return Far.Api.Editor!;
	}

	/// <summary>
	/// Returns PowerShellFar home path. Designed for internal use.
	/// </summary>
	public string AppHome => Path.GetDirectoryName(typeof(Actor).Assembly.Location)!;

	/// <summary>
	/// Checks whether it is possible to exit the session safely (may require user interaction).
	/// </summary>
	/// <returns>true if exit is safe.</returns>
	/// <remarks>
	/// If there are background jobs this methods calls <see cref="ShowJobs"/>
	/// so that you are prompted to remove jobs manually. If you do not remove all the jobs
	/// then the method returns false.
	/// <para>
	/// It can be used to prevent closing of Far by [F10] with existing background jobs.
	/// </para>
	/// </remarks>
	public bool CanExit() => Job.CanExit();

	/// <summary>
	/// Gets PowerShellFar commands from history.
	/// </summary>
	/// <remarks>
	/// PowerShellFar command history is absolutely different from PowerShell command history; PowerShell mechanism is not used
	/// internally and you should not use it, i.e. forget about <c>Add-History</c>, <c>$MaximumHistoryCount</c>, and etc. - you
	/// don't need them in PowerShellFar. The history is stored in a file, so that commands can be used in other sessions.
	/// <para>
	/// Some standard history commands are partially implemented as internal functions.
	/// <c>Get-History</c> returns command strings, <c>Invoke-History</c> calls <see cref="ShowHistory"/>.
	/// </para>
	/// </remarks>
	/// <param name="count">Number of last commands to be returned. 0: all commands.</param>
	public IList<string> GetHistory(int count)
	{
		var lines = History.ReadLines();
		if (count <= 0 || count >= lines.Length)
			return lines;

		var list = new List<string>(lines);
		list.RemoveRange(0, list.Count - count);
		return list;
	}

	/// <summary>
	/// Shows a new main session interactive in the specified mode.
	/// </summary>
	/// <param name="mode">The editor open mode. Default: <see cref="OpenMode.Modal"/>.</param>
	/// <remarks>
	/// The modal interactive may be called in the middle of something to perform actions manually
	/// and then continue interrupted execution on exit. It is similar to PowerShell nested prompt.
	/// </remarks>
	public void ShowInteractive(OpenMode mode = OpenMode.Modal)
	{
		var inter = Interactive.Create(false);
		if (inter != null)
			inter.Editor.Open(mode);
	}

	/// <summary>
	/// Shows a menu of available PowerShellFar panels to open.
	/// Called on "Power panel".
	/// </summary>
	public void ShowPanel()
	{
		A.Psf.SyncPaths();

		var drive = UI.SelectMenu.SelectPowerPanel();
		if (drive is null)
			return;

		AnyPanel panel;
		if (drive == UI.SelectMenu.TextFolderTree)
			panel = new FolderTree();
		else if (drive == UI.SelectMenu.TextAnyObjects)
			panel = new ObjectPanel();
		else
			panel = new ItemPanel(drive);

		panel.Open();
	}

	/// <summary>
	/// Shows the background job list.
	/// Called on "Background jobs" and by <see cref="CanExit"/>.
	/// </summary>
	public void ShowJobs() => Job.ShowJobs();

	/// <summary>
	/// Shows PowerShell command history.
	/// Called on "Command history".
	/// </summary>
	/// <remarks>
	/// The selected command is inserted to available known editors.
	/// Otherwise a new command input box is shown with this command.
	/// </remarks>
	/// <seealso cref="GetHistory"/>
	public void ShowHistory()
	{
		var code = History.ShowHistory();
		if (code != null)
			InvokeInputCodePrivate(code);
	}

	/// <summary>
	/// Shows PowerShell debugger tools menu.
	/// Called on "Debugger".
	/// </summary>
	public void ShowDebugger()
	{
		var ui = new UI.DebuggerMenu();
		ui.Show();
	}

	/// <summary>
	/// Shows PowerShell errors.
	/// </summary>
	public void ShowErrors()
	{
		var ui = new UI.ErrorsMenu();
		ui.Show();
	}

	/// <summary>
	/// Shows help, normally for the current command or parameter in an editor line.
	/// </summary>
	/// <remarks>
	/// For the current token in an editor line (editor, editbox, cmdline) it gets help
	/// information and shows it in the viewer. In code editors and input code boxes
	/// this action is associated with [ShiftF1].
	/// </remarks>
	public void ShowHelp() => Help.ShowHelpForContext();

	/// <summary>
	/// Expands PowerShell code in the specified edit line.
	/// </summary>
	/// <param name="editLine">Editor line, command line or dialog edit box line; if null then <see cref="IFar.Line"/> is used.</param>
	public void ExpandCode(ILine? editLine) => EditorKit.ExpandCode(editLine, null);

	// PS host
	FarHost FarHost = null!;
	// PS UI
	internal FarUI FarUI = null!;
	// PS runspace
	internal Runspace Runspace { get; private set; } = null!;
	// Main pipeline
	PowerShell? Pipeline;
	// PS engine
	EngineIntrinsics? _engine_;
	internal EngineIntrinsics Engine => _engine_!;

	/// <summary>
	/// Gets a new pipeline or nested one.
	/// </summary>
	/// <returns>Pipeline; it has to be disposed.</returns>
	internal PowerShell NewPowerShell()
	{
		if (IsRunning)
			return Pipeline!.CreateNestedPowerShell();

		Pipeline = PowerShell.Create();
		Pipeline.Runspace = Runspace;
		return Pipeline;
	}

	/// <summary>
	/// Is it running?
	/// </summary>
	internal bool IsRunning => Pipeline != null && Pipeline.InvocationStateInfo.State == PSInvocationState.Running;

	// Current command being invoked (e.g. used as Out-FarPanel title)
	internal string? _myCommand;

	/// <summary>
	/// Runs the PowerShell command pipeline.
	/// Null or empty commands are ignored.
	/// </summary>
	/// <returns>False if the code fails.</returns>
	internal bool Run(RunArgs args)
	{
		var code = args.Code;
		if (string.IsNullOrEmpty(code))
			return true;

		// push writer
		if (args.Writer is null)
		{
			// use own lazy output
			FarUI.PushWriter(new TranscriptOutputWriter());
		}
		else
		{
			// specified output
			FarUI.PushWriter(args.Writer);
		}

		try
		{
			// progress
			FarUI.IsProgressStarted = false;
			Far.Api.UI.SetProgressState(TaskbarProgressBarState.Indeterminate);

			// invoke command
			using (var ps = NewPowerShell())
			{
				_myCommand = code;
				var command = ps.Commands.AddScript(code, args.UseLocalScope);
				if (args.Arguments != null)
				{
					foreach (var arg in args.Arguments)
						command.AddArgument(arg);
				}

				var output = FarUI.Writer is ConsoleOutputWriter ? A.OutDefaultCommand : A.OutHostCommand;
				command.AddCommand(output);
				ps.Invoke();
				args.Reason = ps.InvocationStateInfo.Reason;
			}

			return true;
		}
		catch (Exception reason)
		{
			if (FarNet.Works.Test.IsTestCommand)
				throw;

			args.Reason = reason;
			if (args.NoOutReason)
				return false;

			var color1 = ConsoleColor.Black;
			try
			{
				// push console color
				if (args.Writer is ConsoleOutputWriter)
				{
					color1 = Far.Api.UI.ForegroundColor;
					Far.Api.UI.ShowUserScreen();
					Far.Api.UI.ForegroundColor = Settings.ErrorForegroundColor;
				}

				// write the reason
				using var ps = NewPowerShell();
				A.OutReason(ps, reason);
			}
			finally
			{
				// pop console color
				if (color1 != ConsoleColor.Black)
					Far.Api.UI.ForegroundColor = color1;
			}

			return false;
		}
		finally
		{
			// progress
			FarUI.IsProgressStarted = false;
			Far.Api.UI.SetProgressState(TaskbarProgressBarState.NoProgress);

			_myCommand = null;

			// pop writer
			var usedWriter = FarUI.PopWriter();
			if (args.Writer is null)
			{
				// it is the writer created here, view its file, if any
				var myWriter = (TranscriptOutputWriter)usedWriter;
				myWriter.Close();
				if (myWriter.FileName != null)
				{
					var viewer = Far.Api.CreateViewer();
					viewer.FileName = myWriter.FileName;
					//! code with \n may come from ReadLine editors
					viewer.Title = code.IndexOf('\n') < 0 ? code.Trim() : "Command output";
					viewer.DeleteSource = DeleteSource.File;
					viewer.Switching = Switching.Enabled;
					viewer.DisableHistory = true;
					viewer.CodePage = 1200;
					viewer.Open();
				}
			}
		}
	}

	/// <summary>
	/// Provider settings.
	/// </summary>
	/// <remarks>
	/// These optional settings configure appearance of provider data.
	/// See <c>Profile-.ps1</c> for examples.
	/// <para>
	/// Keys are provider names, e.g. 'FileSystem', 'Registry', 'Alias', and etc.
	/// Keys are case sensitive by default, but you can replace the hashtable with case insensitive (e.g. @{...}).
	/// </para>
	/// <para>
	/// Values are dictionaries with keys mapped to property names of <see cref="ItemPanel"/>,
	/// e.g. <see cref="TablePanel.Columns"/>, <see cref="TablePanel.ExcludeMemberPattern"/>, and etc.
	/// Their values depend on that properties, see help.
	/// </para>
	/// </remarks>
	public IDictionary Providers
	{
		get => _Providers;
		set => _Providers = value ?? new Hashtable();
	}
	IDictionary _Providers = new Hashtable();

	/// <summary>
	/// Invokes the script opened in the current editor.
	/// </summary>
	/// <remarks>
	/// [F5] is the hardcoded shortcut. A different key can be used with a macro:
	/// see the example macro in the manual.
	/// <para>
	/// The action is the same as to invoke the script from the input command box
	/// but if the file is modified then it is saved before invoking.
	/// </para>
	/// </remarks>
	public void InvokeScriptFromEditor()
	{
		EditorKit.InvokeScriptFromEditor(null);
	}

	HashSet<LineBreakpoint>? _breakpoints_;
	internal HashSet<LineBreakpoint> Breakpoints => _breakpoints_ ??= new HashSet<LineBreakpoint>();

	void OnBreakpointUpdated(object? sender, BreakpointUpdatedEventArgs e)
	{
		if (!string.IsNullOrEmpty(e.Breakpoint.Script))
		{
			if (e.Breakpoint is LineBreakpoint bp)
			{
				if (e.UpdateType == BreakpointUpdateType.Removed)
					Breakpoints.Remove(bp);
				else
					Breakpoints.Add(bp);
			}
		}
	}

	/// <summary>
	/// FarNet module manager of the PowerShellFar module.
	/// </summary>
	/// <remarks>
	/// It may be used in scripts in order to register new module actions, get/set the current UI culture, and etc.
	/// <para>
	/// In order to just get the current UI culture better use the standard PowerShell way:
	/// <c>$PSUICulture</c> or <c>$Host.CurrentUICulture</c>
	/// </para>
	/// </remarks>
	public IModuleManager Manager => Entry.Instance.Manager;

	/// <summary>
	/// Transcript writer, may be null.
	/// </summary>
	internal TranscriptOutputWriter? Transcript { get; set; }
}
