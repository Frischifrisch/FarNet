﻿
// FarNet module RightWords
// Copyright (c) Roman Kuzmin

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FarNet.RightWords
{
	[ModuleDrawer(Name = "Spelling mistakes", Mask = "*.hlf;*.htm;*.html;*.lng;*.restext", Priority = 1)]
	[System.Runtime.InteropServices.Guid("bbed2ef1-97d1-4ba2-ac56-9de56bc8030c")]
	public class Highlighter : ModuleDrawer
	{
		readonly MultiSpell Spell = MultiSpell.Get();
		readonly Regex RegexSkip = Actor.GetRegexSkip();
		readonly Regex RegexWord = new Regex(Settings.Default.WordPattern, RegexOptions.IgnorePatternWhitespace);
		readonly HashSet<string> CommonWords = Actor.GetCommonWords();
		readonly ConsoleColor HighlightingBackgroundColor = Settings.Default.HighlightingBackgroundColor;
		readonly ConsoleColor HighlightingForegroundColor = Settings.Default.HighlightingForegroundColor;
		readonly int MaximumLineLength = Settings.Default.MaximumLineLength;
		public override void Invoke(IEditor editor, ModuleDrawerEventArgs e)
		{
			foreach (var line in e.Lines)
			{
				var text = line.Text;
				if (text.Length == 0)
					continue;

				if (MaximumLineLength > 0 && text.Length > MaximumLineLength)
				{
					e.Colors.Add(new EditorColor(
						line.Index,
						0,
						text.Length,
						HighlightingForegroundColor,
						HighlightingBackgroundColor));
					continue;
				}

				MatchCollection skip = null;
				for (var match = RegexWord.Match(text); match.Success; match = match.NextMatch())
				{
					// the target word
					var word = Actor.MatchToWord(match);

					// check cheap skip lists
					if (CommonWords.Contains(word) || Actor.IgnoreWords.Contains(word))
						continue;

					// check spelling, expensive but better before the skip pattern
					if (Spell.Spell(word))
						continue;

					// expensive skip pattern
					if (Actor.HasMatch(skip ?? (skip = Actor.GetMatches(RegexSkip, text)), match))
						continue;

					// add color
					e.Colors.Add(new EditorColor(
						line.Index,
						match.Index,
						match.Index + match.Length,
						HighlightingForegroundColor,
						HighlightingBackgroundColor));
				}
			}
		}
	}
}
