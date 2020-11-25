
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;
using FarNet;
using FarNet.Forms;
using FarNet.Tools;

namespace PowerShellFar
{
	/// <summary>
	/// Editor tools.
	/// </summary>
	static class EditorKit
	{
		const string CompletionText = "CompletionText";
		const string ListItemText = "ListItemText";
		/*
		V3 completion. CompletionText is used in lists for:
		Command - avoid two Get-History -> Get-History, Microsoft.PowerShell.Core\Get-History
		ProviderItem - avoid two Test-Far.ps1 -> .\Test-Far.ps1, Test-Far.ps1
		ProviderContainer - for consistency with ProviderItem

		//! `@args` instead `param($inputScript, $cursorColumn)` to avoid visible variables.
		*/
		const string CallTabExpansionV3 = @"
$r = TabExpansion2 @args
@{
	ReplacementIndex = $r.ReplacementIndex
	ReplacementLength = $r.ReplacementLength
	CompletionMatches = @(foreach($m in $r.CompletionMatches) {
		switch ($m.ResultType) {
			Command { $m.CompletionText }
			ProviderItem { $m.CompletionText }
			ProviderContainer { $m.CompletionText }
			default { @{CompletionText = $m.CompletionText; ListItemText = $m.ListItemText} }
		}
	})
}
";
		// V2 completion
		//! Ideally, we should use private variable. But who cares of v2?
		const string CallTabExpansionV2 = @"
param($inputScript, $cursorColumn)
$line = $inputScript.Substring(0, $cursorColumn)
$word = if ($line -match '(?:^|\s)(\S+)$') {$matches[1]} else {''}
@{
	ReplacementIndex = $line.Length - $word.Length
	ReplacementLength = $word.Length
	CompletionMatches = @(TabExpansion $line $word)
}
";

		static bool _doneTabExpansion;
		static string _pathTabExpansion;
		static string _callTabExpansion;

