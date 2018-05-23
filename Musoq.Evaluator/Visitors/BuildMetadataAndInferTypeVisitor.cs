﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.RuntimeScripts;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors
{
    public class BuildMetadataAndInferTypeVisitor : IScopeAwareExpressionVisitor
    {
        protected Stack<Node> Nodes { get; } = new Stack<Node>();
        public List<Assembly> Assemblies { get; } = new List<Assembly>();

        private Scope _currentScope;
        private string _identifier;
        private bool _hasGroupByOrJoin;
        private bool _hasGroupBy;
        private int _nesting;
        private string _queryAlias;
        private FieldNode[] _generatedColumns = new FieldNode[0];
        private bool _insideSelect;
        private readonly ISchemaProvider _provider;
        public readonly List<AccessMethodNode> RefreshMethods = new List<AccessMethodNode>();
        public Stack<string> Methods = new Stack<string>();
        public IDictionary<string, string> CteAliases { get; } = new Dictionary<string, string>();
        public IDictionary<string, int[]> SetOperatorFieldPositions { get; } = new Dictionary<string, int[]>();

        public BuildMetadataAndInferTypeVisitor(ISchemaProvider provider)
        {
            _provider = provider;
        }

        private void AddAssembly(Assembly asm)
        {
            if(Assemblies.Contains(asm))
                return;

            Assemblies.Add(asm);
        }

        public RootNode Root => (RootNode)Nodes.Peek();

        public void Visit(Node node)
        {
        }

        public void Visit(DescNode node)
        {

            Nodes.Push(new DescNode((FromNode)Nodes.Pop()));
        }

        public void Visit(StarNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new StarNode(left, right));
        }

        public void Visit(FSlashNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new FSlashNode(left, right));
        }

        public void Visit(ModuloNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ModuloNode(left, right));
        }

        public void Visit(AddNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AddNode(left, right));
        }

        public void Visit(HyphenNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new HyphenNode(left, right));
        }

        public void Visit(AndNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new AndNode(left, right));
        }

        public void Visit(OrNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new OrNode(left, right));
        }

        public void Visit(ShortCircuitingNodeLeft node)
        {
            Nodes.Push(new ShortCircuitingNodeLeft(Nodes.Pop(), node.UsedFor));
        }

        public void Visit(ShortCircuitingNodeRight node)
        {
            Nodes.Push(new ShortCircuitingNodeRight(Nodes.Pop(), node.UsedFor));
        }

        public void Visit(EqualityNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new EqualityNode(left, right));
        }

        public void Visit(GreaterOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterOrEqualNode(left, right));
        }

        public void Visit(LessOrEqualNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessOrEqualNode(left, right));
        }

        public void Visit(GreaterNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new GreaterNode(left, right));
        }

        public void Visit(LessNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LessNode(left, right));
        }

        public void Visit(DiffNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new DiffNode(left, right));
        }

        public void Visit(NotNode node)
        {
            Nodes.Push(new NotNode(Nodes.Pop()));
        }

        public void Visit(LikeNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new LikeNode(left, right));
        }

        public virtual void Visit(FieldNode node)
        {
            Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder, node.FieldName));
        }

        public void Visit(SelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new SelectNode(fields.ToArray()));
        }

        public void Visit(GroupSelectNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new GroupSelectNode(fields.ToArray()));
        }

        public void Visit(StringNode node)
        {
            AddAssembly(typeof(string).Assembly);
            Nodes.Push(new StringNode(node.Value));
        }

        public void Visit(DecimalNode node)
        {
            AddAssembly(typeof(decimal).Assembly);
            Nodes.Push(new DecimalNode(node.Value.ToString(CultureInfo.InvariantCulture)));
        }

        public void Visit(IntegerNode node)
        {
            AddAssembly(typeof(int).Assembly);
            Nodes.Push(new IntegerNode(node.Value.ToString()));
        }

        public void Visit(WordNode node)
        {
            AddAssembly(typeof(string).Assembly);
            Nodes.Push(new WordNode(node.Value));
        }

        public void Visit(ContainsNode node)
        {
            var right = Nodes.Pop();
            var left = Nodes.Pop();
            Nodes.Push(new ContainsNode(left, right as ArgsListNode));
        }

        public virtual void Visit(AccessMethodNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3, alias) => new AccessMethodNode(token, node1 as ArgsListNode, exargs, arg3, alias));

        }

        public void Visit(GroupByAccessMethodNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3, alias) => new GroupByAccessMethodNode(token, node1 as ArgsListNode, exargs, arg3, alias));
        }

        public void Visit(AccessRefreshAggreationScoreNode node)
        {
            VisitAccessMethod(node,
                (token, node1, exargs, arg3, alias) =>
                    new AccessRefreshAggreationScoreNode(token, node1 as ArgsListNode, exargs, arg3, alias));
        }

        public void Visit(AccessColumnNode node)
        {
            var identifier = _currentScope.ContainsAttribute(MetaAttributes.ProcessedQueryId) ? _currentScope[MetaAttributes.ProcessedQueryId] : _identifier;

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(identifier);

            (ISchema Schema, ISchemaTable Table, string TableName) tuple;
            if (!string.IsNullOrEmpty(node.Alias))
                tuple = tableSymbol.GetTableByAlias(node.Alias);
            else
                tuple = tableSymbol.GetTableByColumnName(node.Name);

            var column = tuple.Table.Columns.SingleOrDefault(f => f.ColumnName == node.Name);

            AddAssembly(column.ColumnType.Assembly);
            node.ChangeReturnType(column.ColumnType);

            var accessColumn = new AccessColumnNode(column.ColumnName, tuple.TableName, column.ColumnType, node.Span);
            Nodes.Push(accessColumn);
        }

        public void Visit(AllColumnsNode node)
        {
            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
            var tuple = tableSymbol.GetTableByAlias(_identifier);
            var table = tuple.Table;
            _generatedColumns = new FieldNode[table.Columns.Length];

            for (int i = 0; i < table.Columns.Length; i++)
            {
                var column = table.Columns[i];

                AddAssembly(column.ColumnType.Assembly);
                _generatedColumns[i] = new FieldNode(new AccessColumnNode(column.ColumnName, _identifier, column.ColumnType, TextSpan.Empty), i, tableSymbol.HasAlias ? _identifier : column.ColumnName);
            }

            Nodes.Push(node);
        }

        public void Visit(IdentifierNode node)
        {
            if(node.Name != _identifier)
            {
                var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);
                var column = tableSymbol.GetColumnByAliasAndName(_identifier, node.Name);
                Visit(new AccessColumnNode(node.Name, string.Empty, column.ColumnType, TextSpan.Empty));
                return;
            }
            Nodes.Push(new IdentifierNode(node.Name));
        }

        public void Visit(AccessObjectArrayNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new AccessObjectArrayNode(node.Token, parentNodeType.GetProperty(node.Name)));
        }

        public void Visit(AccessObjectKeyNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new AccessObjectKeyNode(node.Token, parentNodeType.GetProperty(node.ObjectName)));
        }

        public void Visit(PropertyValueNode node)
        {
            var parentNodeType = Nodes.Peek().ReturnType;
            Nodes.Push(new PropertyValueNode(node.Name, parentNodeType.GetProperty(node.Name)));
        }

        public void Visit(DotNode node)
        {
            var exp = Nodes.Pop();
            var root = Nodes.Pop();

            Nodes.Push(new DotNode(root, exp, node.IsOuter, string.Empty, exp.ReturnType));
        }

        public virtual void Visit(AccessCallChainNode node)
        {
            var chainPretend = Nodes.Pop();

            if (chainPretend is AccessColumnNode)
                Nodes.Push(chainPretend);
            else
            {
                var dotNode = chainPretend;

                DotNode theMostInnerDotNode = null;
                while (dotNode != null && dotNode is DotNode dot)
                {
                    theMostInnerDotNode = dot;
                    dotNode = dot.Root;
                }

                var chain = theMostInnerDotNode;
                var column = (AccessColumnNode) dotNode;
                dotNode = theMostInnerDotNode;
                var props = new List<(PropertyInfo, Object)>();

                var type = column.ReturnType;
                while (dotNode != null && dotNode is DotNode dot)
                {
                    switch (dot.Expression)
                    {
                        case PropertyValueNode prop:
                            props.Add((prop.PropertyInfo, null));
                            break;
                        case AccessObjectKeyNode objKey:
                            props.Add((objKey.PropertyInfo, objKey.Token.Key));
                            break;
                        case AccessObjectArrayNode objArr:
                            props.Add((objArr.PropertyInfo, objArr.Token.Index));
                            break;
                    }
                    dotNode = dot.Expression;
                }

                Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias));
            }
        }

        public void Visit(ArgsListNode node)
        {
            var args = new Node[node.Args.Length];

            for (var i = node.Args.Length - 1; i >= 0; --i)
                args[i] = Nodes.Pop();

            Nodes.Push(new ArgsListNode(args));
        }

        public void Visit(WhereNode node)
        {
            Nodes.Push(new WhereNode(Nodes.Pop()));
        }

        public void Visit(GroupByNode node)
        {
            _hasGroupByOrJoin = true;
            _hasGroupBy = true;

            var having = Nodes.Peek() as HavingNode;

            if (having != null)
                Nodes.Pop();

            var fields = new FieldNode[node.Fields.Length];

            for (var i = node.Fields.Length - 1; i >= 0; --i)
            {
                fields[i] = Nodes.Pop() as FieldNode;
            }

            Nodes.Push(new GroupByNode(fields, having));
        }

        public void Visit(HavingNode node)
        {
            Nodes.Push(new HavingNode(Nodes.Pop()));
        }

        public void Visit(SkipNode node)
        {
            Nodes.Push(new SkipNode((IntegerNode)node.Expression));
        }

        public void Visit(TakeNode node)
        {
            Nodes.Push(new TakeNode((IntegerNode)node.Expression));
        }

        public void Visit(SchemaFromNode node)
        {
            var schema = _provider.GetSchema(node.Schema);
            var table = schema.GetTableByName(node.Method, node.Parameters);

            AddAssembly(schema.GetType().Assembly);

            _queryAlias = CreateAliasIfEmpty(node.Alias);
            var tableSymbol = new TableSymbol(_queryAlias, schema, table, !string.IsNullOrEmpty(node.Alias));
            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, tableSymbol);

            _nesting += 1;
            Nodes.Push(new SchemaFromNode(node.Schema, node.Method, node.Parameters, _queryAlias));
        }

        private string CreateAliasIfEmpty(string alias)
        {
            return string.IsNullOrEmpty(alias) ? new string(Guid.NewGuid().ToString("N").Where(char.IsLetter).ToArray()).Substring(0, 4) : alias;
        }

        public void Visit(JoinSourcesTableFromNode node)
        {
            var exp = Nodes.Pop();
            var b = (FromNode)Nodes.Pop();
            var a = (FromNode)Nodes.Pop();

            Nodes.Push(new JoinSourcesTableFromNode(a, b, exp));
        }

        public void Visit(InMemoryTableFromNode node)
        {
            _queryAlias = CreateAliasIfEmpty(node.Alias);

            TableSymbol tableSymbol;

            if (_currentScope.Parent.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(node.VariableName))
                tableSymbol = _currentScope.Parent.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
            else
            {
                var scope = _currentScope;
                while (scope != null && scope.Name != "CTE")
                {
                    scope = scope.Parent;
                }

                tableSymbol = scope.ScopeSymbolTable.GetSymbol<TableSymbol>(node.VariableName);
            }

            var tableSchemaPair = tableSymbol.GetTableByAlias(node.VariableName);
            _currentScope.ScopeSymbolTable.AddSymbol(_queryAlias, new TableSymbol(_queryAlias, tableSchemaPair.Schema, tableSchemaPair.Table, node.Alias == _queryAlias));

            Nodes.Push(new InMemoryTableFromNode(node.VariableName, _queryAlias));
        }

        public void Visit(JoinFromNode node)
        {
            _hasGroupByOrJoin = true;
            var expression = Nodes.Pop();
            var joinedTable = (FromNode)Nodes.Pop();
            var source = (FromNode)Nodes.Pop();
            var joinedFrom = new JoinFromNode(source, joinedTable, expression, node.JoinType);
            _identifier = joinedFrom.Alias;
            Nodes.Push(joinedFrom);
        }

        public void Visit(ExpressionFromNode node)
        {
            var from = (FromNode) Nodes.Pop();
            _identifier = from.Alias;
            Nodes.Push(new ExpressionFromNode(from));

            var tableSymbol =_currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_identifier);

            foreach (var tableAlias in tableSymbol.CompoundTables)
            {
                var tuple = tableSymbol.GetTableByAlias(tableAlias);

                foreach(var column in tuple.Table.Columns)
                    AddAssembly(column.ColumnType.Assembly);
            }
        }

        public void Visit(CreateTableNode node)
        {
            var fields = CreateFields(node.Fields);

            Nodes.Push(new CreateTableNode(node.Name, node.Keys, fields));
        }

        public void Visit(RenameTableNode node)
        {
            Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
        }

        public void Visit(TranslatedSetTreeNode node)
        {
        }

        public void Visit(IntoNode node)
        {
            Nodes.Push(new IntoNode(node.Name));
        }

        public void Visit(QueryScope node)
        {
        }

        public void Visit(ShouldBePresentInTheTable node)
        {
            Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
        }

        public void Visit(TranslatedSetOperatorNode node)
        {
        }

        public void Visit(QueryNode node)
        {
            var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

            if (groupBy == null && RefreshMethods.Count > 0)
            {
                groupBy = new GroupByNode(
                    new FieldNode[]
                    {
                        new FieldNode(new IntegerNode("1"), 0, String.Empty), 
                    }, null);
            }

            var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
            var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

            var select = Nodes.Pop() as SelectNode;
            var where = Nodes.Pop() as WhereNode;
            var from = Nodes.Pop() as FromNode;
            
            _currentScope.ScopeSymbolTable.AddSymbol(from.Alias.ToRefreshMethodsSymbolName(), new RefreshMethodsSymbol(RefreshMethods));
            RefreshMethods.Clear();

            if (_currentScope.ScopeSymbolTable.SymbolIsOfType<TableSymbol>(string.Empty))
                _currentScope.ScopeSymbolTable.UpdateSymbol(string.Empty, from.Alias);

            _currentScope.Script.Append(new DeclarationStatements().TransformText());

            if (_hasGroupByOrJoin)
            {
                _currentScope.Script.Append(new NestedForeaches() { HasGroupBy = _hasGroupBy, Nesting = _nesting }.TransformText());
                _currentScope.Script.Append(new Select().TransformText());
                _currentScope.Script.Replace("{pre_script_dependant}", "{skip}{take}{select_statements}");
            }
            else
            {
                _currentScope.Script.Append(new Select().TransformText());
                _currentScope.Script.Replace("{pre_script_dependant}", "{where_statement}{skip}{take}{select_statements}");
            }

            _nesting = 0;
            _hasGroupBy = false;
            _hasGroupByOrJoin = false;
            Methods.Push(from.Alias);
            Nodes.Push(new QueryNode(select, from, where, groupBy, null, skip, take));
        }

        public void Visit(JoinInMemoryWithSourceTableFromNode node)
        {
            var exp = Nodes.Pop();
            var from = (FromNode)Nodes.Pop();
            Nodes.Push(new JoinInMemoryWithSourceTableFromNode(node.InMemoryTableAlias, from, exp));
        }

        public void Visit(InternalQueryNode node)
        {
            throw new NotSupportedException();
        }

        public void Visit(RootNode node)
        {
            Nodes.Push(new RootNode(Nodes.Pop()));
        }

        public void Visit(SingleSetNode node)
        {
        }

        public void Visit(RefreshNode node)
        {
        }

        public void Visit(UnionNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode)node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Union_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName, _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode)left).From.Alias));

            Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(UnionAllNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode)node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_UnionAll_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName, _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode)left).From.Alias));

            Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(ExceptNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode)node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Except_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName, _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode)left).From.Alias));

            Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(IntersectNode node)
        {
            var key = CreateSetOperatorPositionKey();
            _currentScope[MetaAttributes.SetOperatorName] = key;
            SetOperatorFieldPositions.Add(key, CreateSetOperatorPositionIndexes((QueryNode)node.Left, node.Keys));

            var right = Nodes.Pop();
            var left = Nodes.Pop();

            var rightMethodName = Methods.Pop();
            var leftMethodName = Methods.Pop();

            var methodName = $"{leftMethodName}_Intersect_{rightMethodName}";
            Methods.Push(methodName);
            _currentScope.ScopeSymbolTable.AddSymbol(methodName, _currentScope.Child[0].ScopeSymbolTable.GetSymbol(((QueryNode)left).From.Alias));

            Nodes.Push(new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne));
        }

        public void Visit(PutTrueNode node)
        {
            Nodes.Push(new PutTrueNode());
        }

        public void Visit(MultiStatementNode node)
        {
            var items = new Node[node.Nodes.Length];

            for (var i = node.Nodes.Length - 1; i >= 0; --i)
                items[i] = Nodes.Pop();

            Nodes.Push(new MultiStatementNode(items, node.ReturnType));
        }

        public void Visit(CteExpressionNode node)
        {
            var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

            var set = Nodes.Pop();

            for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
                sets[i] = (CteInnerExpressionNode)Nodes.Pop();

            Nodes.Push(new CteExpressionNode(sets, set));
        }

        public void Visit(CteInnerExpressionNode node)
        {
            var set = Nodes.Pop();
            
            var collector = new SelectFieldsCollectVisitor();
            var traverser = new CloneTraverseVisitor(collector);

            set.Accept(traverser);

            var table = new VariableTable(collector.CollectedFieldNames);
            _currentScope.Parent.ScopeSymbolTable.AddSymbol(node.Name, new TableSymbol(node.Name, new TransitionSchema(node.Name, table), table, false));

            Nodes.Push(new CteInnerExpressionNode(set, node.Name));
        }

        public void Visit(JoinsNode node)
        {
            _identifier = node.Alias;
            Nodes.Push(new JoinsNode((JoinFromNode)Nodes.Pop()));
        }

        public void Visit(JoinNode node)
        {
            var expression = Nodes.Pop();
            var fromNode = (FromNode)Nodes.Pop();

            if (node is OuterJoinNode outerJoin)
                Nodes.Push(new OuterJoinNode(outerJoin.Type, fromNode, expression));
            else
                Nodes.Push(new InnerJoinNode(fromNode, expression));
        }

        private FieldNode[] CreateFields(FieldNode[] oldFields)
        {
            var reorderedList = new FieldNode[oldFields.Length];
            var fields = new List<FieldNode>(reorderedList.Length);

            for (var i = reorderedList.Length - 1; i >= 0; i--) reorderedList[i] = Nodes.Pop() as FieldNode;


            for (int i = 0, j = reorderedList.Length, p = 0; i < j; ++i)
            {
                var field = reorderedList[i];

                if (field.Expression is AllColumnsNode)
                {
                    fields.AddRange(_generatedColumns.Select(column => new FieldNode(column.Expression, p++, column.FieldName)));
                    continue;
                }

                fields.Add(new FieldNode(field.Expression, p++, field.FieldName));
            }

            return fields.ToArray();
        }

        private void VisitAccessMethod(AccessMethodNode node,
            Func<FunctionToken, Node, ArgsListNode, MethodInfo, string, AccessMethodNode> func)
        {
            var args = Nodes.Pop() as ArgsListNode;

            var groupArgs = new List<Type> { typeof(string) };
            groupArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1).Select(f => f.ReturnType));

            var alias = !string.IsNullOrEmpty(node.Alias) ? node.Alias : _identifier;

            var tableSymbol = _currentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(alias);
            var schemaTablePair = tableSymbol.GetTableByAlias(alias);
            if (!schemaTablePair.Schema.TryResolveAggreationMethod(node.Name, groupArgs.ToArray(), out var method))
                method = schemaTablePair.Schema.ResolveMethod(node.Name, args.Args.Select(f => f.ReturnType).ToArray());

            var isAggregateMethod = method.GetCustomAttribute<AggregationMethodAttribute>() != null;

            AccessMethodNode accessMethod;
            if (isAggregateMethod)
            {
                accessMethod = func(node.FToken, args, node.ExtraAggregateArguments, method, alias);
                var identifier = accessMethod.ToString();

                var newArgs = new List<Node> { new WordNode(identifier) };
                newArgs.AddRange(args.Args.Where((f, i) => i < args.Args.Length - 1));
                var newSetArgs = new List<Node> { new WordNode(identifier) };
                newSetArgs.AddRange(args.Args);

                var setMethodName = $"Set{method.Name}";

                if(!schemaTablePair.Schema.TryResolveAggreationMethod(
                    setMethodName, 
                    newSetArgs.Select(f => f.ReturnType).ToArray(), 
                    out var setMethod))
                    throw new NotSupportedException();

                var setMethodNode = func(new FunctionToken(setMethodName, TextSpan.Empty), new ArgsListNode(newSetArgs.ToArray()), null, setMethod,
                    alias);

                RefreshMethods.Add(setMethodNode);

                accessMethod = func(node.FToken, new ArgsListNode(newArgs.ToArray()), null, method, alias);
            }
            else
            {
                accessMethod = func(node.FToken, args, new ArgsListNode(new Node[0]), method, alias);
            }

            AddAssembly(method.DeclaringType.Assembly);
            AddAssembly(method.ReturnType.Assembly);

            node.ChangeMethod(method);

            Nodes.Push(accessMethod);
        }

        private int[] CreateSetOperatorPositionIndexes(QueryNode node, string[] keys)
        {
            var indexes = new int[keys.Length];

            var fieldIndex = 0;
            var index = 0;

            foreach (var field in node.Select.Fields)
            {
                if (keys.Contains(field.FieldName))
                    indexes[index++] = fieldIndex;

                fieldIndex += 1;
            }

            return indexes;
        }

        private int _setKey = 0;

        private string CreateSetOperatorPositionKey()
        {
            var key = _setKey++;
            return key.ToString().ToSetOperatorKey(key.ToString());
        }

        public void SetScope(Scope scope)
        {
            _currentScope = scope;
        }
    }
}
