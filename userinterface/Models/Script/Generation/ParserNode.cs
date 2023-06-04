using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public enum NodeType
    {
        Root,
        Number,
        Identifier,
        Assignment,
        Condition,
        Branch,
        Expression,
        UnaryExpression,
        BinaryExpression,
        FunctionCall,
    }

    public class ParserNode
    {
        public ParserNode(NodeType type, Token token)
        {
            Debug.Assert(type == NodeType.Root);
            Type = type;
            Token = token;
            Parent = this;
        }

        public ParserNode(NodeType type, Token token, ParserNode parent)
        {
            Type = type;
            Token = token;
            Parent = parent;
        }

        public NodeType Type { get; }

        public Token Token { get; }

        public ParserNode Parent { get; }

        public NodeList Children { get; } = new(1);
    }

    public class NodeList : List<ParserNode>, IList<ParserNode>
    {
        public NodeList() : base() {}

        public NodeList(int capacity) : base(capacity) {}
    }

    public class TokenStack : Stack<Token>
    {
        public TokenStack() : base() {}
    }
}
