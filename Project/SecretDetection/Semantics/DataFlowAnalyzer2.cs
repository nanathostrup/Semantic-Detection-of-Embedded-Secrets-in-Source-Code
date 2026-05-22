using System;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project.SecretDetection.Semantics{
    class DataFlowAnalyzer2
    {
        public List<SyntaxToken> dataflowAnalysis1(List<SyntaxTree> trees, List<SyntaxToken> idTokens) //OPTIMIZE!!! DER ER 4 FOR LOOPS! og rekursion:)
        {
            //For each idToken that we want to trace
                // For each tree 
                    // find all idTokens
                    //if idToken input is is among found idTokens
                        //save idTokens and go again until there are no new idTokens to be added (done recursively for specific syntax's because you want specific tokens)

            var newTokens = new List<SyntaxToken>();

            foreach (var idToken in idTokens.ToList()) // To.List makes a copy to safely iterate -- Gør rekursivt på et tidspunkt i stedet
            {
                for (int i = 0; i < trees.Count; i++)//Ka blive gjort MEGET mere smart
                {
                    SyntaxNode root = trees[i].GetRoot(); //SKAL VÆRE i
                    var matchingTokens = root.DescendantTokens()
                        .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
                                    t.Text == idToken.Text)
                        .ToList();

                    foreach (var match in matchingTokens) //MEEEGET MERE SMART
                    {
                        // Console.WriteLine("found matching token: " + match);
                        var additionalTokens = howIsVariableUsed(trees, idTokens, match.Parent);

                        // collect new tokens
                        foreach (var token in additionalTokens)
                        {
                            if (!idTokens.Any(t => t.Text == token.Text) && !newTokens.Any(t => t.Text == token.Text)) //also remove possible duplicates
                            {
                                newTokens.Add(token);
                            }
                        }
                    }
                }
            }
            // Console.WriteLine("Run over");
            // merge new tokens into the main list after iteration
            if (newTokens.Count > 0)
            {
                // Console.WriteLine("we go again REKURSION VIRKERRRRRR!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                idTokens.AddRange(newTokens);

                return dataflowAnalysis1(trees, idTokens);// recursively get hold of the new tokens
            }
            return idTokens; // No more recursion, we finish with what we have
        }

        public List<SyntaxToken> howIsVariableUsed(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            //is variable part of an invocationmethod?
                //send on to how to save the new identificationTokens according to method handling
            // is variable part of ...

            switch (node)
            {
                case VariableDeclaratorSyntax variableDeclarator:
                    Console.WriteLine($"Token {node} is part of an assignment: {variableDeclarator}");
                    return variableDeclaratorHandler(trees, idTokens, node.Parent);
                    // return idTokens;
                    // break;
                case InvocationExpressionSyntax invocation:
                    Console.WriteLine($"Token {node} is part of an invocationExpression: {invocation}");
                    return idTokens;
                    // break;
                case AssignmentExpressionSyntax assignment:
                    Console.WriteLine($"Token {node} is part of an assignment: {assignment}");
                    return idTokens;
                    // break;
                case ParameterSyntax parameter:
                    Console.WriteLine($"Token {node} is part of a method parameter: {parameter.Identifier.Text}");
                    return idTokens;
                    // break;
                case MemberAccessExpressionSyntax memberAccess:
                    Console.WriteLine($"Token {node} is part of a member access: {memberAccess}");
                    return memberAccessHandler(trees, idTokens, node);
                    // break;
                default:
                    // Console.WriteLine($"Wompidi womp {node} does not have an assigned switch method");
                    if (node.Parent != null){
                        return howIsVariableUsed(trees, idTokens, node.Parent);
                    }
                    return new List<SyntaxToken>();
                    // return howIsVariableUsed(trees, idTokens, node.Parent); //Bliver faaaarliiigg - skal stop klods på
                    // break;
            }
        }
        public List<SyntaxToken> memberAccessHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            // Console.WriteLine("Heeey girl");
            bool parentIsInvocation = node.Parent is InvocationExpressionSyntax;
            if (parentIsInvocation)
            {
                // Console.WriteLine("I KNEW IT. Need handling of other cases in memberaccessHandler");
                return invocationHandler(trees, idTokens, node.Parent);
            }
            //HANDLE OTHER CASES OF THIS INSTANCE
            return idTokens;
        }
        public List<SyntaxToken> invocationHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            //find the idtokens that are not already in idTokens (get only new instances)
            //return that list

            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;
                        //Lavet rekursions venlig med en ny liste der bliver returneret, hvis man tilføjer til idTokens, og returnerer den er den ikke rekursions venlig altså den vil ikke kunne se der er sket noget nyt forrest i rekursionen.
        }
        public List<SyntaxToken> variableDeclaratorHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t)) // && t.ValueText != "city") //city is outcommented for now - to make debugging easier
                .ToList();

            return newIdTokens;
        }
    }
}
