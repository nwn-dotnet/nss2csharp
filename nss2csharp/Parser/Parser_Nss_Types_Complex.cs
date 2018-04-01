﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nss2csharp.Parser
{

    public struct CompilationUnitMetadata
    {
        public string m_Name;
    }

    public struct CompilationUnitDebugInfo
    {
        public string[] m_SourceData;
    }

    public class CompilationUnit : Node
    {
        public CompilationUnitMetadata m_Metadata;
        public CompilationUnitDebugInfo m_DebugInfo;
        public List<Node> m_Nodes = new List<Node>();
    }

    public class LvalueDecl : Node
    {
        public Type m_Type;
        public Lvalue m_Lvalue;
    }

    public class LvalueDeclWithAssignment : LvalueDecl
    {
        public Value m_AssignedValue;
    }

    public class ConstLvalueDeclWithAssignment : LvalueDeclWithAssignment
    { }

    public class FunctionParameter : Node
    {
        public Type m_Type;
        public Lvalue m_Name;
    }

    public class FunctionParameterWithDefault : FunctionParameter
    {
        public Value m_Default;
    }

    public class Block : Node
    {
        public List<Node> m_Nodes = new List<Node>();
    }

    public class FunctionDeclaration : Function
    { }

    public class FunctionImplementation : Function
    {
        public Block m_Block;
    }

    public class FunctionCall : Node
    {
        public Lvalue m_Name;
        public List<Value> m_Arguments;
    }

    public class Expression : Node
    {
        public string m_Expression; // Just store it as a string - for this tool, we don't need to semantically understand it.
    }

    public class ArithmeticExpression : Expression
    { }

    public class LogicalExpression : Expression
    { }

    public class LValueAssignment : Node
    {
        public Lvalue m_Lvalue;
        public Expression m_Expression;
    }

    public class WhileLoop : Node
    {
        public Expression m_Expression;
    }

    public class ForLoop : Node
    {
        public Expression m_Pre;
        public Expression m_Condition;
        public Expression m_Post;
    }

    public class DoWhileLoop : Node
    {
        public Expression m_Expression;
    }

    public class IfStatement : Node
    {
        public Expression m_Expression;
    }
}