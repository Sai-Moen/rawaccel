namespace scripting.Script;

/// <summary>
/// Saves the Token of a Variable and its initialization assignment.
/// </summary>
public readonly record struct Variable(string Name, ITokenList Expr);
