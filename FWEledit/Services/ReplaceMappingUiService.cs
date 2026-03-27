using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ReplaceMappingUiService
    {
        public void ReplaceSkills(
            eListCollection listCollection,
            string initialDirectory,
            ReplaceMappingFileService mappingFileService,
            ElementsReplaceService replaceService)
        {
            ReplaceSimpleMapping(
                listCollection,
                initialDirectory,
                "Skill Replace File (*.txt)|*.txt|All Files (*.*)|*.*",
                mappingFileService,
                replaceService != null ? new Action<Dictionary<string, string>>(map => replaceService.ReplaceSkills(listCollection, map)) : null);
        }

        public void ReplaceProperties(
            eListCollection listCollection,
            string initialDirectory,
            ReplaceMappingFileService mappingFileService,
            ElementsReplaceService replaceService)
        {
            ReplaceSimpleMapping(
                listCollection,
                initialDirectory,
                "Property Replace File (*.txt)|*.txt|All Files (*.*)|*.*",
                mappingFileService,
                replaceService != null ? new Action<Dictionary<string, string>>(map => replaceService.ReplaceProperties(listCollection, map)) : null);
        }

        public void ReplaceTomes(
            eListCollection listCollection,
            string initialDirectory,
            ReplaceMappingFileService mappingFileService,
            ElementsReplaceService replaceService,
            Action<string> showMessage)
        {
            if (listCollection == null || mappingFileService == null || replaceService == null)
            {
                return;
            }

            string filePath = PromptForMappingFile(
                initialDirectory,
                "Tome Replace File (*.txt)|*.txt|All Files (*.*)|*.*");
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            Dictionary<string, string[]> mappings = mappingFileService.LoadTomeMapping(filePath);
            if (mappings.Count == 0)
            {
                return;
            }

            ReplacementSummary summary = replaceService.ReplaceTomeProperties(listCollection, mappings);
            for (int i = 0; i < summary.Warnings.Count; i++)
            {
                if (showMessage != null)
                {
                    showMessage(summary.Warnings[i]);
                }
            }
        }

        private static void ReplaceSimpleMapping(
            eListCollection listCollection,
            string initialDirectory,
            string filter,
            ReplaceMappingFileService mappingFileService,
            Action<Dictionary<string, string>> applyMapping)
        {
            if (listCollection == null || mappingFileService == null || applyMapping == null)
            {
                return;
            }

            string filePath = PromptForMappingFile(initialDirectory, filter);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            Dictionary<string, string> mappings = mappingFileService.LoadSimpleMapping(filePath);
            if (mappings.Count == 0)
            {
                return;
            }

            applyMapping(mappings);
        }

        private static string PromptForMappingFile(string initialDirectory, string filter)
        {
            using (OpenFileDialog load = new OpenFileDialog())
            {
                load.InitialDirectory = initialDirectory ?? string.Empty;
                load.Filter = filter ?? "All Files (*.*)|*.*";
                return load.ShowDialog() == DialogResult.OK ? load.FileName : string.Empty;
            }
        }
    }
}
