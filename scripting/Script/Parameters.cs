using scripting.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace scripting.Script;

/// <summary>
/// Collection of <see cref="Parameter"/> declarations.
/// </summary>
public class Parameters : List<Parameter>, IList<Parameter>
{
    internal Parameters() : base(Constants.MAX_PARAMETERS) {}

    private Parameters(Parameters parameters) : base(parameters) {}

    public bool TryFindByName(string name, [MaybeNullWhen(false)] out Parameter p)
    {
        p = Find(match => match.Name == name);
        return p is not null;
    }

    internal Parameters Clone()
    {
        Parameters clone = new(this);
        for (int i = 0; i < clone.Count; i++)
        {
            clone[i] = clone[i].Clone();
        }
        return clone;
    }
}

/// <summary>
/// Read-Only collection of <see cref="ReadOnlyParameter"/>.
/// </summary>
public class ReadOnlyParameters : ReadOnlyCollection<ReadOnlyParameter>, IList<ReadOnlyParameter>
{
    internal ReadOnlyParameters(IList<Parameter> parameters) : base(Wrap(parameters)) {}

    private static List<ReadOnlyParameter> Wrap(IList<Parameter> parameters)
    {
        List<ReadOnlyParameter> ro = new(parameters.Count);
        foreach (Parameter parameter in parameters)
        {
            ro.Add(new(parameter));
        }
        return ro;
    }
}
