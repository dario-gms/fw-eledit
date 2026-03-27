using System;
using System.Collections;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class ElementsJoinWorkflowService
    {
        public ElementsJoinResult Join(eListCollection target, ElementsJoinOptions options, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            ElementsJoinResult result = new ElementsJoinResult { Success = false };
            if (target == null)
            {
                result.ErrorMessage = "No File Loaded!";
                return result;
            }
            if (options == null || string.IsNullOrWhiteSpace(options.SourceFilePath))
            {
                result.ErrorMessage = "Invalid join file.";
                return result;
            }
            if (!File.Exists(options.SourceFilePath))
            {
                result.ErrorMessage = "Join file not found.";
                return result;
            }

            string logDirectory = options.LogDirectory;
            if (string.IsNullOrWhiteSpace(logDirectory) || !Directory.Exists(logDirectory))
            {
                logDirectory = options.SourceFilePath + ".JOIN";
                Directory.CreateDirectory(logDirectory);
            }

            if (options.BackupNew)
            {
                Directory.CreateDirectory(Path.Combine(logDirectory, "added.backup"));
            }
            if (options.BackupChanged)
            {
                Directory.CreateDirectory(Path.Combine(logDirectory, "replaced.backup"));
            }
            if (options.BackupMissing)
            {
                Directory.CreateDirectory(Path.Combine(logDirectory, "removed.backup"));
            }

            try
            {
                eListCollection newCollection = new eListCollection(options.SourceFilePath, ref progressBar);
                if (!string.Equals(target.ConfigFile, newCollection.ConfigFile, StringComparison.OrdinalIgnoreCase))
                {
                    result.WarningMessage = "You're going to join two different element.data versions. The merged file will become invalid!";
                }

                if (target.ConversationListIndex > -1 && newCollection.Lists.Length > target.ConversationListIndex)
                {
                    result.ShouldUpdateConversationList = true;
                    try
                    {
                        result.ConversationList = new eListConversation((byte[])newCollection.Lists[target.ConversationListIndex].elementValues[0][0]);
                    }
                    catch
                    {
                        result.ConversationList = null;
                    }
                }

                using (StreamWriter sw = new StreamWriter(Path.Combine(logDirectory, "LOG.TXT"), false, Encoding.Unicode))
                {
                    for (int l = 0; l < target.Lists.Length; l++)
                    {
                        if (l == target.ConversationListIndex)
                        {
                            continue;
                        }

                        ArrayList report = target.Lists[l].JoinElements(
                            newCollection.Lists[l],
                            l,
                            options.AddNew,
                            options.BackupNew,
                            options.ReplaceChanged,
                            options.BackupChanged,
                            options.RemoveMissing,
                            options.BackupMissing,
                            Path.Combine(logDirectory, "added.backup"),
                            Path.Combine(logDirectory, "replaced.backup"),
                            Path.Combine(logDirectory, "removed.backup"));

                        report.Sort();
                        if (report.Count > 0)
                        {
                            sw.WriteLine("List " + l + ": " + report.Count + " Item(s) Affected");
                            sw.WriteLine();
                            for (int n = 0; n < report.Count; n++)
                            {
                                sw.WriteLine((string)report[n]);
                            }
                            sw.WriteLine();
                        }
                    }
                }

                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage = "LOADING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch";
                return result;
            }
        }
    }
}
