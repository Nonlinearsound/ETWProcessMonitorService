using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace etwcli
{
    class ETWSettings
    {
        public string EMailAddress { get; set; }
        public string EMailPassword { get; set;}    
        public string EMailServer { get; set; }
        public string APIUrl { get; set; }  
        public string APIKey { get; set; }

    }

    internal class Program
    {
        static void Main(string[] args)
        {
            byte[] entropy = { 1, 2, 3, 4, 5, 6 }; //the entropy

            string Encrypt(string text,byte[] _entropy)
            {
                // first, convert the text to byte array 
                byte[] originalText = Encoding.Unicode.GetBytes(text);

                // then use Protect() to encrypt your data 
                byte[] encryptedText = ProtectedData.Protect(originalText, _entropy, DataProtectionScope.CurrentUser);

                //and return the encrypted message 
                return Convert.ToBase64String(encryptedText);
            }

            string Decrypt(string text, byte[] _entropy)
            {
                // the encrypted text, converted to byte array 
                byte[] encryptedText = Convert.FromBase64String(text);

                // calling Unprotect() that returns the original text 
                byte[] originalText = ProtectedData.Unprotect(encryptedText, _entropy, DataProtectionScope.CurrentUser);

                // finally, returning the result 
                return Encoding.Unicode.GetString(originalText);
            }

            void WriteSettings(ETWSettings _settings, string _configfilepath)
            {
                string output = JsonConvert.SerializeObject(_settings);
                string encryptedOutput = Encrypt(output, entropy);
                try
                {
                    Console.WriteLine(" Info: Config file could not be found. Creating the config file at {0}", _configfilepath);
                    File.WriteAllText(_configfilepath, encryptedOutput);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: Could not create the config file at {0}", _configfilepath);
                    // No need to continue the application if the config file cannot be written
                    System.Environment.Exit(-1);
                }
            }

            // 1) Find config file or create if not found in user path
            string appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configfilepath = Path.Combine(appdatapath, "etw_config.cfg");
            ETWSettings settings = new ETWSettings();

            if (File.Exists(configfilepath))
            {
                // read the file
                string filecontent = File.ReadAllText(configfilepath);
                if(filecontent != null)
                {
                    string decryptedString = Decrypt(filecontent, entropy);
                    if(decryptedString != null)
                    {
                        settings = JsonConvert.DeserializeObject<ETWSettings>(decryptedString);
                        if(settings != null)
                        {
                            // the settings object is now present and can be used to change its data based on application arguments
                            Console.WriteLine("Success: Config file content successfully read from {0}", configfilepath);
                        }
                        else
                        {
                            Console.WriteLine("Error: Could not read data from the decrypted config file content!");
                            System.Environment.Exit(-1);
                        }

                    }
                    else
                    {
                        Console.WriteLine("Error: Could not decrypt the config file content!");
                        System.Environment.Exit(-1);
                    }
                }
                else
                {
                    Console.WriteLine("Error: Could not read the config file at {0}", configfilepath);
                    System.Environment.Exit(-1);
                }
            }
            else
            {
                // the config file could not be found
                // create the new file using a new ETWSettings object
                WriteSettings(settings, configfilepath);

                // the config file should now be present in the file system
                // the settings empty object is present 
            }


            // cli setemailpw
            // cli setemailadr
            // cli setapikey
            // cli setapiurl

            if(args.Length > 0)
            {
                if (args[0] == null)
                {
                    Console.WriteLine("Usage: cli [setmailpw|setemailadr|setapikey|setapiurl] VALUE\nIf VALUE is empty, the command will prompt you for the input.\n");
                }
                else
                {
                    bool settingsUpdated = false;
                    switch (args[0])
                    {
                        case "setemailpw":
                            if(args.Length > 1)
                            {
                                settings.EMailPassword = args[1];
                                settingsUpdated = true;
                            }
                            else
                            {
                                Console.Write("Enter E-Mail Password:");
                                settings.EMailPassword = Console.ReadLine();
                                settingsUpdated = true;
                            }
                            break;

                        case "setemailadr":
                            if (args.Length > 1)
                            {
                                settings.EMailAddress = args[1];
                                settingsUpdated = true;
                            }
                            else
                            {
                                Console.Write("Enter E-Mail User:");
                                settings.EMailAddress = Console.ReadLine();
                                settingsUpdated = true;
                            }
                            break;

                        case "setemailserver":
                            if (args.Length > 1)
                            {
                                settings.EMailServer = args[1];
                                settingsUpdated = true;
                            }
                            else
                            {
                                Console.Write("Enter E-Mail Server:");
                                settings.EMailServer = Console.ReadLine();
                                settingsUpdated = true;
                            }
                            break;

                        case "setapikey":
                            if (args.Length > 1)
                            {
                                settings.APIKey = args[1];
                                settingsUpdated = true;
                            }
                            else
                            {
                                Console.Write("Enter Slack API-Key:");
                                settings.APIKey = Console.ReadLine();
                                settingsUpdated = true;
                            }
                            break;

                        case "setapiurl":
                            if (args.Length > 1)
                            {
                                settings.APIUrl = args[1];
                                settingsUpdated = true;
                            }
                            else
                            {
                                Console.Write("Enter Slack API-URL:");
                                settings.APIUrl = Console.ReadLine();
                                settingsUpdated = true;
                            }
                            break;
                        default:               
                            break;
                    }

                    if (settingsUpdated)
                    {
                        WriteSettings(settings,configfilepath);
                    }
                }
            }
            else
            {
                // print out the content of the config object
                // this is allowed as security has to be provided by the user
                // It is recommended to use this cli command only in an administrator environment
                // Only allow execution of the command by an Administrator account as you are able to set and read email and API credentials
                // that could potentially by used by an adversary.

                Console.WriteLine("Content of config file:");
                Console.WriteLine("EMail Server Address: {0}", settings.EMailServer);
                Console.WriteLine("EMail Address       : {0}", settings.EMailAddress);
                Console.WriteLine("EMail Password      : {0}", settings.EMailPassword);
                Console.WriteLine("Slack Web-API Url   : {0}", settings.APIUrl);
                Console.WriteLine("Slack Web-API AppKey: {0}", settings.APIKey);
                Console.ReadLine();
            }
        }
    }
}
