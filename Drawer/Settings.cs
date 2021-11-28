﻿
// FarNet module Drawer
// Copyright (c) Roman Kuzmin

using FarNet.Settings;
using System;
using System.Configuration;
using System.Text.RegularExpressions;

namespace FarNet.Drawer
{
	[SettingsProvider(typeof(ModuleSettingsProvider))]
	public sealed class Settings : ModuleSettings
	{
		public const string CurrentWordGuid = "a9a6f877-e049-4438-a315-d5914b200988";
		public const string CurrentWordName = "Current word";
		public const string FixedColumnGuid = "efe9454e-0284-4047-ba74-a00685fe40a6";
		public const string FixedColumnName = "Fixed column";

		public static Settings Default { get; } = new Settings();
		[UserScopedSetting]
		[DefaultSettingValue(@"\w[-\w]*")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public string CurrentWordPattern
		{
			get { return (string)this["CurrentWordPattern"]; }
			set { this["CurrentWordPattern"] = value; }
		}
		[UserScopedSetting]
		[DefaultSettingValue("Black")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public ConsoleColor CurrentWordColorForeground
		{
			get { return (ConsoleColor)this["CurrentWordColorForeground"]; }
			set { this["CurrentWordColorForeground"] = value; }
		}
		[UserScopedSetting]
		[DefaultSettingValue("Gray")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public ConsoleColor CurrentWordColorBackground
		{
			get { return (ConsoleColor)this["CurrentWordColorBackground"]; }
			set { this["CurrentWordColorBackground"] = value; }
		}
		[UserScopedSetting]
		[DefaultSettingValue("80")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public int FixedColumnNumber
		{
			get { return (int)this["FixedColumnNumber"]; }
			set { this["FixedColumnNumber"] = value; }
		}
		[UserScopedSetting]
		[DefaultSettingValue("Black")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public ConsoleColor FixedColumnColorForeground
		{
			get { return (ConsoleColor)this["FixedColumnColorForeground"]; }
			set { this["FixedColumnColorForeground"] = value; }
		}
		[UserScopedSetting]
		[DefaultSettingValue("Gray")]
		[SettingsManageability(SettingsManageability.Roaming)]
		public ConsoleColor FixedColumnColorBackground
		{
			get { return (ConsoleColor)this["FixedColumnColorBackground"]; }
			set { this["FixedColumnColorBackground"] = value; }
		}
		public override void Save()
		{
			if (string.IsNullOrWhiteSpace(CurrentWordPattern))
				throw new ModuleException("Empty current word pattern is invalid.");

			try
			{
				new Regex(CurrentWordPattern, RegexOptions.IgnorePatternWhitespace);
			}
			catch (ArgumentException ex)
			{
				throw new ModuleException("Invalid current word pattern: " + ex.Message);
			}

			base.Save();
		}
	}
}
