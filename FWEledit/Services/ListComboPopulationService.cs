using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListComboPopulationService
    {
        public void PopulateLists(ComboBox comboBox, eListCollection listCollection, ListDisplayService listDisplayService)
        {
            if (comboBox == null)
            {
                return;
            }

            comboBox.Items.Clear();
            if (listCollection == null)
            {
                return;
            }

            for (int l = 0; l < listCollection.Lists.Length; l++)
            {
                string friendlyListName = listDisplayService != null
                    ? listDisplayService.GetFriendlyListName(listCollection.Lists[l].listName)
                    : listCollection.Lists[l].listName;
                comboBox.Items.Add("[" + l + "] " + friendlyListName + " (" + listCollection.Lists[l].elementValues.Length + ")");
            }
        }
    }
}
