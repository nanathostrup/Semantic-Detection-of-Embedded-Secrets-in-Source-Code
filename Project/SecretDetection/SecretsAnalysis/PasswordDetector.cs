using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public class PasswordDetector : SecretDetector
    {
        public float score = 0.0F;
        public override float detect(string secret)
        {
            score = 0.0F;

            if (isItPassword(secret))
            {
                 return score += 40000.0F;
            }

            return score;
        }

        public bool isItPassword(string secret)
        {
            string filepath = Directory.GetCurrentDirectory(); //non-hardcoded
            string CommonPasswords = filepath+ @"\SecretDetection\SecretsAnalysis\CommonPasswords";            

            var txtFiles = Directory.GetFiles(CommonPasswords, "*.txt");

            foreach (var file in txtFiles)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string contents = reader.ReadToEnd();
                    if (contents.Contains(secret))
                    
                    // if (contents.Trim() == secret.Trim()) // we need an exact match 
                    {
                        return true;
                    }
                }
            }
            return false;
        } 
    }
}