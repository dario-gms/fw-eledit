using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace FWEledit
{
    public sealed class ModelPreviewService
    {
        public bool TryOpenPreview(
            AssetManager assetManager,
            CacheSave database,
            int pathId,
            string fieldName,
            ModelPickerService modelPickerService,
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (assetManager == null || modelPickerService == null)
            {
                errorMessage = "Model preview unavailable.";
                return false;
            }
            if (pathId <= 0)
            {
                errorMessage = "Invalid model PathID.";
                return false;
            }

            int resolvedPathId;
            string mappedPath;
            if (!modelPickerService.TryResolveModelPathById(database, pathId, fieldName, out resolvedPathId, out mappedPath, true))
            {
                errorMessage = "Model PathID not found in path.data:\n" + pathId;
                return false;
            }

            string fullPath = assetManager.ResolveResourcePath(mappedPath);
            if (string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath))
            {
                errorMessage = "Model file not found.\nPathID: " + pathId + "\nMapped: " + mappedPath;
                return false;
            }

            try
            {
                string viewerExe = FindExternalModelViewerExecutable(AssetManager.GameRootPath);
                if (!string.IsNullOrWhiteSpace(viewerExe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = viewerExe,
                        Arguments = "\"" + fullPath + "\"",
                        UseShellExecute = true
                    });
                    return true;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "MODEL PREVIEW ERROR!\n" + ex.Message;
                return false;
            }
        }

        private static string FindExternalModelViewerExecutable(string gameRootPath)
        {
            List<string> candidates = new List<string>();
            try
            {
                if (!string.IsNullOrWhiteSpace(gameRootPath))
                {
                    candidates.Add(Path.Combine(gameRootPath, "fELedit", "Elements Editor Pago", "SKIPreview_RAE.exe"));
                    candidates.Add(Path.Combine(gameRootPath, "fELedit", "Elements Editor Pago", "Angelica Editor.exe"));
                    candidates.Add(Path.Combine(gameRootPath, "fELedit", "Elements Editor Pago", "rae_api.exe"));
                }
            }
            catch
            {
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return string.Empty;
        }
    }
}
