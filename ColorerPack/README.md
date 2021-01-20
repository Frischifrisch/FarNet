# ColorerPack - syntax and color schemes for Colorer

- fsharp.hrc
- graphql.hrc
- markdown.hrc
- [powershell.hrc](#powershell-hrc)
- [r.hrc](#r-hrc)
- [visual.hrd](#visual-hrd)

*********************************************************************
## powershell.hrc

Windows PowerShell syntax scheme. It is designed for PowerShell 2.0 and above.
In addition to standard language it recognizes some useful external features.

Together with *visual.hrd* it adds some colors to PowerShellFar console, the
Far Manager editor which works somewhat similar to PowerShell console.

### Outlined regions

- Functions and filters;
- Triple-hash line comments `###`;
- `task` entries (DSL of *Invoke-Build*, *psake*).

### Regex syntax

Regex syntax is automatically colored in string literals following the regex
type shortcut `[regex]` and regex operators (`-match`, `-replace`, ...):

<pre>
<span style='color:#008080'>[regex]</span><span style='color:#ff0000'>'(</span><span style='color:#ff0000'>?</span><span style='color:#0000ff'>i</span><span style='color:#ff0000'>)</span><span style='color:#0000ff'>^</span><span style='color:#800000'>text</span><span style='color:#0000ff'>$</span><span style='color:#ff0000'>'</span>
<span style='color:#0000ff'>-match</span> <span style='color:#ff0000'>'(</span><span style='color:#ff0000'>?</span><span style='color:#0000ff'>i</span><span style='color:#ff0000'>)</span><span style='color:#0000ff'>^</span><span style='color:#800000'>text</span><span style='color:#0000ff'>$</span><span style='color:#ff0000'>'</span>
<span style='color:#0000ff'>-replace</span> <span style='color:#ff0000'>'(</span><span style='color:#ff0000'>?</span><span style='color:#0000ff'>i</span><span style='color:#ff0000'>)</span><span style='color:#0000ff'>^</span><span style='color:#800000'>text</span><span style='color:#0000ff'>$</span><span style='color:#ff0000'>'</span>
</pre>

In addition to natural recognition regex syntax is colored in strings after the
conventional comment `<#regex#>`. The example also shows use of a here-string:

<pre>
<span style='color:#008000'>&lt;#regex#></span><span style='color:#ff0000'>@'</span>
<span style='color:#ff0000'>(?</span><span style='color:#0000ff'>ix</span><span style='color:#ff0000'>)</span>
<span style='color:#0000ff'>^</span><span style='color:#800000'> text1 </span><span style='color:#008000'>  # comment1</span>
<span style='color:#800000'> text2 </span><span style='color:#0000ff'>$</span><span style='color:#008000'>  # comment2</span>
<span style='color:#ff0000'>'@</span>
</pre>

Note useful inline regex options (see .NET manuals for the others):

- `(?i)` - *IgnoreCase* (remember, .NET regex is case sensitive by default);
- `(?x)` - *IgnorePatternWhitespace* (for multiline regex, inline comments, ...).

### SQL syntax

SQL syntax is colored in here-strings (`@'...'@`, `@"..."@`) after the
conventional comment `<#sql#>`:

<pre>
<span style='color:#008000'>&lt;#sql#></span><span style='color:#ff0000'>@'</span>
<span style='color:#0000ff'>SELECT</span> <span style='color:#0000ff'>*</span>
<span style='color:#0000ff'>FROM</span> table1
<span style='color:#0000ff'>WHERE</span> data1 <span style='color:#0000ff'>=</span> <span style='color:#ff0000'>@param1</span>
<span style='color:#ff0000'>'@</span>
</pre>

### PSF console output

- Output numbers, dates, times, and paths are colored.
- Output area markers `<=` and `=>` are invisible.
- Output areas have light gray background.

*********************************************************************
## r.hrc

R syntax scheme. R is a programming language and software environment for
statistical computing and graphics.
See [R project home page](http://www.r-project.org/).

### Features

- Outlined functions.
- Outlined triple-hash comments `###`.
- TODO, BUG, FIX, web-addresses, etc. in other comments.

The scheme covers most of R syntax quite well.
This code snippet highlights some typical features:

<pre>
<span style='color:#008000'>### Draws simple quantiles/ECDF</span>
<span style='color:#008000'># R home page: </span><span style='color:#0000ff'>http://www.r-project.org/</span>
<span style='color:#008000'># </span><span style='color:#800000; background:#c0c0c0; '>!!</span><span style='color:#008000'> see ecdf() {library(stats)} for a better example</span>
<span style='color:#000000'>plot</span><span style='color:#0000ff'>(</span><span style='color:#000000'>x</span> <span style='color:#0000ff'>&lt;-</span> <span style='color:#000000'>sort</span><span style='color:#0000ff'>(</span><span style='color:#000000'>rnorm</span><span style='color:#0000ff'>(</span><span style='color:#800080'>47</span><span style='color:#0000ff'>))</span><span style='color:#0000ff'>,</span> <span style='color:#000000'>type</span> <span style='color:#0000ff'>=</span> <span style='color:#ff0000'>"</span><span style='color:#800000'>s</span><span style='color:#ff0000'>"</span><span style='color:#0000ff'>,</span> <span style='color:#000000'>main</span> <span style='color:#0000ff'>=</span> <span style='color:#ff0000'>"</span><span style='color:#800000'>plot(x, type = </span><span style='color:#800000; background:#ffff00; '>\"</span><span style='color:#800000'>s</span><span style='color:#800000; background:#ffff00; '>\"</span><span style='color:#800000'>)</span><span style='color:#ff0000'>"</span><span style='color:#0000ff'>)</span>
<span style='color:#000000'>points</span><span style='color:#0000ff'>(</span><span style='color:#000000'>x</span><span style='color:#0000ff'>,</span> <span style='color:#000000'>cex</span> <span style='color:#0000ff'>=</span> <span style='color:#800080'>.5</span><span style='color:#0000ff'>,</span> <span style='color:#000000'>col</span> <span style='color:#0000ff'>=</span> <span style='color:#ff0000'>"</span><span style='color:#800000'>dark red</span><span style='color:#ff0000'>"</span><span style='color:#0000ff'>)</span>
</pre>

*********************************************************************
## visual.hrd

***visual.hrd***

Console color scheme with white background. It is initially called *visual* for
similarity to Visual Studio default colors used for some file types. It is also
designed to support *powershell.hrc* features and to customize appearance of
other schemes.

Another visual.hrd feature is colored console color codes in HRD files.

As far as color schemes are all about personal preferences, one may use this
scheme as the example in order to build an own scheme with similar features.

***visual-rgb.hrd***

RGB color scheme generated from *visual.hrd* with RGB values for standard
console colors. This scheme can be used with the *colorer.exe* in order to
create HTML files with the same colors as they are in the Far Manager editor
with *visual.hrd*.

*********************************************************************
