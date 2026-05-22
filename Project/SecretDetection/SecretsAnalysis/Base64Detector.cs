using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public class Base64Detector : SecretDetector
    {
        public float score = 0;
        public override float detect(string secret)
        {
            score = 0.0F;
            bool base64 = isItBase64(secret);
            if (base64)
            {
                score += 3000.0F;
            }
            return score;
        }
        public bool isItBase64(string secret)
        {
            try
            {
                var base64 = secret.Replace('-', '+').Replace('_', '/')
                    .PadRight(secret.Length + (4 - secret.Length % 4) % 4, '='); //URL safe - vigtig specielt med jwt tokens

                var bytes = Convert.FromBase64String(base64);
                var decoded = new System.Text.UTF8Encoding(false, true).GetString(bytes); // Tak til chat:)

                return true;
            }
            catch
            {
                return false;
            }
        }
        
        //SÆT IND I SecretDetector.cs FOR AT FÅ RESULTATER PÅ SAMME TESTS SOM DE ANDRE DETECTORS ER TESTED MED:
        
            // Console.WriteLine(" ############ TEST ############ ");
            // List<string> randomwords = new List<string>() {"oranges", "google", "traffic light", "cykel", "random", "ApiKeys", "Cryptography", "durumrulle", "laptopskærm", "entropy", "ARGGHHHHHH", "pneumonoultramicroscopicsilicovolcanoconiosis", "Antidisestablishmentarianism", "kat", "Champ", "Titin", "Aegilops", "Champichamp", "DemonChild", "Bæstet"};
            // foreach(string rand in randomwords)
            // {
            //     int val = base64Detector.detect(rand);
            //     Console.WriteLine("Does this string look like base64? {0}: {1}", rand, val);

            // }
            // Console.WriteLine(" ############ TEST ############ ");
            // List<string> secretsIsh = new List<string>() {"ea413b8c6e9657e69c24ca2b83e6d895", "password", "api_XweVmYIoqSCHxVOb4Q6C1zMFs0O92zPu", "AKIAIOSFODNN73XAMPL3", "wJalrXUtnFEMI/K7MDENG/bPxRfiCY3XAMPL3K3Y", "IQoJb3JpZ2luX2VjEFAaCXVzLXdlc3QtMiJIMEYCIQDFAK3", "AIzaSyDaGmWKa4JsXZ-HjGw7ISLn_3namBGewQe", "sk_live_C8YfyXfzocnRZNE36yzd7Pg3Wl0aqCad", "sk_live_skfikf5682lfjas896dsndhfuek9hy654", "pk_t3st_4RxUQ9rE2xn8vIbplcQlCLQN", "sk_live_3hmB4s6o0a62C7vrsK00sBJPb3z4CzY9GSEz1dfMtloMec9LpD949IbDPwbeW", "ghp_abcdefghijklmnopqrstuvwxyz123456", "github_pat_11ABCDEF1234567890FAK3", "glpat-abcdefghijklmnopqrstuvwxyz", "123456789:AAFAK3-telegram-bot-token-3xampl3", "your_auth_token_32charslong", "SG.fak3_sendgrid_api_k3y_3xampl3", "SuperLongJWTSigningSecretK3y123456789", "WOlJeDRXzIDR9N0xXrQjIOYNoMYrlEvMz3HF91RTy", "YXZhaWxhYmxldGlyZWRldmVudHRhbGVzcmVndWxhcnByb2R1Y2VlbGV2ZW5zdGFydGM", "ec820703bf716f1bf64a2e54199395ed"};
            // foreach(string str in secretsIsh)
            // {
            //     int val = base64Detector.detect(str);
            //     Console.WriteLine("Does this string look like base64? {0}: {1}", str, val);
            // }
    }
}