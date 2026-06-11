using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using FWEledit.Controls;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private Panel fwNpcSellPagePanel;
        private ThemedTabControl fwNpcSellPageTabs;
        private ThemedActionButton fwNpcSellPriceEditorButton;
        private bool suppressNpcSellPageTabChange;

        private void InitializeNpcSellServicePageUi()
        {
            if (fwValuesTab == null || fwNpcSellPagePanel != null)
            {
                return;
            }

            fwNpcSellPagePanel = new Panel();
            fwNpcSellPagePanel.Dock = DockStyle.Top;
            fwNpcSellPagePanel.Height = 36;
            fwNpcSellPagePanel.Padding = new Padding(0, 0, 0, 6);
            fwNpcSellPagePanel.Visible = false;

            fwNpcSellPageTabs = new ThemedTabControl();
            fwNpcSellPageTabs.Dock = DockStyle.Fill;
            fwNpcSellPageTabs.Margin = new Padding(0);
            fwNpcSellPageTabs.ItemSize = new Size(112, 24);
            fwNpcSellPageTabs.SizeMode = TabSizeMode.Fixed;
            fwNpcSellPageTabs.SelectedIndexChanged += fwNpcSellPageTabs_SelectedIndexChanged;

            for (int pageIndex = 0; pageIndex < NpcSellServiceCatalog.PageCount; pageIndex++)
            {
                TabPage page = new TabPage("Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture));
                page.Tag = pageIndex;
                fwNpcSellPageTabs.TabPages.Add(page);
            }

            fwNpcSellPriceEditorButton = new ThemedActionButton();
            fwNpcSellPriceEditorButton.Dock = DockStyle.Fill;
            fwNpcSellPriceEditorButton.Margin = new Padding(0);
            fwNpcSellPriceEditorButton.MinimumSize = new Size(118, 0);
            fwNpcSellPriceEditorButton.Text = "Shop Editor";
            fwNpcSellPriceEditorButton.Click += fwNpcSellPriceEditorButton_Click;
            ToolTip priceEditorTip = new ToolTip();
            priceEditorTip.SetToolTip(fwNpcSellPriceEditorButton, "Open the editor for shop item prices on all pages.");

            fwNpcSellPagePanel.Controls.Add(fwNpcSellPageTabs);
            fwValuesTab.Controls.Add(fwNpcSellPagePanel);
            fwValuesTab.Controls.SetChildIndex(fwNpcSellPagePanel, 0);

            if (fwNpcSellEditorHostPanel != null)
            {
                fwNpcSellEditorHostPanel.Controls.Clear();
                fwNpcSellEditorHostPanel.Controls.Add(fwNpcSellPriceEditorButton);
                fwNpcSellPriceEditorButton.BringToFront();
            }
        }

        private void fwNpcSellPageTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressNpcSellPageTabChange || !IsNpcSellServiceListSelected() || viewModel == null || !viewModel.EnableSelectionItem)
            {
                return;
            }

            change_item(null, null);
        }

        private void fwNpcSellPriceEditorButton_Click(object sender, EventArgs e)
        {
            OpenNpcSellPriceEditorForCurrentSelection();
        }

        private bool IsNpcSellServiceListSelected()
        {
            return comboBox_lists != null
                && sessionService != null
                && sessionService.ListCollection != null
                && NpcSellServiceCatalog.IsNpcSellServiceList(sessionService.ListCollection, comboBox_lists.SelectedIndex);
        }

        private int GetSelectedNpcSellPageIndex()
        {
            if (fwNpcSellPageTabs == null || fwNpcSellPageTabs.SelectedIndex < 0)
            {
                return 0;
            }

            return fwNpcSellPageTabs.SelectedIndex;
        }

        private bool ShouldIncludeNpcSellFieldInValuesTab(int listIndex, int fieldIndex, string fieldName)
        {
            if (sessionService == null || sessionService.ListCollection == null)
            {
                return false;
            }

            if (!NpcSellServiceCatalog.IsNpcSellServiceList(sessionService.ListCollection, listIndex))
            {
                return false;
            }

            if (string.Equals(fieldName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_dialog", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int selectedPageIndex = GetSelectedNpcSellPageIndex();
            return NpcSellServiceCatalog.TryGetFieldLocation(
                sessionService.ListCollection,
                listIndex,
                fieldIndex,
                fieldName,
                out int pageIndex,
                out _,
                out _)
                && pageIndex == selectedPageIndex;
        }

        private void UpdateNpcSellServiceUiForSelection()
        {
            if (fwNpcSellPagePanel == null)
            {
                return;
            }

            bool visible = IsNpcSellServiceListSelected();
            fwNpcSellPagePanel.Visible = visible;
            if (fwNpcSellEditorHostPanel != null)
            {
                fwNpcSellEditorHostPanel.Visible = visible;
            }
            if (!visible)
            {
                return;
            }

            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int elementIndex = ResolveCurrentElementIndex();
            bool hasValidElement = HasValidNpcSellElement(listIndex, elementIndex);
            suppressNpcSellPageTabChange = true;
            try
            {
                for (int pageIndex = 0; pageIndex < fwNpcSellPageTabs.TabPages.Count; pageIndex++)
                {
                    fwNpcSellPageTabs.TabPages[pageIndex].Text = hasValidElement
                        ? BuildNpcSellPageTitle(listIndex, elementIndex, pageIndex)
                        : "Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture);
                }

                if (fwNpcSellPageTabs.SelectedIndex < 0 || fwNpcSellPageTabs.SelectedIndex >= NpcSellServiceCatalog.PageCount)
                {
                    fwNpcSellPageTabs.SelectedIndex = 0;
                }
            }
            finally
            {
                suppressNpcSellPageTabChange = false;
            }

            fwNpcSellPriceEditorButton.Enabled = hasValidElement;
            if (fwNpcSellPriceEditorButton != null)
            {
                fwNpcSellPriceEditorButton.ApplyTheme(fwDarkMode);
            }
        }

        private string BuildNpcSellPageTitle(int listIndex, int elementIndex, int pageIndex)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || !HasValidNpcSellElement(listIndex, elementIndex)
                || !NpcSellServiceCatalog.IsNpcSellServiceList(sessionService.ListCollection, listIndex))
            {
                return "Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture);
            }

            int titleFieldIndex = NpcSellServiceCatalog.GetPageTitleFieldIndex(sessionService.ListCollection, listIndex, pageIndex);
            string title = titleFieldIndex >= 0
                ? sessionService.ListCollection.GetValue(listIndex, elementIndex, titleFieldIndex)
                : string.Empty;
            title = string.IsNullOrWhiteSpace(title)
                ? "Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture)
                : title.Trim();
            return title;
        }

        private void OpenNpcSellPriceEditorForCurrentSelection()
        {
            if (!IsNpcSellServiceListSelected() || sessionService == null || sessionService.ListCollection == null)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int elementIndex = ResolveCurrentElementIndex();
            if (!HasValidNpcSellElement(listIndex, elementIndex))
            {
                return;
            }

            List<NpcSellPageEditorPageModel> pages = BuildNpcSellEditorPages(listIndex, elementIndex);
            using (NpcSellServicePriceEditorWindow editor = new NpcSellServicePriceEditorWindow(
                GetSelectedElementName(),
                pages,
                GetSelectedNpcSellPageIndex(),
                sessionService.Database,
                itemReferenceService.BuildSearchableItemOptions(sessionService.ListCollection, sessionService.Database, iconResolutionService)))
            {
                if (editor.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                ApplyNpcSellPriceEditorChanges(editor.Pages);
            }
        }

        private List<NpcSellPageEditorPageModel> BuildNpcSellEditorPages(int listIndex, int elementIndex)
        {
            List<NpcSellPageEditorPageModel> pages = new List<NpcSellPageEditorPageModel>();
            if (!HasValidNpcSellElement(listIndex, elementIndex))
            {
                return pages;
            }

            for (int pageIndex = 0; pageIndex < NpcSellServiceCatalog.PageCount; pageIndex++)
            {
                int moneyTypeFieldIndex = NpcSellServiceCatalog.GetMoneyTypeFieldIndex(sessionService.ListCollection, listIndex, pageIndex);
                int moneyType = 0;
                if (moneyTypeFieldIndex >= 0)
                {
                    int.TryParse(sessionService.ListCollection.GetValue(listIndex, elementIndex, moneyTypeFieldIndex), out moneyType);
                }

                NpcSellPageEditorPageModel page = new NpcSellPageEditorPageModel
                {
                    PageIndex = pageIndex,
                    Title = BuildNpcSellPageTitle(listIndex, elementIndex, pageIndex),
                    MoneyType = moneyType
                };

                for (int slotIndex = 0; slotIndex < NpcSellServiceCatalog.GoodsPerPage; slotIndex++)
                {
                    int goodsFieldIndex = NpcSellServiceCatalog.GetGoodsFieldIndex(sessionService.ListCollection, listIndex, pageIndex, slotIndex);
                    if (goodsFieldIndex < 0)
                    {
                        continue;
                    }

                    string rawValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, goodsFieldIndex);
                    if (!int.TryParse(rawValue, out int itemId) || itemId <= 0)
                    {
                        continue;
                    }

                    if (!itemReferenceService.TryResolveReferenceOption(
                        sessionService.ListCollection,
                        listIndex,
                        elementIndex,
                        sessionService.ListCollection.Lists[listIndex].elementFields[goodsFieldIndex],
                        rawValue,
                        sessionService.Database,
                        iconResolutionService,
                        out ItemReferenceOption option))
                    {
                        continue;
                    }

                    int priceFieldIndex = ResolveNpcSellPriceFieldIndex(option.ListIndex, moneyType);
                    int rawPrice = 0;
                    if (priceFieldIndex >= 0)
                    {
                        int.TryParse(sessionService.ListCollection.GetValue(option.ListIndex, option.ElementIndex, priceFieldIndex), out rawPrice);
                    }

                    page.Entries.Add(new NpcSellPageEditorEntryModel
                    {
                        SlotIndex = slotIndex,
                        GoodsFieldIndex = goodsFieldIndex,
                        ItemId = itemId,
                        ItemName = option.Name ?? string.Empty,
                        IconKey = option.IconKey ?? string.Empty,
                        Quality = option.Quality,
                        MoneyType = moneyType,
                        CurrencyLabel = NpcSellPriceCatalog.BuildCurrencyDisplay(moneyType, rawPrice),
                        RawPrice = rawPrice,
                        Price = NpcSellPriceCatalog.DecodeDisplayAmount(rawPrice),
                        ItemListIndex = option.ListIndex,
                        ItemElementIndex = option.ElementIndex,
                        PriceFieldIndex = priceFieldIndex
                    });
                }

                pages.Add(page);
            }

            return pages;
        }

        private bool HasValidNpcSellElement(int listIndex, int elementIndex)
        {
            return sessionService != null
                && sessionService.ListCollection != null
                && listIndex >= 0
                && listIndex < sessionService.ListCollection.Lists.Length
                && sessionService.ListCollection.Lists[listIndex] != null
                && sessionService.ListCollection.Lists[listIndex].elementValues != null
                && elementIndex >= 0
                && elementIndex < sessionService.ListCollection.Lists[listIndex].elementValues.Length;
        }

        private int ResolveNpcSellPriceFieldIndex(int itemListIndex, int moneyType)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || itemListIndex < 0
                || itemListIndex >= sessionService.ListCollection.Lists.Length
                || sessionService.ListCollection.Lists[itemListIndex] == null
                || sessionService.ListCollection.Lists[itemListIndex].elementFields == null)
            {
                return -1;
            }

            string[] candidateFieldNames = new[]
            {
                "shop_price",
                "price",
                moneyType == 1 ? "sell_for_bind_money" : string.Empty
            };
            string[] fields = sessionService.ListCollection.Lists[itemListIndex].elementFields;
            for (int candidateIndex = 0; candidateIndex < candidateFieldNames.Length; candidateIndex++)
            {
                string expectedFieldName = candidateFieldNames[candidateIndex];
                if (string.IsNullOrWhiteSpace(expectedFieldName))
                {
                    continue;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(fields[i], expectedFieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private void ApplyNpcSellPriceEditorChanges(IList<NpcSellPageEditorPageModel> pages)
        {
            if (pages == null)
            {
                return;
            }

            int sourceListIndex = comboBox_lists.SelectedIndex;
            int sourceElementIndex = ResolveCurrentElementIndex();
            bool anyChange = false;
            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                NpcSellPageEditorPageModel page = pages[pageIndex];
                if (page == null || page.Entries == null)
                {
                    continue;
                }

                for (int entryIndex = 0; entryIndex < page.Entries.Count; entryIndex++)
                {
                    NpcSellPageEditorEntryModel entry = page.Entries[entryIndex];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (entry.GoodsFieldIndex >= 0)
                    {
                        string newItemValue = entry.ItemId > 0
                            ? entry.ItemId.ToString(CultureInfo.InvariantCulture)
                            : "0";
                        string oldItemValue = sessionService.ListCollection.GetValue(sourceListIndex, sourceElementIndex, entry.GoodsFieldIndex);
                        if (!string.Equals(oldItemValue, newItemValue, StringComparison.Ordinal))
                        {
                            sessionService.ListCollection.SetValue(sourceListIndex, sourceElementIndex, entry.GoodsFieldIndex, newItemValue);
                            mainWindowDirtyTrackingService.MarkRowDirty(
                                dirtyStateTracker,
                                listDisplayService,
                                ref viewModel.HasUnsavedChanges,
                                sourceListIndex,
                                sourceElementIndex);
                            mainWindowDirtyTrackingService.MarkFieldDirty(
                                dirtyStateTracker,
                                ref viewModel.HasUnsavedChanges,
                                sourceListIndex,
                                sourceElementIndex,
                                entry.GoodsFieldIndex);
                            mainWindowDirtyTrackingService.ClearFieldInvalid(
                                dirtyStateTracker,
                                sourceListIndex,
                                sourceElementIndex,
                                entry.GoodsFieldIndex);
                            anyChange = true;
                        }
                    }

                    int priceFieldIndex = ResolveNpcSellPriceFieldIndex(entry.ItemListIndex, entry.MoneyType);
                    if (priceFieldIndex < 0 || entry.ItemListIndex < 0 || entry.ItemElementIndex < 0)
                    {
                        continue;
                    }

                    string newValue = NpcSellPriceCatalog.FormatRawValue(NpcSellPriceCatalog.EncodeRawAmount(entry.Price, entry.RawPrice));
                    string oldValue = sessionService.ListCollection.GetValue(entry.ItemListIndex, entry.ItemElementIndex, priceFieldIndex);
                    if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    sessionService.ListCollection.SetValue(entry.ItemListIndex, entry.ItemElementIndex, priceFieldIndex, newValue);
                    mainWindowDirtyTrackingService.MarkRowDirty(
                        dirtyStateTracker,
                        listDisplayService,
                        ref viewModel.HasUnsavedChanges,
                        entry.ItemListIndex,
                        entry.ItemElementIndex);
                    mainWindowDirtyTrackingService.MarkFieldDirty(
                        dirtyStateTracker,
                        ref viewModel.HasUnsavedChanges,
                        entry.ItemListIndex,
                        entry.ItemElementIndex,
                        priceFieldIndex);
                    mainWindowDirtyTrackingService.ClearFieldInvalid(
                        dirtyStateTracker,
                        entry.ItemListIndex,
                        entry.ItemElementIndex,
                        priceFieldIndex);
                    anyChange = true;
                }
            }

            if (!anyChange)
            {
                return;
            }

            InvalidateItemReferenceOptionCaches();
            if (IsNpcSellServiceListSelected())
            {
                change_item(null, null);
            }
        }

        private string GetSelectedElementName()
        {
            if (dataGridView_elems == null || dataGridView_elems.CurrentCell == null)
            {
                return string.Empty;
            }

            object value = dataGridView_elems.Rows[dataGridView_elems.CurrentCell.RowIndex].Cells[2].Value;
            return Convert.ToString(value) ?? string.Empty;
        }
    }

    public sealed class NpcSellPageEditorPageModel
    {
        public int PageIndex { get; set; }
        public string Title { get; set; }
        public int MoneyType { get; set; }
        public List<NpcSellPageEditorEntryModel> Entries { get; } = new List<NpcSellPageEditorEntryModel>();
    }

    public sealed class NpcSellPageEditorEntryModel
    {
        public int SlotIndex { get; set; }
        public int GoodsFieldIndex { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string IconKey { get; set; }
        public int Quality { get; set; }
        public int MoneyType { get; set; }
        public string CurrencyLabel { get; set; }
        public int RawPrice { get; set; }
        public int Price { get; set; }
        public int ItemListIndex { get; set; }
        public int ItemElementIndex { get; set; }
        public int PriceFieldIndex { get; set; }
    }
}
