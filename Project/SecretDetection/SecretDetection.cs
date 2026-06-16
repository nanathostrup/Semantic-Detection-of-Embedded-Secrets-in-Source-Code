using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.Semantics;
using Project.SecretDetection.SecretsAnalysis;
using Project.SecretDetection.DetectionsTypes;
using Project.SecretDetection.PlaceAnalysis;
using System.Linq.Expressions;
using System.Reflection;


namespace Project.SecretDetection{
    public class SecretDetector
    {
        public void Detect(string filePath)
        {
            Console.WriteLine("");
            Console.WriteLine(" ============================= CONVERTING TO AST ============================= ");

            var ast = new AST();
            List<SyntaxTree> trees = ast.createASTs(filePath);

            Console.WriteLine("");
            Console.WriteLine(" ================================== WALK AST ================================= ");

            Console.WriteLine("Walking ASTs");

            //walk tree to find special rule for invocation expressions with "GetEnvironmentVariable" condition
            var walker = new Walker();
            foreach (SyntaxTree tree in trees)
            {
                SyntaxNode root = tree.GetRoot(); //Get the root of the tree
                walker.Visit(root); //Check the current AST for invocation expressions. Walker går gennem træet, og der er blevet lavet sær regel for invocation expressions
            }
            Dictionary<string, string> environmentVariableMap = walker.EnvironmentVariableMap; //making a new dictionary that is not a walker.field -- can send this on
            
            Console.WriteLine("");
            Console.WriteLine(" =========================== EXTRACTING VARIABLES ============================ ");
            var envFileDetection = new EnvironmentFileDetection();
            envFileDetection.handleDetection(trees, filePath, environmentVariableMap); // OUTCOMMENT WHEN DONE DEBUGGING
            
            

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// DEBUGGING///

            //For printing each AST
            // foreach (SyntaxTree tree in trees)
            // {
            //     Console.WriteLine(" ================================= NEW SYNTAX TREE ================================= ");
            //     // Get the root of the tree
            //     SyntaxNode root = tree.GetRoot();
            //     PrintNode(root, 0);
            // }
            // SyntaxNode rooot = trees[2].GetRoot();
            // PrintNode(rooot, 0);
        
        }

        static void PrintNode(SyntaxNode node, int indent)
        {
            var padding = new string(' ', indent * 2);
            Console.WriteLine($"{padding}{node.Kind()}");

            // Print tokens with values
            foreach (var token in node.ChildTokens())
            {
                Console.WriteLine($"{padding} {token.Kind()} {token.ValueText}");
            }

            foreach (var child in node.ChildNodes())
            {
                PrintNode(child, indent + 1);
            }
        }
    }
}