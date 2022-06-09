
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using FarNet.Forms;
using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace PowerShellFar.UI
{
	class DebuggerDialog
	{
		static DebuggerResumeAction _LastAction = DebuggerResumeAction.StepInto;
		public EventHandler<ButtonClickedEventArgs> OnView { get; set; }
		readonly InvocationInfo _InvocationInfo;
		readonly IDialog _Dialog;
		readonly IListBox _List1;
		readonly IListBox _List2;
		readonly IButton _Step;
		readonly IButton _Over;
		readonly IButton _Out;
		readonly IButton _Console;
		readonly IButton _Edit;
		readonly IButton _View;
		readonly IButton _Goto;
		readonly IButton _Quit;
		public DebuggerDialog(DebuggerStopEventArgs e)
		{
			_InvocationInfo = e.InvocationInfo;

			int maxLine = 0;
			string[] lines = null;
			if (!string.IsNullOrEmpty(e.InvocationInfo.ScriptName) && File.Exists(e.InvocationInfo.ScriptName))
			{
				try
				{
					lines = File.ReadAllLines(e.InvocationInfo.ScriptName, Encoding.Default);
					foreach (string s in lines)
					{
						if (s.Length > maxLine)
							maxLine = s.Length;
					}
				}
				catch (IOException) { }
			}

			int dw = Math.Max(Math.Min(Far.Api.UI.WindowSize.X - 7, maxLine + 12), 73);
			int dh = 22;

			string title;
			int h1;
			if (e.Breakpoints.Count > 0)
			{
				title = "DEBUG: Hit breakpoint(s)";
				h1 = e.Breakpoints.Count + 2;
			}
			else
			{
				title = "DEBUG: Step";
				h1 = 2;
			}

			_Dialog = Far.Api.CreateDialog(-1, -1, dw, dh);
			_Dialog.HelpTopic = Entry.Instance.GetHelpTopic(HelpTopic.DebuggerDialog);
			_Dialog.AddBox(3, 1, dw - 4, dh - 2, title);

			_List1 = _Dialog.AddListBox(4, 2, dw - 5, h1 + 1, null);
			_List1.Disabled = true;
			_List1.NoBox = true;
			_List1.NoClose = true;
			_List1.NoFocus = true;
			if (e.Breakpoints.Count > 0)
			{
				foreach (Breakpoint bp in e.Breakpoints)
				{
					if (bp is CommandBreakpoint bpc && Kit.Equals(bpc.Command, Commands.AssertFarCommand.MyName))
						A.InvokeCode("Remove-PSBreakpoint -Breakpoint $args[0]", bpc);
				}
			}
			foreach (string s in e.InvocationInfo.PositionMessage.Trim().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
				_List1.Add(s);

			_Dialog.AddText(0, -_List1.Rect.Height, 0, null).Separator = 1;

			_List2 = _Dialog.AddListBox(4, _List1.Rect.Bottom + 2, dw - 5, dh - 5, null);
			_List2.NoBox = true;
			_List2.NoClose = true;
			if (lines != null)
			{
				foreach (string s in lines)
					_List2.Add(s);
				int i = e.InvocationInfo.ScriptLineNumber - 1;
				_List2.Items[i].Checked = true;
			}

			_Dialog.AddText(0, -_List2.Rect.Height, 0, null).Separator = 1;

			_Step = _Dialog.AddButton(0, -1, BtnStep);
			_Step.CenterGroup = true;

			_Over = _Dialog.AddButton(0, 0, BtnOver);
			_Over.CenterGroup = true;

			_Out = _Dialog.AddButton(0, 0, BtnOut);
			_Out.CenterGroup = true;

			_Console = _Dialog.AddButton(0, 0, BtnInteractive);
			_Console.CenterGroup = true;
			_Console.NoBrackets = true;

			_Edit = _Dialog.AddButton(0, 0, BtnEdit);
			_Edit.CenterGroup = true;
			_Edit.NoBrackets = true;

			// to be completed on show
			_View = _Dialog.AddButton(0, 0, BtnView);
			_View.CenterGroup = true;
			_View.NoBrackets = true;
			_View.NoClose = true;

			_Goto = _Dialog.AddButton(0, 0, BtnLine);
			_Goto.CenterGroup = true;
			_Goto.NoBrackets = true;
			_Goto.NoClose = true;
			_Goto.ButtonClicked += OnGoto;

			_Quit = _Dialog.AddButton(0, 0, BtnQuit);
			_Quit.CenterGroup = true;
			_Quit.NoBrackets = true;

			_Dialog.Initialized += OnInitialized;
		}
		void SetFrame()
		{
			int i = _InvocationInfo.ScriptLineNumber - 1;
			_List2.SetFrame(i, i - _List2.Rect.Height / 2);
		}
		void OnInitialized(object sender, EventArgs e)
		{
			// set listbox frame
			if (_List2.Items.Count > 0)
				SetFrame();
		}
		void OnGoto(object sender, EventArgs e)
		{
			// set listbox frame
			if (_List2.Items.Count > 0)
				SetFrame();
		}
		public DebuggerResumeAction Show()
		{
			A.InvokeCode("Get-PSBreakpoint -Variable daf01ff6-f004-43bd-b6bf-cf481e9333d3 | Remove-PSBreakpoint");

			if (OnView == null)
				_View.Disabled = true;
			else
				_View.ButtonClicked += OnView;

			switch (_LastAction)
			{
				case DebuggerResumeAction.StepInto: _Dialog.Focused = _Step; break;
				case DebuggerResumeAction.StepOver: _Dialog.Focused = _Over; break;
			}

			while (_Dialog.Show())
			{
				if (_Dialog.Selected == _Step)
				{
					_LastAction = DebuggerResumeAction.StepInto;
					return DebuggerResumeAction.StepInto;
				}

				if (_Dialog.Selected == _Over)
				{
					_LastAction = DebuggerResumeAction.StepOver;
					return DebuggerResumeAction.StepOver;
				}

				if (_Dialog.Selected == _Out)
					return DebuggerResumeAction.StepOut;

				if (_Dialog.Selected == _Console)
				{
					var console = Interactive.Create(false);
					console.Editor.Open(OpenMode.Modal);
					continue;
				}

				if (_Dialog.Selected == _Edit)
				{
					if (_List2.Items.Count > 0)
					{
						IEditor editor = Far.Api.CreateEditor();
						editor.FileName = _InvocationInfo.ScriptName;
						editor.IsLocked = true;
						editor.GoToLine(_InvocationInfo.ScriptLineNumber - 1);
						editor.Open(OpenMode.Modal);
					}
					continue;
				}

				if (_Dialog.Selected == _Quit)
					return DebuggerResumeAction.Stop; //throw new PipelineStoppedException();
			}

			return DebuggerResumeAction.Continue;
		}
		const string
			BtnStep = "&Step",
			BtnOver = "O&ver",
			BtnOut = "&Out",
			BtnInteractive = "&Interactive",
			BtnEdit = "&Edit",
			BtnView = "Vie&w",
			BtnLine = "&Line",
			BtnQuit = "&Quit";
	}
}
