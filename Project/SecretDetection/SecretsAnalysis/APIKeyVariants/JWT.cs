using System;
using System.Buffers.Text;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.SecretsAnalysis.APIKeyVariants{
    public class JWTDetector : APIVariant
    {
        public string apiType = "JWT secret token";
        public override bool isItAPI(string secret) //Does the string follow the format of a jwt secret?
        {
            int count = 0;
            foreach (char c in secret) { // Should only be 2 periods: 1 between header and payload and one between payload and signature
                if (c == '.') count++;
            }

            if (count == 2){
                try // Prøv på at inddele secreten i formaten og se om det følger
                {
                    //Header: Usually consists of two things - secifies the token type (JWT), and the signing algorithm
                    //Payload: contains the "claims"
                    //Signature: a hashvalue of encoded header, payload and a secret
                    string header = secret.Split('.')[0];
                    string payload = secret.Split('.')[1];
                    string signature = secret.Split('.')[2]; // Her kan vi allerede se at det måske godt kunne følge en format. Sus goes up

                    var base64Detector = new Base64Detector(); 
                    bool headerIsBase64 = base64Detector.isItBase64(header);
                    bool payloadIsBase64 = base64Detector.isItBase64(payload);

                    //Her ved vi at der er 3 sections, og header og payload er base64 encoded.
                    //Nok til at sige det er en token?
                    //Is the decoded stuff in json format? As it should be?
                    if (headerIsBase64 && payloadIsBase64)
                    {
                        //Verify that the decoded header and payload looks like json files
                        return true; //Yes it looks like a JWT secret    
                    }
                    return false; //Både header og payload SKAL være base64 encoded
                }
                catch //Eller følger det ikke format og ligner ikke en secret
                {
                    return false;
                }
            }
            return false;
        }
    }
}