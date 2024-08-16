﻿using scripting.Lexing;
using scripting.Script;
using System.Collections.Generic;
using System.Diagnostics;

namespace scripting.Parsing;

/// <summary>
/// Defines the API of a RawAccelScript parser.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Makes the parser begin parsing (single-use only).
    /// </summary>
    /// <returns>Result of parsing</returns>
    /// <exception cref="ParserException"/>
    ParsingResult Parse();
}

/// <summary>
/// The result of parsing a list of lexical tokens.
/// </summary>
/// <param name="Description">The description of the script (usually derived from the 'comments' section in lexical analysis)</param>
/// <param name="Parameters">The user-controlled parameters</param>
/// <param name="Declarations">The declarations used by the script</param>
/// <param name="Callbacks">The callbacks parsed from the script</param>
public record ParsingResult(
    string Description,
    Parameters Parameters,
    IBlock Declarations,
    IList<ParsedCallback> Callbacks);

/// <summary>
/// The Ast Node interface, a safe wrapper over the tagged union used internally.
/// </summary>
public interface IASTNode
{
    ASTTag Tag { get; }

    ASTAssign? Assign { get; }
    ASTIf? If { get; }
    ASTWhile? While { get; }
    ASTFunction? Function { get; }
    ASTReturn? Return { get; }
}

/// <summary>
/// AST tag.
/// </summary>
public enum ASTTag : byte
{
    None,
    Assign,
    If, While,
    Function, Return,
}

public record ASTAssign(Token Identifier, Token Operator, ITokenList Initializer);
public record ASTIf(ITokenList Condition, IBlock If, IBlock? Else);
public record ASTWhile(ITokenList Condition, IBlock While);
public record ASTFunction(Token Identifier, ITokenList Args, IBlock Code);
public record ASTReturn();

/// <summary>
/// Represents a callback that has been parsed but not yet validated and emitted.
/// </summary>
/// <param name="Name">Name</param>
/// <param name="Args">Arguments</param>
/// <param name="Code">Code (as an AST)</param>
public record ParsedCallback(string Name, ITokenList Args, IBlock Code);

/// <summary>
/// Exception for parsing-related errors.
/// </summary>
public sealed class ParserException(string message, uint line) : GenerationException(message, line)
{
}