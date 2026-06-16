using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public abstract class SecretDetector
    {
        public abstract float score {get; set;}
        public abstract float detect(string secret);//used to detect if the string is a secret
                                                    //should return the score of the severity of the type of secret it looks like
    }
}