using System.IO;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project.SecretDetection.SecretsAnalysis;
using Project.SecretDetection.PlaceAnalysis;
using Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections;
using Project.SecretDetection.Semantics;

namespace Project.SecretDetection.DetectionsTypes{
    public class EnvironmentFileDetection : DetectionsType //Abstract class for different scoring systems
    {
        public struct EnvironmentVariable //SKAL IKKE VÆRE STRUCKT -OG NOGLE SKAL IND I ABSTRACT CLASS
        {
            public int index { get; set;} //where is it located?
            public string envfile { get; set;} //where is it located?
            public string secret { get; set;} //what is the detection "secret"?
            public string name { get; set;} //what is it initialized as? What is the secrets "name"?
            public float score { get; set;} //how critical a detection is
            public string comment { get; set;} //what kind of "secret" is it if any?
            public bool used { get; set;} //is the variable used in code?
            public EnvironmentVariable(int index, string envfile, string secret, string name, float score, string comment, bool used)
            {
                this.index = index;
                this.envfile = envfile;
                this.secret = secret; //abstract
                this.name = name; //abstract
                this.score = score; //abstract
                this.comment = comment; //abstract
                this.used = used;
            }
        }
        
        public void handleDetection(List<SyntaxTree> trees, string filePath, Dictionary<string, string> environmentVariableMap)
        {
            List<EnvironmentVariable> usedEnvironmentVariables = checkEnvironmentFileForUsed(environmentVariableMap, filePath);
            List<EnvironmentVariable> unusedEnvironmentVariables = checkEnvironmentFileForUnused(environmentVariableMap, filePath);

            Console.WriteLine("");
            Console.WriteLine(" =========================== EXTRACTING VARIABLES ============================= ");
            foreach(var usedEnvironmentVariable in usedEnvironmentVariables) //Debugging
            {
                Console.WriteLine("USED environment variable {0} found in file: {1},\n on line {2}. secret: {3}", 
                usedEnvironmentVariable.name, usedEnvironmentVariable.envfile, usedEnvironmentVariable.index, usedEnvironmentVariable.secret);
            }
            Console.WriteLine("");
            foreach(var unusedEnvironmentVariable in unusedEnvironmentVariables)
            {
                Console.WriteLine("UNUSED environment variable {0} found in file: {1},\n on line {2}. secret: {3}", 
                unusedEnvironmentVariable.name, unusedEnvironmentVariable.envfile, unusedEnvironmentVariable.index, unusedEnvironmentVariable.secret);
            }
            
            Console.WriteLine("");
            Console.WriteLine(" ================================= SCORING =================================== "); 
            List<EnvironmentVariable> usedEnvVarWithScores = giveScore(trees, usedEnvironmentVariables); 
            List<EnvironmentVariable> unusedEnvVarWithScores = giveScore(trees, unusedEnvironmentVariables); 
            foreach(var usedEnvVarWithScore in usedEnvVarWithScores)
            {
                Console.WriteLine("Name: {0}. Final score for secret {1}: {2}", usedEnvVarWithScore.name, usedEnvVarWithScore.secret, usedEnvVarWithScore.score);
                Console.WriteLine("Comments on detection: {0}", usedEnvVarWithScore.comment);
            }
            foreach(var unusedEnvVarWithScore in unusedEnvVarWithScores)
            {
                Console.WriteLine("Name: {0}. Final score for secret {1}: {2}", unusedEnvVarWithScore.name, unusedEnvVarWithScore.secret, unusedEnvVarWithScore.score);
                Console.WriteLine("Comments on detection: {0}", unusedEnvVarWithScore.comment);
            }

            //build report
            Console.WriteLine("");
            Console.WriteLine(" ================================== REPORT =================================== ");
            buildReport(usedEnvVarWithScores, false);
            buildReport(unusedEnvVarWithScores, true); //hardcoded append feature. Fix in future.
            string logpath = Path.Combine(Directory.GetCurrentDirectory(), "Report.txt"); 
            
            Console.WriteLine("A report has been made in " + logpath);

        }
        public List<EnvironmentVariable> checkEnvironmentFileForUsed(Dictionary<string, string> environmentVariableMap, string filePath)
        {
            var envChecker =  new EnvChecker();
            List<EnvironmentVariable> usedEnvironmentVariables = new List<EnvironmentVariable>();
            return usedEnvironmentVariables = envChecker.getUsedEnvVariables(environmentVariableMap, filePath);
        }
        public List<EnvironmentVariable> checkEnvironmentFileForUnused(Dictionary<string, string> environmentVariableMap, string filePath)
        {
            var envChecker =  new EnvChecker();
            List<EnvironmentVariable> unusedEnvironmentVariables = new List<EnvironmentVariable>();
            return unusedEnvironmentVariables = envChecker.getUnusedEnvVariables(environmentVariableMap, filePath); 
        }

        public List<EnvironmentVariable> giveScore(List<SyntaxTree> trees, List<EnvironmentVariable> environmentVariables)
        {
            var envScorer = new EnvScorer();
            return envScorer.getEnvironmentScore(environmentVariables, trees);
        }
        public void buildReport(List<EnvironmentVariable> environmentVariables, bool appendable)
        {
            string logpath = Path.Combine(Directory.GetCurrentDirectory(), "Report.txt"); //Create the output log
            logpath = Path.GetFullPath(logpath);

            using (StreamWriter writer = new StreamWriter(logpath, append: appendable)) // append: false to overwrite, true to append to existing file, tak til chat:)
            {
                foreach (var envVar in environmentVariables)
                {
                    if (envVar.used)
                    {
                        writer.WriteLine("An environment variable is used in file {0} on line {1} and has a score of {2}. {3}", envVar.envfile, envVar.index, envVar.score, envVar.comment);
                    }
                    else
                    {
                        writer.WriteLine("An unused environment variable is detected in file {0} on line {1} and has a score of {2}. {3}", envVar.envfile, envVar.index, envVar.score, envVar.comment);
                    }
                }
            }
        }
    }
}