# RightControl

FarNet module RightControl for Far Manager

*********************************************************************
## Synopsis

This tool alters some operations in editors, edit boxes, and the command line.
They are: *Step*, *Select*, *Delete* by words, *Go*, *Select* to *smart home*.
New actions are similar to what many popular editors do on stepping, selecting,
deleting by words, and etc. Example: Visual Studio, Word, WordPad, etc.

**Project**

 * Source: <https://github.com/nightroman/FarNet/tree/master/RightControl>
 * Author: Roman Kuzmin

*********************************************************************
## Installation

**Requirements**

 * Far Manager
 * Package FarNet
 * Package FarNet.RightControl

**Instructions**

How to install and update FarNet and modules:\
<https://github.com/nightroman/FarNet#readme>

After installing the module copy the macro file
*RightControl.macro.lua* to *%FARPROFILE%\Macros\scripts*.

*********************************************************************
## Commands

The module works by commands called from macros associated with keys:

**Word commands**

- `step-left ~ [CtrlLeft]`
- `step-right ~ [CtrlRight]`
- `select-left ~ [CtrlShiftLeft]`
- `select-right ~ [CtrlShiftRight]`
- `delete-left ~ [CtrlBS]`
- `delete-right ~ [CtrlDel]`
- `vertical-left ~ [CtrlAltLeft]`
- `vertical-right ~ [CtrlAltRight]`

**Smart home commands**

- `go-to-smart-home ~ [Home]`
- `select-to-smart-home ~ [ShiftHome]`

*********************************************************************
## Settings

Module settings panel: `[F11] \ FarNet \ Settings \ RightControl`

- `Regex`

    A regular expression pattern that defines text break points.

**Regex examples**

Default pattern. Breaks are very similar to Visual Studio:

    ^ | $ | (?<=\b|\s)\S

Pattern with breaks similar to Word/WordPad. "_" breaks, too:

    ^ | $ | (?<=\b|\s)\S | (?<=[^_])_ | (?<=_)[^_\s]

Default pattern with two more breaks: letter case and number breaks:

    ^ | $ | (?<=\b|\s)\S | (?<=\p{Ll})\p{Lu} | (?<=\D)\d | (?<=\d)[^\d\s]

The same pattern written with inline comments. All the text below is a valid
regular expression pattern that can be stored in settings just like that:

```
^ | $ # start or end of line
|
(?<=\b|\s)\S # not a space with a word bound or a space before
|
(?<=\p{Ll})\p{Lu} # an upper case letter with a lower case letter before
|
(?<=\D)\d | (?<=\d)[^\d\s] # a digit/not-digit with a not-digit/digit before
```

*********************************************************************
