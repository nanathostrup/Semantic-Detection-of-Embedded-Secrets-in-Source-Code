using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public class DictionaryDetector : SecretDetector
    {
        public float score = 0.0F;
        public override float detect(string secret)
        {
            return 0.0F; // Brug evt. den her: https://github.com/loresoft/NetSpell
                      // Skal researches først
                      // Eksempel brug af NetSpell: https://stackoverflow.com/questions/38416265/c-sharp-checking-if-a-word-is-in-an-english-dictionary 
        
            //Tag hensyn til at der kan være flere ord i en string input e.g. den i test env filen.
        }
    }
}