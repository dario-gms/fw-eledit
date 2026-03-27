using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsSessionService
    {
        public bool SaveCurrentSession(ElementsSaveContext context)
        {
            if (context == null)
            {
                return false;
            }

            bool progressStarted = false;
            bool completed = false;
            try
            {
                if (context.ListCollection == null)
                {
                    return true;
                }

                NavigationSnapshot navSnapshot = context.CaptureNavigationSnapshot != null ? context.CaptureNavigationSnapshot() : null;
                if (context.BeginProgress != null)
                {
                    context.BeginProgress();
                    progressStarted = true;
                }
                if (context.SetProgress != null)
                {
                    context.SetProgress(5);
                }

                string elementsPath = context.ElementsPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(elementsPath) || !File.Exists(elementsPath))
                {
                    if (context.PromptSavePath == null)
                    {
                        return false;
                    }
                    elementsPath = context.PromptSavePath();
                    if (string.IsNullOrWhiteSpace(elementsPath))
                    {
                        return false;
                    }
                    context.ElementsPath = elementsPath;
                }

                if (context.ValidateUniqueIds != null && !context.ValidateUniqueIds())
                {
                    return false;
                }
                if (context.FlushPendingDescriptions != null && !context.FlushPendingDescriptions())
                {
                    return false;
                }
                if (context.ConversationList != null
                    && context.ListCollection.ConversationListIndex > -1
                    && context.ListCollection.Lists.Length > context.ListCollection.ConversationListIndex)
                {
                    context.ListCollection.Lists[context.ListCollection.ConversationListIndex].elementValues[0][0] =
                        context.ConversationList.GetBytes();
                }

                if (context.SetProgress != null)
                {
                    context.SetProgress(25);
                }
                if (!SaveElementsFileSafely(context.ListCollection, context.ElementsPath, context.AssetManager, context.LogError))
                {
                    if (context.ErrorMessageHandler != null)
                    {
                        context.ErrorMessageHandler("SAVING ERROR!\nFailed to write elements.data safely.");
                    }
                    return false;
                }

                if (context.AssetManager != null)
                {
                    if (context.SetProgress != null)
                    {
                        context.SetProgress(70);
                    }
                    string syncSummary;
                    if (!context.AssetManager.ApplyWorkspaceChangesToGame(out syncSummary))
                    {
                        if (context.ShowInfoMessage != null)
                        {
                            context.ShowInfoMessage("Saved elements.data, but resource sync failed:\n" + syncSummary);
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(syncSummary)
                        && !syncSummary.StartsWith("No PCK", StringComparison.OrdinalIgnoreCase))
                    {
                        if (context.SyncSummaryHandler != null)
                        {
                            context.SyncSummaryHandler(syncSummary);
                        }
                    }
                }

                if (context.ClearDirtyTracking != null)
                {
                    context.ClearDirtyTracking();
                }
                if (context.SetProgress != null)
                {
                    context.SetProgress(100);
                }
                if (context.RestoreNavigationSnapshot != null)
                {
                    context.RestoreNavigationSnapshot(navSnapshot);
                }
                if (context.EndProgress != null)
                {
                    context.EndProgress();
                }
                completed = true;
                return true;
            }
            catch (Exception ex)
            {
                if (context.LogError != null)
                {
                    context.LogError("SaveCurrentSession", ex);
                }
                if (context.ErrorMessageHandler != null)
                {
                    context.ErrorMessageHandler("SAVING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
                }
                return false;
            }
            finally
            {
                if (!completed && progressStarted && context.EndProgress != null)
                {
                    context.EndProgress();
                }
            }
        }

        private bool SaveElementsFileSafely(eListCollection listCollection, string targetPath, AssetManager assetManager, Action<string, Exception> logError)
        {
            string tempPath = string.Empty;
            try
            {
                string dir = Path.GetDirectoryName(targetPath);
                if (string.IsNullOrWhiteSpace(dir))
                {
                    return false;
                }
                Directory.CreateDirectory(dir);

                tempPath = Path.Combine(dir, Path.GetFileName(targetPath) + ".tmp_fweledit");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                listCollection.Save(tempPath);
                if (!File.Exists(tempPath))
                {
                    return false;
                }

                if (File.Exists(targetPath))
                {
                    string backupRoot = assetManager != null && !string.IsNullOrWhiteSpace(AssetManager.GameRootPath)
                        ? AssetManager.GameRootPath
                        : dir;
                    string backupDir = Path.Combine(backupRoot, "backup_elements");
                    AssetManager.CreateTimestampedZipBackup(targetPath, backupDir, "elements");
                    File.Copy(targetPath, targetPath + ".bak", true);
                }

                File.Copy(tempPath, targetPath, true);
                File.Delete(tempPath);
                return true;
            }
            catch (Exception ex)
            {
                if (logError != null)
                {
                    logError("SaveElementsFileSafely(" + targetPath + ")", ex);
                }
                try
                {
                    if (!string.IsNullOrWhiteSpace(tempPath) && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                { }
                return false;
            }
        }
    }
}
