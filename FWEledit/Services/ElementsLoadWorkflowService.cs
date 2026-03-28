using System;
using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class ElementsLoadWorkflowService
    {
        private const short SupportedVersion = 608;
        private readonly ElementsLoadService loadService;
        private readonly NavigationStateService navigationStateService;
        private readonly ElementsFileInfoService elementsFileInfoService;

        public ElementsLoadWorkflowService(
            ElementsLoadService loadService,
            NavigationStateService navigationStateService,
            ElementsFileInfoService elementsFileInfoService)
        {
            if (loadService == null)
            {
                throw new ArgumentNullException(nameof(loadService));
            }
            this.loadService = loadService;
            this.navigationStateService = navigationStateService;
            this.elementsFileInfoService = elementsFileInfoService;
        }

        public ElementsLoadResult LoadFromGameFolder(string gameFolderPath, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            ElementsLoadResult result = new ElementsLoadResult
            {
                Success = false,
                GameFolderPath = gameFolderPath ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(gameFolderPath) || !Directory.Exists(gameFolderPath))
            {
                result.ErrorMessage = "Invalid game folder.";
                return result;
            }

            string elementsFile = loadService.ResolveElementsPath(gameFolderPath);
            if (string.IsNullOrWhiteSpace(elementsFile) || !File.Exists(elementsFile))
            {
                result.ErrorMessage = "Could not find elements.data inside the selected game folder.";
                return result;
            }

            if (elementsFileInfoService != null)
            {
                ElementsFileInfo info = elementsFileInfoService.ReadFileInfo(elementsFile);
                if (info == null || !info.Success)
                {
                    result.ErrorMessage = "Could not read elements.data header.";
                    result.IsVersionUnsupported = true;
                    result.ElementsPath = elementsFile;
                    return result;
                }
                if (info != null && info.Success && info.Version != SupportedVersion)
                {
                    result.ErrorMessage =
                        "Unsupported elements.data version (" + info.Version + ").\n\n" +
                        "Only version " + SupportedVersion + " is compatible right now.";
                    result.IsVersionUnsupported = true;
                    result.ElementsPath = elementsFile;
                    return result;
                }
            }

            try
            {
                eListCollection listCollection = loadService.LoadCollection(elementsFile, ref progressBar);
                result.ListCollection = listCollection;
                result.ElementsPath = elementsFile;
                result.ExportRules = loadService.LoadExportRuleNames(listCollection);

                string[][] xrefs;
                bool hasXrefs = loadService.TryLoadXrefs(listCollection, out xrefs);
                result.Xrefs = xrefs;
                result.HasXrefs = hasXrefs;

                result.ConversationList = loadService.TryLoadConversationList(listCollection);
                result.NavigationSnapshot = navigationStateService != null
                    ? navigationStateService.LoadSnapshot()
                    : new NavigationSettingsSnapshot();

                result.Success = true;
                return result;
            }
            catch
            {
                result.ErrorMessage =
                    "LOADING ERROR!\n\nThis error usually occurs if incorrect configuration, structure, or encrypted elements.data file...\n" +
                    "If you are using elements.list.count trying to decrypt, its likely the last list item count is incorrect... \n" +
                    "Use details below to assist... \n\nRead Failed at this point :\n" +
                    eListCollection.SStat[0].ToString() + " - List #\n" +
                    eListCollection.SStat[1].ToString() + " - # Items This List\n" +
                    eListCollection.SStat[2].ToString() + " - Item ID";
                return result;
            }
        }
    }
}
