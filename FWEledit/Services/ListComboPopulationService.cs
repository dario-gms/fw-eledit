using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListComboPopulationService
    {
        public sealed class ListComboItem
        {
            public int ListIndex { get; set; }
            public string Text { get; set; }
            public Bitmap Icon { get; set; }
            public bool IconResolved { get; set; }

            public override string ToString()
            {
                return Text ?? string.Empty;
            }
        }

        public void PopulateLists(
            ComboBox comboBox,
            eListCollection listCollection,
            CacheSave database,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService)
        {
            PopulateLists(comboBox, listCollection, database, listDisplayService, listRowBuilderService, true);
        }

        public void PopulateLists(
            ComboBox comboBox,
            eListCollection listCollection,
            CacheSave database,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            bool includeIcons)
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
                comboBox.Items.Add(BuildListComboItem(listCollection, database, listDisplayService, listRowBuilderService, l, includeIcons));
            }
        }

        public ListComboItem BuildListComboItem(
            eListCollection listCollection,
            CacheSave database,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            int listIndex)
        {
            return BuildListComboItem(listCollection, database, listDisplayService, listRowBuilderService, listIndex, true);
        }

        public ListComboItem BuildListComboItem(
            eListCollection listCollection,
            CacheSave database,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            int listIndex,
            bool includeIcon)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null)
            {
                return new ListComboItem
                {
                    ListIndex = listIndex,
                    Text = "[" + listIndex + "] Unknown (0)",
                    Icon = Properties.Resources.NoIcon,
                    IconResolved = true
                };
            }

            string friendlyListName = listDisplayService != null
                ? listDisplayService.GetFriendlyListName(listCollection.Lists[listIndex].listName)
                : listCollection.Lists[listIndex].listName;

            int count = listCollection.Lists[listIndex].elementValues != null
                ? listCollection.Lists[listIndex].elementValues.Length
                : 0;

            Bitmap icon = Properties.Resources.NoIcon;
            if (includeIcon && count > 0 && listRowBuilderService != null)
            {
                icon = listRowBuilderService.BuildRowIcon(listCollection, database, listIndex, 0) ?? Properties.Resources.NoIcon;
            }

            return new ListComboItem
            {
                ListIndex = listIndex,
                Text = "[" + listIndex + "] " + friendlyListName + " (" + count + ")",
                Icon = icon,
                IconResolved = includeIcon || count == 0
            };
        }

        public static void SetItemTextPreservingIcon(ComboBox comboBox, int listIndex, string text)
        {
            if (comboBox == null || listIndex < 0 || listIndex >= comboBox.Items.Count)
            {
                return;
            }

            ListComboItem existing = comboBox.Items[listIndex] as ListComboItem;
            if (existing == null)
            {
                comboBox.Items[listIndex] = text ?? string.Empty;
                return;
            }

            existing.Text = text ?? string.Empty;
            comboBox.Items[listIndex] = existing;
        }

        public void EnsureListIcon(
            ComboBox comboBox,
            eListCollection listCollection,
            CacheSave database,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            int listIndex)
        {
            if (comboBox == null
                || listCollection == null
                || listIndex < 0
                || listIndex >= comboBox.Items.Count)
            {
                return;
            }

            ListComboItem existing = comboBox.Items[listIndex] as ListComboItem;
            if (existing != null && existing.IconResolved)
            {
                return;
            }

            comboBox.Items[listIndex] = BuildListComboItem(
                listCollection,
                database,
                listDisplayService,
                listRowBuilderService,
                listIndex,
                true);
        }
    }
}
