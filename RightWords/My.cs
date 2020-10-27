﻿
// FarNet module RightWords
// Copyright (c) Roman Kuzmin

using System;

namespace FarNet.RightWords
{
	static class My
	{
		public const string GuidString = "ca7ecdc0-f446-4bff-a99d-06c90fe0a3a9";
		public readonly static Guid Guid = new Guid(GuidString);
		#region private
		static readonly IModuleManager Manager = Far.Api.GetModuleManager(Settings.ModuleName);
		static string GetString(string name) { return Manager.GetString(name); }
		#endregion
		static public string AddToDictionary { get { return GetString("AddToDictionary"); } }
		static public string DoIgnore { get { return GetString("DoIgnore"); } }
		static public string DoIgnoreAll { get { return GetString("DoIgnoreAll"); } }
		static public string DoAddToDictionary { get { return GetString("DoAddToDictionary"); } }
		static public string Word { get { return GetString("Word"); } }
		static public string Thesaurus { get { return GetString("Thesaurus"); } }
		static public string DoCorrectWord { get { return GetString("DoCorrectWord"); } }
		static public string DoCorrectText { get { return GetString("DoCorrectText"); } }
		static public string DoThesaurus { get { return GetString("DoThesaurus"); } }
		static public string Common { get { return GetString("Common"); } }
		static public string Searching { get { return GetString("Searching"); } }
		static public string NewWord { get { return GetString("NewWord"); } }
		static public string ExampleStem { get { return GetString("ExampleStem"); } }
	}
}
