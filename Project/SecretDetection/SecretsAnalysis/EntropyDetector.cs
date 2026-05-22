using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public class EntropyDetector : SecretDetector
    {
        public float score;
        public override float detect (string secret)
        {
            score = 0.0F; 
            double entropy = ShannonEntropy(secret);
            entropy = Math.Round(entropy);
            int convetedEntropy = Convert.ToInt32(entropy);            
            //Evt lav til switch case
            // if (convetedEntropy > 5) // eks på mulig expansion in the future - men out of scope here
            // {
            //     return 100000;
            // }
            // else if (convetedEntropy>4)
            // {
            //     return 100;
            // }
            if (convetedEntropy > 3)// Vilkårlig threshhold, som kan ændres senere hen
            {
                score =+ 10.0F;
            }
            return score;
        }

        //This function is a translation of the function here: https://thesagardahal.medium.com/understanding-shannon-entropy-measuring-randomness-for-secure-code-auditing-4b3c5697a7f9
        //Exact implementation is in python - see python/entrpy.py for their full code implementation
        public double ShannonEntropy(string str)
        {
            if (str == null)
            {
                return 0.0F;
            }

            //Count the frequency of each character in the string
            var freq = new Dictionary<char, int>();
            foreach (char ch in str)
            {
                freq[ch] = freq.GetValueOrDefault(ch, 0) + 1;
            }

            double entropy = 0.0;
            double totalLength = str.Length;

            //Calculate the probability of each character and sum the entropy terms
            foreach (double count in freq.Values)
            {
                double probability = count/totalLength;
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }

        
        //SÆT IND I SecretDetector.cs FOR AT FÅ RESULTATER. HER BOUNDEN 3> KOMMER FRA. FÅR RESULTATER PÅ SAMME TESTS SOM DE ANDRE DETECTORS ER TESTED MED:

            // Console.WriteLine(" ############ TEST ############ ");
            // List<string> randomwords = new List<string>() {"oranges", "google", "traffic light", "cykel", "random", "ApiKeys", "Cryptography", "durumrulle", "laptopskærm", "entropy", "ARGGHHHHHH", "pneumonoultramicroscopicsilicovolcanoconiosis", "Antidisestablishmentarianism", "kat", "Champ", "Titin", "Aegilops", "Champichamp", "DemonChild", "Bæstet"};
            // foreach(string rand in randomwords)
            // {
            //     int val = entropyDetector.detect(rand);
            //     Console.WriteLine("Measured entropy for string {0}: {1}", rand, val);

            // }
            // Console.WriteLine(" ############ TEST ############ ");
            // List<string> secretsIsh = new List<string>() {"ea413b8c6e9657e69c24ca2b83e6d895", "password", "api_XweVmYIoqSCHxVOb4Q6C1zMFs0O92zPu", "AKIAIOSFODNN73XAMPL3", "wJalrXUtnFEMI/K7MDENG/bPxRfiCY3XAMPL3K3Y", "IQoJb3JpZ2luX2VjEFAaCXVzLXdlc3QtMiJIMEYCIQDFAK3", "AIzaSyDaGmWKa4JsXZ-HjGw7ISLn_3namBGewQe", "sk_live_C8YfyXfzocnRZNE36yzd7Pg3Wl0aqCad", "sk_live_skfikf5682lfjas896dsndhfuek9hy654", "pk_t3st_4RxUQ9rE2xn8vIbplcQlCLQN", "sk_live_3hmB4s6o0a62C7vrsK00sBJPb3z4CzY9GSEz1dfMtloMec9LpD949IbDPwbeW", "ghp_abcdefghijklmnopqrstuvwxyz123456", "github_pat_11ABCDEF1234567890FAK3", "glpat-abcdefghijklmnopqrstuvwxyz", "123456789:AAFAK3-telegram-bot-token-3xampl3", "your_auth_token_32charslong", "SG.fak3_sendgrid_api_k3y_3xampl3", "SuperLongJWTSigningSecretK3y123456789", "WOlJeDRXzIDR9N0xXrQjIOYNoMYrlEvMz3HF91RTy", "YXZhaWxhYmxldGlyZWRldmVudHRhbGVzcmVndWxhcnByb2R1Y2VlbGV2ZW5zdGFydGM", "ec820703bf716f1bf64a2e54199395ed"};
            // foreach(string str in secretsIsh)
            // {
            //     int val = entropyDetector.detect(str);
            //     Console.WriteLine("Measured entropy for string {0}: {1}", str, val);
            // }
    }
}