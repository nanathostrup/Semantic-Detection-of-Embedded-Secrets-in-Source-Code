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
        public override float score { get; set; }
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
            string CommonPasswords = filepath + @"\SecretDetection\SecretsAnalysis\CommonPasswords";

            string normalizedSecret = StripQuotes(secret.Trim());

            var txtFiles = Directory.GetFiles(CommonPasswords, "*.txt");

            foreach (var file in txtFiles)
            {
                foreach (string line in File.ReadLines(file))
                {
                    if (line.Trim() == normalizedSecret)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private string StripQuotes(string value)
        {
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}