using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.SecretsAnalysis.APIKeyVariants;


namespace Project.SecretDetection.SecretsAnalysis{
    public class APIKeyDetector: SecretDetector
    {
        public override float score { get; set; }
        public string apiType = "NONE";
        public override float detect(string secret)
        {
            score = 0.0F;
            if (doesItLookLikeAPIKey(secret))
            {
                score += 500000.0F;
            }
            return score;
        }

        public bool doesItLookLikeAPIKey(string secret) 
        {
            apiType = "NONE";
            var jwt = new JWTDetector();
            if (jwt.isItAPI(secret))
            {
                apiType = "a JWT secret token";
                return true;
            }
            return false;
        } 
    }
}