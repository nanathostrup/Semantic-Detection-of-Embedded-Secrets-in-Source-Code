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
    class DataFlowAnalyzer2
    {
        public Dictionary<SyntaxToken, List<SyntaxToken>> initDataflow2(SyntaxTree tree, List<SyntaxToken> idTokens)
        {
            //LAV EN DATAFLOW FOR HVER AF TRÆERNE
            //MED DATAFLOW FRA ROSLYN, SÅ BRUG FLOWSOUT FOR AT SE HVAD GÅR UD OG SE HVAD DER GÅR IND
                //HVIS DER ER NOGEN DER MATCHES SÅ SKAL DER EN CONNECTION I MELLEM DEM I ENDELIG DICTIONARY

            //HUSK AT SØRG FOR AT TOKENS DER ER I INDPUTTET KUN BLIVER TJEKKET I DERES EGET TRÆ



            // List<SyntaxToken> foundInTrees = getIdTokenInTree(trees, idTokens);
            var dict = new Dictionary<SyntaxToken, List<SyntaxToken>>(); 
            // var compilation = CSharpCompilation.Create(
            //     assemblyName: "Analysis",
            //     syntaxTrees: trees,
            //     references: new[]
            //     {
            //         MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

            //         MetadataReference.CreateFromFile(
            //             typeof(Console).Assembly.Location),

            //         MetadataReference.CreateFromFile(
            //             typeof(Enumerable).Assembly.Location)
            //     });
            
            var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("MyCompilation",syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);

            foreach (var token in idTokens)
            {
                dict[token] = new List<SyntaxToken>();
            }
            List<SyntaxToken> visited = new List<SyntaxToken>();
            return dataflowAnalysis(tree, dict, visited, compilation);//, 0);
        }

        public Dictionary<SyntaxToken, List<SyntaxToken>> dataflowAnalysis(SyntaxTree tree, Dictionary<SyntaxToken, List<SyntaxToken>> idTokens, List<SyntaxToken> visited, CSharpCompilation compilation)//, int counter) //Global dictionary? - Bøvlet at nulstille. Eller dictionary der bliver sendt rundt? Det er bare supre besværligt når man skal kalde den her funktion ude fra?
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
            
            Console.WriteLine("Entered dataflow");

            //We need to look for all instances of the keys in the tree so that we can use them for the analysis
            List<SyntaxToken> lookFor = new List<SyntaxToken>(); 
            foreach (var kv in idTokens)
            {
                lookFor.Add(kv.Key);
            }
            List<SyntaxToken> foundInTrees = getIdTokenInTree(tree, lookFor, compilation);
            foreach(var f in foundInTrees)
            {
                if (!idTokens.Keys.Contains(f))
                {
                    idTokens.Add(f, new List<SyntaxToken>());
                }
            }
            
            //FILTER OUT TOKENS THAT DO NOT HAVE A DIRECT CONNECTION!



            // //filter out the global variables once we've reached their end point
            // foreach (var kv in idTokens)
            // {
            //     var key = kv.Key;
            //     var value = new List<SyntaxToken>(kv.Value);
            //     //IF foundintrees[i] er under alle vores idtokens, 
            //     //  remove



            //     // MethodDeclarationSyntax enclosingMethod = key.Parent!.FirstAncestorOrSelf<MethodDeclarationSyntax>()!;
            //     // // 3. Cast the direct parent to a specific type
            //     // if (enclosingMethod.Parent is ClassDeclarationSyntax classNode)
            //     // {

               
            //     // }
            // }



            //Dont go to the already visited nodes
            Dictionary<SyntaxToken, List<SyntaxToken>> newFinds = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            foreach (var kv in idTokens)
            {
                var key = kv.Key;
                var value = new List<SyntaxToken>(kv.Value); // doing this way avoids writing directly to idTokens

                if(!visited.Contains(kv.Key)){ //avoids recomputing if we check for already visited tokens
                    List<SyntaxToken> someName = howIsVariableUsed(tree, new List<SyntaxToken>(), key.Parent!);
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
            // var model = compilation.GetSemanticModel(tree);
            // foreach (var add in additions)
            // {
            //     var originalSymbol = model.GetSymbolInfo(add.Parent!).Symbol
            //                     ?? model.GetDeclaredSymbol(add.Parent!);
                
            //     if (originalSymbol == null || 
            //         (originalSymbol.Kind != SymbolKind.Local && 
            //         originalSymbol.Kind != SymbolKind.Field &&
            //         originalSymbol.Kind != SymbolKind.Parameter))
            //     {
            //         // Still add the token itself, just don't expand usages
            //         if (!newFinds.ContainsKey(add))
            //             newFinds[add] = new List<SyntaxToken>();
            //         continue;
            //     }

            //     var allOccurrences = tree.GetRoot().DescendantTokens()
            //         .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
            //         .Where(t => {
            //             var sym = model.GetSymbolInfo(t.Parent!).Symbol
            //                 ?? model.GetDeclaredSymbol(t.Parent!);
            //             return SymbolEqualityComparer.Default.Equals(sym, originalSymbol);
            //         });

            //     foreach (var occurrence in allOccurrences)
            //     {
            //         if (!newFinds.ContainsKey(occurrence))
            //             newFinds[occurrence] = new List<SyntaxToken>();
            //     }
            // }

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
                return dataflowAnalysis(tree, newFinds, visited, compilation);//, counter);
            }
        }

    //   public List<SyntaxToken> getIdTokenInTree(SyntaxTree tree, List<SyntaxToken> idTokens, CSharpCompilation compilation)
    //     {
    //         fo
    //     }

        // public List<SyntaxToken> getIdTokenInTree(SyntaxTree tree, List<SyntaxToken> idTokens, CSharpCompilation compilation) // RETHINK THiS METHOD
        // {

        //     var root = tree.GetRoot();
        //     var model = compilation.GetSemanticModel(tree);

        //     foreach (var idtoken in idTokens){
        //         // var originalSymbol = GetSymbol(model, idToken.Parent!);

        //         // if (originalSymbol == null)
        //         //     return new List<SyntaxToken>();

        //         var matchingTokens = root.DescendantNodes()
        //             .OfType<IdentifierNameSyntax>()
        //             .Where(id =>
        //             {
        //                 var symbol = GetSymbol(model, id);

        //                 return SymbolEqualityComparer.Default.Equals(
        //                     symbol,
        //                     originalSymbol);
        //             })
        //             .Select(id => id.Identifier)
        //             .ToList();
        //         }

        //     return matchingTokens;
        //     }    
        public List<SyntaxToken> getIdTokenInTree(SyntaxTree tree, List<SyntaxToken> idTokens, CSharpCompilation compilation)
        {
            var foundInTree = new List<SyntaxToken>();

            var root = tree.GetRoot();
            var model = compilation.GetSemanticModel(tree);

            foreach (var idToken in idTokens)
            {
                // if (idToken.Parent == null)
                //     continue;

                // Resolve original symbol
                var originalSymbol = model.GetDeclaredSymbol(idToken.Parent!);
                    // GetSymbol(model, idToken.Parent!);

                // if (originalSymbol == null)
                //     continue;

                // Find ALL references/usages
                var matches = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(identifier =>
                    {
                        var symbol = model.GetDeclaredSymbol(identifier);
                            // GetSymbol(model, identifier);

                        return SymbolEqualityComparer.Default.Equals(symbol,originalSymbol!);
                    })
                    .Select(identifier => identifier.Identifier);

                foundInTree.AddRange(matches);

                // OPTIONAL:
                // Also include declaration token itself
                // foundInTree.Add(idToken);
            }

            return foundInTree
                .Distinct()
                .ToList();
        }
 
            // List<SyntaxToken> foundInTree = new List<SyntaxToken>();
            // foreach (var idToken in idTokens)
            // {
            //     var root = tree.GetRoot();
            //     var model = compilation.GetSemanticModel(tree);

            //     var originalSymbol =
            //         model.GetSymbolInfo(idToken.Parent!).Symbol;

            //     if (originalSymbol == null)
            //         return new List<SyntaxToken>();

            //     var matchingTokens = root.DescendantNodes()
            //         .OfType<IdentifierNameSyntax>()
            //         .Where(id =>
            //         {
            //             var symbol = model.GetSymbolInfo(id).Symbol;

            //             return SymbolEqualityComparer.Default.Equals(
            //                 symbol,
            //                 originalSymbol);
            //         })
            //         .Select(id => id.Identifier)
            //         .ToList();
                
                // foundInTree.AddRange(matchingTokens);
                // var sourceModel = compilation.GetSemanticModel(idToken.Parent!.SyntaxTree);

                //  for (int i = 0; i < trees.Count; i++)
                //     {
                        // var model = compilation.GetSemanticModel(tree);
                        // SyntaxNode root = tree.GetRoot();
                        // var matchingTokens = root.DescendantTokens()
                            // .Where(t => 
                                    // t.IsKind(SyntaxKind.IdentifierToken) &&
                //                         // (AssemblyName.GetAssemblyName(t.ValueText)==AssemblyName.GetAssemblyName(idToken.Text)))
                //                         // (Assembly.FullName.ToString(t) == Assembly.FullName.ToString(idToken)))
                                        // t.Text == idToken.Text) //SYNDEREN
                //                     // var symbol=model.GetSymbolInfo(t).Symbol
                //                     // SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(t.Parent!).Symbol, sourceModel.GetSymbolInfo(idToken.Parent!).Symbol))
                //                     // (model.GetDeclaredSymbol(t!.Parent!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.GetDeclaredSymbol(idToken!.Parent!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)))
                //                     // (model.GetSymbolInfo(t.Parent!).Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.GetSymbolInfo(idToken.Parent!).Symbol!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) 
                                // )
                            // .ToList();
                            // {
                            //     if (!t.IsKind(SyntaxKind.IdentifierToken))
                            //         return false;

                            //     var symbol1 = model.GetSymbolInfo(t.Parent!).Symbol;
                            //     var symbol2 = model.GetSymbolInfo(idToken.Parent!).Symbol;

                            //     if (symbol1 == null || symbol2 == null)
                            //         return false;


                            //     return SymbolEqualityComparer.Default.Equals(symbol1, symbol2);
                            //     // return symbol1.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            //         // == symbol2.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            // })
                        //     .ToList();
                        // foundInTree.AddRange(matchingTokens);
                    // }


                // for (int i = 0; i < trees.Count; i++)
                //     {
                //         SyntaxNode root = trees[i].GetRoot();
                //         var matchingTokens = root.DescendantTokens()
                //             .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
                //                         // (AssemblyName.GetAssemblyName(t.ValueText)==AssemblyName.GetAssemblyName(idToken.Text)))
                //                         // (Assembly.FullName.ToString(t) == Assembly.FullName.ToString(idToken)))
                //                         t.Text == idToken.Text) //SYNDEREN
                //             .ToList();

                //         foundInTree.AddRange(matchingTokens);
                //     }
        //     }
        //     return foundInTree;
        // }

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

        
        public List<SyntaxToken> howIsVariableUsed(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {


            // HVAD MED PREDEFINED IDENTIFIER NAMES I NOGLE KONTEKSTER? -- e.g. Console.WriteLine eller GetEnvironmentVariable ... 
            switch (node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    // return new List<SyntaxToken>();
                    Console.WriteLine("member access");
                    return memberAccessHandler(tree, idTokens, node);
                case InvocationExpressionSyntax invocation:
                    Console.WriteLine("Invocation");
                    // return new List<SyntaxToken>();
                    return invocationHandler(tree, idTokens, node);
                case VariableDeclaratorSyntax variableDeclarator:
                    // return new List<SyntaxToken>();
                    Console.WriteLine("variable declarator");
                    return variableDeclaratorHandler(tree, idTokens, node);
                case AssignmentExpressionSyntax assignment:
                    // return new List<SyntaxToken>();
                    Console.WriteLine("Assigment");
                    return assignmentExpressionHandler(tree, idTokens, node); 
                case ParameterSyntax parameter:
                    Console.WriteLine("parameter");
                    return new List<SyntaxToken>();
                    // return new List<SyntaxToken>(); //Needs handling
                case ExpressionStatementSyntax expression:
                    Console.WriteLine("expression");
                    return new List<SyntaxToken>();
                case InterpolationSyntax interpolation:
                    Console.WriteLine("interpolation");
                    return InterpolationSyntaxHandler(tree, idTokens, node);
                    // return expressionStatementHandler(trees, idTokens, node);
                //might be missing some cases - needs researching
                default:
                    if (node is FieldDeclarationSyntax || node is LocalDeclarationStatementSyntax)
                    {
                        return new List<SyntaxToken>();
                    }
                    if (node.Parent != null){ 
                        return howIsVariableUsed(tree, idTokens, node.Parent); //Not sure if this should be done like this? 
                    }
                    return new List<SyntaxToken>();
            }
        }

        public List<SyntaxToken> memberAccessHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            bool parentIsInvocation = node.Parent is InvocationExpressionSyntax;
            if (parentIsInvocation)
            {
                return invocationHandler(tree, idTokens, node.Parent!);
            }
            //HANDLE OTHER CASES OF THIS INSTANCE
            // return idTokens;
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
                .ToList();

            return newIdTokens;

        }
        public List<SyntaxToken> invocationHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            // we look at the arguments that go into the invocation method only. 
            // Not the other stuff. This can be reevaluated for the future, but for the sake of this project it does not make sense. Time is also ticking:)))))
            // FAKTISK : implementer for alle børn for hvis der er en metode der skal traces, så bliver den det ikke her...
            // if(node is InvocationExpressionSyntax invocation)
            // {
            //     var newIdTokens = invocation.ArgumentList
            //         .Arguments
            //         .Select(t => t.Expression)
            //         .OfType<IdentifierNameSyntax>()
            //         .Select(t => t.Identifier)
            //         .ToList();

            //     return newIdTokens;
            // }
            // //To handle other cases
            // return idTokens;
            // var newIdTokens = node.DescendantNodes()
            //     .Where(t=> t.IsKind(SyntaxKind.TypeArgumentList));
            //     return 

            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
                .ToList();

            return newIdTokens;
        }
        
        //The next couple of functions are identical except for their name
        // public List<SyntaxToken> variableDeclaratorHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        // {
        //     //Kan laves til endnu en switch case med afarter af delcarators
        //     var newIdTokens = node.DescendantTokens()
        //         .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
        //         .ToList();

        //     return newIdTokens;
        // }
        public List<SyntaxToken> variableDeclaratorHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            if (node is VariableDeclaratorSyntax declarator && declarator.Initializer != null)
            {
                // Only trace the right-hand side, not the declared name itself
                var newIdTokens = declarator.Initializer.DescendantTokens()
                    .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                    .ToList();
                return newIdTokens;
            }
            return new List<SyntaxToken>();
        }


        public List<SyntaxToken> expressionStatementHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;
        }

        public List<SyntaxToken> assignmentExpressionHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;
        }
        
        public List<SyntaxToken>  InterpolationSyntaxHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {            
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))
                .ToList();

            return newIdTokens;


            // return idTokens;
        }
    }
}