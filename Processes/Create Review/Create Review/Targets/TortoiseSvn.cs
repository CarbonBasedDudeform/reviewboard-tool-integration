﻿using System.Windows.Forms;
using System.IO;
using System;
using System.Text.RegularExpressions;

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
        public static void OpenCommitDialog(Utilities.Review.Properties properties, string reviewUrl)
        {
            string logFile = GenerateLogMessage(properties, reviewUrl);
            OpenTortoiseSVN(properties.Contents.Files, properties.Path, logFile);
        }

        // Private Properties
        private const string JiraState_Tag = "[Jira Issue(s): {0}]\n";
        private const string ReviewState_Tag = "[Review State:{0}]\n{1}\n";

        private const string JiraState_NoJira = "N/A";


        //
        // Creates a new log file and returns the path
        //
        private static string GenerateLogMessage(Utilities.Review.Properties properties, string reviewUrl)
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
                string reviewLevel = Regex.Replace(properties.ReviewLevel.ToString(), "[A-Z]", " $0");
                sw.WriteLine(ReviewState_Tag, reviewLevel, reviewUrl == null ? string.Empty : reviewUrl.Replace("diff/", ""));

                // Add a tag identifying what it was generated by
                sw.WriteLine("\n* Commit log generated by '{0}' v{1}", Application.ProductName, Application.ProductVersion);
            }

            // Return the log file
            return logFile;
        }

        //
        // Opens TortoiseSVN with the given commit options
        //
        private static void OpenTortoiseSVN(string[] files, string path, string logFile)
        {
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
                
                // Build up the IDs we need
                string[] jiras = jiraId.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                if (jiras == null || jiras.Length == 0)
                    jiras = new string[] { jiraId };

                // Add the links
                for (int i = 0; i < jiras.Length; ++i)
                {
                    string thisJiraLink = string.Format("{0}{1}\n", Settings.Jira.Default.Server, jiras[i]);
                    jiraTag += thisJiraLink;
                }
            }

            // Send the tag back
            return jiraTag;
        }
    }
}