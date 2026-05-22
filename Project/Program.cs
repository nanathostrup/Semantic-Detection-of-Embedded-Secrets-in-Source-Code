using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection;

namespace Project{
    class Program
    {
        public static void Main(String[] args)
        {
            // GetCurrentDirectory()
            string filePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WeatherSimple")
            );

            Console.WriteLine(filePath);
            // string filePath =  "";
            var secretDetector = new SecretDetector();
            // secretDetector.Detect(filePath);
        }
    }
}