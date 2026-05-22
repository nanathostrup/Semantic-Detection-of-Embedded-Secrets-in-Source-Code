using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections{
    class EnvChecker
    {
        public List<EnvironmentFileDetection.EnvironmentVariable> getUnusedEnvVariables(Dictionary<string, string> environmentVariableMap, string filePath) //OPTIMIZE!!!
        {
            //For each stringargument 
                //is it present in the envfiles?
                    //if so - extract the suffix in the file
            
            List<EnvironmentFileDetection.EnvironmentVariable> unusedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();
            bool used = false;

            var EnvFiles = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories)
                .Where(f =>
                    f.Contains(@".env")
                );
            Console.WriteLine("Checking these files for unused env variables in .env files found in directory"); //Just to make life easier we print what we check 
            foreach (var envfile in EnvFiles)
            {
                Console.WriteLine("     " + envfile);
            }

            foreach (var envfile in EnvFiles)
            {
                int i = 0; //create an index counter so we can track the location in the env file and can write this to the report
                foreach (var line in File.ReadLines(envfile))
                {
                    i++; //update the index counter every time we move line :)
                    used = false;
                    
                    foreach (var kvp in environmentVariableMap)
                    {
                        if (line.Contains(kvp.Key))
                        {
                            used = true;
                        }
                    }

                    if (!used)
                    {
                        // unusedEnvironmentVariables.Add(line); //Hele linjen in envfilen
                        string extractedStr = extractString(line); //Kun selve valuen
                        int locationIndex = i;//Lokationen i env filen
                        float score = 0.0F; //Den skal initialiseres her, og opdateres i analysen af stringen
                        string comment = ""; //Den skal initialiseres her, og opdateres i analyse delen   
                        string name = extractName(line); //smid navnet på hvad secreten er initialiseret som ind 
                                                         // -- Anderledes end usedenvvars fordi det den er intialiseret som bare er i env filen og ikke i koden
                        var envVar = new EnvironmentFileDetection.EnvironmentVariable(locationIndex, envfile, extractedStr, name, score, comment, false);
                        unusedEnvironmentVariables.Add(envVar);
                    }
                }
            }
            return unusedEnvironmentVariables;
        }


        public List<EnvironmentFileDetection.EnvironmentVariable> getUsedEnvVariables(Dictionary<string, string> environmentVariableMap, string filePath) //OPTIMIZE!!!
        {
            List<EnvironmentFileDetection.EnvironmentVariable> usedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();

            var EnvFiles = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories)
                .Where(f =>
                    f.Contains(@".env")
                );
            Console.WriteLine("Checking these files for used env variables in .env files found in directory"); //Just to make life easier we print what we check 
            foreach (var envfile in EnvFiles)
            {
                Console.WriteLine("     " + envfile);
            }

            foreach (var envfile in EnvFiles)
            {
                int i = 0; //create an index counter so we can track the location in the env file and can write this to the report
                foreach (var line in File.ReadLines(envfile))
                {
                    i++; //update the index counter every time we move line :)
                    
                    // for(int j = 0; j < StringArgs.Count; j++)
                    foreach (var kvp in environmentVariableMap)
                    {
                        if (line.Contains(kvp.Key))
                        {
                            // usedEnvironmentVariables.Add(line); //Hele linjen in envfilen
                            string extractedStr = extractString(line); //Kun selve valuen
                            int locationIndex = i; //Lokationen i env filen - burde måske være sin egen funtion men det her er så meget nemmere:)))
                            float score = 0.0F; //Den skal initialiseres her, og opdateres i analysen af stringen
                            string comment = ""; //Den skal initialiseres her, og opdateres i analyse delen
                            string name = kvp.Value; //Hvad selve secreten er initializeret som i koden. Skal bruges til dataflow analysen
                            var envVar = new EnvironmentFileDetection.EnvironmentVariable(locationIndex, envfile, extractedStr, name, score, comment, true);
                            usedEnvironmentVariables.Add(envVar);
                        }
                    }
                }
            }
            return usedEnvironmentVariables;
        }
        
        public string extractString(string envVariables) // Giver dig din string uden = og uden mellemrummet. Hvis ikke "= " passer så får man bare alt.
        {
            try
            {
                string extractedString = envVariables.Split('=')[1];
                if (extractedString.StartsWith(' '))
                {
                    string newExtractedString = extractedString.Split(' ')[1];
                    return newExtractedString;
                }
                else
                {
                    return extractedString;
                }
            }
            catch
            {
                return envVariables;
            }
        }
        public string extractName(string envVariables) // Giver dig din string uden = og uden mellemrummet. Hvis ikke "= " passer så får man bare alt.
        {
            try
            {
                string extractedName = envVariables.Split('=')[0];
                return extractedName;
            }
            catch
            {
                return envVariables;
            }
        }
    }
}