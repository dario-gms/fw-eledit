using System;

namespace FWEledit
{
    public sealed class MainWindowReplaceMappingUiService
    {
        public void ReplaceSkills(
            ReplaceMappingUiService replaceMappingUiService,
            eListCollection listCollection,
            string basePath,
            ReplaceMappingFileService fileService,
            ElementsReplaceService replaceService)
        {
            if (replaceMappingUiService == null)
            {
                return;
            }

            replaceMappingUiService.ReplaceSkills(
                listCollection,
                basePath,
                fileService,
                replaceService);
        }

        public void ReplaceProperties(
            ReplaceMappingUiService replaceMappingUiService,
            eListCollection listCollection,
            string basePath,
            ReplaceMappingFileService fileService,
            ElementsReplaceService replaceService)
        {
            if (replaceMappingUiService == null)
            {
                return;
            }

            replaceMappingUiService.ReplaceProperties(
                listCollection,
                basePath,
                fileService,
                replaceService);
        }

        public void ReplaceTomes(
            ReplaceMappingUiService replaceMappingUiService,
            eListCollection listCollection,
            string basePath,
            ReplaceMappingFileService fileService,
            ElementsReplaceService replaceService,
            Action<string> showMessage)
        {
            if (replaceMappingUiService == null)
            {
                return;
            }

            replaceMappingUiService.ReplaceTomes(
                listCollection,
                basePath,
                fileService,
                replaceService,
                showMessage);
        }
    }
}
