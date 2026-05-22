// using System.Buffers.Text;
// using System.ComponentModel.DataAnnotations;
// using System.IO;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;
// using Project.SecretDetection.Semantics;

// namespace Project.SecretDetection.PlaceAnalysis{
//     public class HttpDetectorList : PlaceDetector
//     {
//         public float weight = 1.0F;
//         public override float getWeightL(List<SyntaxTree> trees, string secret)
//         {
//             weight = 1.0F; //reset
//             var dataflow = new DataFlowAnalyzer();
//             List<SyntaxToken> initAs = whatIsVarInitializedAs(trees, "HttpClient"); //hardcoded "HttpClient" because we want to know what a developer has called a predefined HttpClient
//                                                                                     //could also have been hardcoded into the function whatIsVarInitializedAs as lookFor

//             List<SyntaxToken> results = dataflow.dataflowAnalysis(trees, initAs);   //returns a list of all id tokens that "have been touched" by a http client through out code
           
//             // Debugging
//             foreach(var res in results) 
//             {
//                 Console.WriteLine("the variables found associated with input variables: {0}", res);
//             }

//             if (results.Any(t => t.ValueText == secret)) //if we find the secret in the list, then we flag that secret
//             {
//                 weight = 100.0F;
//             }

//             return weight;
//         }
        
//         //Where is the http client initalized? Where do we need to look in the dataflow analysis?
//         public List<SyntaxToken> whatIsVarInitializedAs(List<SyntaxTree> trees, string lookFor)
//         {
//             //Find HttpClient i AST som identificationToken
//             //Find hvad den er initialized som?
            
//             var results = new List<SyntaxToken>();
//             foreach (var tree in trees)
//             {
//                 // Console.WriteLine("New Tree");
//                 SyntaxNode root = tree.GetRoot(); //get root
                
//                 //Find where the Identification Tokens are located in AST
//                 //Save the SyntaxNodes where we find what we are looking for - saved as nodes so its easier for me to work with after this step
//                 var idTokensSyntaxNodes = root.DescendantTokens()
//                     .Where(t => t.IsKind(SyntaxKind.IdentifierToken) &&
//                                 t.ValueText == lookFor)
//                     .Select(t => t.Parent)
//                     .Where(n => n != null)
//                     .ToList();
                
//                 // Find out what the Identification Tokens in AST are initialized as
//                 var initAs = idTokensSyntaxNodes
//                     .Select(n => n.FirstAncestorOrSelf<VariableDeclaratorSyntax>())
//                     .Where(v => v != null)
//                     .Select(v => v.Identifier)
//                     .ToList();

//                 if (initAs.Any())
//                 {
//                     results.AddRange(initAs);
//                 }
//             }
//             return results;

//         }
//     }
// }