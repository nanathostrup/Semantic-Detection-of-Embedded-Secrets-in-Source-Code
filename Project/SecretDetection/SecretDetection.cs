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



using Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections; //SLET WHEN DONE DEBUGGING


namespace Project.SecretDetection{
    public class SecretDetector
    {
        public void Detect(string filePath)
        {
            // foreach file
            //     make AST
            
            // Foreach AST
            //     if something accesses env file
            //         check that variable in cs file in env file
            //             is it a string litteral?

            Console.WriteLine("");
            Console.WriteLine(" ============================= CONVERTING TO AST ============================= ");
            var ast = new AST();
            List<SyntaxTree> trees = ast.createASTs(filePath);

            Console.WriteLine("");
            Console.WriteLine(" ================================= WALK AST ================================= ");
            
            //walk tree to find special rule for invocation expressions with "GetEnvironmentVariable" condition
            var walker = new Walker();
            foreach (SyntaxTree tree in trees)
            {
                SyntaxNode root = tree.GetRoot(); //Get the root of the tree
                walker.Visit(root); //Check the current AST for invocation expressions. Walker går gennem træet, og der er blevet lavet sær regel for invocation expressions
            }
            
            Dictionary<string, string> environmentVariableMap = walker.EnvironmentVariableMap; //making a new dictionary that is not a walker.field -- can send this on

            foreach (var kvp in walker.EnvironmentVariableMap) //For debugging
            {
                Console.WriteLine("GetEnvironmentVariable() input: " + kvp.Key);
                Console.WriteLine("                Initialized as: " + kvp.Value);
                // Console.WriteLine("");
            }
            
            // Console.WriteLine("");
            // Console.WriteLine(" ================================= ENV CHECK ================================= ");
            // var envFileDetection = new EnvironmentFileDetection();
            // envFileDetection.handleDetection(trees, filePath, environmentVariableMap); // OUTCOMMENT WHEN DONE DEBUGGING
            
            

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// DEBUGGING///
            
            Console.WriteLine("");
            Console.WriteLine(" ================================= DATAFLOW ANALYSIS ================================= ");
            var httpDetector= new HttpDetector();
            List<SyntaxTree> firstTree = new List<SyntaxTree> { trees[3] };
            var dfa = new DataFlowAnalyzer2();
            

            List<SyntaxToken> httper = httpDetector.whatIsVarInitializedAs(firstTree, "HttpClient");
            foreach(var http in httper)
            {
                Console.WriteLine("HTTPS: " + http);
            }
            
            dfa.initDataflow2(trees[3], httper);
            Dictionary<SyntaxToken, List<SyntaxToken>> dfaTest = dfa.initDataflow2(trees[3], httper);


            foreach (var kvp in dfaTest)
            {
                SyntaxToken key = kvp.Key;
                List<SyntaxToken> values = kvp.Value;
                Console.WriteLine("Key: " + key);
                foreach (var value in values)
                {
                    Console.WriteLine($"       Value: {value}");
                }
            }






            // string teststring = "random";
            // float weight = httpDetector.getWeight(trees, teststring);
            // Console.WriteLine("The weight of http location detection: {0}, for variable {1}", weight, teststring); 


            // List<SyntaxToken> httper = httpDetector.whatIsVarInitializedAs(firstTree, "HttpClient");
            // foreach(var http in httper)
            // {
            //     Console.WriteLine("HTTPS: " + http);
            // }
            // var dfa = new DataFlowAnalyzer2();
            // Dictionary<SyntaxToken, List<SyntaxToken>> dfaTest = dfa.initDataflow2(firstTree, httper);


            // foreach (var kvp in dfaTest)
            // {
            //     SyntaxToken key = kvp.Key;
            //     List<SyntaxToken> values = kvp.Value;
            //     Console.WriteLine("Key: " + key);
            //     foreach (var value in values)
            //     {
            //         Console.WriteLine($"       Value: {value}");
            //     }
            // }


            // Console.WriteLine("");
            // Console.WriteLine(" ================================= SCORING =================================== ");
            // var entropyDetector = new EntropyDetector();
            // var hexDetector = new HexDetector();
            // var base64Detector = new Base64Detector();

            // var apikeyDetector = new APIKeyDetector();
            // apikeyDetector.detect("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYWRtaW4iOnRydWUsImlhdCI6MTUxNjIzOTAyMn0.KMUFsIDTnFmyG3nMiGM6H9FNFUROf3wh7SmqJp-QV30");

            // var envScorer = new EnvScorer();
            // envScorer.getScore(usedEnvironmentVariables, trees);

            // Console.WriteLine("");
            // envScorer.getScore(unusedEnvironmentVariables, trees);
        

            // // // Console.WriteLine(" ############ TEST ############ ");
            // // // List<string> randomwords = new List<string>() {"oranges", "google", "traffic light", "cykel", "random", "ApiKeys", "Cryptography", "durumrulle", "laptopskærm", "entropy", "ARGGHHHHHH", "pneumonoultramicroscopicsilicovolcanoconiosis", "Antidisestablishmentarianism", "kat", "Champ", "Titin", "Aegilops", "Champichamp", "DemonChild", "Bæstet"};
            // // // foreach(string rand in randomwords)
            // // // {
            // // //     int val = base64Detector.detect(rand);
            // // //     Console.WriteLine("Measured base64 for string {0}: {1}", rand, val);
            // // // }
            // // // Console.WriteLine(" ############ TEST ############ ");
            // // // List<string> secretsIsh = new List<string>() {"ea413b8c6e9657e69c24ca2b83e6d895", "password", "api_XweVmYIoqSCHxVOb4Q6C1zMFs0O92zPu", "AKIAIOSFODNN73XAMPL3", "wJalrXUtnFEMI/K7MDENG/bPxRfiCY3XAMPL3K3Y", "IQoJb3JpZ2luX2VjEFAaCXVzLXdlc3QtMiJIMEYCIQDFAK3", "AIzaSyDaGmWKa4JsXZ-HjGw7ISLn_3namBGewQe", "sk_live_C8YfyXfzocnRZNE36yzd7Pg3Wl0aqCad", "sk_live_skfikf5682lfjas896dsndhfuek9hy654", "pk_t3st_4RxUQ9rE2xn8vIbplcQlCLQN", "sk_live_3hmB4s6o0a62C7vrsK00sBJPb3z4CzY9GSEz1dfMtloMec9LpD949IbDPwbeW", "ghp_abcdefghijklmnopqrstuvwxyz123456", "github_pat_11ABCDEF1234567890FAK3", "glpat-abcdefghijklmnopqrstuvwxyz", "123456789:AAFAK3-telegram-bot-token-3xampl3", "your_auth_token_32charslong", "SG.fak3_sendgrid_api_k3y_3xampl3", "SuperLongJWTSigningSecretK3y123456789", "WOlJeDRXzIDR9N0xXrQjIOYNoMYrlEvMz3HF91RTy", "YXZhaWxhYmxldGlyZWRldmVudHRhbGVzcmVndWxhcnByb2R1Y2VlbGV2ZW5zdGFydGM", "ec820703bf716f1bf64a2e54199395ed"};
            // // // foreach(string str in secretsIsh)
            // // // {
            // // //     int val = base64Detector.detect(str);
            // // //     Console.WriteLine("Measured entropy for string {0}: {1}", str, val);
            // // // }


            //For printing each AST
            // foreach (SyntaxTree tree in trees)
            // {
            //     Console.WriteLine(" ================================= NEW SYNTAX TREE ================================= ");
            //     // Get the root of the tree
            //     SyntaxNode root = tree.GetRoot();
            //     PrintNode(root, 0);
            // // }
            // SyntaxNode rooot = trees[1].GetRoot();
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