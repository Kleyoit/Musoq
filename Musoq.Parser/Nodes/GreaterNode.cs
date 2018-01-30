﻿using System;

namespace FQL.Parser.Nodes
{
    public class GreaterNode : BinaryNode
    {
        public GreaterNode(Node left, Node right) 
            : base(left, right)
        {
            Id = CalculateId(this);
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"{Left.ToString()} > {Right.ToString()}";
        }
    }
}