using System;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project.SecretDetection.Semantics{
    class DataFlowAnalyzer2
    {
        public Dictionary<SyntaxToken, List<SyntaxToken>> initDataflow2(List<SyntaxTree> trees, List<SyntaxToken> idTokens)
        {
            // List<SyntaxToken> foundInTrees = getIdTokenInTree(trees, idTokens);

            var dict = new Dictionary<SyntaxToken, List<SyntaxToken>>(); 
            foreach (var token in idTokens)
            {
                dict[token] = new List<SyntaxToken>();
            }
            List<SyntaxToken> visited = new List<SyntaxToken>();
            return dataflowAnalysis(trees, dict, visited);//, 0);
        }

        public Dictionary<SyntaxToken, List<SyntaxToken>> dataflowAnalysis(List<SyntaxTree> trees, Dictionary<SyntaxToken, List<SyntaxToken>> idTokens, List<SyntaxToken> visited)//, int counter) //Global dictionary? - Bøvlet at nulstille. Eller dictionary der bliver sendt rundt? Det er bare supre besværligt når man skal kalde den her funktion ude fra?
        {
            //we look into all id tokens
            //then for each id token we look through all trees
                //We need to find the idtoken in the tree
            //then we want to know what its parents are, so we can add the correct new id tokens, and repeat
            // Console.WriteLine("Dataflow analysis entered");
            
            
            //For debugging
            // if (counter == 2)
            // {
            //     return idTokens;
            // }
            
            //We need to look for all instances of the keys in the tree so that we can use them for the analysis
            List<SyntaxToken> lookFor = new List<SyntaxToken>(); 
            foreach (var kv in idTokens)
            {
                lookFor.Add(kv.Key);
            }
            List<SyntaxToken> foundInTrees = getIdTokenInTree(trees, lookFor);
            foreach(var f in foundInTrees)
            {
                if (!idTokens.Keys.Contains(f))
                {
                    idTokens.Add(f, new List<SyntaxToken>());
                }
            }


            Dictionary<SyntaxToken, List<SyntaxToken>> newFinds = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            foreach (var kv in idTokens)
            {
                var key = kv.Key;
                var value = new List<SyntaxToken>(kv.Value); // doing this way avoids writing directly to idTokens

                if(!visited.Contains(kv.Key)){ //avoids recomputing if we check for already visited tokens
                    List<SyntaxToken> someName = howIsVariableUsed(trees, new List<SyntaxToken>(), key.Parent);
                    value.AddRange(someName);
                    if (value.Contains(key))
                    {
                        value.Remove(key);  // we dont want cycles, so if we have added the key to the list of references, we want to remove it
                                            // might have to reconsidder this at some point, does not seem future proof...
                    }
                    newFinds.Add(key, value.Distinct().ToList()); //new and old values added without repeats for that key
                    visited.Add(key);
                }
                else
                {
                    newFinds.Add(key, value); //add idTokens to newFinds, we dont want to recompute things we already have computed
                }
            }

            //Add the values as new keys in newFinds unless they already exist as keys
            List<SyntaxToken> additions = new List<SyntaxToken>();
            foreach(var kv in newFinds)
            {
                foreach(var value in kv.Value)
                {
                    if (!newFinds.Keys.Contains(value))
                    {
                        additions.Add(value);
                    }
                }
            }
            foreach (var add in additions)
            {
                newFinds[add] = new List<SyntaxToken>();
            }

            //check if idTokens and newFinds are the same - if they are the analysis did not add anything and we can end the function
            bool equal = areEqual(newFinds, idTokens);
            if (equal)
            {
                return idTokens; // stop klods - if there are no new tokens to add then we stop
            }
            else
            {
                // counter ++; //til debugging
                return dataflowAnalysis(trees, newFinds, visited);//, counter);
            }
        }

        public List<SyntaxToken> getIdTokenInTree(List<SyntaxTree> trees, List<SyntaxToken> idTokens) // RETHINK THiS METHOD
        {
            List<SyntaxToken> foundInTree = new List<SyntaxToken>();
            foreach (var idToken in idTokens)
            {
                for (int i = 0; i < trees.Count; i++)
                    {
                        SyntaxNode root = trees[i].GetRoot();
                        var matchingTokens = root.DescendantTokens()
                            .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
                                        t.Text == idToken.Text) //SYNDEREN
                            .ToList();
                        
                        foundInTree.AddRange(matchingTokens);
                    }
            }
            return foundInTree;
        }

        public bool areEqual(Dictionary<SyntaxToken, List<SyntaxToken>> dict1 , Dictionary<SyntaxToken, List<SyntaxToken>> dict2)
        {
            if (dict1.Count != dict2.Count)
            {
                return false;   
            }
            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var list2))
                    return false;

                var set1 = new HashSet<SyntaxToken>(kvp.Value);
                var set2 = new HashSet<SyntaxToken>(list2);

                if (!set1.SetEquals(set2))
                    return false;
            }

            return true;
        }   

        
        public List<SyntaxToken> howIsVariableUsed(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {


            // HVAD MED PREDEFINED IDENTIFIER NAMES I NOGLE KONTEKSTER? -- e.g. Console.WriteLine eller GetEnvironmentVariable ... 
            switch (node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    // return new List<SyntaxToken>();
                    return memberAccessHandler(trees, idTokens, node);
                case InvocationExpressionSyntax invocation:
                    return new List<SyntaxToken>();
                    // return invocationHandler(trees, idTokens, node);
                case VariableDeclaratorSyntax variableDeclarator:
                    // return new List<SyntaxToken>();
                    return variableDeclaratorHandler(trees, idTokens, node);
                case AssignmentExpressionSyntax assignment:
                    // return new List<SyntaxToken>();
                    return assignmentExpressionHandler(trees, idTokens, node); 
                case ParameterSyntax parameter:
                    return new List<SyntaxToken>();
                    // return new List<SyntaxToken>(); //Needs handling
                case ExpressionStatementSyntax expression:
                    return new List<SyntaxToken>();
                case InterpolationSyntax interpolation:
                    return InterpolationSyntaxHandler(trees, idTokens, node);
                    // return expressionStatementHandler(trees, idTokens, node);
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
            // we look at the arguments that go into the invocation method only. 
            // Not the other stuff. This can be reevaluated for the future, but for the sake of this project it does not make sense. Time is also ticking:)))))
            //FAKTISK : implementer for alle børn for hvis der er en metode der skal traces, så bliver den det ikke her...
            if(node is InvocationExpressionSyntax invocation)
            {
                var newIdTokens = invocation.ArgumentList
                    .Arguments
                    .Select(t => t.Expression)
                    .OfType<IdentifierNameSyntax>()
                    .Select(t => t.Identifier)
                    .ToList();

                return newIdTokens;
            }
            // //To handle other cases
            return idTokens;
            // var newIdTokens = node.DescendantNodes()
            //     .Where(t=> t.IsKind(SyntaxKind.TypeArgumentList));
            //     return 

            // var newIdTokens = node.DescendantTokens()
            //     .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
            //     .ToList();

            // return newIdTokens;
        }
        
        //The next couple of functions are identical except for their name
        public List<SyntaxToken> variableDeclaratorHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            //Kan laves til endnu en switch case med afarter af delcarators
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
        
        public List<SyntaxToken>  InterpolationSyntaxHandler(List<SyntaxTree> trees, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            return idTokens;
        }
    }
}