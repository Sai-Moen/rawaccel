using scripting.Common;
using scripting.Script;
using System.Collections.Generic;

namespace scripting.Syntactical;

/// <summary>
/// Defines the API of a RawAccelScript parser.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Makes the parser begin parsing (single-use only).
    /// </summary>
    /// <returns>result of parsing</returns>
    /// <exception cref="ParserException"/>
    ParsingResult Parse();
}

/// <summary>
/// The result of performing syntactic analysis on a list of lexical tokens.
/// </summary>
/// <param name="Description">the description of the script (usually derived from the 'comments' section in lexical analysis)</param>
/// <param name="Parameters">the user-controlled parameters</param>
/// <param name="Variables">the variables used by the script</param>
/// <param name="Callbacks">the callbacks parsed from the script</param>
public record ParsingResult(string Description, Parameters Parameters, Variables Variables, IList<ParsedCallback> Callbacks);

/// <summary>
/// Represents a callback that has been parsed but not yet validated and emitted.
/// </summary>
/// <param name="Name">name</param>
/// <param name="Args">arguments</param>
/// <param name="Code">code (as tokens)</param>
public record ParsedCallback(string Name, ITokenList Args, ITokenList Code);

/// <summary>
/// Exception for parsing-related errors.
/// </summary>
public sealed class ParserException(string message, uint line) : GenerationException(message, line)
{
}
