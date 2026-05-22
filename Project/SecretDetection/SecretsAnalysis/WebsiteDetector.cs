
using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis{
    public class WebsiteDetector : SecretDetector
    {
        public float score = 0.0F;
        public override float detect(string secret)
        {
            return 0.0F; // https://uibakery.io/regex-library/url
                         // IDEA FOR IMPLEMENTATIO ^^^
        }
    }
}