# RawAccelScript - By SaiMoen

A way for users to automatically generate LUT points,
while being nearly as easy to set up as preset modes.
It provides a safe way to load someone else's code into RawAccel,
and have the language it's written in to actually be designed for this use case.

## Motivation

Ever since LUT released, people have used external tools to create curves,
since creating the points manually is highly impractical and leads to weirdly overfitted curves.

The problem with the external tools however, is that they present a barrier of entry for the average user to overcome.
This barrier mostly consists of having to mess with and run someones python script (a security problem on its own),
use something like WebPlotDigitizer and having to deal with whatever problems that might cause,
or copy a Google sheet to their drive and edit certain values and then copy those (somehow).
After all that, they get to paste that into a box which can't even show most of it, nor can the 'applied values' box.
The only upside to this is that with the maximum amount of points, Angle Snapping gets hidden better down the settings file.

All of this just to do some math that they probably don't care too much about,
to generate and save some points that they probably don't care about.
The only thing they really need is a formula that integrates with RA,
and allows the grapher to automatically calculate the points based on given parameters.

## Basic File Structure

Proposed file name extension for custom scripts = `.ras` (RawAccelScript)

The public interface of a loaded script file will be `IScriptFile`.
There will be a `Wrapper` static class to easily load files.

### Docs

Documentation (like this document).

### Compiler

Converts scripts into bytecode which an interpreter can run.

Important classes:
- Lexer, produces tokens based on the input text.
- Parser, checks if the tokens follow the grammar of the language, creates an AST (Abstract Syntax Tree).
- Emitter, traverses the AST to produce bytecode, creates runnable Programs.
- Interpreter, executes Programs.

## Scripting Language

The input variable will be `x`, and the output variable will be whatever is in `y` (1 by default).

### Sections

#### Description (1)

This section exists because the tokenizer is waiting for the next section to commence.
Therefore, the script writer is allowed to write anything here, and this text is saved.

#### Parameters (2)

Delimited by `[]`

This section holds a maximum of 8 parameters.
These parameters correspond to RawAccel parameters in the UI, and are bound accordingly.

The assignment format is strict, and only allows the script writer to assign a number to them,
optionally a minimum and maximum value as well, either exclusive or inclusive.
These values are parsed immediately upon going through the Parser,
and can be queried through the ScriptFile instance.
When the parameters are changed in the UI, the ScriptFile instance can be updated through a method.

#### Declarations (3)

This section holds declarations:
- Callbacks
- (Global) Variables
- Functions

Declarations can only be used after they have been declared, from top-to-bottom.
Modern general purpose languages often do out-of-order global declarations, but this is a domain-specific language anyway,
and doing ordered declarations at global scope does make it easy to prevent circular definitions (by not making them possible).

There are 2 kinds of callbacks:

The calculation callback is mandatory to implement.
It is declared with `callback calculation`, followed by a block, and it does not take any arguments.
This callback should calculate the output value `y`, given some input value `x`.

The distribution callback is optional to implement.
It is declared with `callback distribution`, optionally followed by an argument, followed by a block.
An example of the argument being included in the declaration would be `callback distribution(200)`.
The meaning of the argument is how many points the distribution will have.
By default this is the maximum number allowed by the RawAccel driver.
This callback should alter the distribution of input values given to the calculation callback.

There are 3 kinds of global variable declarations:
- Immutable, for variables that cannot be modified (which automatically makes them persistent as well).
- Persistent, for variables that persist while a series of callbacks is being made.
- Impersistent, for variables that always reset after a callback runs.

For example, if an array of input values is put through the calculation callback,
impersistent variables would be reset after each calculation, whereas the values of other variables would persist.

The initializer of any variable can be left out (e.g. `var v;`), which will zero-initialize it instead.

Last but not least, custom functions can also be defined.
These will compute a value based on the given arguments as well as global variables.
The arguments are passed by value.

### Keywords

```
x y          "Input/Output variables"
false true   "Boolean values (0 and 1 respectively)"
e pi tau     "Math Constants"
capacity     "LUT_POINTS_CAPACITY from rawaccel-base.hpp"

const      "Immutable 'variable'"
let        "Persistent variable"
var        "Impersistent variable"
fn         "Function"
callback   "Callback"
```


Control Flow in Extended Backus-Naur Form (sort of).

```
if    = 'if' condition '{' { statement } '}' [ else '{' { statement } '}' ] ;
while = 'while' condition '{' { statement } '}' ;
```

`condition` is just an expression, which makes the program jump over the if-block/while-block if it evaluates to zero.

### Separators/Delimiters

```
.     "Decimal Point"
,     "Function argument separator"
;     "Line Terminator"
( )   "Grouping/Exclusive Bounds"
[ ]   "Parameters/Inclusive Bounds"
{ }   "Blocks/Open Bounds"
```

### Operators

```
:=                  "(Re-)Assignment"
+= -= *= /= %= ^=   "Inline Arithmetic"
+ - * / % ^         "Arithmetic"
& | !               "Logical Operators"
== != < > <= >=     "Comparison Operators"
```

Due to a tokenizer hack turned feature, operators must be 1 character normally,
but can optionally 'append' `=`, which changes their meaning (usually to comparison or assignment, depends on operator).

### Functions

```
abs sign copysign
round trunc floor ceil clamp
min max minm maxm

sqrt cbrt
log log2 log10 logb

sin sinh asin asinh
cos cosh acos acosh
tan tanh atan atanh atan2

fma scaleb

pow, exp   "indirectly, with the ^ operator"
```

These are all C# System.Math-supported functions that even have the possibility of being useful here.