//Scoring logic for the environment checkusing System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.SecretsAnalysis;
using Project.SecretDetection.PlaceAnalysis;

namespace Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections{
    class EnvScorer //Abstract class til scoring så det kan udvides senere?
    {

        //Class does analysis on the secrets found in the envChecker, and based on the fact that the potential secret is from the env file,
        //are given a score of how critical the "secret" is based on this fact.

        //run different analysis on the strings that are found
        //get score from analysis
        //sum up the different scores based on the fact its in the env

        public List<EnvironmentFileDetection.EnvironmentVariable> getEnvironmentScore(List<EnvironmentFileDetection.EnvironmentVariable> environmentVariables, List<SyntaxTree> trees) //Til abstract class
        {
            var updatedScore = new List<EnvironmentFileDetection.EnvironmentVariable>(); 
            for (int i = 0; i < environmentVariables.Count; i++)
            {
                updatedScore.Add(giveScore(environmentVariables[i], trees));

                if (environmentVariables[i].used) //does not make sense to check where the unused variables are going. They are going nowhere already
                {
                    updatedScore[i] = giveWeight(updatedScore[i], trees);
                }
            }
            return updatedScore;
        }

        //For secret analysis
        public EnvironmentFileDetection.EnvironmentVariable giveScore(EnvironmentFileDetection.EnvironmentVariable environmentVariable, List<SyntaxTree> trees)
        {
            //The different secret analysis methods to be done on the environmentVariable
            var entropyDetector = new EntropyDetector();
            var hexDetector = new HexDetector();
            var base64Detector = new Base64Detector(); 
            var apiKeyDetector = new APIKeyDetector();
            var passwordDetector = new PasswordDetector();

            float entVal = entropyDetector.detect(environmentVariable.secret); //If entVal = 1 har stringen en entropi på over 3, hvilket er en bound JEG har sat, men kan ændres
            float hexVal = hexDetector.detect(environmentVariable.secret);
            float base64Val = base64Detector.detect(environmentVariable.secret);
            float apiVal = apiKeyDetector.detect(environmentVariable.secret);
            float pwVal = passwordDetector.detect(environmentVariable.secret);

            //Does it look like API?
                //Return kritical score
            //Does it look like pw? --- MANGLER IMPLEMENTATION AF CHECK
                //Return Critical score
            //Does it look like a word? --- MANGLER IMPLEMENTATION AF CHECK
                //Return without score
            //Does it look like base64?
                //Return medium score
            //Does it look like Hex?
                //Does it have high entropy?
                    //Return mild score
                //Return no score
            //Does it have high entropy?
                //Does the length look sus?
                    //Return mild score
                //Return no score
            //Return no score

            if(apiVal > 0) //critical
            {
                environmentVariable.score += apiVal;
                environmentVariable.comment = environmentVariable.comment +  "This string looks like " + apiKeyDetector.apiType +". " ; //måske en dum måde at give en kommentar?
            }
            if (pwVal > 0.0)
            {
                environmentVariable.score += pwVal;
                environmentVariable.comment = environmentVariable.comment +  "This string looks like a password. " ; //måske en dum måde at give en kommentar?
            }
            // if (secret looks like a word && not password) //no detection needed
            //     {
            //          return without a detection og slut her.
            //     }
            if (base64Val > 0) //medium detection
            {
                environmentVariable.score += base64Val;
                environmentVariable.comment = environmentVariable.comment + "This string looks like it is base64 encoded. ";
            }
            if (hexVal > 0) // medium detection
            {
                environmentVariable.score += hexVal;
                environmentVariable.comment = environmentVariable.comment + "This string looks like hex. ";
            }
            if (entVal > 0)
            {
                if (environmentVariable.secret.Length > 15) //mild detection
                                                            //Ryk evt denne bound ind i entropy detector klassen (det er dens ansvar at give en score)
                                                            //15 fordi det er et langt ord. Kan ændres. 
                                                            //Kan evt slettes da det er del af entropi analyse anyways?
                {
                    environmentVariable.score += entVal;
                    environmentVariable.comment = environmentVariable.comment + "This string is long and has high entropy. ";
                }
            }
            return environmentVariable;
        }

        //For dataflow analysis
        public EnvironmentFileDetection.EnvironmentVariable giveWeight(EnvironmentFileDetection.EnvironmentVariable environmentVariable, List<SyntaxTree> trees)
        {
            //place detectors
            var httpDetector = new HttpDetector();
            float httpWeight = httpDetector.getWeight(trees, environmentVariable.name);
            float usedWeight = 1.0F;

            if (environmentVariable.used) //lige gyldigt check, da vi ikke går ind i denne funktion hvis den ikke er brugt, men for tydelighedens skyld
            {
                usedWeight += 1.0F; //Bare lige for at markere at det er noget der bliver brugt og har lidt mere vægt? Kan tages ud igen
            }

            environmentVariable.score = environmentVariable.score * httpWeight * usedWeight;

            if(httpWeight != 1.0)
            {
                environmentVariable.comment = environmentVariable.comment + "This string is going into a http. ";
            }
            return environmentVariable;
        }
    } 
}