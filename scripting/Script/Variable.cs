using scripting.Common;
using scripting.Lexical;

namespace scripting.Script;

/// <summary>
/// Saves the Token of a Variable and its initialization assignment.
/// </summary>
public readonly record struct Variable(string Name, IList<Token> Expr);

/// <summary>
/// Collection of Variable assignments.
/// </summary>
public class Variables : List<Variable>, IList<Variable>
{
    public Variables() : base(Constants.MAX_VARIABLES) { }
}
