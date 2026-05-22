//LAV DATAFLOW ANALYSE DER ER SUPER ABSTRAKT I SEMANTICS
//IMPLEMENTER ANALYSE AF ALLE ENDPOINTS HVOR EN VARIABEL ENDER HER I FORSKELLIGE FILER.
//LAV EN ABSTRACT CLASS HER OG UDVID MED SPECIFIKKE STEDER, EG ENDER DET I EN HTTP NOGET ELLER WHATEVER, EN HEADER TIL JWT...using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.PlaceAnalysis{
    public abstract class PlaceDetector
    {
        public float weight; //how critical is the place the secret is leaked to?
        public abstract float getWeight(List<SyntaxTree> trees, string secret); //input is where to look for the leak (the code base as syntax trees)
                                                                                //and the string secret you are looking to trace
                                                                                //should return the weight of place detection
    }
}