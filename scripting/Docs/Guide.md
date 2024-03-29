# RawAccelScript - Script Writer's Guide - By SaiMoen

RawAccelScript will be referred to as 'RAS' from this point.

## Introduction to RAS

RAS is a simple scripting language, made specifically for generating LookUpTable (LUT) curves inside of RawAccel automatically.

The motivation for its creation,
is the fact that most people who use LUT mode generate their curve with some external script (often written in Python).
Sharing this script and instructions on how to configure it, or even run it at all,
can be a hassle for the scripter and person trying to use the script.
RAS is here to change that situation and standardize LUT scripting.

## Script structure overview

### Description

A script starts with a description section, where the author can write pretty much anything.

(e.g.)

```
Arc mode by SaiMoen.

...
```

After that point, only specific characters are allowed (ASCII subset that the language accepts).

### Parameters

As soon as a `[` is read, the next part of the script begins.
This is the parameters section, which are the user-controlled variables passed in by the UI.

The syntax for parameter assignments is `name := default`, where default is the default value as a number.
The name can only be sequences of letters, separated by underscores.
For parameter names, underscores are replaced by spaces, so they look better in the UI.

Optionally, the script can also specify 'Guards' that cause the user to get an error,
if the value they entered is outside the allowed range.
They resemble the following form (represented in EBNF): `[ ( ">" | ">=" ) , number ] , [ ( "<" | "<=" ) , number ]`.

Alternatively, the value of a parameter can also be a boolean (`true` or `false`).
In this case, the Guards functionality is not available.
When used in expressions, the value 0 is used if the parameter is `false`, and the value 1 is used if the parameter is `true`.

Finally, the line is ended with `;`.
The section is ended with `]`.

(e.g.)

```
[

	Input_Offset := 0 >= 0;
	Limit := 4;
	Midpoint := 16 > 0;

]
```

A maximum of 8 parameters can be declared,
but it is recommended that you only expose very essential variables to the user.
This usually results in 2-4 parameters for normal modes.

Important to note is that after the parameters section, when a parameter is used,
it will be set to the value entered by the user, and the value 'assigned' to a parameter is only the default value.

### Variables

Next up is the variables section.
This section does not have any explicit delimiters, as it is placed between two sections that do.
This section is for hidden variables that improve the readability of the script.

The syntax for variable assignments is `name := expression`.
The expression can be any mathematical expression (more on that later).
For each variable declaration, parameters can be used, as well as previously declared variables.

Just like with parameters, the line is ended with `;`.

(e.g.)

```

	pLimit := Limit - 1;

```

If you want to signal that a variable has no meaningful intial value, you could use the `zero` keyword, but it's not required.

### Calculation

Finally, the calculation section is where we use these variables to calculate a LUT point.
It starts with a `{`, and ends with a `}`, after which only whitespace is allowed.

The input speed is given by the built-in variable `x`, which is set by the application.
The output speed is given by the built-in variable `y`, which is set to 1 upon entering the calculation section.

The goal of a script is modify `y` depending on the value of `x` and the parameters.
This can be done using control flow and maths (covered in the next part).
The control flow is as basic as it gets (without a `goto` statement), with only `if` and `while`.

These work as you most likely already expect, and this is their syntax:

```
if (condition):
	statement;
	statement;
:
```

^^ Only execute the statements if `condition` is true (i.e. nonzero).

```
while (condition):
	statement;
	statement;
:
```

^^ Execute the statements as long as `condition` is true (i.e. nonzero), 0 to many times.

The blocks of control flow are delimited by `:`, and not ended by `;`.
A statement can be one of the following:

1. Assignment
2. Return
3. Control flow (again)

Assignment has already been discussed, although other possible forms will come up later.
The return statement (`ret`) allows you to quit calculating early, if you feel like `y` already has the correct value.
Control flow can be nested, executing it means that the condition is evaluated, and the expected behavior occurs.

By combining these elements, you can represent many formulas.

(e.g.)

```
	{

		if (x > Input_Offset):
			x -= Input_Offset;
			y += (pLimit / x) * (x - Midpoint * atan(x / Midpoint));
		:

	}
```

(with an early return)

```
	{

		if (x <= Input_Offset):
			ret;
		:

		x -= Input_Offset;
		y += (pLimit / x) * (x - Midpoint * atan(x / Midpoint));

	}
```

## Mathematical Expressions

Alright, now the math part...
Note that the C# Double type is used for calculations,
therefore numbers do not have infinite precision and can accrue an error over the course of a calculation,
but this should not be a problem in the vast majority of cases,
as long as you are wary of strict equality comparison with numbers that have changed.

### Arithmetic

The `+` operator adds two numbers together.
The `-` operator subtracts two numbers.
The `*` operator multiplies two numbers.
The `/` operator divides two numbers.
The `%` operator does modular arithmetic (remainder after division).
The `^` operator raises the left number to the right (exponentiation).

(e.g)

```
2 + 2 == 4
3 - 1 == 2
4 * 2 == 8
2 / 4 == 0.5
13 % 5 == 3
2 ^ 4 == 16
```

### Inline Assignments

The normal assignment operator `:=` was already shown, and it changes the value of the target to the value of the expression.
The remaining operators perform a similar task, but modify the existing value of the target instead of only changing it.

```
+= -= *= /= %= ^=
```

(e.g.)
`x += 2;` is equivalent to `x := x + 2;`.
`y *= 4;` is equivalent to `y := y * 4;`.
`z ^= 1 + 2;` is equivalent to `z := z ^ (1 + 2);`.

### Logic

In this part, whether a number is true is determined by whether it is non-zero.

The first three operators are 'and', 'or', and 'not', respectively.
The 'and' operation will result in 1 when both numbers are non-zero, otherwise 0.
The 'or' operation will result in 1 when at least one number is non-zero, otherwise 0.
The 'not' operation will be 1 when the number is zero, and vice versa.

The other operators are for comparison.
Firstly, there is equality, which results in 1 when both numbers are equal.
Then, there is the opposite of that, which results in 1 when both numbers are not equal.
Then, there is (strictly) less than and greater than, respectively.
Lastly, there is less than or equal and greater than or equal, respectively.

```
& | !
== != < > <= >=
```

### Built-In Math keywords

[From the C# Math library](https://learn.microsoft.com/en-us/dotnet/api/system.math)

Constants:

```
e pi tau
```

Functions:

```
abs sqrt cbrt
round trunc ceil floor
log log2 log10
sin sinh asin asinh
cos cosh acos acosh
tan tanh atan atanh
fma scaleb
```