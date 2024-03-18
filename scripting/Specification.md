# RawAccelScript - A Specification - By SaiMoen

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

### Common

Utils-esque namespace.
Contains:

- Helper classes (used throughout the stages of generation)

### Script

Models elements of .ras source code.
Contains:

- Number, the number/boolean literals
- Parameter, the user-controlled variables
- Variable, the temporary variables declared after the parameters

### Lexical

Lexical Analysis namespace.
Contains:

- ILexer, the public API for lexing.
- Lexer (a.k.a. Tokenizer)
	- Does lexical analysis on a string (the user-made script).
	- Produces a LexingResult object that contains the comments as a string and the tokens in lexical order.
- Helper classes

### Syntactical

Syntactic Analysis namespace.
Contains:

- IParser, the public API for parsing.
- Parser
	- Does syntactic analysis on a list of tokens (produced by the lexer).
	- Produces a ParsingResult object that contains the script description, parameters, variables, the parsed list of tokens.
- Helper classes

### Interpretation

Interpreter namespace.
Contains:

- IInterpreter, the public API for interpreting parsed scripts and controlling an interpreter.
- Interpreter
	- Runs Programs, keeps track of parameters and variables.
- Program
	- Effectively just the parsed list of tokens with some thin semantic analysis on top.
- Helper classes

## Scripting language specification

The input variable will be `x`, and the output variable will be whatever is in `y` (1 by default).
Using control flow cleverly should make it so that multiple return, or even return at all, is not necessary,
so there is not a return keyword.

### Sections

#### Comments (1)

This section exists because the tokenizer is waiting for the next section to commence.
Therefore, the script writer is allowed to write anything here, and this text is saved.

#### Parameters (2)

Delimited by `[]`

This section holds a maximum of 8 parameters.
These parameters correspond to RawAccel parameters in the UI, and are bound accordingly.

The assignment format is fixed, and only allows the script writer to assign a number to them,
optionally a minimum and maximum value as well, either exclusive or inclusive.
These numbers are parsed immediately upon going through the Parser,
and can be queried through the Interpreter as soon as the constructor is done.
When the parameters are changed, the Interpreter can be updated through a property.

#### Variables (3)

This section holds temporary variables that store expressions.
The expressions can only contain Parameters, Constants and Numbers.
The maximum amount of Variables should be at least 256 minus the maximum amount of Parameters,
because of it enabling 1-byte addressing, but is otherwise left to implementation details.

#### Calculation (4)

Delimited by `{}`

This section is where the calculation actually happens.
A statement does not just have to be normal assignment, but can also be inline assignment,
or a branch statement. A branch statement itself can contain multiple statements.
It starts with a condition, that makes it jump past the block if the input is equal to 0.
Inline assignment performs a calculation with the expression after it and the old value of the variable,
and assigns it to that variable.
Parameters cannot be assigned to, only Input, Output and Variables.
Expressions however, can contain all of those, but numbers will have to appear in the variable declarations.

Input variable 'x' will be selected externally, and then given to the Interpreter through a public method.
At the same time, Output variable 'y' will be accumulating until the end of the Calculation block,
It starts at 1, which means by default the smallest possible script "[]{}" will generate a static curve.
After the Calculation block it will be returned to the caller of the aforementioned public method.

### Keywords

```
x y           "Input/Output variables"
false true    "Boolean values (0 and 1 respectively)"
zero          "Used for quick unary minus/booleans (zero - x == -x && zero == false && !zero == true)"
e pi tau      "Math Constants"

if (c): s :       "c means condition, s means statements"
while (c): s :    "c means condition, s means statements"
```

### Separators/Delimiters

```
.      "Floating Point"
,      "Function argument separator"
:      "Control Flow Block"
;      "Line Terminator"
( )    "Precedence"
[ ]    "Parameters"
{ }    "Calculation"
```

### Operators

```
:=                   "(Re-)Assignment"
+= -= *= /= %= ^=    "Inline Arithmetic"
+ - * / % ^          "Arithmetic"
& | !                "Logical Operators"
== != < > <= >=      "Comparison Operators"
```

Due to a tokenizer hack turned feature, operators must be 1 character normally,
but can optionally 'append' `=`, which changes their meaning (usually to comparison or assignment, depends on operator).

### Functions

```
abs sqrt cbrt
round trunc ceil floor
log log2 log10
sin sinh asin asinh
cos cosh acos acosh
tan tanh atan atanh
fma scaleb

pow, exp    "indirectly, with the ^ operator"
```

These are all C# System.Math-supported functions that even have the possibility of being useful here.