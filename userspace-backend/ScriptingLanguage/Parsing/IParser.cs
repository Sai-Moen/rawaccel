using System.Collections.Generic;
using System.Runtime.InteropServices;
using userspace_backend.ScriptingLanguage.Lexing;
using userspace_backend.ScriptingLanguage.Script;

namespace userspace_backend.ScriptingLanguage.Parsing;

/// <summary>
/// Defines the API of a RawAccelScript parser.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Makes the parser begin parsing (single-use only).
    /// </summary>
    /// <returns>Result of parsing.</returns>
    /// <exception cref="ParserException"/>
    ParsingResult Parse();
}

/// <summary>
/// The result of parsing a list of lexical tokens.
/// </summary>
/// <param name="Description">The description of the script (usually derived from the 'comments' section in lexical analysis).</param>
/// <param name="SymbolSideTable">The symbol side-table.</param>
/// <param name="Parameters">The user-controlled parameters.</param>
/// <param name="Declarations">The declarations used by the script.</param>
/// <param name="Callbacks">The callbacks parsed from the script.</param>
public record ParsingResult(
    string Description,
    IList<string> SymbolSideTable,
    Parameters Parameters,
    IList<ASTNode> Declarations,
    IList<ParsedCallback> Callbacks);

/// <summary>
/// Saves a statement as an AST node (tagged union).
/// </summary>
/// <param name="Tag">Tag.</param>
/// <param name="Union">Union.</param>
public readonly record struct ASTNode(ASTTag Tag, ASTUnion Union);

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

/// <summary>
/// Union of all possible statements.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct ASTUnion
{
    [FieldOffset(0)] public ASTAssign astAssign;
    [FieldOffset(0)] public ASTIf astIf;
    [FieldOffset(0)] public ASTWhile astWhile;
    [FieldOffset(0)] public ASTFunction astFunction;
    [FieldOffset(0)] public ASTReturn astReturn;
}

public record ASTAssign(Token Identifier, Token Operator, Token[] Initializer);
public record ASTIf(Token[] Condition, ASTNode[] If, ASTNode[] Else);
public record ASTWhile(Token[] Condition, ASTNode[] While);
public record ASTFunction(Token Identifier, ASTNode[] Code);
public record ASTReturn();

/// <summary>
/// Represents a callback that has been parsed but not yet validated and emitted.
/// </summary>
/// <param name="Name">Name.</param>
/// <param name="Args">Arguments.</param>
/// <param name="Code">Code (as an AST).</param>
public record ParsedCallback(string Name, Token[] Args, ASTNode[] Code);

/// <summary>
/// Exception for parsing-related errors.
/// </summary>
public sealed class ParserException(string message, Token suspect)
    : GenerationException(message, suspect)
{ }
