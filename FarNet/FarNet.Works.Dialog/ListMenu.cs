
// FarNet plugin for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet.Forms;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FarNet.Works
{
	public sealed class ListMenu : AnyMenu, IListMenu
	{
		IListBox _box;
		// Original user defined filter
		string _Incremental_;
		PatternOptions _IncrementalOptions;
		// Currently used filter
		string _filter;
		// To update the filter
		bool _toFilter;
		// Key handler was invoked
		bool _isKeyHandled;
		// Filtered
		List<int> _ii;
		Regex _re;

		public bool AutoSelect { get; set; }
		public bool NoInfo { get; set; }
		public bool UsualMargins { get; set; }
		public int ScreenMargin { get; set; }
		public Guid TypeId { get; set; }

		public PatternOptions IncrementalOptions
		{
			get { return _IncrementalOptions; }
			set
			{
				if ((value & PatternOptions.Regex) != 0)
					throw new ArgumentException("Incremental filter can not be 'Regex'.");
				_IncrementalOptions = value;
			}
		}

		public string Incremental
		{
			get { return _Incremental_; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_Incremental_ = value;
				_filter = value;
				_re = null;
			}
		}

		public ListMenu()
		{
			_Incremental_ = string.Empty;
			_filter = string.Empty;
			TypeId = new Guid("01a43865-b81d-4bca-b3a4-a9ae4f9f7b55");
		}

		// Simple wildcard (* and ?)
		static string Wildcard(string pattern)
		{
			pattern = Regex.Escape(pattern);
			for (int i = 0; i < pattern.Length - 1; ++i)
			{
				if (pattern[i] != '\\')
					continue;

				if (pattern[i + 1] == '*')
					pattern = pattern.Substring(0, i) + ".*" + pattern.Substring(i + 2);
				if (pattern[i + 1] == '?')
					pattern = pattern.Substring(0, i) + ".?" + pattern.Substring(i + 2);
				else
					++i;
			}
			return pattern;
		}

		static Regex CreateRegex(string pattern, PatternOptions options)
		{
			if (string.IsNullOrEmpty(pattern) || options == 0)
				return null;

			// regex?
			if ((options & PatternOptions.Regex) != 0)
			{
				try
				{
					if (pattern.StartsWith("?"))
					{
						// prefix
						if (pattern.Length <= 1)
							return null;

						pattern = pattern.Substring(1);
						options |= PatternOptions.Prefix;
					}
					else if (pattern.StartsWith("*"))
					{
						// substring
						if (pattern.Length <= 1)
							return null;

						pattern = pattern.Substring(1);
						options |= PatternOptions.Substring;
					}
					else
					{
						//! standard regex; errors may come here
						return new Regex(pattern, RegexOptions.IgnoreCase);
					}
				}
				catch (ArgumentException)
				{
					return null;
				}
			}

			// literal else wildcard
			string re;
			if ((options & PatternOptions.Literal) != 0)
				re = Regex.Escape(pattern);
			else
				re = Wildcard(pattern);

			// prefix?
			if ((options & PatternOptions.Prefix) != 0)
				re = "^" + re;

			//! normally errors must not come here
			return new Regex(re, RegexOptions.IgnoreCase);
		}

		void MakeFilter()
		{
			// filter
			if (!_toFilter)
				return;
			_toFilter = false;

			// Do not filter by predefined. Case:
			// TabEx: 'sc[Tab]' gets 'Set-Contents' which doesn't match the prefix 'sc', but it should be shown.
			if (_filter == Incremental)
				return;

			// create
			if (_re == null)
				_re = CreateRegex(_filter, IncrementalOptions);
			if (_re == null)
				return;

			// case: filter already filtered
			if (_ii != null)
			{
				var ii = new List<int>();
				foreach(int k in _ii)
				{
					if (_re.IsMatch(myItems[k].Text))
						ii.Add(k);
				}
				_ii = ii;
				return;
			}

			// case: not yet filtered
			_ii = new List<int>();
			int i = -1;
			foreach(var mi in Items)
			{
				++i;
				if (_re.IsMatch(mi.Text))
					_ii.Add(i);
			}
		}

		string InfoLine()
		{
			var r = "(";
			if (_ii != null)
				r += _ii.Count + "/";
			r += Items.Count + ")";
			return Kit.JoinText(r, Bottom);
		}

		void GetInfo(out string head, out string foot)
		{
			head = Title == null ? string.Empty : Title;
			foot = NoInfo ? string.Empty : InfoLine();
			if (!string.IsNullOrEmpty(Bottom))
				foot += " ";
			if (IncrementalOptions != 0 && _filter.Length > 0)
			{
				if (SelectLast)
					foot = "[" + _filter + "]" + foot;
				else
					head = Kit.JoinText("[" + _filter + "]", head);
			}
		}

		// Validates rect position and width by screen size so that rect is visible.
		static void ValidateRect(ref int x, ref int w, int min, int size)
		{
			if (x < 0)
				x = min + (size - w) / 2;
			int r = x + w - 1;
			if (r > min + size - 1)
			{
				x -= (r - min - size + 1);
				if (x < min)
					x = min;
				r = x + w - 1;
				if (r > min + size - 1)
					w -= (r - min - size + 1);
			}
		}

		void MakeSizes(IDialog dialog, Point size)
		{
			// controls with text
			var border = dialog[0];
			var bottom = dialog[2];

			// text lengths
			var borderText = border.Text;
			int borderTextLength = borderText == null ? 0 : borderText.Length;
			var bottomText = bottom?.Text;
			int bottomTextLength = bottomText == null ? 0 : bottomText.Length;

			// margins
			int ms = ScreenMargin > 1 ? ScreenMargin : 1;
			int mx = UsualMargins ? 2 : 0;
			int my = UsualMargins ? 1 : 0;

			// width
			int w = 0;
			if (_ii == null)
			{
				foreach(var mi in myItems)
					if (mi.Text.Length > w)
						w = mi.Text.Length;
			}
			else
			{
				foreach(int k in _ii)
					if (myItems[k].Text.Length > w)
						w = myItems[k].Text.Length;
			}
			w += 2 + 2 * mx; // if less last chars are lost

			// height
			int n = _ii == null ? myItems.Count : _ii.Count;
			if (MaxHeight > 0 && n > MaxHeight)
				n = MaxHeight;

			// adjust width
			if (w < borderTextLength)
				w = borderTextLength + 4;
			if (w < bottomTextLength)
				w = bottomTextLength + 4;
			if (w < 20)
				w = 20;

			// X
			int dw = w + 4, dx = X;
			ValidateRect(ref dx, ref dw, ms, size.X - 2 * ms);

			// Y
			int dh = n + 2 + 2 * my, dy = Y;
			ValidateRect(ref dy, ref dh, ms, size.Y - 2 * ms);

			// dialog
			dialog.Rect = new Place(dx, dy, dx + dw - 1, dy + dh - 1);

			// border
			border.Rect = new Place(mx, my, dw - 1 - mx, dh - 1 - my);

			// list
			dialog[1].Rect = new Place(1 + mx, 1 + my, dw - 2 - mx, dh - 2 - my);

			// bottom
			if (bottom != null)
			{
				var xy = new Point(1 + mx, dh - 1 - my);
				bottom.Rect = new Place(xy.X, xy.Y, xy.X + bottomTextLength - 1, xy.Y);
			}
		}

		void OnConsoleSizeChanged(object sender, SizeEventArgs e)
		{
			MakeSizes((IDialog)sender, e.Size);
		}

		void OnKeyPressed(object sender, KeyPressedEventArgs e)
		{
			// Tab: go to next
			if (e.Key.VirtualKeyCode == KeyCode.Tab)
			{
				var box = (IListBox)e.Control;
				++box.Selected;
				e.Ignore = true;
				return;
			}

			var dialog = (IDialog)sender;

			//! break keys first
			var key = new KeyData(e.Key.VirtualKeyCode, e.Key.CtrlAltShift());
			myKeyIndex = myKeys.IndexOf(key);
			if (myKeyIndex >= 0)
			{
				if (myHandlers[myKeyIndex] == null)
				{
					dialog.Close();
				}
				else
				{
					_isKeyHandled = true;
					Selected = _box.Selected;
					if (_ii != null && Selected >= 0)
						Selected = _ii[Selected];

					var a = new MenuEventArgs((Selected >= 0 ? myItems[Selected] : null));
					myHandlers[myKeyIndex](Sender ?? this, a);
					if (a.Ignore)
					{
						e.Ignore = true;
						return;
					}
					dialog.Close();
					if (a.Restart)
					{
						myKeyIndex = -2;
						_ii = null;
						_toFilter = true;
					}
				}
				return;
			}

			// CtrlC: copy to clipboard
			if (e.Key.IsCtrl(KeyCode.C) || e.Key.IsCtrl(KeyCode.Insert))
			{
				var box = (IListBox)e.Control;
				Far.Api.CopyToClipboard(box.Text);
				e.Ignore = true;
				return;
			}

			// incremental
			if (IncrementalOptions == 0)
				return;

			if (key.Is(KeyCode.Backspace) || key.IsShift(KeyCode.Backspace))
			{
				if (_filter.Length == 0)
					return;

				// case: Shift, drop incremental
				if (key.IsShift())
				{
					Incremental = string.Empty;
					_ii = null;
					myKeyIndex = -2;
					_toFilter = false;
					dialog.Close();
					return;
				}

				if (_filter.Length > Incremental.Length || _filter.Length == Incremental.Length && Incremental.EndsWith("*"))
				{
					char c = _filter[_filter.Length - 1];
					_filter = _filter.Substring(0, _filter.Length - 1);
					_re = null;
					// * and ?
					if (0 == (IncrementalOptions & PatternOptions.Literal) && (c == '*' || c == '?'))
					{
						// update title/bottom
						GetInfo(out string t, out string b);
						dialog[0].Text = t;
						dialog[2].Text = b;
					}
					else
					{
						_ii = null;
						_toFilter = true;
					}
				}
			}
			else
			{
				var c = e.Key.Character;
				if (c >= ' ')
				{
					// keep and change filter
					var filterBak = _filter;
					var reBak = _re;
					_filter += c;
					_re = null;

					// * and ?
					if (0 == (IncrementalOptions & PatternOptions.Literal) && (c == '*' || c == '?'))
					{
						// update title/bottom
						GetInfo(out string t, out string b);
						dialog[0].Text = t;
						dialog[2].Text = b;
						return;
					}

					// try the filter, rollback on empty
					var iiBak = _ii;
					_toFilter = true;
					MakeFilter();
					if (_ii != null && _ii.Count == 0)
					{
						_filter = filterBak;
						_re = reBak;
						_ii = iiBak;
						return;
					}

					_toFilter = true;
				}
			}

			if (_toFilter)
				dialog.Close();
		}

		public override bool Show()
		{
			//! drop filter indexes because they are invalid on the second show if items have changed
			_ii = null;
			_toFilter = true;

			// main loop
			for (int pass = 0; ; ++pass)
			{
				// filter
				MakeFilter();

				// filtered item number
				int nItem2 = _ii == null ? myItems.Count : _ii.Count;
				if (nItem2 < 2 && AutoSelect)
				{
					if (nItem2 == 1)
					{
						Selected = _ii == null ? 0 : _ii[0];
						return true;
					}
					else if (pass == 0)
					{
						Selected = -1;
						return false;
					}
				}

				// title, bottom
				string title, info;
				GetInfo(out title, out info);

				// dialog
				var dialog = Far.Api.CreateDialog(1, 1, 1, 1);
				dialog.HelpTopic = string.IsNullOrEmpty(HelpTopic) ? "list-menu" : HelpTopic;
				dialog.NoShadow = NoShadow;
				dialog.TypeId = TypeId;

				// title
				dialog.AddBox(1, 1, 1, 1, title);

				// list
				_box = dialog.AddListBox(1, 1, 1, 1, string.Empty);
				_box.Selected = Selected;
				_box.SelectLast = SelectLast;
				_box.NoBox = true;
				_box.WrapCursor = WrapCursor;
				if (IncrementalOptions == PatternOptions.None)
				{
					_box.AutoAssignHotkeys = AutoAssignHotkeys;
					_box.NoAmpersands = !ShowAmpersands;
				}

				// "bottom"
				if (info.Length > 0)
					dialog.AddText(1, 1, 1, info);

				// items and filter
				_box.ReplaceItems(myItems, _ii);

				// now we are ready to make sizes
				MakeSizes(dialog, Far.Api.UI.WindowSize);

				// handlers
				dialog.ConsoleSizeChanged += OnConsoleSizeChanged;
				_box.KeyPressed += OnKeyPressed;

				// go!
				_toFilter = _isKeyHandled = false;
				myKeyIndex = -1;
				bool ok = dialog.Show();
				if (!ok)
					return false;
				if (myKeyIndex == -2 || _toFilter)
					continue;

				// correct by filter
				Selected = _box.Selected;
				if (_ii != null && Selected >= 0)
					Selected = _ii[Selected];

				// call click if a key was not handled yet
				if (Selected >= 0 && !_isKeyHandled)
				{
					var item = myItems[Selected];
					if (item.Click != null)
					{
						var e = new MenuEventArgs(item);
						item.Click(Sender ?? this, e);
						if (e.Ignore || e.Restart)
							continue;
					}
				}

				//! [Enter] on empty gives -1
				return Selected >= 0;
			}
		}
	}
}
