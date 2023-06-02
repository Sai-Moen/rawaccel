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

        public bool TryCreateNodes(Token[] tokens);
    }

    public static class ParseNode
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

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new ParserException("Do not call this on Root!");
        }

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

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new ParserException("Do not call this on a leaf node!");
        }
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

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new ParserException("Do not call this on a leaf node!");
        }
    }

    public class Assignment : INode
    {
        private const string This = Tokens.Operators.ASSIGN;

        public Token? NodeToken { get; private set; }

        public NodeList Children { get; private set; } = new();

        /// <summary>
        /// Expected token order ->
        /// (concrete) Identifier, This, (concrete) Identifier or Number, Separator(TERMINATOR)
        /// </summary>
        public bool TryCreateNodes(Token[] tokens)
        {
            if (tokens.Length != 4)
            {
                return false;
            }

            switch (tokens[0].Type)
            {
                case TokenType.Parameter:
                case TokenType.Variable:
                    Children.Add(ParseNode.Factory(NodeType.Variable, tokens[0]));
                    break;
                default:
                    return false;
            }

            if (tokens[1].Type == TokenType.Operator && tokens[1].Symbol == This)
            {
                NodeToken = tokens[1];
            }
            else
            {
                return false;
            }

            switch (tokens[2].Type)
            {
                case TokenType.Parameter:
                case TokenType.Variable:
                    Children.Add(ParseNode.Factory(NodeType.Variable, tokens[2]));
                    break;
                case TokenType.Number:
                    Children.Add(ParseNode.Factory(NodeType.Number, tokens[2]));
                    break;
                default:
                    return false;
            }

            return tokens[3].Type == TokenType.Separator &&
                tokens[3].Symbol == Tokens.Separators.TERMINATOR;
        }
    }

    public class BinaryExpression : INode
    {
        public Token? NodeToken { get; private set; }
        public NodeList Children { get; private set; } = new();

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class AssignmentExpression : INode
    {
        public Token? NodeToken { get; private set; }
        public NodeList Children { get; private set; } = new();

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class Comparison : INode
    {
        public Token? NodeToken { get; private set; }
        public NodeList Children { get; private set; } = new();

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class Branch : INode
    {
        public Token? NodeToken { get; private set; }
        public NodeList Children { get; private set; } = new();

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class FunctionCall : INode
    {
        public Token? NodeToken { get; private set; }
        public NodeList Children { get; private set; } = new();

        public bool TryCreateNodes(Token[] tokens)
        {
            throw new NotImplementedException();
        }
    }

    public class NodeList : List<INode>, IList<INode>
    {
        public NodeList() : base() {}
    }
}
