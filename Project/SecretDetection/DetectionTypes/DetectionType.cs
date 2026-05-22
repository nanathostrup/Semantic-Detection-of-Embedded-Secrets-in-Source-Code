using System.IO;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.SecretsAnalysis;
using Project.SecretDetection.PlaceAnalysis;

namespace Project.SecretDetection.DetectionsTypes{
    public abstract class DetectionsType //Abstract class for different scoring systems
    {
        // public float score; //Total score for that specific type of detection
        // public abstract void getScore(List<SyntaxTree> trees); // get the score for any type of detection
        // public abstract void handleDetection(); //Something like this
        // public abstract void buildReport();
    }
}