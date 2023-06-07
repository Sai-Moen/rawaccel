namespace userinterface.Models.Script.Generation
{
    internal class Interpreter
    {
        private readonly ParameterAssignment[] Parameters;

        private readonly VariableAssignment[] Variables;

        private readonly Token[] Tokens;

        internal Interpreter(ParameterAssignment[] parameters, VariableAssignment[] variables, Token[] tokens)
        {
            Parameters = parameters;
            Variables = variables;
            Tokens = tokens;
        }
    }
}
