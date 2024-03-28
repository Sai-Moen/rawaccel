using scripting.Common;
using System.Collections.Generic;

namespace scripting.Script;

/// <summary>
/// Saves the Token of a Variable and its initialization assignment.
/// </summary>
public readonly record struct Variable(string Name, ITokenList Expr);

/// <summary>
/// Collection of Variable assignments.
/// </summary>
public class Variables : List<Variable>, IList<Variable>
{
    internal Variables() : base(Constants.MAX_VARIABLES) { }
}
