using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;

namespace GitHubProjectUpdater
{
    internal class Program
    {
        static List<string> commandLineParams = new List<string>();
        
        //  Values we need for the update
        static string originalExecutable = string.Empty;
        static string originalName = string.Empty;
        static Version programVersion = null, githubVersion = null;
        static string githubRepo = string.Empty;
        static List<string> changes = new List<string>();

        static void Main(string[] args)
        {
            //  First off, lets get our command line paramenters
            commandLineParams = args.ToList();

            foreach(string cmd in commandLineParams)
            {
                string cmdCheck = cmd.ToLower();

                if (cmdCheck.Contains("executable")) originalExecutable = cmdCheck.Split(' ')[1];

                if (cmdCheck.Contains("myversion")) programVersion = new Version(cmdCheck.Split(' ')[1]);
                if (cmdCheck.Contains("repo")) githubRepo = cmdCheck.Split(' ')[1];
            }

            //  We have no executable, so we don't know what to start. Throw an error
            if (originalExecutable == string.Empty)
            {
                Console.WriteLine("Please start the Updater with the full path of the application you wish to check." + Environment.NewLine + Environment.NewLine + "Press any key to terminate...");
                Console.ReadKey();
            }
            else
            {
                //  Check if we received both versions
                if (programVersion == null || githubRepo == string.Empty)
                {
                    Console.WriteLine("Updater could not determine if a new update is available." + Environment.NewLine);
                    Console.Write(@"Do you want to continue anyways [y/n]? ");
                    
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Y) StartOriginalProgram();
                    else return;
                }
                else
                {
                    //  Get version from GitHub
                    originalName = originalExecutable.Substring(0, originalExecutable.LastIndexOf('.'));
                    originalName = originalName.Substring(originalName.LastIndexOf(@"\") + 1);

                    string webFile = @"https://raw.githubusercontent.com/docfo4r/" + originalName + @"/main/" + originalName + @"/version.txt";

                    WebClient webClient = new WebClient();
                    Stream stream = webClient.OpenRead(webFile);
                    StreamReader streamReader = new StreamReader(stream);
                    String content = streamReader.ReadToEnd();

                    string[] lines = content.Split(new char[] { '\r', '\n' });
                    bool versionFound = false;

                    foreach(string line in lines)
                    {
                        if (line.StartsWith("version="))
                        {
                            if (versionFound) break;
                            else
                            {
                                versionFound = true;
                                githubVersion = new Version(line.Split('=')[1]);
                                continue;
                            }
                        }

                        changes.Add(line);
                    }

                    //  Compare versions
                    if (githubVersion.CompareTo(programVersion) > 0)
                    {
                        //  We have an update!
                        Console.WriteLine("There is a new update of " + originalName + " available.");
                        Console.WriteLine();
                        Console.WriteLine("    Your version: " + programVersion.ToString());
                        Console.WriteLine("    New version:  " + githubVersion.ToString());
                        Console.WriteLine();

                        int changesCounter = 0;

                        foreach(string change in changes)
                        {
                            changesCounter++;

                            if (changesCounter > 20)
                            {
                                Console.WriteLine("    ... and more");
                                break;
                            }
                            Console.WriteLine("    " + change);
                        }

                        Console.WriteLine();
                        Console.Write(@"Would you like to update [y/n]? ");

                        ConsoleKeyInfo key = Console.ReadKey();

                        if (key.Key == ConsoleKey.Y) UpdateOriginalProgram(originalName, originalExecutable.Substring(0, originalExecutable.LastIndexOf(@"\")));
                        else StartOriginalProgram();
                    }
                    else
                    {
                        //  No update available, start the program
                        StartOriginalProgram();
                    }

                    Console.ReadKey();
                }
            }
        }

        static void StartOriginalProgram()
        {
            try
            {
                Process.Start(originalExecutable);
                return;
            }
            catch (Exception err)
            {
                Console.WriteLine("Error while starting the following program:" + Environment.NewLine + originalExecutable + Environment.NewLine + Environment.NewLine + err.Message);
                Console.ReadKey();
                return;
            }
        }

        static void UpdateOriginalProgram(string programName, string destinationDir)
        {
            try
            {
                Console.WriteLine("Updating in progress, please wait!");

                //  At first, download the latest build as ZIP
                Console.WriteLine(@"[1/4] Downloading ZIP...");
                string zipFile = @"%temp%\" + programName + ".zip";
                WebClient webClient = new WebClient();
                webClient.DownloadFile(@"https://github.com/docfo4r/" + programName + @"/archive/refs/heads/main.zip", zipFile);

                //  Now unpack the ZIP to users TEMP
                Console.WriteLine(@"[2/4] Unpacking ZIP...");
                string tempDir = @"%temp%\" + programName;
                ZipFile.ExtractToDirectory(zipFile, tempDir);

                //  Replace all files
                Console.WriteLine(@"[3/4] Replacing program files...");
                Directory.Move(tempDir, destinationDir);

                //  Clean up the TEMP directory
                Console.WriteLine(@"[4/4] Cleaning up...");
                Directory.Delete(tempDir, true);
                File.Delete(zipFile);

                //  Start the program
                Console.WriteLine("Update completed, press any key to launch " + programName);
                Console.ReadKey();
                StartOriginalProgram();
            }
            catch (Exception err)
            {
                Console.WriteLine("Something went wrong during the update. Please reinstall " + programName + " by yourself!");
                Console.WriteLine(err.Message);
                Console.ReadKey();
                return;
            }
        }
    }
}