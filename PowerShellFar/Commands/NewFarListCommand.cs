
// PowerShellFar module for Far Manager
// Copyright (c) Roman Kuzmin

using FarNet;
using System.Management.Automation;

namespace PowerShellFar.Commands
{
	//! Why _set*: we set preferences from settings first, then change them if _set*.
	[OutputType(typeof(IListMenu))]
	class NewFarListCommand : BaseMenuCmdlet
	{
		[Parameter]
		public SwitchParameter AutoSelect
		{
			get { return _AutoSelect; }
			set
			{
				_AutoSelect = value;
				_setAutoSelect = true;
			}
		}
		SwitchParameter _AutoSelect;
		bool _setAutoSelect;

		[Parameter]
		public string Incremental
		{
			get { return _Incremental; }
			set
			{
				_Incremental = value;
				_setIncremental = true;
			}
		}
		string _Incremental;
		bool _setIncremental;

		[Parameter]
		public PatternOptions IncrementalOptions
		{
			get { return _IncrementalOptions; }
			set
			{
				_IncrementalOptions = value;
				_setIncrementalOptions = true;
			}
		}
		PatternOptions _IncrementalOptions;
		bool _setIncrementalOptions;

		[Parameter]
		public int ScreenMargin
		{
			get { return _ScreenMargin; }
			set
			{
				_ScreenMargin = value;
				_setScreenMargin = true;
			}
		}
		int _ScreenMargin;
		bool _setScreenMargin;

		[Parameter]
		public SwitchParameter UsualMargins
		{
			get { return _UsualMargins; }
			set
			{
				_UsualMargins = value;
				_setUsualMargins = true;
			}
		}
		SwitchParameter _UsualMargins;
		bool _setUsualMargins;

		[Parameter]
		public SwitchParameter Popup { get; set; }

		internal IListMenu Create()
		{
			IListMenu menu = Far.Api.CreateListMenu();
			Init(menu);

			if (Popup)
				Settings.Default.PopupMenu(menu);
			else
				Settings.Default.ListMenu(menu);

			if (_setAutoSelect)
				menu.AutoSelect = _AutoSelect;
			if (_setIncremental)
				menu.Incremental = _Incremental;
			if (_setIncrementalOptions)
				menu.IncrementalOptions = _IncrementalOptions;
			if (_setNoShadow)
				menu.NoShadow = _NoShadow;
			if (_setScreenMargin)
				menu.ScreenMargin = _ScreenMargin;
			if (_setUsualMargins)
				menu.UsualMargins = _UsualMargins;

			return menu;
		}

		protected override void BeginProcessing()
		{
			WriteObject(Create());
		}
	}
}
