using scripting.Common;
using scripting.Lexing;
using scripting.Script;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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
/// <param name="Variables">The variables used by the script</param>
/// <param name="Callbacks">The callbacks parsed from the script</param>
public record ParsingResult(
    string Description,
    Parameters Parameters,
    IList<ASTAssign> Variables,
    IList<ParsedCallback> Callbacks);

/// <summary>
/// Represents a callback that has been parsed but not yet validated and emitted.
/// </summary>
/// <param name="Name">Name</param>
/// <param name="Args">Arguments</param>
/// <param name="Code">Code (as an AST)</param>
public record ParsedCallback(string Name, ITokenList Args, Block Code);

/// <summary>
/// Saves a statement as an AST node (tagged union).
/// </summary>
/// <param name="Tag">Tag</param>
/// <param name="Union">Union</param>
public readonly record struct ASTNode(ASTTag Tag, ASTUnion Union);

/// <summary>
/// Statement tag.
/// </summary>
public enum ASTTag
{
    None,
    Assign,
    Return,
    If, While,
}

// using structs here absolutely scares the jeepers out of the CLR at runtime
public record ASTAssign(Token Identifier, Token Operator, ITokenList Initializer);
public record ASTReturn();
public record ASTIf(ITokenList Condition, Block If, Block? Else);
public record ASTWhile(ITokenList Condition, Block While);

/// <summary>
/// Union of all possible statements.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct ASTUnion
{
    [FieldOffset(0)] public ASTAssign astAssign;
    [FieldOffset(0)] public ASTReturn astReturn;
    [FieldOffset(0)] public ASTIf astIf;
    [FieldOffset(0)] public ASTWhile astWhile;
}

/// <summary>
/// Exception for parsing-related errors.
/// </summary>
public sealed class ParserException(string message, uint line) : GenerationException(message, line)
{
}
