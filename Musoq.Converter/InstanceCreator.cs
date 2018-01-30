﻿using FQL.Evaluator;
using FQL.Evaluator.Visitors;
using FQL.Parser;
using FQL.Parser.Lexing;
using FQL.Schema;

namespace FQL.Converter
{
    public static class InstanceCreator
    {
        public static VirtualMachine Create(string script, ISchemaProvider schemaProvider)
        {
            var lexer = new Lexer(script, true);
            var parser = new FqlParser(lexer);

            var query = parser.ComposeAll();

            var rewriter = new RewriteTreeVisitor(schemaProvider);
            var rewriteTraverser = new RewriteTreeTraverseVisitor(rewriter);

            query.Accept(rewriteTraverser);

            query = rewriter.RootScript;

            var metadataCreator = new PreGenerationVisitor();
            var metadataTraverser = new PreGenerationTraverseVisitor(metadataCreator);

            query.Accept(metadataTraverser);

            var codeGenerator = new CodeGenerationVisitor(schemaProvider, metadataCreator.TableMetadata);
            var traverser =
                new CodeGenerationTraverseVisitor(codeGenerator, metadataCreator.AggregationMethods.AsReadOnly());

            query.Accept(traverser);

            return codeGenerator.VirtualMachine;
        }
    }
}