using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace userinterface.Models.Script.Generation
{
    public enum NodeType
    {
        Root,
        Variable,
        Number,
        Assignment,
        BinaryExpression,
        AssignmentExpression,
        Comparison,
        Branch,
        FunctionCall,
    }

    public interface INode
    {
        /// <summary>
        /// Token of this node, null if Root.
        /// </summary>
        public Token? NodeToken { get; }

        /// <summary>
        /// Children of this node, null if variable/number literal.
        /// </summary>
        public NodeList? Children { get; }
    }

    public static class ParserNode
    {
        public static INode Factory(NodeType type)
        {
            Debug.Assert(type != NodeType.Variable && type != NodeType.Number);
            return Factory(type, null);
        }

        public static INode Factory(NodeType type, Token? nodeToken) =>
            type switch
            {
                NodeType.Root                   => throw new ParserException("Create Root manually!"),

                NodeType.Variable               => new Variable(nodeToken),
                NodeType.Number                 => new Number(nodeToken),
                NodeType.Assignment             => new Assignment(),
                NodeType.BinaryExpression       => new BinaryExpression(),
                NodeType.AssignmentExpression   => new AssignmentExpression(),
                NodeType.Comparison             => new Comparison(),
                NodeType.Branch                 => new Branch(),
                NodeType.FunctionCall           => new FunctionCall(),

                _ => throw new NotImplementedException(),
        };
    }

    public class Root : INode
    {
        public Token? NodeToken => null;

        public NodeList Children { get; } = new();

        public void AddExistingNode(INode node)
        {
            Children!.Add(node);
        }
    }

    public class Variable : INode
    {
        public Variable(Token? nodeToken)
        {
            Debug.Assert(nodeToken != null);
            NodeToken = nodeToken!;
        }

        public Token NodeToken { get; }

        public NodeList? Children => null;
    }

    public class Number : INode
    {
        public Number(Token? nodeToken)
        {
            Debug.Assert(nodeToken != null);
            NodeToken = nodeToken!;

            if (double.TryParse(NodeToken.Symbol, out Value))
            {
                return;
            }

            throw new ParserException(NodeToken.Line, "Invalid number sequence!");
        }

        public readonly double Value;

        public Token NodeToken { get; }

        public NodeList? Children => null;
    }

    public class Assignment : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class BinaryExpression : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class AssignmentExpression : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class Comparison : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class Branch : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class FunctionCall : INode
    {
        public Token? NodeToken { get; init; }

        public NodeList Children { get; } = new();
    }

    public class NodeList : List<INode>, IList<INode>
    {
        public NodeList() : base() {}

        public NodeList(int capacity) : base(capacity) {}
    }

    public class TokenStack : Stack<Token>
    {
        public TokenStack() : base() {}
    }
}
