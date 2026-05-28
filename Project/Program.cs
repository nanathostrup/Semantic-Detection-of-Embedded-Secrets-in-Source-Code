using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection;
using System.Reflection;


namespace Project{
    class Program
    {
        public static void Main(String[] args)
        {
            string filePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WeatherSimple")
            );
            var secretDetector = new SecretDetector();
            secretDetector.Detect(filePath);
            
            
            // Console.WriteLine(symbol.SymbolDisplayFormat.FullyQualifiedFormat);
            // Console.WriteLine(AssemblyName.GetAssemblyName(@".\bin\Debug\net10.0\Secret Detection.dll"));


            // var code = @"
            //     using System;

            //     namespace Demo
            //     {
            //         class MyClass
            //         {
            //             int myField;
            //         }
            //     }";

            //     var tree = CSharpSyntaxTree.ParseText(code);

            //     var compilation = CSharpCompilation.Create(
            //         "Test",
            //         new[] { tree },
            //         new[]
            //         {
            //             MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            //         });

            //     var model = compilation.GetSemanticModel(tree);

            //     // find the identifier
            //     var root = tree.GetRoot();
            //     var identifier = root.DescendantNodes()
            //         .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax>()
            //         .First();

            //     // var symbol = model.GetDeclaredSymbol(identifier);

            //     var fqName = model.GetDeclaredSymbol(identifier).ToDisplayString(
            //         SymbolDisplayFormat.FullyQualifiedFormat);

            //     Console.WriteLine(fqName);
            
        }
    }
}