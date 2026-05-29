using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.Semantics;

namespace Project.SecretDetection.PlaceAnalysis{
    public class HttpDetector : PlaceDetector
    {
        public float weight = 1.0F;
        public override float getWeight(List<SyntaxTree> trees, string secret)
        {
            weight = 1.0F; //reset
            var dataflow = new DataFlowAnalyzer();
            List<SyntaxToken> initAs = whatIsVarInitializedAs(trees, "HttpClient"); //hardcoded "HttpClient" because we want to know what a developer has called a predefined HttpClient
                                                                                    //could also have been hardcoded into the function whatIsVarInitializedAs as lookFor


            //AD COMPILATION OSV HER SÅ DET KAN SENDES TIL GETTOKENSINTREE
            //GENOVERVEJ LIGE INITAS SÅ DET INDKOORPORERER INDPUTTET ER ALLE INSTANCER AF TOKENS ISTEDDET!


            Dictionary<SyntaxToken, List<SyntaxToken>> results = dataflow.initDataflow(trees, initAs);   //returns a list of all id tokens that "have been touched" by a http client through out code

            // Debugging
            // foreach(var res in results) 
            // {
            //     Console.WriteLine("the variables found associated with variable '{0}' are:", res.Key);
            //     foreach(var list in res.Value)
            //     {
            //         Console.WriteLine("   "+ list);
            //     }
            // }

            // if (results.Keys.Any(t => t.ValueText == secret)) //if we find the secret in the list, then we flag that secret
            if (results.Keys.Any(key => ReachesSecret(key, results, secret)))
            {
                weight = 100.0F;
            }

            return weight;
        }
        
        //Where is the http client initalized? Where do we need to look in the dataflow analysis?
        public List<SyntaxToken> whatIsVarInitializedAs(List<SyntaxTree> trees, string lookFor)
        {
            //Find HttpClient i AST som identificationToken
            //Find hvad den er initialized som?
            
            var results = new List<SyntaxToken>();
            foreach (var tree in trees)
            {
                // Console.WriteLine("New Tree");
                SyntaxNode root = tree.GetRoot(); //get root
                
                //Find where the Identification Tokens are located in AST
                //Save the SyntaxNodes where we find what we are looking for - saved as nodes so its easier for me to work with after this step
                var idTokensSyntaxNodes = root.DescendantTokens()
                    .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
                                t.ValueText == lookFor)
                    .Select(t => t.Parent)
                    .Where(n => n != null)
                    .ToList();
                
                // Find out what the Identification Tokens in AST are initialized as
                var initAs = idTokensSyntaxNodes
                    .Select(n => n.FirstAncestorOrSelf<VariableDeclaratorSyntax>())
                    .Where(v => v != null)
                    .Select(v => v!.Identifier)
                    .ToList();

                if (initAs.Any())
                {
                    results.AddRange(initAs);
                }
            }
            return results;

        }

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

                        return SymbolEqualityComparer.Default.Equals(
                            symbol,
                            originalSymbol!);
                    })
                    .Select(identifier => identifier.Identifier)
                    .Where(t => !idTokens.Contains(t)); 
                foundInTree.AddRange(matches);

                // OPTIONAL:
                // Also include declaration token itself
                // foundInTree.Add(idToken);
            }

            return foundInTree
                .Distinct()
                .ToList();
        }
        private bool ReachesSecret(SyntaxToken start, Dictionary<SyntaxToken, List<SyntaxToken>> graph, string secret)
        {
            var visited = new HashSet<SyntaxToken>();
            var stack = new Stack<SyntaxToken>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!visited.Add(current))
                    continue;

                // Check match
                if (current.ValueText == secret)
                    return true;

                // Traverse further if current is also a key
                if (graph.TryGetValue(current, out var nextTokens))
                {
                    foreach (var next in nextTokens)
                        stack.Push(next);
                }
            }

            return false;
        }
    }
}