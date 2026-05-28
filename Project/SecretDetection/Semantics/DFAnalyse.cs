using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;


namespace Project.SecretDetection.Semantics{
    class DataFlowAnalyzer3
    {
        public void initDataflow3(List<SyntaxTree> trees, List<SyntaxToken> idTokens)
        {
            foreach (var tree in trees)
            {
                dataflow3(tree, idTokens);
            }
        }
        public void dataflow3(SyntaxTree tree, List<SyntaxToken> idTokens)
        {
            Console.WriteLine("1");
            var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            Console.WriteLine("2");
            var compilation = CSharpCompilation.Create("MyCompilation",syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            Console.WriteLine("3");
            var model = compilation.GetSemanticModel(tree);
            Console.WriteLine("4");
            var blocks = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>();
            Console.WriteLine("5");
            // foreach(var block in blocks)
            foreach(var idtoken in idTokens)
            {
                var result = model.AnalyzeDataFlow(idtoken);
                Console.WriteLine("6");
                // DataFlowAnalysis result = model.AnalyzeDataFlow(methodDeclaration);
                if (result.Succeeded)
                {
                    Console.WriteLine("Yayyy");
                    foreach(var c in result.DataFlowsOut)
                    {
                        Console.WriteLine("     "+c);
                    }
                }
                else
                {
                    Console.WriteLine(":(");
                }
                
            }
        }
    }
}