		static void InitTabExpansion()
		{
			if (!_doneTabExpansion)
			{
				_doneTabExpansion = true;
				InitTabExpansion(null);
			}
		}
		//! It is called once in the main session and once per each local and remote session.
		public static void InitTabExpansion(Runspace runspace)
		{
			// init path and caller
			if (_pathTabExpansion == null)
			{
				if (A.Psf.PSVersion.Major > 2)
				{
					_pathTabExpansion = Path.Combine(A.Psf.AppHome, "TabExpansion2.ps1");
					_callTabExpansion = CallTabExpansionV3;
				}
				else
				{
					_pathTabExpansion = Path.Combine(A.Psf.AppHome, "TabExpansion.ps1");
					_callTabExpansion = CallTabExpansionV2;
				}
			}

			// load TabExpansion
			using (var ps = runspace == null ? A.Psf.NewPowerShell() : PowerShell.Create())
			{
				if (runspace != null)
					ps.Runspace = runspace;

				ps.AddCommand(_pathTabExpansion, false).Invoke();
			}
		}
		static string TECompletionText(object value)
		{
			var t = Cast<Hashtable>.From(value); //! remote gets PSObject
			if (t == null)
				return value.ToString();

			return t[CompletionText].ToString();
		}
		static string TEListItemText(object value)
		{
			var t = Cast<Hashtable>.From(value); //! remote gets PSObject
			if (t == null)
				return value.ToString();

			var r = t[ListItemText];
			if (r != null)
				return r.ToString();

			return t[CompletionText].ToString();
		}
		/// <summary>
		/// Expands PowerShell code in an edit line.
		/// </summary>
		/// <param name="editLine">Editor line, command line or dialog edit box line; if null then <see cref="IFar.Line"/> is used.</param>
		/// <param name="runspace">Runspace or null for the main.</param>
		/// <seealso cref="Actor.ExpandCode"/>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public static void ExpandCode(ILine editLine, Runspace runspace)
		{
			InitTabExpansion();

			// hot line
			if (editLine == null)
			{
				editLine = Far.Api.Line;
				if (editLine == null)
				{
					A.Message("There is no current editor line.");
					return;
				}
			}

			int lineOffset = 0;
			string inputScript;
			int cursorColumn;
			var prefix = string.Empty;

			IEditor editor = null;
			InteractiveArea area;

			// script?
			if (A.Psf.PSVersion.Major > 2 && editLine.WindowKind == WindowKind.Editor && My.PathEx.IsPSFile((editor = Far.Api.Editor).FileName))
			{
				int lineIndex = editor.Caret.Y;
				int lastIndex = editor.Count - 1;

				// previous text
				var sb = new StringBuilder();
				for (int i = 0; i < lineIndex; ++i)
					sb.AppendLine(editor[i].Text);

				// current line
				lineOffset = sb.Length;
				cursorColumn = lineOffset + editLine.Caret;

				// remaining text
				for (int i = lineIndex; i < lastIndex; ++i)
					sb.AppendLine(editor[i].Text);
				sb.Append(editor[lastIndex]);

				// whole text
				inputScript = sb.ToString();
			}
			// area?
			else if (editor != null && editor.Host is Interactive console && (area = console.CommandArea()) != null)
			{
				int lineIndex = area.Caret.Y;
				int lastIndex = area.LastLineIndex;

				// previous text
				var sb = new StringBuilder();
				for (int i = area.FirstLineIndex; i < lineIndex; ++i)
					sb.AppendLine(editor[i].Text);

				// current line
				lineOffset = sb.Length;
				cursorColumn = lineOffset + area.Caret.X;

				// remaining text
				for (int i = lineIndex; i < lastIndex; ++i)
					sb.AppendLine(editor[i].Text);
				sb.Append(editor[lastIndex]);

				// whole text
				inputScript = sb.ToString();
			}
			// line
			else
			{
				// original line
				inputScript = editLine.Text;
				cursorColumn = editLine.Caret;

				//_200805_i3 Deal with auto complete selection.
				// use selection start as cursor column
				var selectionSpan = editLine.SelectionSpan;
				if (cursorColumn == selectionSpan.End)
					cursorColumn = selectionSpan.Start;

				// process prefix, used to be just for panels but it is needed in dialogs, too
				Entry.SplitCommandWithPrefix(ref inputScript, out prefix);

				// correct caret
				cursorColumn -= prefix.Length;
				if (cursorColumn < 0)
					return;
			}

			// skip empty (also avoid errors)
			if (inputScript.Length == 0)
				return;

			// invoke
			try
			{
				// call TabExpansion
				Hashtable result;
				using (var ps = runspace == null ? A.Psf.NewPowerShell() : PowerShell.Create())
				{
					if (runspace != null)
						ps.Runspace = runspace;

					result = (Hashtable)ps.AddScript(_callTabExpansion, true).AddArgument(inputScript).AddArgument(cursorColumn).Invoke()[0].BaseObject;
				}

				// results
				var words = Cast<IList>.From(result["CompletionMatches"]); //! remote gets PSObject
				int replacementIndex = (int)result["ReplacementIndex"];
				int replacementLength = (int)result["ReplacementLength"];
				replacementIndex -= lineOffset;
				if (replacementIndex < 0 || replacementLength < 0)
					return;

				// variables from the current editor
				if (editLine.WindowKind == WindowKind.Editor)
				{
					// replaced text
					var lastWord = inputScript.Substring(lineOffset + replacementIndex, replacementLength);

					//! as TabExpansion.ps1 but ends with \$(\w*)$
					var matchVar = Regex.Match(lastWord, @"^(.*[!;\(\{\|""'']*)\$(global:|script:|private:)?(\w*)$", RegexOptions.IgnoreCase);
					if (matchVar.Success)
					{
						var start = matchVar.Groups[1].Value;
						var scope = matchVar.Groups[2].Value;
						var re = new Regex(@"\$(global:|script:|private:)?(" + scope + matchVar.Groups[3].Value + @"\w+:?)", RegexOptions.IgnoreCase);

						var variables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						foreach (var line1 in Far.Api.Editor.Lines)
						{
							foreach (Match m in re.Matches(line1.Text))
							{
								var all = m.Value;
								if (all[all.Length - 1] != ':')
								{
									variables.Add(start + all);
									if (scope.Length == 0 && m.Groups[1].Value.Length > 0)
										variables.Add(start + "$" + m.Groups[2].Value);
								}
							}
						}

						// union lists
						foreach (var x in words)
							if (x != null)
								variables.Add(TECompletionText(x));

						// final sorted list
						words = variables.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
					}
				}

				// expand
				ExpandText(editLine, replacementIndex + prefix.Length, replacementLength, words);
			}
			catch (RuntimeException) { }
		}
		public static void ExpandText(ILine editLine, int replacementIndex, int replacementLength, IList words)
		{
			bool isEmpty = words.Count == 0;

			// select a word
			string word;
			if (words.Count == 1)
			{
				// 1 word
				if (words[0] == null)
					return;

				word = TECompletionText(words[0]);
			}
			else
			{
				// make menu
				IListMenu menu = Far.Api.CreateListMenu();
				var cursor = Far.Api.UI.WindowCursor;
				menu.X = cursor.X;
				menu.Y = cursor.Y;
				Settings.Default.PopupMenu(menu);
				if (isEmpty)
				{
					menu.Add(Res.Empty).Disabled = true;
					menu.NoInfo = true;
					menu.Show();
					return;
				}

				menu.Incremental = "*";
				menu.IncrementalOptions = PatternOptions.Substring;

				foreach (var it in words)
				{
					if (it == null) continue;
					var item = new SetItem
					{
						Text = TEListItemText(it),
						Data = it
					};
					menu.Items.Add(item);
				}

				if (menu.Items.Count == 0)
					return;

				if (menu.Items.Count == 1)
				{
					word = TECompletionText(menu.Items[0].Data);
				}
				else
				{
					// show menu
					if (!menu.Show())
						return;
					word = TECompletionText(menu.Items[menu.Selected].Data);
				}
			}

			// get original text and custom mode
			var text = editLine.Text;
			var last = replacementIndex + replacementLength - 1;
			bool custom = last > 0 && last < text.Length && text[last] == '='; //_140112_150217 last can be out of range

			//_200805_i3 Deal with auto complete selection.
			// remove selected text before replacement
			var selectionSpan = editLine.SelectionSpan;
			if (selectionSpan.Start == replacementIndex + replacementLength)
				text = text.Substring(0, selectionSpan.Start) + text.Substring(selectionSpan.End, text.Length - selectionSpan.End);

			// replace

			// head before replaced part
			string head = text.Substring(0, replacementIndex);

			// custom pattern
			int index, caret;
			if (custom && (index = word.IndexOf('#')) >= 0)
			{
				word = word.Substring(0, index) + word.Substring(index + 1);
				caret = head.Length + index;
			}
			// standard
			else
			{
				caret = head.Length + word.Length;
			}

			// set new text = old head + expanded + old tail
			editLine.Text = head + word + text.Substring(replacementIndex + replacementLength);

			// set caret
			editLine.Caret = caret;
		}
		public static string ActiveText
		{
			get
			{
				// case: editor
				if (Far.Api.Window.Kind == WindowKind.Editor)
				{
					var editor = Far.Api.Editor;
					if (editor.SelectionExists)
						return editor.GetSelectedText();
					return editor.Line.Text;
				}

				// other lines
				ILine line = Far.Api.Line;
				if (line == null)
					return string.Empty;
				else
					return line.ActiveText;
			}
			set
			{
				// case: editor
				if (Far.Api.Window.Kind == WindowKind.Editor)
				{
					var editor = Far.Api.Editor;
					switch (editor.SelectionKind)
					{
						case PlaceKind.Column:
							throw new NotSupportedException("Rectangular selection is not supported.");
						case PlaceKind.Stream:
							editor.SetSelectedText(value);
							return;
					}

					editor.Line.Text = value;
					return;
				}

				// other lines
				ILine line = Far.Api.Line;
				if (line == null)
					throw new InvalidOperationException("There is no current text to set.");
				else
					line.ActiveText = value;
			}
		}
		public static void OnEditorFirstOpening(object sender, EventArgs e)
		{
			A.Psf.Invoking();

			try
			{
				var profile = Entry.RoamingData + "\\Profile-Editor.ps1";
				if (File.Exists(profile))
				{
					using (var ps = A.Psf.NewPowerShell())
						ps.AddCommand(profile, false).Invoke();
				}
			}
			catch (RuntimeException ex)
			{
				throw new RuntimeException("Error in Profile-Editor.ps1, see $Error for details.", ex);
			}
		}
		public static void OnEditorOpened(object sender, EventArgs e)
		{
			var editor = (IEditor)sender;
			var fileName = editor.FileName;
			bool isInteractive = fileName.EndsWith(Word.InteractiveSuffix, StringComparison.OrdinalIgnoreCase);
			if (isInteractive)
			{
				if (editor.Host == null)
					editor.Host = new Interactive(editor);
			}
			else if (My.PathEx.IsPSFile(fileName))
			{
				editor.KeyDown += OnKeyDownPSFile;
				editor.Changed += OnChangedPSFile;
			}
		}
		static void OnChangedPSFile(object sender, EditorChangedEventArgs e)
		{
			if (e.Kind == EditorChangeKind.LineChanged)
				return;

			var editor = (IEditor)sender;
			var script = editor.FileName;
			var line = e.Line + 1;

			IEnumerable<LineBreakpoint> bps = null;
			int delta = 0;
			if (e.Kind == EditorChangeKind.LineAdded)
			{
				delta = 1;
				bps = A.Psf.Breakpoints.Where(x => x.Line >= line && x.Script.Equals(script, StringComparison.OrdinalIgnoreCase)).ToArray();
			}
			else
			{
				var bp = A.Psf.Breakpoints.FirstOrDefault(x => x.Line == line && x.Script.Equals(script, StringComparison.OrdinalIgnoreCase));
				if (bp != null)
					A.RemoveBreakpoint(bp);

				delta = -1;
				bps = A.Psf.Breakpoints.Where(x => x.Line > line && x.Script.Equals(script, StringComparison.OrdinalIgnoreCase)).ToArray();
			}

			foreach (var bp in bps)
			{
				A.RemoveBreakpoint(bp);
				A.SetBreakpoint(bp.Script, bp.Line + delta, bp.Action);
			}
		}
		/// <summary>
		/// Called on key in *.ps1.
		/// </summary>
		static void OnKeyDownPSFile(object sender, KeyEventArgs e)
		{
			// editor; skip if selected
			IEditor editor = (IEditor)sender;

			switch (e.Key.VirtualKeyCode)
			{
				case KeyCode.F1:
					{
						if (e.Key.IsShift())
						{
							// [ShiftF1]
							e.Ignore = true;
							Help.ShowHelpForContext();
						}
						return;
					}
				case KeyCode.F5:
					{
						if (e.Key.Is())
						{
							// [F5]
							e.Ignore = true;
							InvokeScriptBeingEdited(editor);
						}
						return;
					}
				case KeyCode.Tab:
					{
						if (e.Key.Is())
						{
							// [Tab]
							if (!editor.SelectionExists && NeedsTabExpansion(editor))
							{
								// TabExpansion
								e.Ignore = true;
								A.Psf.ExpandCode(editor.Line);
								editor.Redraw();
							}
						}
						return;
					}
			}
		}
		public static void InvokeSelectedCode()
		{
			string code;
			bool toCleanCmdLine = false;
			WindowKind wt = Far.Api.Window.Kind;

			if (wt == WindowKind.Editor)
			{
				var editor = Far.Api.Editor;
				code = editor.GetSelectedText();
				if (string.IsNullOrEmpty(code))
					code = editor[editor.Caret.Y].Text;
			}
			else if (wt == WindowKind.Dialog)
			{
				IDialog dialog = Far.Api.Dialog;
				if (!(dialog.Focused is IEdit edit))
				{
					Far.Api.Message("The current control must be an edit box.", Res.Me);
					return;
				}
				code = edit.Line.SelectedText;
				if (string.IsNullOrEmpty(code))
					code = edit.Text;
			}
			else
			{
				ILine cl = Far.Api.CommandLine;
				code = cl.SelectedText;
				if (string.IsNullOrEmpty(code))
				{
					code = cl.Text;
					toCleanCmdLine = true;
				}

				Entry.SplitCommandWithPrefix(ref code, out _);
			}
			if (code.Length == 0)
				return;

			// go
			bool ok = A.Psf.Act(code, null, wt != WindowKind.Editor);

			// clean the command line if ok
			if (ok && toCleanCmdLine && wt != WindowKind.Editor)
				Far.Api.CommandLine.Text = string.Empty;
		}
		// PSF sets the current directory and location to the script directory.
		// This is often useful and consistent with invoking from panels.
		// NOTE: ISE [F5] does not.
		public static void InvokeScriptBeingEdited(IEditor editor)
		{
			// editor
			if (editor == null)
				editor = A.Psf.Editor();

			// commit
			editor.Save();

			// sync the directory and location to the script directory
			// maybe it is questionable but it is very handy too often
			string dir0, dir1;

			// save/set the directory, allow failures (e.g. a long path)
			// note: GetDirectoryName fails on a long path, too
			var fileName = editor.FileName;
			try
			{
				dir1 = Path.GetDirectoryName(fileName);
				dir0 = Environment.CurrentDirectory;
				Environment.CurrentDirectory = dir1;
			}
			catch (PathTooLongException)
			{
				// PowerShell is not able to invoke this script anyway, almost for sure
				Far.Api.Message("The script path is too long.\rInvoking is not supported.");
				return;
			}

			try
			{
				Far.Api.UI.WindowTitle = "Running...";

				// push/set the location; let's ignore issues
				A.Psf.Engine.SessionState.Path.PushCurrentLocation(null);
				A.Psf.Engine.SessionState.Path.SetLocation(Kit.EscapeWildcard(dir1));

				// invoke the script by the runner or directly
				if (fileName.EndsWith(".fas.ps1", StringComparison.OrdinalIgnoreCase))
				{
					A.InvokeCode("Start-FarTask $args[0]", editor.FileName);
				}
				else if (dir1.EndsWith(".ps1.commands", StringComparison.OrdinalIgnoreCase))
				{
					var runner = dir1.Substring(0, dir1.Length - 9);
					A.Psf.Act($"& '{runner.Replace("'", "''")}' {Path.GetFileNameWithoutExtension(fileName)}", null, false);
				}
				else
				{
					A.Psf.Act($"& '{fileName.Replace("'", "''")}'", null, false);
				}
			}
			finally
			{
				// restore the directory first
				Environment.CurrentDirectory = dir0;

				// then pop the location, it may fail perhaps
				A.Psf.Engine.SessionState.Path.PopLocation(null);
			}
		}
		// true if there is a solid char anywhere before the caret
		internal static bool NeedsTabExpansion(IEditor editor)
		{
			ILine line = editor.Line;
			string text = line.Text;

			int pos = line.Caret;
			if (pos > text.Length)
				return false;

			while (--pos >= 0)
				if (text[pos] > ' ')
					return true;

			return false;
		}
	}
}
