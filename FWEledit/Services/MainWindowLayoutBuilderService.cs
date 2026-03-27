using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowLayoutBuilderService
    {
        public MainWindowLayoutResult BuildLayout(
            Form owner,
            MenuStrip menuStrip,
            Label versionLabel,
            ComboBox listCombo,
            Label offsetLabel,
            TextBox offsetBox,
            DataGridView elementsGrid,
            CheckBox searchAll,
            CheckBox searchExact,
            CheckBox searchMatchCase,
            TextBox searchBox,
            Button searchButton,
            DataGridView valuesGrid,
            TextBox setValueBox,
            Button setValueButton,
            ColorProgressBar.ColorProgressBar progressBar,
            ListBox legacyListBox,
            ProgressBar legacyProgressBar,
            ItemListThemeService itemListThemeService,
            Action updatePickIconButtonState,
            Action changeItem,
            Func<bool> canChangeItem,
            MouseEventHandler suggestionMouseClick,
            KeyEventHandler suggestionKeyDown,
            EventHandler pickIconClick,
            EventHandler descriptionChanged,
            EventHandler saveDescriptionClick)
        {
            if (owner == null)
            {
                return new MainWindowLayoutResult();
            }

            owner.SuspendLayout();

            if (legacyListBox != null)
            {
                legacyListBox.Visible = false;
            }
            if (legacyProgressBar != null)
            {
                legacyProgressBar.Visible = false;
            }

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            int topInset = (menuStrip != null ? menuStrip.Height : 24) + 6;
            root.Padding = new Padding(6, topInset, 6, 6);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 16F));

            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;
            topPanel.Margin = new Padding(0);

            if (versionLabel != null)
            {
                versionLabel.Parent = topPanel;
                versionLabel.Dock = DockStyle.Right;
                versionLabel.Width = 180;
                versionLabel.TextAlign = ContentAlignment.MiddleRight;
                versionLabel.Margin = new Padding(0);
            }

            if (listCombo != null)
            {
                listCombo.Parent = topPanel;
                listCombo.Dock = DockStyle.Fill;
                listCombo.Margin = new Padding(0);
            }

            TableLayoutPanel offsetPanel = new TableLayoutPanel();
            offsetPanel.Dock = DockStyle.Right;
            offsetPanel.Width = 480;
            offsetPanel.Margin = new Padding(0);
            offsetPanel.ColumnCount = 2;
            offsetPanel.RowCount = 1;
            offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 56F));
            offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            offsetPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            if (offsetLabel != null)
            {
                offsetLabel.Parent = offsetPanel;
                offsetLabel.AutoSize = false;
                offsetLabel.Dock = DockStyle.Fill;
                offsetLabel.Margin = new Padding(0);
                offsetLabel.TextAlign = ContentAlignment.MiddleLeft;
            }

            if (offsetBox != null)
            {
                offsetBox.Parent = offsetPanel;
                offsetBox.Dock = DockStyle.Fill;
                offsetBox.Margin = new Padding(0);
            }

            if (offsetLabel != null)
            {
                offsetPanel.Controls.Add(offsetLabel, 0, 0);
            }
            if (offsetBox != null)
            {
                offsetPanel.Controls.Add(offsetBox, 1, 0);
            }

            if (listCombo != null)
            {
                topPanel.Controls.Add(listCombo);
            }
            topPanel.Controls.Add(offsetPanel);
            if (versionLabel != null)
            {
                topPanel.Controls.Add(versionLabel);
            }

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Margin = new Padding(0);
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.SplitterWidth = 5;

            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;

            if (elementsGrid != null)
            {
                elementsGrid.Parent = leftPanel;
                elementsGrid.Dock = DockStyle.Fill;
            }

            Panel searchOptionsPanel = new Panel();
            searchOptionsPanel.Dock = DockStyle.Bottom;
            searchOptionsPanel.Height = 26;

            if (searchAll != null)
            {
                searchAll.Parent = searchOptionsPanel;
                searchAll.Dock = DockStyle.Left;
                searchAll.Width = 82;
            }

            if (searchExact != null)
            {
                searchExact.Parent = searchOptionsPanel;
                searchExact.Dock = DockStyle.Left;
                searchExact.Width = 105;
            }

            if (searchMatchCase != null)
            {
                searchMatchCase.Parent = searchOptionsPanel;
                searchMatchCase.Dock = DockStyle.Left;
                searchMatchCase.Width = 96;
            }

            TableLayoutPanel searchPanel = new TableLayoutPanel();
            searchPanel.Dock = DockStyle.Bottom;
            searchPanel.Height = 30;
            searchPanel.Margin = new Padding(0);
            searchPanel.ColumnCount = 2;
            searchPanel.RowCount = 1;
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 94F));
            searchPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            if (searchBox != null)
            {
                searchBox.Dock = DockStyle.Fill;
                searchBox.Margin = new Padding(0);
                searchPanel.Controls.Add(searchBox, 0, 0);
            }

            if (searchButton != null)
            {
                searchButton.Dock = DockStyle.Fill;
                searchButton.Margin = new Padding(0);
                searchPanel.Controls.Add(searchButton, 1, 0);
            }

            ListBox searchSuggestionList = new ListBox();
            searchSuggestionList.Dock = DockStyle.Bottom;
            searchSuggestionList.Height = 140;
            searchSuggestionList.Visible = false;
            searchSuggestionList.IntegralHeight = false;
            searchSuggestionList.BorderStyle = BorderStyle.FixedSingle;
            searchSuggestionList.SelectionMode = SelectionMode.One;
            searchSuggestionList.TabStop = false;
            if (suggestionMouseClick != null)
            {
                searchSuggestionList.MouseClick += suggestionMouseClick;
            }
            if (suggestionKeyDown != null)
            {
                searchSuggestionList.KeyDown += suggestionKeyDown;
            }

            if (elementsGrid != null)
            {
                leftPanel.Controls.Add(elementsGrid);
            }
            leftPanel.Controls.Add(searchSuggestionList);
            leftPanel.Controls.Add(searchOptionsPanel);
            leftPanel.Controls.Add(searchPanel);
            mainSplit.Panel1.Controls.Add(leftPanel);

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;

            TableLayoutPanel setValuePanel = new TableLayoutPanel();
            setValuePanel.Dock = DockStyle.Bottom;
            setValuePanel.Height = 30;
            setValuePanel.Margin = new Padding(0);
            setValuePanel.ColumnCount = 2;
            setValuePanel.RowCount = 1;
            setValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            setValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128F));
            setValuePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            setValuePanel.Visible = false;

            if (setValueBox != null)
            {
                setValueBox.Dock = DockStyle.Fill;
                setValueBox.Margin = new Padding(0);
                setValueBox.TextAlign = HorizontalAlignment.Center;
                setValuePanel.Controls.Add(setValueBox, 0, 0);
            }

            if (setValueButton != null)
            {
                setValueButton.Dock = DockStyle.Fill;
                setValueButton.Margin = new Padding(0);
                setValueButton.Enabled = false;
                setValuePanel.Controls.Add(setValueButton, 1, 0);
            }

            TabControl rightTabs = new TabControl();
            rightTabs.Dock = DockStyle.Fill;
            rightTabs.Margin = new Padding(0);
            if (updatePickIconButtonState != null)
            {
                rightTabs.SelectedIndexChanged += (s, e) => updatePickIconButtonState();
            }

            TabPage valuesTab = new TabPage("Values");
            valuesTab.Padding = new Padding(0);

            TabControl equipmentTabs = new TabControl();
            equipmentTabs.Dock = DockStyle.Top;
            equipmentTabs.Height = 26;
            equipmentTabs.Margin = new Padding(0);
            equipmentTabs.Visible = false;
            if (changeItem != null)
            {
                equipmentTabs.SelectedIndexChanged += (s, e) =>
                {
                    if (canChangeItem == null || canChangeItem())
                    {
                        changeItem();
                    }
                };
            }

            TabPage equipmentTabMain = new TabPage("Main");
            TabPage equipmentTabRefine = new TabPage("Refine");
            TabPage equipmentTabModels = new TabPage("Models");
            TabPage equipmentTabOther = new TabPage("Other");

            equipmentTabs.TabPages.Add(equipmentTabMain);
            equipmentTabs.TabPages.Add(equipmentTabRefine);
            equipmentTabs.TabPages.Add(equipmentTabModels);
            equipmentTabs.TabPages.Add(equipmentTabOther);

            if (valuesGrid != null)
            {
                valuesGrid.Parent = valuesTab;
                valuesGrid.Dock = DockStyle.Fill;
                if (updatePickIconButtonState != null)
                {
                    valuesGrid.CurrentCellChanged += (s, e) => updatePickIconButtonState();
                    valuesGrid.Scroll += (s, e) => updatePickIconButtonState();
                    valuesGrid.SizeChanged += (s, e) => updatePickIconButtonState();
                }
                valuesTab.Controls.Add(valuesGrid);
            }
            valuesTab.Controls.Add(equipmentTabs);
            valuesTab.Controls.Add(setValuePanel);
            if (valuesGrid != null)
            {
                valuesTab.Controls.SetChildIndex(valuesGrid, 0);
            }
            valuesTab.Controls.SetChildIndex(equipmentTabs, 1);
            valuesTab.Controls.SetChildIndex(setValuePanel, 2);

            Button inlinePickIconButton = new Button();
            inlinePickIconButton.Text = "...";
            inlinePickIconButton.Visible = false;
            inlinePickIconButton.Enabled = false;
            inlinePickIconButton.TabStop = false;
            if (pickIconClick != null)
            {
                inlinePickIconButton.Click += pickIconClick;
            }
            if (valuesGrid != null)
            {
                valuesGrid.Controls.Add(inlinePickIconButton);
            }

            TabPage descriptionTab = new TabPage("Description");
            descriptionTab.Padding = new Padding(6);

            TableLayoutPanel descriptionLayout = new TableLayoutPanel();
            descriptionLayout.Dock = DockStyle.Fill;
            descriptionLayout.Margin = new Padding(0);
            descriptionLayout.ColumnCount = 1;
            descriptionLayout.RowCount = 3;
            descriptionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));

            Panel descriptionHeader = new Panel();
            descriptionHeader.Dock = DockStyle.Fill;
            descriptionHeader.Margin = new Padding(0);

            Button descriptionSaveButton = new Button();
            descriptionSaveButton.Text = "Stage Description";
            descriptionSaveButton.Dock = DockStyle.Right;
            descriptionSaveButton.Width = 140;
            if (saveDescriptionClick != null)
            {
                descriptionSaveButton.Click += saveDescriptionClick;
            }

            Label descriptionStatusLabel = new Label();
            descriptionStatusLabel.Dock = DockStyle.Fill;
            descriptionStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            descriptionStatusLabel.Text = "Loaded from configs.pck.files";

            descriptionHeader.Controls.Add(descriptionStatusLabel);
            descriptionHeader.Controls.Add(descriptionSaveButton);

            TextBox descriptionEditor = new TextBox();
            descriptionEditor.Dock = DockStyle.Fill;
            descriptionEditor.Multiline = true;
            descriptionEditor.ScrollBars = ScrollBars.Both;
            descriptionEditor.AcceptsReturn = true;
            descriptionEditor.AcceptsTab = true;
            descriptionEditor.WordWrap = false;
            descriptionEditor.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            if (descriptionChanged != null)
            {
                descriptionEditor.TextChanged += descriptionChanged;
            }

            RichTextBox descriptionPreview = new RichTextBox();
            descriptionPreview.Dock = DockStyle.Fill;
            descriptionPreview.ReadOnly = true;
            descriptionPreview.WordWrap = true;
            descriptionPreview.BorderStyle = BorderStyle.FixedSingle;
            descriptionPreview.ScrollBars = RichTextBoxScrollBars.Vertical;
            descriptionPreview.BackColor = Color.FromArgb(28, 29, 33);
            descriptionPreview.ForeColor = Color.White;
            descriptionPreview.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            descriptionLayout.Controls.Add(descriptionHeader, 0, 0);
            descriptionLayout.Controls.Add(descriptionEditor, 0, 1);
            descriptionLayout.Controls.Add(descriptionPreview, 0, 2);
            descriptionTab.Controls.Add(descriptionLayout);

            rightTabs.TabPages.Add(valuesTab);
            rightTabs.TabPages.Add(descriptionTab);

            rightPanel.Controls.Add(rightTabs);
            mainSplit.Panel2.Controls.Add(rightPanel);

            if (progressBar != null)
            {
                progressBar.Parent = root;
                progressBar.Dock = DockStyle.Fill;
                progressBar.Margin = new Padding(0);
            }

            root.Controls.Add(topPanel, 0, 0);
            root.Controls.Add(mainSplit, 0, 1);
            if (progressBar != null)
            {
                root.Controls.Add(progressBar, 0, 2);
            }

            owner.Controls.Add(root);
            if (menuStrip != null)
            {
                menuStrip.BringToFront();
            }

            if (itemListThemeService != null && elementsGrid != null)
            {
                itemListThemeService.ApplyDarkTheme(elementsGrid);
            }

            owner.ResumeLayout();

            return new MainWindowLayoutResult
            {
                MainSplit = mainSplit,
                RightTabs = rightTabs,
                ValuesTab = valuesTab,
                EquipmentTabs = equipmentTabs,
                EquipmentTabMain = equipmentTabMain,
                EquipmentTabRefine = equipmentTabRefine,
                EquipmentTabModels = equipmentTabModels,
                EquipmentTabOther = equipmentTabOther,
                DescriptionTab = descriptionTab,
                DescriptionEditor = descriptionEditor,
                DescriptionPreview = descriptionPreview,
                DescriptionSaveButton = descriptionSaveButton,
                DescriptionStatusLabel = descriptionStatusLabel,
                InlinePickIconButton = inlinePickIconButton,
                SearchSuggestionList = searchSuggestionList
            };
        }
    }
}
