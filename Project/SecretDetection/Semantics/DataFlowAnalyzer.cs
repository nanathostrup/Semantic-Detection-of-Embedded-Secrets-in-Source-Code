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
    class DataFlowAnalyzer
    {
        public Dictionary<SyntaxToken, List<SyntaxToken>> initDataflow(List<SyntaxTree> trees, List<SyntaxToken> idTokens)
        {
            //LAV EN DATAFLOW FOR HVER AF TRÆERNE
            //MED DATAFLOW FRA ROSLYN, SÅ BRUG FLOWSOUT FOR AT SE HVAD GÅR UD OG SE HVAD DER GÅR IND
                //HVIS DER ER NOGEN DER MATCHES SÅ SKAL DER EN CONNECTION I MELLEM DEM I ENDELIG DICTIONARY

            //HUSK AT SØRG FOR AT TOKENS DER ER I INDPUTTET KUN BLIVER TJEKKET I DERES EGET TRÆ

            var dict = new Dictionary<SyntaxToken, List<SyntaxToken>>(); 
            var res = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            foreach (var tree in trees)
            {
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
            Dictionary<SyntaxToken, List<SyntaxToken>> res2 = new Dictionary<SyntaxToken, List<SyntaxToken>>();
            res2 = res2.Concat(res).ToDictionary(x => x.Key, x => x.Value); // copy the results into a new dictionary so we can itterate over res and save them to res2
            

            for (int i=0; i < trees.Count;  i ++)
            {
                var root = trees[i].GetRoot();
                var model = compilation1.GetSemanticModel(trees[i]);
                
                //Foreach (var r in res)
                    //if r.key is input in invocation method
                        //if the method CREATED/DECLARED in another tree
                            //find that methood usage (invocations), and input (all of the usages and the inpupts)
                                //apply dataflow on the inputsss

                foreach (var r in res)
                {
                    if (r.Key.Parent?.SyntaxTree != trees[i]) continue;  
                    var arg = r.Key.Parent?.Parent as ArgumentSyntax;
                    
                    if (r.Key.Parent is not ParameterSyntax paramSyntax) continue;   // parameter, not argument
                    
                    var model1 = compilation1.GetSemanticModel(paramSyntax.SyntaxTree);
                    //
                    
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
                            r.Value.Add(argExpr.Identifier); //add the argExp.identfier to r's values so that we can do dataflow again

                            List<SyntaxToken> token = new List<SyntaxToken>{argExpr.Identifier};
                            Dictionary<SyntaxToken, List<SyntaxToken>> newDict = initDataflow(trees, token);
                            res2 = res2.Concat(newDict).ToDictionary(x => x.Key, x => x.Value);
                        }
                    }
                }
            }
            return res2;
        }

        public List<SyntaxToken> findTokensForSymbol(SyntaxTree tree, SemanticModel model, ISymbol target, int boundary)
        {
            var result = new List<SyntaxToken>();

            foreach (var n in tree.GetRoot().DescendantNodes().Where(n => n.SpanStart <= boundary))
            {
                SyntaxNode node;
                SyntaxToken token;
                switch (n)
                {
                    case IdentifierNameSyntax id:    node = id; token = id.Identifier; break;
                    case VariableDeclaratorSyntax v: node = v;  token = v.Identifier;  break;
                    case ParameterSyntax p:          node = p;  token = p.Identifier;  break;
                    default: continue;
                }

                var sym = model.GetDeclaredSymbol(node) ?? model.GetSymbolInfo(node).Symbol;
                if (sym == null) continue;
                if (SymbolEqualityComparer.Default.Equals(sym, target))
                    result.Add(token);
            }

            return result.Distinct().ToList();
        }



        public Dictionary<SyntaxToken, List<SyntaxToken>> dataflowAnalysis(SyntaxTree tree, Dictionary<SyntaxToken, List<SyntaxToken>> idTokens, List<SyntaxToken> visited, CSharpCompilation compilation, int searchBoundary, SemanticModel model)//, int counter) //Global dictionary? - Bøvlet at nulstille. Eller dictionary der bliver sendt rundt? Det er bare supre besværligt når man skal kalde den her funktion ude fra?
        {
            //we look into all id tokens
            //then for each id token we look through all trees
                //We need to find the idtoken in the tree
            //then we want to know what its parents are, so we can add the correct new id tokens, and repeat
            
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

                newFinds[add] = new List<SyntaxToken>();

            }

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

            foreach (var idToken in idTokens)
            {
                var originalSymbol = model.GetDeclaredSymbol(idToken.Parent!) ?? model.GetSymbolInfo(idToken.Parent!).Symbol;

                if (originalSymbol == null) continue;

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
                        IdentifierNameSyntax id      => (node: (SyntaxNode?)id, token: id.Identifier),
                        VariableDeclaratorSyntax v   => (node: v,  token: v.Identifier),
                        ParameterSyntax p            => (node: p,  token: p.Identifier),
                        _                            => (node: null, token: default)
                    })
                    .Where(x => x.node != null);

                foreach (var (node, token) in candidates)
                {
                    var symbol = model.GetDeclaredSymbol(node!) ?? model.GetSymbolInfo(node!).Symbol;
                    if (symbol == null) continue;                       // keep the null guard
                    if (SymbolEqualityComparer.Default.Equals(symbol, originalSymbol))
                        foundInTree.Add(token);
                }

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
            switch (node)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    return memberAccessHandler(tree, idTokens, node);
                case InvocationExpressionSyntax invocation:
                    return invocationHandler(tree, idTokens, node);
                case VariableDeclaratorSyntax variableDeclarator:
                    return variableDeclaratorHandler(tree, idTokens, node);
                case AssignmentExpressionSyntax assignment:
                    return assignmentExpressionHandler(tree, idTokens, node); 
                case ParameterSyntax parameter:
                    return new List<SyntaxToken>();
                case ExpressionStatementSyntax expression:
                    return new List<SyntaxToken>();
                // case InterpolationSyntax interpolation:
                //     return InterpolationSyntaxHandler(tree, idTokens, node);
                //might be missing some cases
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