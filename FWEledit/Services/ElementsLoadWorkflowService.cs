using System;
using System.Collections.Generic;
using System.IO;

namespace FWEledit
{
    public sealed class ElementsLoadWorkflowService
    {
        private readonly ElementsLoadService loadService;
        private readonly NavigationStateService navigationStateService;

        public ElementsLoadWorkflowService(ElementsLoadService loadService, NavigationStateService navigationStateService)
        {
            if (loadService == null)
            {
                throw new ArgumentNullException(nameof(loadService));
            }
            this.loadService = loadService;
            this.navigationStateService = navigationStateService;
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
