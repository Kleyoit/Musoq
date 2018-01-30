﻿using System;
using System.Collections.Generic;
using FQL.Parser;
using FQL.Parser.Nodes;

namespace FQL.Evaluator.Visitors
{
    public class RewriteTreeTraverseVisitor : IExpressionVisitor
    {
        private readonly ISchemaAwareExpressionVisitor _visitor;

        public RewriteTreeTraverseVisitor(ISchemaAwareExpressionVisitor visitor)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        public void Visit(SelectNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(StringNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IntegerNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(WordNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ContainsNode node)
        {
            node.Right.Accept(this);
            node.Left.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessMethodNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByAccessMethodNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            node.Arguments.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AccessColumnNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AllColumnsNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectArrayNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(AccessObjectKeyNode node)
        {}

        public void Visit(PropertyValueNode node)
        {}

        public void Visit(AccessPropertyNode node)
        {
            Node current = node;
            AccessPropertyNode mostOuter = node;

            while (current is AccessPropertyNode prop)
            {
                mostOuter = prop;
                current = prop.Root;
            }

            mostOuter.Accept(_visitor);
        }

        public void Visit(AccessCallChainNode node)
        {
            node.Accept(_visitor);
        }

        public virtual void Visit(WhereNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GroupByNode node)
        {
            foreach (var field in node.Fields)
                field.Accept(this);

            node.Having?.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(HavingNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ExistingTableFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(SchemaFromNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(NestedQueryFromNode node)
        {
            node.Query.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(CreateTableNode node)
        {
            var oldSchema = _visitor.CurrentSchema;
            var oldMethod = _visitor.CurrentTable;
            var oldParameters = _visitor.CurrentParameters;

            _visitor.CurrentSchema = node.Schema;
            _visitor.SetCurrentTable(node.Method, node.Parameters);

            foreach (var item in node.Fields)
                item.Accept(this);

            node.Accept(_visitor);

            _visitor.CurrentSchema = oldSchema;
            _visitor.SetCurrentTable(oldMethod, oldParameters);
        }

        public void Visit(TranslatedSetTreeNode node)
        {
            foreach (var item in node.Nodes)
                item.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(IntoNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(IntoGroupNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(ShouldBePresentInTheTable node)
        {
            node.Accept(_visitor);
        }

        public void Visit(TranslatedSetOperatorNode node)
        {
            foreach (var item in node.CreateTableNodes)
                item.Accept(_visitor);

            node.FQuery.Accept(this);
            node.SQuery.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(QueryNode node)
        {
            var oldSchema = _visitor.CurrentSchema;
            var oldMethod = _visitor.CurrentTable;
            var oldParameters = _visitor.CurrentParameters;

            _visitor.CurrentSchema = node.From.Schema;
            _visitor.SetCurrentTable(node.From.Method, node.From.Parameters);

            node.GroupBy?.Accept(this);
            node.From.Accept(this);
            node.Where.Accept(this);
            node.Select.Accept(this);
            node.Accept(_visitor);

            _visitor.CurrentSchema = oldSchema;
            _visitor.SetCurrentTable(oldMethod, oldParameters);
        }

        public void Visit(OrNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(HyphenNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AndNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(EqualityNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GreaterOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LessOrEqualNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(GreaterNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LessNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(DiffNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(NotNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(LikeNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(FieldNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ArgsListNode node)
        {
            foreach (var item in node.Args)
                item.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(DecimalNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(Node node)
        {
            throw new NotSupportedException();
        }

        public void Visit(StarNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(FSlashNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(ModuloNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(AddNode node)
        {
            node.Left.Accept(this);
            node.Right.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(InternalQueryNode node)
        {
            var oldSchema = _visitor.CurrentSchema;
            var oldMethod = _visitor.CurrentTable;
            var oldParameters = _visitor.CurrentParameters;

            _visitor.CurrentSchema = node.From.Schema;
            _visitor.SetCurrentTable(node.From.Method, node.From.Parameters);

            node.GroupBy?.Accept(this);
            node.Refresh?.Accept(this);
            node.From.Accept(this);
            node.Where.Accept(this);
            node.Select?.Accept(this);
            node.Accept(_visitor);

            _visitor.CurrentSchema = oldSchema;
            _visitor.SetCurrentTable(oldMethod, oldParameters);
        }

        public void Visit(RootNode node)
        {
            node.Expression.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(UnionNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(UnionAllNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(ExceptNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(RefreshNode node)
        {
            foreach (var item in node.Nodes)
                item.Accept(this);

            node.Accept(_visitor);
        }

        public void Visit(IntersectNode node)
        {
            TraverseSetOperator(node);
        }

        public void Visit(PutTrueNode node)
        {
            node.Accept(_visitor);
        }

        public void Visit(MultiStatementNode node)
        {
            foreach (var cNode in node.Nodes)
                cNode.Accept(this);
            node.Accept(_visitor);
        }

        public void Visit(FromNode node)
        {
            node.Accept(_visitor);
        }

        private void TraverseSetOperator(SetOperatorNode node)
        {
            var nodes = new Stack<SetOperatorNode>();
            nodes.Push(node);

            while (nodes.Count > 0)
            {
                var current = nodes.Pop();

                if (current.Right is SetOperatorNode operatorNode)
                {
                    nodes.Push(operatorNode);

                    node.Left.Accept(this);
                    operatorNode.Left.Accept(this);
                    current.Accept(_visitor);
                }
                else
                {
                    current.Left.Accept(this);
                    current.Right.Accept(this);
                    current.Accept(_visitor);
                }
            }
        }
    }
}