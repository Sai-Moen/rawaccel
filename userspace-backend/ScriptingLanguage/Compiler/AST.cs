using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace userspace_backend.ScriptingLanguage.Compiler;

/// <summary>
/// The root AST node.
/// </summary>
/// <param name="Description">The description of the script.</param>
/// <param name="Parameters">The user-controlled parameters.</param>
/// <param name="Declarations">The declarations used by the script.</param>
public record AST(string Description, Parameters Parameters, IList<ASTNode> Declarations);

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
    Return, Function, Callback,
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
    [FieldOffset(0)] public ASTReturn astReturn;
    [FieldOffset(0)] public ASTFunction astFunction;
    [FieldOffset(0)] public ASTCallback astCallback;
}

public record ASTAssign(Token Identifier, Token Operator, Token[] Initializer);
public record ASTIf(Token[] Condition, ASTNode[] If, ASTNode[] Else);
public record ASTWhile(Token[] Condition, ASTNode[] While);
public record ASTReturn(Token[] Expression);
public record ASTFunction(Token Identifier, Token[] Args, ASTNode[] Code);
public record ASTCallback(Token Identifier, Token[] Expressions, ASTNode[] Code);
