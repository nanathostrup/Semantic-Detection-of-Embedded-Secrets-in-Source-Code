using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections
{
    class EnvChecker
    {
        // Find env files once, reuse for both used/unused passes.
        // Old filter f.Contains(".env") also matched files like "config.environment".
        private List<string> GetEnvFiles(string filePath)
        {
            return Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var name = Path.GetFileName(f);
                    return name.StartsWith(".env", StringComparison.OrdinalIgnoreCase)
                        || name.EndsWith(".env", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();
        }

        // Resolve the parent dir once; Directory.GetParent can be null at a filesystem root.
        private static string GetParentPath(string filePath)
        {
            var parent = Directory.GetParent(filePath);
            return parent?.FullName ?? filePath;
        }

        // Folds common look-alike characters back to plain ASCII so parsing doesn't
        // silently skip a line (e.g. a fullwidth U+FF1D instead of an ASCII '=').
        private static string NormalizeLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return line;

            var sb = new System.Text.StringBuilder(line.Length);
            foreach (var c in line)
            {
                switch (c)
                {
                    // Strip zero-width / BOM characters that wedge into pasted text.
                    case '\uFEFF': // BOM / zero-width no-break space
                    case '\u200B': // zero-width space
                    case '\u200C': // zero-width non-joiner
                    case '\u200D': // zero-width joiner
                    case '\u2060': // word joiner
                        break;

                    // Fold look-alike equals signs to ASCII '='.
                    case '\uFF1D': // fullwidth equals sign
                        sb.Append('=');
                        break;

                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public List<EnvironmentFileDetection.EnvironmentVariable> getUnusedEnvVariables(
            Dictionary<string, string> environmentVariableMap, string filePath)
        {
            // For each entry in the .env files: if its key is NOT referenced anywhere
            // in the scanned code (environmentVariableMap), report it as unused.
            var unusedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();

            var envFiles = GetEnvFiles(filePath);
            string parentPath = GetParentPath(filePath);

            Console.WriteLine("Checking these files for unused env variables:");
            foreach (var envfile in envFiles)
                Console.WriteLine("     " + envfile);

            foreach (var envfile in envFiles)
            {
                string shortenedPath = Path.GetRelativePath(parentPath, envfile);

                foreach (var entry in extractEntries(envfile))
                {
                    bool used = environmentVariableMap.Keys
                        .Any(mapKey => entry.key.Trim() == mapKey.Trim());

                    if (!used)
                    {
                        float score = 0.0F;   // filled in later by the analysis step
                        string comment = "";  // filled in later by the analysis step

                        var envVar = new EnvironmentFileDetection.EnvironmentVariable(
                            entry.lineNumber,
                            shortenedPath,
                            entry.value.Trim(),
                            entry.key,
                            score,
                            comment,
                            false);
                        unusedEnvironmentVariables.Add(envVar);
                    }
                }
            }
            return unusedEnvironmentVariables;
        }

        public List<EnvironmentFileDetection.EnvironmentVariable> getUsedEnvVariables(
            Dictionary<string, string> environmentVariableMap, string filePath)
        {
            var usedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();

            var envFiles = GetEnvFiles(filePath);
            string parentPath = GetParentPath(filePath);

            Console.WriteLine("Checking these files for used env variables:");
            foreach (var envfile in envFiles)
                Console.WriteLine("     " + envfile);

            foreach (var envfile in envFiles)
            {
                string shortenedPath = Path.GetRelativePath(parentPath, envfile);

                foreach (var entry in extractEntries(envfile))
                {
                    foreach (var kvp in environmentVariableMap)
                    {
                        if (entry.key.Trim() == kvp.Key.Trim())
                        {
                            float score = 0.0F;
                            string comment = "";
                            string name = kvp.Value; // how the secret is referenced in code (for dataflow analysis)

                            var envVar = new EnvironmentFileDetection.EnvironmentVariable(
                                entry.lineNumber,
                                shortenedPath,
                                entry.value.Trim(),
                                name,
                                score,
                                comment,
                                true);
                            usedEnvironmentVariables.Add(envVar);
                            break; // one map hit per entry is enough; avoids duplicate findings
                        }
                    }
                }
            }
            return usedEnvironmentVariables;
        }

        // Parses an .env file into a LIST of (key, value, lineNumber).
        // A list (not a dictionary) so duplicate keys -- e.g. two "host=" lines --
        // are all preserved instead of overwriting each other.
        public List<(string key, string value, int lineNumber)> extractEntries(string envfile)
        {
            var entries = new List<(string key, string value, int lineNumber)>();
            bool multiline = false;
            string currentValue = "";
            string currentKey = "";
            int lineNumber = 0;
            int entryStartLine = 0;

            foreach (var rawLine in File.ReadLines(envfile))
            {
                lineNumber++;
                var line = NormalizeLine(rawLine); // fold look-alike '=' / strip zero-width chars

                if (multiline)
                {
                    // Preserve the newline that File.ReadLines stripped.
                    currentValue += "\n" + line;
                    if (line.TrimEnd().EndsWith("\""))
                    {
                        multiline = false;
                        entries.Add((currentKey, currentValue.Trim(), entryStartLine));
                        currentKey = "";
                        currentValue = "";
                    }
                    continue;
                }

                var trimmed = line.Trim();

                // Skip blank lines and comments (a line like "# FOO=bar" must not be parsed).
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                    continue;

                // Support the common "export KEY=value" form.
                if (trimmed.StartsWith("export "))
                    trimmed = trimmed.Substring("export ".Length).TrimStart();

                int eq = trimmed.IndexOf('=');
                if (eq < 0)
                {
                    // No separator even after normalization: warn instead of silently dropping.
                    Console.WriteLine(
                        $"[EnvChecker] WARNING: no '=' on line {lineNumber} of {envfile}: <{trimmed}>");
                    continue;
                }

                currentKey = trimmed.Substring(0, eq).Trim();
                currentValue = trimmed.Substring(eq + 1);
                entryStartLine = lineNumber;

                var v = currentValue.Trim();
                if (v.StartsWith("\"") && !v.EndsWith("\""))
                {
                    multiline = true; // opening quote not yet closed on this line
                }
                else
                {
                    entries.Add((currentKey, currentValue.Trim(), lineNumber));
                    currentKey = "";
                    currentValue = "";
                }
            }

            // If a quoted value was never closed, flush it instead of silently dropping
            // it (and everything after it).
            if (multiline && currentKey.Length > 0)
                entries.Add((currentKey, currentValue.Trim(), entryStartLine));

            return entries;
        }

        // Returns the value part of "KEY=value" (without the '=' and leading space).
        public string extractString(string envVariables)
        {
            try
            {
                string extractedString = envVariables.Split('=')[1];
                if (extractedString.StartsWith(' '))
                    return extractedString.Split(' ')[1];
                return extractedString;
            }
            catch
            {
                return envVariables;
            }
        }

        // Returns the key part of "KEY=value".
        public string extractName(string envVariables)
        {
            try
            {
                return envVariables.Split('=')[0];
            }
            catch
            {
                return envVariables;
            }
        }
    }
}
// using System;
// using System.IO;
// using System.Security.Cryptography;
// using Microsoft.CodeAnalysis;
// using Microsoft.CodeAnalysis.CSharp;
// using Microsoft.CodeAnalysis.CSharp.Syntax;


// namespace Project.SecretDetection.DetectionsTypes.EnvironmentFileDetections{
//     class EnvChecker
//     {
//         public List<EnvironmentFileDetection.EnvironmentVariable> getUnusedEnvVariables(Dictionary<string, string> environmentVariableMap, string filePath) //OPTIMIZE!!!
//         {
//             //For each stringargument 
//                 //is it present in the envfiles?
//                     //if so - extract the suffix in the file
            
//             List<EnvironmentFileDetection.EnvironmentVariable> unusedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();
//             bool used = false;

//             var EnvFiles = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories)
//                 .Where(f =>
//                     f.Contains(@".env")
//                 );
//             Console.WriteLine("Checking these files for unused env variables in .env files found in directory"); //Just to make life easier we print what we check 
//             foreach (var envfile in EnvFiles)
//             {
//                 Console.WriteLine("     " + envfile);
//             }

//             foreach (var envfile in EnvFiles)
//             {
//                 int i = 0; //create an index counter so we can track the location in the env file and can write this to the report
//                 // foreach (var line in File.ReadLines(envfile))
//                 foreach (var line in extractEntries(envfile))
//                 {
//                     i++; //update the index counter every time we move line :)
//                     used = false;
//                 //     string extractedStr = extractString(line.Value);
//                 //     var envVar = new EnvironmentFileDetection.EnvironmentVariable(1, envfile, line.Value, line.Key, 0, "", false);
//                 //     unusedEnvironmentVariables.Add(envVar);
//                 // }
                    
//                     foreach (var kvp in environmentVariableMap)
//                     {
//                         if (line.Key.Trim() == kvp.Key.Trim())
//                         {
//                             used = true;
//                         }
//                     }

//                     if (!used)
//                     {
//                         // unusedEnvironmentVariables.Add(line); //Hele linjen in envfilen
//                         // string extractedStr = extractString(line); //Kun selve valuen
//                         string parentPath = Directory.GetParent(filePath).FullName;
//                         string shortenedPath = Path.GetRelativePath(parentPath, envfile);
//                         int locationIndex = line.Value.lineNumber;//Lokationen i env filen
//                         float score = 0.0F; //Den skal initialiseres her, og opdateres i analysen af stringen
//                         string comment = ""; //Den skal initialiseres her, og opdateres i analyse delen   
//                         // string name = extractName(line); //smid navnet på hvad secreten er initialiseret som ind 
//                                                          // -- Anderledes end usedenvvars fordi det den er intialiseret som bare er i env filen og ikke i koden
//                         var envVar = new EnvironmentFileDetection.EnvironmentVariable(locationIndex, shortenedPath, line.Value.value.Trim(), line.Key, score, comment, false);
//                         unusedEnvironmentVariables.Add(envVar);
//                     }
//                 }
//             }
//             return unusedEnvironmentVariables;
//         }

//         public List<EnvironmentFileDetection.EnvironmentVariable> getUsedEnvVariables(Dictionary<string, string> environmentVariableMap, string filePath) //OPTIMIZE!!!
//         {
//             List<EnvironmentFileDetection.EnvironmentVariable> usedEnvironmentVariables = new List<EnvironmentFileDetection.EnvironmentVariable>();

//             var EnvFiles = Directory.EnumerateFiles(filePath, "*", SearchOption.AllDirectories)
//                 .Where(f =>
//                     f.Contains(@".env")
//                 );
//             Console.WriteLine("Checking these files for used env variables in .env files found in directory"); //Just to make life easier we print what we check 
//             foreach (var envfile in EnvFiles)
//             {
//                 Console.WriteLine("     " + envfile);
//             }

//             foreach (var envfile in EnvFiles)
//             {
//                 int i = 0; //create an index counter so we can track the location in the env file and can write this to the report
//                 // foreach (var line in File.ReadLines(envfile))
//                 foreach(var line in extractEntries(envfile))
//                 {
//                     i++; //update the index counter every time we move line :)
                    
//                     // for(int j = 0; j < StringArgs.Count; j++)
//                     foreach (var kvp in environmentVariableMap)
//                     {
//                         if (line.Key.Trim() == kvp.Key.Trim())
//                         {
//                             // usedEnvironmentVariables.Add(line); //Hele linjen in envfilen
//                             // string extractedStr = extractString(line); //Kun selve valuen
//                             string parentPath = Directory.GetParent(filePath).FullName;
//                             string shortenedPath = Path.GetRelativePath(parentPath, envfile);
//                             int locationIndex = line.Value.lineNumber; //Lokationen i env filen - burde måske være sin egen funtion men det her er så meget nemmere:)))
//                             float score = 0.0F; //Den skal initialiseres her, og opdateres i analysen af stringen
//                             string comment = ""; //Den skal initialiseres her, og opdateres i analyse delen
//                             string name = kvp.Value; //Hvad selve secreten er initializeret som i koden. Skal bruges til dataflow analysen
//                             var envVar = new EnvironmentFileDetection.EnvironmentVariable(locationIndex, shortenedPath, line.Value.value.Trim(), name, score, comment, true);
//                             usedEnvironmentVariables.Add(envVar);
//                         }
//                     }
//                 }
//             }
//             return usedEnvironmentVariables;
//         }
//         public Dictionary<string, (string value, int lineNumber)> extractEntries(string envfile)
//         {
//             var entries = new Dictionary<string, (string value, int lineNumber)>();
//             bool multiline = false;
//             string currentValue = "";
//             string currentKey = "";
//             int lineNumber = 0;
//             int entryStartLine = 0;

//             foreach (var line in File.ReadLines(envfile))
//             {
//                 lineNumber++;
//                 if (multiline)
//                 {
//                     currentValue += line;
//                     if (line.TrimEnd().EndsWith("\""))
//                     {
//                         multiline = false;
//                         entries[currentKey] = (currentValue, entryStartLine); // ← use start line
//                         currentKey = "";
//                         currentValue = "";
//                     }
//                     continue;
//                 }

//                 if (line.Contains("="))
//                 {
//                     var parts = line.Split('=', 2);
//                     currentKey = parts[0];
//                     currentValue = parts[1];
//                     entryStartLine = lineNumber; // ← record where this entry started

//                     if (currentValue.Trim().StartsWith("\"") && !currentValue.Trim().EndsWith("\""))
//                     {
//                         multiline = true;
//                     }
//                     else
//                     {
//                         entries[currentKey] = (currentValue, lineNumber);
//                         currentKey = "";
//                         currentValue = "";
//                     }
//                 }
//             }
//             return entries;
//         }


//         // public Dictionary<string,string> extractEntries(string envfile)
//         // {
//         //     Dictionary<string, string> entries = new Dictionary<string, string>();
//         //     bool multiline = false;
//         //     string currentValue="";
//         //     string currentKey="";

//         //     foreach(var line in File.ReadLines(envfile))
//         //     {
//         //        if (multiline)
//         //         {
//         //             currentValue += line;
//         //             if (line.TrimEnd().EndsWith("\""))
//         //             {
//         //                 multiline = false;
//         //                 entries[currentKey] = currentValue;
//         //                 currentKey = "";
//         //                 currentValue ="";
//         //                 continue;
//         //             }
//         //             continue;
//         //         }
//         //         if(!multiline){
//         //             if (line.Contains("="))
//         //             {

//         //                 var parts = line.Split('=', 2);
//         //                 currentKey = parts[0];
//         //                 currentValue = parts[1];

//         //                 if (currentValue.Trim().StartsWith("\"") && !currentValue.Trim().EndsWith("\""))
//         //                 {
//         //                     multiline = true;
//         //                 }
//         //                 else
//         //                 {
//         //                     entries[currentKey] = currentValue;
//         //                     currentKey = "";
//         //                     currentValue = "";
//         //                 }
//         //             }
//         //         }
//         //     }
//         //     return entries;
//         // }

        
//         public string extractString(string envVariables) // Giver dig din string uden = og uden mellemrummet. Hvis ikke "= " passer så får man bare alt.
//         {
//             try
//             {
//                 string extractedString = envVariables.Split('=')[1];
//                 if (extractedString.StartsWith(' '))
//                 {
//                     string newExtractedString = extractedString.Split(' ')[1];
//                     return newExtractedString;
//                 }
//                 else
//                 {
//                     return extractedString;
//                 }
//             }
//             catch
//             {
//                 return envVariables;
//             }
//         }
//         public string extractName(string envVariables) // Giver dig din string uden = og uden mellemrummet. Hvis ikke "= " passer så får man bare alt.
//         {
//             try
//             {
//                 string extractedName = envVariables.Split('=')[0];
//                 return extractedName;
//             }
//             catch
//             {
//                 return envVariables;
//             }
//         }
//     }
// }