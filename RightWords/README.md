﻿# RightWords

FarNet module for Far Manager, spell-checker and thesaurus.

* [Synopsis](#synopsis)
* [Installation](#installation)
* [Description](#description)
* [Dictionaries](#dictionaries)
* [Options](#options)
* [Settings](#settings)

*********************************************************************
## Synopsis

RightWords is the FarNet module for Far Manager. It provides the spell-checker
and thesaurus based on NHunspell. The core Hunspell is used in OpenOffice and
it works with dictionaries published on OpenOffice.org.

**Project**

 * Source: <https://github.com/nightroman/FarNet/tree/master/RightWords>
 * Author: Roman Kuzmin

*********************************************************************
## Installation

**FarNet and RightWords**

How to install and update FarNet and modules:\
<https://github.com/nightroman/FarNet#readme>

**Dictionaries**

The *NHunspell* library is included to the package but dictionaries are not.

OpenOffice dictionaries:
<http://wiki.services.openoffice.org/wiki/Dictionaries>

Copy dictionaries to new subdirectories of the directory *NHunspell*,
e.g. to directories *English* and *Russian*.

---

The installed file structure (dictionaries may be different):

    %FARHOME%\FarNet\Modules\RightWords

        About-RightWords.htm - documentation
        RightWords.macro.lua - sample macros
        History.txt - the change log
        LICENSE.txt - the license

        RightWords.dll - module assembly
        RightWords.resources - English UI strings
        RightWords.ru.resources - Russian UI strings

    %FARHOME%\FarNet\NHunspell

        NHunspell.dll, Hunspellx86.dll, Hunspellx64.dll

        English
            en_GB.aff, en_GB.dic - spelling dictionaries
            th_en_US_v2.dat - thesaurus (optional)

        Russian
            ru_RU.aff, ru_RU.dic - spelling dictionaries
            th_ru_RU_v2.dat - thesaurus (optional)

NOTES

- Do not rename or move the NHunspell directory.
- Collection of dictionaries is up to a user.
- Thesaurus files are optional.
- Dictionary directories may have any names. The names are used in the
  dictionary menu and in the user dictionary file names (e.g. English ->
  RightWords.English.dic).

*********************************************************************
## Description

In order to turn "Spelling mistakes" highlighting on and off use the menu:
`[F11] \ FarNet \ Drawers \ Spelling mistakes`

For other actions use the module menu `[F11] \ RightWords`:

- Correct word (editor, dialog, command line)

    Checks spelling and shows the suggestion menu for the current word. Menu
    actions are the same as for *Correct text*.

- Correct text (editor)

    Checks spelling, shows suggestions, and corrects words in the selected text
    or starting from the caret position. `[Enter]` in the suggestion menu
    replaces the highlighted word with the selected suggestion.

- Menu commands
    - *Ignore* - ignores the word once
    - *Ignore All* - ignores the word in the current session
    - *Add to Dictionary* - adds the word to the user dictionary

<!---->

- Thesaurus

    Prompts to enter a word and shows the list of available meanings and
    synonyms. `[Enter]` in the menu copies the current item text to the
    clipboard.

*********************************************************************
## Dictionaries

*Add to Dictionary* command supports the common and language dictionaries. In
order to add a word into a language dictionary two stems should be provided: a
new word stem and its example stem. If the example stem is empty then the word
is added as it is, this case is not very different from the common dictionary.

Examples:

English stems: plugin + pin
These forms become correct:

    plugin   plugins   Plugin   Plugins

Russian stems: плагин + камин
These forms become correct:

    плагин   плагины   Плагин   Плагины
    плагина  плагинов  Плагина  Плагинов
    плагину  плагинам  Плагину  Плагинам
    плагином плагинами Плагином Плагинами
    плагине  плагинах  Плагине  Плагинах

CAUTION: Mind word capitalization, e.g. add "plugin", not "Plugin".

User dictionaries are UTF-8 text files in the module roaming directory:
*RightWords.dic* (common) and files like *RightWords.XYZ.dic* (languages).

*********************************************************************
## Options

`[F9] \ Options \ Plugin configuration \ FarNet \ Drawers \ Spelling mistakes`

* Mask - mask of files where the "Spelling mistakes" is turned on automatically.
* Priority - drawer color priority.

*********************************************************************
## Settings

Open the module settings panel: `[F11] \ FarNet \ Settings \ RightWords`

Regular expression patterns are created with IgnorePatternWhitespace option, so
that they support line comments (`#`) and all white spaces should be explicitly
specified as `\ `, `\t`, `\s`, etc.

**WordPattern**

Defines the regular expression pattern for word recognition in texts.

The default pattern: `[\p{Lu}\p{Ll}]\p{Ll}+` (words with 2+ letters,
"RightWords" is treated as "Right" and "Words")

All capturing groups `(...)` are removed from the word before spell-checking.
This is used for checking spelling of words with embedded "noise" parts, like
the hotkey markers `&` in *.lng* or *.restext* files. Use not capturing groups
`(?:)` in all other cases where grouping is needed.

NOTE: Nested capturing groups are not supported and they are not really needed.
For performance reasons no checks are done in order to detected nested groups.

Example pattern for *.lng* and *.restext* files:
`[\p{Lu}\p{Ll}](?:\p{Ll}|(&))+`

**SkipPattern**

Defines the regular expression pattern for text areas to be ignored. The
default pattern is null (not specified, nothing is ignored).

Sample pattern:

    \w*\d\w* # words with digits
    | "(?:\w+:|\.+)?[\\/][^"]+" # quoted path-like text
    | (?:\w+:|\.+)?[\\/][^\s]+ # simple path-like text

**HighlightingBackgroundColor**\
**HighlightingForegroundColor**

Highlighting colors: Black, DarkBlue, DarkGreen, DarkCyan, DarkRed,
DarkMagenta, DarkYellow, Gray, DarkGray, Blue, Green, Cyan, Red, Magenta,
Yellow, White.

**UserDictionaryDirectory**

The custom directory of user dictionaries. Environment variables are expanded.
The default is the module roaming directory.

**MaximumLineLength**

If it is set to a positive value then it tells to not check too long lines and
highlight them all. Otherwise too long lines may cause lags on highlighting.
