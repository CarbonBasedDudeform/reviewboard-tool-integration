﻿using System.Windows.Forms;
using System.IO;
using System;
using System.Text.RegularExpressions;
using RB_Tools.Shared.Server;
using RB_Tools.Shared.Extensions;
using RB_Tools.Shared.Logging;

namespace Create_Review
{
    //
    // Sends the review request properties to Tortoise SVN
    //
    class TortoiseSvn
    {
        //
        // Opens a new commit dialog with the given options
        //
        public static void OpenCommitDialog(Review.Review.Properties properties, string reviewUrl, Logging logger)
        {
            string logFile = GenerateLogMessage(properties, reviewUrl, logger);
            OpenTortoiseSVN(properties.Contents.Files, properties.Path, logFile, logger);

            // Clean up
            CleanUpTemporaryFiles(logFile, logger);
        }

        // Private Properties
        private const string JiraState_Tag = "[Jira Issue(s): {0}]\n";
        private const string ReviewState_Tag = "[Review State: {0}]\n{1}\n";

        private const string JiraState_NoJira = "N/A";


        //
        // Creates a new log file and returns the path
        //
        private static string GenerateLogMessage(Review.Review.Properties properties, string reviewUrl, Logging logger)
        {
            // Get the temp file
            string logFile = Path.GetTempFileName();
            using(StreamWriter sw = new StreamWriter(logFile))
            {
                // Write our log to the file
                if (string.IsNullOrWhiteSpace(properties.Summary) == false)
                    sw.WriteLine(properties.Summary + "\n");

                if (string.IsNullOrWhiteSpace(properties.Description) == false)
                    sw.WriteLine(properties.Description + "\n");

                // Post the Jira state to this commit
                string jiraState = BuildJiraLogContent(properties.JiraId);
                sw.WriteLine(jiraState);

                // Post the review state to this commit
                sw.WriteLine(ReviewState_Tag, properties.ReviewLevel.GetSplitName(), reviewUrl == null ? string.Empty : reviewUrl.Replace("diff/", ""));

                // Add a tag identifying what it was generated by
                sw.WriteLine("\n* Commit log generated by '{0}' v{1}", "Reviewboard Integration Tools", RB_Tools.Shared.Utilities.Version.VersionNumber);
            }

            // Return the log file
            logger.Log("Log file generated - {0}", logFile);
            return logFile;
        }

        //
        // Opens TortoiseSVN with the given commit options
        //
        private static void OpenTortoiseSVN(string[] files, string path, string logFile, Logging logger)
        {
            logger.Log("Requesting TortoiseSVN");

            // Do we have a specific list of files or a path?
            string tsvnPath = path;
            if (files != null && files.Length > 0)
            {
                // Create the list of files we need to add to the /path option
                // https://tortoisesvn.net/docs/nightly/TortoiseSVN_en/tsvn-automation.html
                tsvnPath = files[0];
                for (int i = 1; i < files.Length; ++i)
                    tsvnPath += "*" + files[i];
            }

            // Build up Tortoise SVN 
            System.Diagnostics.Process ExternalProcess = new System.Diagnostics.Process();
            ExternalProcess.StartInfo.FileName = "tortoiseproc";
            ExternalProcess.StartInfo.Arguments = string.Format(@"/command:commit /logmsgfile:""{0}"" /path:""{1}""", logFile, tsvnPath);

            // Update process
            logger.Log(" * Calling {0} {1}", ExternalProcess.StartInfo.FileName, ExternalProcess.StartInfo.Arguments);

            // Start the process
            ExternalProcess.Start();
            ExternalProcess.WaitForExit();
        }

        //
        // Builds up the Jira log entry
        //
        private static string BuildJiraLogContent(string jiraId)
        {
            // Default state
            string jiraTag = string.Format(JiraState_Tag, JiraState_NoJira);

            // Build up the tag up if we need to
            if (string.IsNullOrWhiteSpace(jiraId) == false)
            {
                // Get the header
                jiraTag = string.Format(JiraState_Tag, jiraId);
                
                // Build up the IDs we needs
                string[] jiras = jiraId.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (jiras == null || jiras.Length == 0)
                    jiras = new string[] { jiraId };

                // Add the links
                string jiraServer = Names.Url[(int)Names.Type.Jira];
                for (int i = 0; i < jiras.Length; ++i)
                {
                    string thisJiraLink = string.Format(@"{0}\browse\{1}\n", jiraServer, jiras[i]);
                    jiraTag += thisJiraLink;
                }
            }

            // Send the tag back
            return jiraTag;
        }

        //
        // Deletes the temporary files we created for the review
        //
        private static void CleanUpTemporaryFiles(string logFile, Logging logger)
        {
            Utilities.Storage.Keep(logFile, "SVN Log File.txt", true, logger);
        }
    }
}
