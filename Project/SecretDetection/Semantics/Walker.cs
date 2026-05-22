using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project.SecretDetection.Semantics{
    class Walker : CSharpSyntaxWalker
    {
        //Måske lav til dictionary så vi er sikker på at de hænger sammen på den rigtige måde
        public Dictionary<string, string> EnvironmentVariableMap = new();
        public override void VisitInvocationExpression(InvocationExpressionSyntax invocation)
            {
            //Does the invocation have a child that is GetEnvVar?
                //No? Return
                //Yes 
                    //get invocation's argumentList and add the string litteral (which is an argument to GetEnvVar) to a list

            bool GetEnvVar = invocation.DescendantTokens()
                    .Any(t => t.IsKind(SyntaxKind.IdentifierToken) 
                                && (t.ValueText == "GetEnvironmentVariable" || t.ValueText == "GetEnvironmentVariables"));

            if (GetEnvVar)
            {
                Console.WriteLine("GetEnvVar is used");
                var arglist = invocation.ArgumentList //We want the string litteral that is from the argument list to ensure we have the input for the GetEnvironmentVariable().
                    .Arguments
                    .Select(t => t.Expression)
                    .OfType<LiteralExpressionSyntax>()  
                    .Where(t => t.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(t => t.Token.ValueText)
                    .ToList();

                
                //First parent whta is variable declarator,
                    //Thich child is the name that it is declared as
                
                //FIX THIS - IT MAY NOT HAPPEN!
                var initializedas = invocation //Now we want what the variable name is initilialized as (so we can tract the variable with dataflow analysis)
                    .FirstAncestorOrSelf<VariableDeclaratorSyntax>() //Not declaraTION - then we get the entire line which is too much for getting the name its initialized as
                    ?.Identifier.Text;

                if (arglist.Any())
                {
                    foreach (var arg in arglist)
                    {
                        // if (initializedas != null)
                        // {
                            EnvironmentVariableMap[arg] = initializedas;   
                        // }
                    }
                }
            }
            base.VisitInvocationExpression(invocation);

        }
    }
}