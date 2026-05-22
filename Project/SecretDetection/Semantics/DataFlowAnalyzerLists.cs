using System;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project.SecretDetection.Semantics{
    class DataFlowAnalyzerLists
    {
        public void initDateflow(List<SyntaxTree> trees, List<SyntaxToken> idTokens)
        {
            //Find ud af hvor idtokensne kommer fra i træet
            //Lav et dictionary hvor ting hænger sammen
            //Send dictionary videre i stedet for liste
            //return dataflowAnalysis(tree, idtokens)
                //Så er det den der kører rekursivt
                //Skal lige handle man får dictionary ud som return value.
        }
        public List<SyntaxToken> dataflowAnalysis(List<SyntaxTree> trees, List<SyntaxToken> idTokens) //Global dictionary? - Bøvlet at nulstille. Eller dictionary der bliver sendt rundt? Det er bare supre besværligt når man skal kalde den her funktion ude fra?
        {
            //we look into all id tokens
            //then for each id token we look through all trees
                //We need to find the idtoken in the tree
            //then we want to know what its parents are, so we can add the correct new id tokens, and repeat

            List<SyntaxToken> foundInTrees = getIdTokenInTree(trees, idTokens); // find the id tokens in the tree
            List<SyntaxToken> newFinds = new List<SyntaxToken>();

            foreach(var id in foundInTrees) //Not sure I want it handled like this
            {
                newFinds.AddRange(howIsVariableUsed(trees, foundInTrees, id.Parent)); //adds new tokens to a list depending on the nodes parent (expression syntax...)
            }
            newFinds.AddRange(idTokens);
            
            List<SyntaxToken> newNewFinds = newFinds.Distinct().ToList(); //the new and old tokens mixed, without repeated tokens

            var onlyInFirst = newNewFinds.Except(idTokens);
            var onlyInSecond = idTokens.Except(newNewFinds);
            bool areEqual = !onlyInFirst.Any() && !onlyInSecond.Any(); //Filter out the repeat tokens

            if(areEqual) // Checker om de to lister er ens
            {
                return idTokens; //Stop klods - no new tokens to analyze
            }
            else
            {
                return dataflowAnalysis(trees, newNewFinds); //rekursivt kald for at analysere den nye liste af id tokens
            }
        }
        public List<SyntaxToken> getIdTokenInTree(List<SyntaxTree> trees, List<SyntaxToken> idTokens) // RETHINK THiS METHOD
        {
            List<SyntaxToken> foundInTree = new List<SyntaxToken>();
            foreach (var idToken in idTokens){
                for (int i = 0; i < trees.Count; i++)
                    {
                        SyntaxNode root = trees[i].GetRoot();
                        var matchingTokens = root.DescendantTokens()
                            .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
                                        t.Text == idToken.Text)
                            .ToList();
                        
                        foundInTree.AddRange(matchingTokens);
                    }
            }
            return foundInTree;
        }
        
        public List<SyntaxToken> howIsVariableUsed(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            switch (node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    return memberAccessHandler(trees, idTokens, node);
                case InvocationExpressionSyntax invocation:
                    return invocationHandler(trees, idTokens, node);
                case VariableDeclaratorSyntax variableDeclarator:
                    return variableDeclaratorHandler(trees, idTokens, node);
                case AssignmentExpressionSyntax assignment:
                    return assignmentExpressionHandler(trees, idTokens, node); 
                case ParameterSyntax parameter:
                    return new List<SyntaxToken>(); //Needs handling
                case ExpressionStatementSyntax expression:
                    return expressionStatementHandler(trees, idTokens, node);
                //might be missing some cases - needs researching
                default:
                    if (node.Parent != null){ 
                        return howIsVariableUsed(trees, idTokens, node.Parent); //Not sure if this should be done like this? 
                    }
                    return new List<SyntaxToken>();
            }
        }

        public List<SyntaxToken> memberAccessHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            bool parentIsInvocation = node.Parent is InvocationExpressionSyntax;
            if (parentIsInvocation)
            {
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
        }
        
        //The next couple of functions are identical except for their name
        public List<SyntaxToken> variableDeclaratorHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
                .ToList();

            return newIdTokens;
        }

        public List<SyntaxToken> expressionStatementHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;
        }

        public List<SyntaxToken> assignmentExpressionHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;
        }

    }
}