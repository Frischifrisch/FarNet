# Explore

FarNet module Explore for Far Manager

* [Synopsis](#synopsis)
* [Installation](#installation)
* [Command syntax](#command-syntax)
* [Result panel](#result-panel)
* [Examples](#examples)

*********************************************************************
## Synopsis

The tool searches in FarNet module panels and opens the result panel.
It is invoked from the command line with the prefix *Explore:*.

**Project**

 * Source: <https://github.com/nightroman/FarNet/tree/master/Explore>
 * Author: Roman Kuzmin

*********************************************************************
## Installation

**Requirements**

 * Far Manager
 * Package FarNet
 * Package FarNet.Explore

**Instructions**

How to install and update FarNet and modules:\
<https://github.com/nightroman/FarNet#readme>

*********************************************************************
## Command syntax

Syntax:

    Explore: [<Mask>] [-Directory] [-Recurse] [-Depth <N>] [-Asynchronous] [-XFile <File>] [-XPath <Expr>]

- `<Mask>`

    Classic Far Manager file name mask including exclude and regex forms.
    Use " to enclose a mask with spaces.

- `-Directory`

    Tells to include directories into the search process and results.

- `-Recurse`

    Tells to search through all directories and sub-directories.

- `-Depth <N>`

    N: 0: ignored; negative: unlimited; positive: search depth, -Recurse is
    ignored. Note: order of -Depth and -Recurse results may be different.

- `-Asynchronous`

    Tells to perform the search in the background and open the result panel
    immediately. Results are added dynamically when the panel is idle.

- `-XFile <File>`

    Tells to read the XPath expression from the file. Use the *.xq* extension
    for files (Colorer processes them as *xquery* which is fine for XPath).

- `-XPath <Expr>`

    The XPath has to be the last parameter because the rest of the command line
    is used as the XPath expression. The Mask can be used with XPath. Recurse
    and Depth parameters are nor used with XPath or XFile.


*********************************************************************
## Result panel

The result panel provides the following keys and operations

- `[Enter]`

    On a found directory opens this directory in its explorer panel as if
    `[Enter]` is pressed in the original panel. The opened panel works as the
    original. `[Esc]` (or more than one) returns to the search result panel.

    On a found file opens it if its explorer supports file opening.

- `[CtrlPgUp]`

    On a found directory or file opens its parent directory in its original
    explorer panel and the item is set current. The opened panel works as usual.
    `[Esc]` returns to the search result panel.

- `[F3]/[F4]`

    On a found file opens not modal viewer/editor if the original explorer
    supports file export. If file import is supported then the files can be
    edited. For now import is called not on saving but when an editor exits.

- `[F5]/[F6]`

    Copies/moves the selected items to their explorer panels.

- `[F7]`

    Just removes the selected items from the result panel.

- `[F8]/[Del]`

    Deletes the selected items if their explorers support this operation.

- `[Esc]`

    Prompts to choose: *[Close]* or *[Push]* the result panel, or *[Stop]* the
    search if it is still in progress in the background.


*********************************************************************
## Examples

All examples are for the FileSystem provider panel of the PowerShellFar module.

---

Find directories and files with names containing "far" recursively:

    Explore: -Directory -Recurse *far*

---

Mixed filter (mask and XPath expression with file attributes):

    Explore: *.dll;*.xml -XPath //File[compare(@LastWriteTime, '2011-04-23') = 1 and @Length > 100000]

Note: `compare()` is a helper function added by FarNet.

---

Find empty directories excluding *.svn*:

    Explore: -XPath //Directory[not(Directory | File) and not((../.. | ../../..)/*[@Name = '.svn'])]

or:

    Explore: -XFile empty-directory.xq

where the file *empty-directory.xq* may look like this:

    //Directory
    [
        not(Directory | File)
        and
        not((../.. | ../../..)/*[@Name = '.svn'])
    ]

---

Find *.sln* files with *.csproj* files in the same directory:

    Explore: -XFile sln-with-csproj.xq

where *sln-with-csproj.xq*:

    //File
    [
        is-match(@Name, '(?i)\.sln$')
        and
        ../File[is-match(@Name, '(?i)\.csproj$')]
    ]

Note: `is-match()` is a helper function added by FarNet.

*********************************************************************
