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
            string filePath = @"C:\Users\natd\OneDrive - Netcompany\Desktop\ad-portal";

            // string filePath = @"C:\Users\natd\OneDrive - Netcompany\Desktop\juice-shop";
            string filePath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WeatherSimple")
            );
            var secretDetector = new SecretDetector();
            secretDetector.Detect(filePath);            
        }
    }
}