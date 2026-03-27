using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class InfoDialogUiService
    {
        public void ShowInfo(eListCollection listCollection, InfoMessageService infoMessageService, Action<string> showMessage)
        {
            if (infoMessageService == null)
            {
                return;
            }

            infoMessageService.ShowInfo(listCollection, showMessage);
        }

        public void ShowElementsInfo(
            ElementsInfoDialogService elementsInfoDialogService,
            ElementsFileInfoService elementsFileInfoService,
            DialogService dialogService,
            IWin32Window owner)
        {
            if (elementsInfoDialogService == null)
            {
                return;
            }

            elementsInfoDialogService.ShowElementsInfo(elementsFileInfoService, dialogService, owner);
        }
    }
}
