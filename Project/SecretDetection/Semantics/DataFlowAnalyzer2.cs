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
using Microsoft.CodeAnalysis.Text;

namespace Project.SecretDetection.Semantics{
    class DataFlowAnalyzer2
    {
        public Dictionary<SyntaxToken, List<SyntaxToken>> initDataflow2(List<SyntaxTree> trees, List<SyntaxToken> idTokens)
        {
            Console.WriteLine("DATAFLOW");
            //LAV EN DATAFLOW FOR HVER AF TRÆERNE
            //MED DATAFLOW FRA ROSLYN, SÅ BRUG FLOWSOUT FOR AT SE HVAD GÅR UD OG SE HVAD DER GÅR IND
                //HVIS DER ER NOGEN DER MATCHES SÅ SKAL DER EN CONNECTION I MELLEM DEM I ENDELIG DICTIONARY

            //HUSK AT SØRG FOR AT TOKENS DER ER I INDPUTTET KUN BLIVER TJEKKET I DERES EGET TRÆ

            // List<SyntaxToken> foundInTrees = getIdTokenInTree(trees, idTokens);
            var dict = new Dictionary<SyntaxToken, List<SyntaxToken>>(); 
            var res = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            foreach (var tree in trees)
            {
                Console.WriteLine("tree");
                    var treeTokens = idTokens //vi vil være sikker på at vi kun kigger på de tokens der er tilstede i det aktuelle træ. Ellers crasher det
                        .Where(t => t.Parent?.SyntaxTree == tree)
                        .ToList();
                    if (!treeTokens.Any())
                        continue;
                    dict = treeTokens.ToDictionary(
                        t => t,
                        t => new List<SyntaxToken>());

                var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
                var compilation = CSharpCompilation.Create("MyCompilation",syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
                var model = compilation.GetSemanticModel(tree);

                // int maxSpanStart = idTokens.Max(t => t.SpanStart);
                int maxSpanStart = treeTokens
                    .Select(t => t.SpanStart)
                    .DefaultIfEmpty(0)
                    .Max();
                var location = tree.GetLocation(new Microsoft.CodeAnalysis.Text.TextSpan(maxSpanStart, 0));
                var lineSpan = location.GetLineSpan();
                int lineIndex = lineSpan.StartLinePosition.Line;
                int searchBoundary = tree.GetText().Lines[lineIndex].End;

                if (dict.Any())
                {
                    List<SyntaxToken> visited = new List<SyntaxToken>();
                    Dictionary<SyntaxToken, List<SyntaxToken>> dataflowRes = dataflowAnalysis(tree, dict, visited, compilation, searchBoundary, model);//, 0);
                    res = res.Concat(dataflowRes).ToDictionary(x => x.Key, x => x.Value);
                }
                
                //se om der er nogle keys der flyder ind i et andet træ og ud fra et andet træ.
                //hvis så, hvilken key er det?
                //apply dataflow analysis på den her key der flyder ind, i det træ            
            
            }

            //Dictionary res er færdig bygget her så vi kan bruge det på alle træerne og ser om der er noget der flyder ind og ud.
            // vi vil se om der er nogle connections i mellem træer, så derfor er det vigtigt at hvert træ er færdig med sin analyse, så vi kan lave en bindeleds analyse
            //hvis der er noget der flyder ud af et træ og ind i et andet, så skal man lave dataflow analyse på den variabel der flyder ind i træet
                //vigtig note den variabel der flyder ud af et træ skal være en key i res.

            var Mscorlib1 = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation1 = CSharpCompilation.Create("MyCompilation", syntaxTrees: trees, references: new[] { Mscorlib1 });

            for (int i=0; i < trees.Count;  i ++)
            {
                var root = trees[i].GetRoot();
                var model = compilation1.GetSemanticModel(trees[i]);
                
                //Foreach (var r in res)
                    //if r.key is input in invocation method
                        //if the method CREATED/DECLARED in another tree
                            //find that methood usage (invocations), and input (all of the usages and the inpupts)
                                //apply dataflow on the inputs

                foreach (var r in res)
                {
                    if (r.Key.Parent?.SyntaxTree != trees[i]) continue;  
                    var arg = r.Key.Parent?.Parent as ArgumentSyntax;
                    
                    if (r.Key.Parent is not ParameterSyntax paramSyntax) continue;   // <-- parameter, not argument
                    
                    var model1 = compilation1.GetSemanticModel(paramSyntax.SyntaxTree);
                    var paramSymbol = model1.GetDeclaredSymbol(paramSyntax) as IParameterSymbol;
                    var methodSymbol = paramSymbol?.ContainingSymbol as IMethodSymbol;
                    if (methodSymbol == null) continue;
                    int idx = methodSymbol.Parameters.IndexOf(paramSymbol!);
                    
                    foreach (var other in trees)
                    {
                        if (other == paramSyntax.SyntaxTree) continue; //same tree
                        var otherModel = compilation1.GetSemanticModel(other);
                        
                        //Find the invocationmethods in the other trees
                        foreach (var inv in other.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                        {
                            //if they are used in the other tree
                            var called = otherModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                            if (!SymbolEqualityComparer.Default.Equals(called, methodSymbol)) continue;
                            if (idx < 0 || idx >= inv.ArgumentList.Arguments.Count) continue;

                            //Find the inputs to the usages
                            var argExpr = inv.ArgumentList.Arguments[idx].Expression as IdentifierNameSyntax;
                            if (argExpr == null) continue;
                                //These inputs should then be traced with dataflow analysis again and set to be a key in the dataflow analysis 
                                    //insert them to the value list to the r.key.
                                        //apply dataflow analysis on the res again (which is ok and not too much because of visited list:) )

                            Console.WriteLine("PARAM {1} <--- ARG {0}", paramSyntax.Identifier.ValueText,argExpr.Identifier.ValueText);
                            // dataflow on argExpr.Identifier in "other" to reach secret1 = "Hello"
                        }
                    }
                }
            }
            return res;
        }

                    // // if (arg?.Parent is ArgumentListSyntax argList && argList.Parent is InvocationExpressionSyntax invocation)
                    //     var methodSymbol = model.GetSymbolInfo(paramSyntax).Symbol as IMethodSymbol;
                    //     if (methodSymbol == null) continue;   // didn't bind (e.g. framework method)
                    //         foreach (var other in trees)
                    //         {
                    //             var otherModel = compilation1.GetSemanticModel(other);

                    //             foreach (var inv in other.GetRoot()
                    //                                     .DescendantNodes()
                    //                                     .OfType<InvocationExpressionSyntax>())
                    //             {
                    //                 var called = otherModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                    //                 if (!SymbolEqualityComparer.Default.Equals(called, methodSymbol)) continue;
                    //                 if (inv == invocation) continue;          // skip the one we started from

                    //                 Console.WriteLine($"CALLED in {System.IO.Path.GetFileName(other.FilePath)}: {inv}");
                    //                 // inv is another call site of the same method.
                    //             }
                    //         }

                        // declRef.SyntaxTree is the tree where the method body lives.
                        // if (declRef.SyntaxTree == trees[j])
                        // {
                    //         Console.WriteLine("ITS FROM ANOTHER FILE: " + methodSymbol);
                    //         // invocation in trees[i] calls a method DECLARED in trees[j].
                    //     // }
                    // }
                    //     }
                    // }
            //     }
            // }            
        //     return res;
        // }



                //For every invocation in the tree
                    //if there is an inv created in another tree
                        //is the arglist in idtokens? 
                            //apply dataflow analysis on the arglist in 

                // foreach (var r in res)
                // {
                //     Console.WriteLine("\n" + r.Key);

                //     //if idtoken in this tree
                //         //if token is input in invocation syntax
                //     if (r.Key.Parent?.SyntaxTree ==trees[i])
                //     {
                //         var arg = r.Key.Parent?.Parent as ArgumentSyntax;
                //         if (arg?.Parent is ArgumentListSyntax argList && argList.Parent is InvocationExpressionSyntax invocation)
                //         {
                //             Console.WriteLine(arg); // VI SKAL SE OM ALLE TOKENS DER LEDER FRA DEN HER KØRER IND I NOGET I ET ANDET TRÆ!
                //             for (int j = 0; j < trees.Count; j++)
                //             {
                //             //if invocation is MADE/CREATED in another tree
                //                 //add the input r to the value for r
                //                 // run dataflow on the dicitonary

                //                 if (i != j)
                //                 {
                //                     var methodSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                //                     if (methodSymbol == null) continue;   // didn't bind (e.g. framework method)

                //                     var declRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                //                     if (declRef == null) continue;        // no source — external/library method

                //                     // declRef.SyntaxTree is the tree where the method body lives.
                //                     if (declRef.SyntaxTree == trees[j])
                //                     {
                //                         Console.WriteLine("ITS FROM ANOTHER FILE: " + methodSymbol);
                //                         // invocation in trees[i] calls a method DECLARED in trees[j].
                //                     }
                //                     //if(invocation method is declared in tree[j])
                //                     // {
                //                     //     ;
                //                     // }
                //                 }
                //             }
                //         }
                //     }
                // }
            // }
        //     return res;
        // }

        public Dictionary<SyntaxToken, List<SyntaxToken>> dataflowAnalysis(SyntaxTree tree, Dictionary<SyntaxToken, List<SyntaxToken>> idTokens, List<SyntaxToken> visited, CSharpCompilation compilation, int searchBoundary, SemanticModel model)//, int counter) //Global dictionary? - Bøvlet at nulstille. Eller dictionary der bliver sendt rundt? Det er bare supre besværligt når man skal kalde den her funktion ude fra?
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
            
            // Console.WriteLine("Entered dataflow");

            //We need to look for all instances of the keys in the tree so that we can use them for the analysis
            List<SyntaxToken> lookFor = new List<SyntaxToken>(); 
            foreach (var kv in idTokens)
            {
                lookFor.Add(kv.Key);
            }
            List<SyntaxToken> foundInTrees = getIdTokenInTree(tree, lookFor, compilation, searchBoundary, model);
            foreach(var f in foundInTrees)
            {
                if (!idTokens.Keys.Contains(f))
                {
                    idTokens.Add(f, new List<SyntaxToken>());
                }
            }

            //FILTER OUT TOKENS THAT DO NOT HAVE A DIRECT CONNECTION!
                //Global variables are not traced further
                //search boundary which is the start inputs line

            //Dont go to the already visited nodes
            Dictionary<SyntaxToken, List<SyntaxToken>> newFinds = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            foreach (var kv in idTokens)
            {
                var key = kv.Key;
                var value = new List<SyntaxToken>(kv.Value); // doing this way avoids writing directly to idTokens

                // if (!visited.Any(v => v.SpanStart == kv.Key.SpanStart && v.ValueText == kv.Key.ValueText))
                // {
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
           
            foreach (var add in additions)
            {
                // if(!IsFieldDeclaration(add, model))
                // {
                    newFinds[add] = new List<SyntaxToken>();
                // }
            }
            // newFinds = newFinds
            //     .Where(kv=> !IsFieldDeclaration(kv.Key, model)) // Ensure we dont go furhter with the class variables once we are at the top of tree
            //     .ToDictionary(kv=> kv.Key, kv => kv.Value);
            
            // var filteredIdTokens = idTokens
            //     .Where(kv=> !IsFieldDeclaration(kv.Key, model)) // Ensure we dont go furhter with the class variables once we are at the top of tree
            //     .ToDictionary(kv=> kv.Key, kv => kv.Value);

            //check if idTokens and newFinds are the same - if they are the analysis did not add anything and we can end the function
            bool equal = areEqual(newFinds, idTokens);
            // bool equal = areEqual(newFinds, filteredIdTokens);

            if (equal)
            {
                return idTokens; // stop klods - if there are no new tokens to add then we stop
            }
            else
            {
                // counter ++; //til debugging
                return dataflowAnalysis(tree, newFinds, visited, compilation, searchBoundary, model);//, counter);
            }
        }
        public bool IsFieldDeclaration(SyntaxToken token, SemanticModel model)
        {
            var symbol = model.GetDeclaredSymbol(token.Parent!);
            if (symbol?.Kind == SymbolKind.Field)
            {
                return true;
            }
            var referencedSymbol = model.GetSymbolInfo(token.Parent!).Symbol;
            if(referencedSymbol?.Kind == SymbolKind.Field)
            {
                return true;
            }
            return false;
        }


        public List<SyntaxToken> getIdTokenInTree(SyntaxTree tree, List<SyntaxToken> idTokens, CSharpCompilation compilation, int searchBoundary, SemanticModel model)
        {
            var foundInTree = new List<SyntaxToken>();

            var root = tree.GetRoot();
            // var model = compilation.GetSemanticModel(tree);

            foreach (var idToken in idTokens)
            {
                // if (idToken.Parent == null)
                //     continue;

                // Get original symbol
                var originalSymbol = model.GetDeclaredSymbol(idToken.Parent!) ?? model.GetSymbolInfo(idToken.Parent!).Symbol;

                if (originalSymbol == null) continue;

                // if(originalSymbol!.Kind != SymbolKind.Local &&
                //    originalSymbol!.Kind != SymbolKind.Field &&
                //    originalSymbol!.Kind != SymbolKind.Parameter) continue;

                var declarationSpan =  originalSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().SpanStart ?? idToken.SpanStart;
                int maxSpanStart = idTokens.Max(t => t.SpanStart);
                var location = tree.GetLocation(new Microsoft.CodeAnalysis.Text.TextSpan(maxSpanStart, 0));
                var lineSpan = location.GetLineSpan();
                int lineIndex = lineSpan.StartLinePosition.Line;
                searchBoundary = tree.GetText().Lines[lineIndex].End;
                var candidates = root.DescendantNodes()
                    .Where(n => n.SpanStart <= searchBoundary)
                    .Select(n => n switch
                    {
                        IdentifierNameSyntax id      => (node: (SyntaxNode)id, token: id.Identifier),
                        VariableDeclaratorSyntax v   => (node: v,  token: v.Identifier),
                        ParameterSyntax p            => (node: p,  token: p.Identifier),
                        _                            => (node: null, token: default)
                    })
                    .Where(x => x.node != null);

                foreach (var (node, token) in candidates)
                {
                    var symbol = model.GetDeclaredSymbol(node) ?? model.GetSymbolInfo(node).Symbol;
                    if (symbol == null) continue;                       // keep the null guard
                    if (SymbolEqualityComparer.Default.Equals(symbol, originalSymbol))
                        foundInTree.Add(token);
                }

                // Find ALL references/usages -- Within the boundaries.
                // var matches = root.DescendantNodes()
                //     .OfType<IdentifierNameSyntax>()
                //     .Where(identifier => identifier.SpanStart <= searchBoundary)

                //     // .Where(identifier => identifier.SpanStart <= idToken.SpanStart)
                //     .Where(identifier =>
                //     {
                //         var symbol = model.GetDeclaredSymbol(identifier) ?? model.GetSymbolInfo(identifier.Parent!).Symbol;
                //         if(symbol == null) return false;
                //         return SymbolEqualityComparer.Default.Equals(symbol,originalSymbol!);
                //     })
                //     .Select(identifier => identifier.Identifier);

                // foundInTree.AddRange(matches);
            }

            return foundInTree.Distinct().ToList();
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

        
        public List<SyntaxToken> howIsVariableUsed(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            // HVAD MED PREDEFINED IDENTIFIER NAMES I NOGLE KONTEKSTER? -- e.g. Console.WriteLine eller GetEnvironmentVariable ... 
            switch (node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    // return new List<SyntaxToken>();
                    // Console.WriteLine("member access");
                    return memberAccessHandler(tree, idTokens, node);
                case InvocationExpressionSyntax invocation:
                    // Console.WriteLine("Invocation");
                    // return new List<SyntaxToken>();
                    return invocationHandler(tree, idTokens, node);
                case VariableDeclaratorSyntax variableDeclarator:
                    // return new List<SyntaxToken>();
                    // Console.WriteLine("variable declarator");
                    return variableDeclaratorHandler(tree, idTokens, node);
                case AssignmentExpressionSyntax assignment:
                    // return new List<SyntaxToken>();
                    // Console.WriteLine("Assigment");
                    return assignmentExpressionHandler(tree, idTokens, node); 
                case ParameterSyntax parameter:
                    // Console.WriteLine("parameter");
                    return new List<SyntaxToken>();
                    // return new List<SyntaxToken>(); //Needs handling
                case ExpressionStatementSyntax expression:
                    // Console.WriteLine("expression");
                    return new List<SyntaxToken>();
                // case InterpolationSyntax interpolation:
                //     // Console.WriteLine("interpolation");
                //     return InterpolationSyntaxHandler(tree, idTokens, node);
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
            // var newIdTokens = node.DescendantTokens();
            if(node is InvocationExpressionSyntax invocation)
            {
                var tokens = invocation.ArgumentList
                    .Arguments
                    .Select(t => t.Expression)
                    .OfType<IdentifierNameSyntax>()
                    .Select(t => t.Identifier)
                    .ToList();

                return tokens;
            }
            return idTokens;
            //To handle other cases
            // var toookens = node.DescendantTokens()
            //     .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
            //     .ToList();

            // return toookens;
        }
        
        //The next couple of functions are identical except for their name
        public List<SyntaxToken> variableDeclaratorHandler(SyntaxTree tree, List<SyntaxToken> idTokens, SyntaxNode node)
        {
            //Kan laves til endnu en switch case med afarter af delcarators
            var newIdTokens = node.DescendantTokens()
                .Where(t => t.IsKind(SyntaxKind.IdentifierToken) && !idTokens.Contains(t))// && t.ValueText != "city") // to make debugging easier
                .ToList();

            return newIdTokens;
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

        }
    }
}