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
            EventHandler saveDescriptionClick,
            EventHandler backClick,
            EventHandler forwardClick)
        {
            if (owner == null)
            {
                return new MainWindowLayoutResult();
            }

            owner.SuspendLayout();
            owner.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

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
            root.BackColor = Color.FromArgb(31, 34, 39);
            int topInset = (menuStrip != null ? menuStrip.Height : 24) + 8;
            root.Padding = new Padding(10, topInset, 10, 8);
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            FlowLayoutPanel historyPanel = new FlowLayoutPanel();
            historyPanel.Dock = DockStyle.Fill;
            historyPanel.Margin = new Padding(0, 0, 4, 6);
            historyPanel.WrapContents = false;
            historyPanel.FlowDirection = FlowDirection.LeftToRight;

            Button backButton = CreateNavButton("<");
            Button forwardButton = CreateNavButton(">");
            if (backClick != null)
            {
                backButton.Click += backClick;
            }
            if (forwardClick != null)
            {
                forwardButton.Click += forwardClick;
            }
            historyPanel.Controls.Add(backButton);
            historyPanel.Controls.Add(forwardButton);

            Label listCaption = CreateCaption("Element List");

            TableLayoutPanel offsetPanel = new TableLayoutPanel();
            offsetPanel.Dock = DockStyle.Fill;
            offsetPanel.Margin = new Padding(0);
            offsetPanel.ColumnCount = 2;
            offsetPanel.RowCount = 1;
            offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
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
                offsetBox.Margin = new Padding(0, 0, 0, 4);
            }

            if (offsetLabel != null)
            {
                offsetPanel.Controls.Add(offsetLabel, 0, 0);
            }
            if (offsetBox != null)
            {
                offsetPanel.Controls.Add(offsetBox, 1, 0);
            }

            Label offsetCaption = CreateCaption("Selection Offset");

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Margin = new Padding(0);
            mainSplit.FixedPanel = FixedPanel.Panel1;
            mainSplit.BackColor = Color.FromArgb(211, 218, 226);
            mainSplit.SplitterWidth = 4;

            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Padding = new Padding(0, 0, 0, 0);
            leftPanel.BackColor = Color.FromArgb(24, 26, 30);

            TableLayoutPanel leftHeader = new TableLayoutPanel();
            leftHeader.Dock = DockStyle.Top;
            leftHeader.Height = 54;
            leftHeader.Margin = new Padding(0);
            leftHeader.ColumnCount = 2;
            leftHeader.RowCount = 2;
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            leftHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftHeader.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            leftHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            leftHeader.Controls.Add(historyPanel, 0, 1);
            leftHeader.Controls.Add(listCaption, 1, 0);

            if (listCombo != null)
            {
                listCombo.Parent = leftHeader;
                listCombo.Dock = DockStyle.Fill;
                listCombo.Margin = new Padding(0, 0, 0, 6);
                listCombo.Height = 28;
                leftHeader.Controls.Add(listCombo, 1, 1);
            }

            Label elementsCaption = CreateSectionLabel("Elements");
            elementsCaption.Dock = DockStyle.Top;
            elementsCaption.Height = 28;

            if (elementsGrid != null)
            {
                elementsGrid.Parent = leftPanel;
                elementsGrid.Dock = DockStyle.Fill;
                elementsGrid.Margin = new Padding(0);
            }

            FlowLayoutPanel searchOptionsPanel = new FlowLayoutPanel();
            searchOptionsPanel.Dock = DockStyle.Bottom;
            searchOptionsPanel.Height = 28;
            searchOptionsPanel.Margin = new Padding(0);
            searchOptionsPanel.Padding = new Padding(4, 3, 0, 0);
            searchOptionsPanel.WrapContents = false;

            if (searchAll != null)
            {
                searchAll.Parent = searchOptionsPanel;
                searchAll.Width = 86;
                searchAll.Margin = new Padding(0, 0, 6, 0);
            }

            if (searchExact != null)
            {
                searchExact.Parent = searchOptionsPanel;
                searchExact.Width = 112;
                searchExact.Margin = new Padding(0, 0, 6, 0);
            }

            if (searchMatchCase != null)
            {
                searchMatchCase.Parent = searchOptionsPanel;
                searchMatchCase.Width = 104;
                searchMatchCase.Margin = new Padding(0, 0, 0, 0);
            }

            TableLayoutPanel searchPanel = new TableLayoutPanel();
            searchPanel.Dock = DockStyle.Bottom;
            searchPanel.Height = 34;
            searchPanel.Margin = new Padding(0);
            searchPanel.ColumnCount = 2;
            searchPanel.RowCount = 1;
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104F));
            searchPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            if (searchBox != null)
            {
                searchBox.Dock = DockStyle.Fill;
                searchBox.Margin = new Padding(0, 4, 6, 4);
                searchPanel.Controls.Add(searchBox, 0, 0);
            }

            if (searchButton != null)
            {
                searchButton.Dock = DockStyle.Fill;
                searchButton.Margin = new Padding(0, 3, 0, 4);
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
            leftPanel.Controls.Add(elementsCaption);
            leftPanel.Controls.Add(leftHeader);
            leftPanel.Controls.Add(searchSuggestionList);
            leftPanel.Controls.Add(searchOptionsPanel);
            leftPanel.Controls.Add(searchPanel);
            mainSplit.Panel1.Controls.Add(leftPanel);

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.BackColor = Color.FromArgb(31, 34, 39);

            TableLayoutPanel rightLayout = new TableLayoutPanel();
            rightLayout.Dock = DockStyle.Fill;
            rightLayout.Margin = new Padding(0);
            rightLayout.ColumnCount = 2;
            rightLayout.RowCount = 4;
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            rightLayout.Controls.Add(offsetCaption, 0, 0);
            rightLayout.Controls.Add(offsetPanel, 0, 1);

            if (versionLabel != null)
            {
                versionLabel.Parent = rightLayout;
                versionLabel.Dock = DockStyle.Fill;
                versionLabel.TextAlign = ContentAlignment.MiddleRight;
                versionLabel.Margin = new Padding(0);
                versionLabel.Font = new Font(owner.Font, FontStyle.Regular);
                rightLayout.Controls.Add(versionLabel, 1, 0);
                rightLayout.SetRowSpan(versionLabel, 2);
            }

            TableLayoutPanel rawValuePanel = new TableLayoutPanel();
            rawValuePanel.Dock = DockStyle.Fill;
            rawValuePanel.Margin = new Padding(0);
            rawValuePanel.ColumnCount = 5;
            rawValuePanel.RowCount = 1;
            rawValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            rawValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rawValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            rawValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            rawValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            rawValuePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label rawValueLabel = CreateCaption("Selected ID");
            rawValueLabel.Dock = DockStyle.Fill;
            rawValueLabel.TextAlign = ContentAlignment.MiddleLeft;
            rawValuePanel.Controls.Add(rawValueLabel, 0, 0);

            if (setValueBox != null)
            {
                setValueBox.Dock = DockStyle.Fill;
                setValueBox.Margin = new Padding(0, 0, 6, 4);
                setValueBox.TextAlign = HorizontalAlignment.Center;
                setValueBox.Text = string.Empty;
                rawValuePanel.Controls.Add(setValueBox, 1, 0);
            }

            if (setValueButton != null)
            {
                setValueButton.Text = "Set";
                setValueButton.Dock = DockStyle.Fill;
                setValueButton.Margin = new Padding(0, 0, 6, 4);
                setValueButton.FlatStyle = FlatStyle.Flat;
                setValueButton.BackColor = Color.FromArgb(232, 237, 243);
                setValueButton.ForeColor = Color.FromArgb(29, 36, 45);
                setValueButton.Visible = true;
                rawValuePanel.Controls.Add(setValueButton, 2, 0);
            }

            Button rawValueUpButton = CreateStepperButton("^");
            Button rawValueDownButton = CreateStepperButton("v");
            rawValuePanel.Controls.Add(rawValueUpButton, 3, 0);
            rawValuePanel.Controls.Add(rawValueDownButton, 4, 0);
            rightLayout.Controls.Add(rawValuePanel, 0, 2);
            rightLayout.SetColumnSpan(rawValuePanel, 2);

            Panel valuesInspectorPanel = new Panel();
            valuesInspectorPanel.Dock = DockStyle.Fill;
            valuesInspectorPanel.Margin = new Padding(0);

            TabControl rightTabs = new TabControl();
            rightTabs.Dock = DockStyle.Fill;
            rightTabs.Margin = new Padding(0);
            rightTabs.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            if (updatePickIconButtonState != null || changeItem != null)
            {
                rightTabs.SelectedIndexChanged += (s, e) =>
                {
                    if (rightTabs.SelectedTab != null && rightTabs.SelectedTab.Tag is EquipmentValuesTab)
                    {
                        MoveValuesInspectorToSelectedTab(rightTabs, valuesInspectorPanel);
                        if (changeItem != null && (canChangeItem == null || canChangeItem()))
                        {
                            changeItem();
                        }
                    }
                    if (updatePickIconButtonState != null)
                    {
                        updatePickIconButtonState();
                    }
                };
            }

            TabPage valuesTab = new TabPage("Values");
            valuesTab.Padding = new Padding(0, 6, 0, 0);
            valuesTab.Tag = EquipmentValuesTab.Main;

            Label valuesCaption = CreateSectionLabel("Properties");
            valuesCaption.Dock = DockStyle.Top;
            valuesCaption.Height = 30;

            TabPage equipmentTabMain = valuesTab;
            TabPage equipmentTabModels = new TabPage("Models");
            equipmentTabModels.Padding = new Padding(0, 6, 0, 0);
            equipmentTabModels.Tag = EquipmentValuesTab.Models;
            TabPage equipmentTabRefine = new TabPage("Refine");
            equipmentTabRefine.Padding = new Padding(0, 6, 0, 0);
            equipmentTabRefine.Tag = EquipmentValuesTab.Refine;
            TabPage equipmentTabDecompose = new TabPage("Decompose");
            equipmentTabDecompose.Padding = new Padding(0, 6, 0, 0);
            equipmentTabDecompose.Tag = EquipmentValuesTab.Decompose;
            TabPage equipmentTabOther = null;

            if (valuesGrid != null)
            {
                valuesGrid.Parent = valuesInspectorPanel;
                valuesGrid.Dock = DockStyle.Fill;
                valuesGrid.Margin = new Padding(0);
                ApplyInspectorGridLayout(valuesGrid);
                if (updatePickIconButtonState != null)
                {
                    valuesGrid.CurrentCellChanged += (s, e) => updatePickIconButtonState();
                    valuesGrid.Scroll += (s, e) => updatePickIconButtonState();
                    valuesGrid.SizeChanged += (s, e) => updatePickIconButtonState();
                }
            }
            if (valuesGrid != null)
            {
                valuesInspectorPanel.Controls.Add(valuesGrid);
            }
            valuesInspectorPanel.Controls.Add(valuesCaption);
            if (valuesGrid != null)
            {
                valuesInspectorPanel.Controls.SetChildIndex(valuesGrid, 0);
            }
            valuesInspectorPanel.Controls.SetChildIndex(valuesCaption, 1);
            valuesTab.Controls.Add(valuesInspectorPanel);

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
            descriptionTab.Padding = new Padding(0, 8, 0, 0);

            TableLayoutPanel descriptionLayout = new TableLayoutPanel();
            descriptionLayout.Dock = DockStyle.Fill;
            descriptionLayout.Margin = new Padding(0);
            descriptionLayout.ColumnCount = 1;
            descriptionLayout.RowCount = 6;
            descriptionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

            Panel descriptionHeader = new Panel();
            descriptionHeader.Dock = DockStyle.Fill;
            descriptionHeader.Margin = new Padding(0);

            Button descriptionSaveButton = new Button();
            descriptionSaveButton.Text = "Stage Description";
            descriptionSaveButton.Dock = DockStyle.Right;
            descriptionSaveButton.Width = 150;
            descriptionSaveButton.Margin = new Padding(0, 3, 0, 4);
            if (saveDescriptionClick != null)
            {
                descriptionSaveButton.Click += saveDescriptionClick;
            }

            Label descriptionStatusLabel = new Label();
            descriptionStatusLabel.Dock = DockStyle.Fill;
            descriptionStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            descriptionStatusLabel.Text = "Loaded from configs.pck.files";
            descriptionStatusLabel.Padding = new Padding(8, 0, 0, 0);

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
            descriptionEditor.BorderStyle = BorderStyle.FixedSingle;
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

            Label previewLabel = CreateSubsectionLabel("Rendered Preview");
            Label sourceLabel = CreateSubsectionLabel("Source Text");

            FlowLayoutPanel descriptionToolbar = new FlowLayoutPanel();
            descriptionToolbar.Dock = DockStyle.Fill;
            descriptionToolbar.Margin = new Padding(0);
            descriptionToolbar.Padding = new Padding(6, 4, 0, 4);
            descriptionToolbar.WrapContents = false;
            descriptionToolbar.FlowDirection = FlowDirection.LeftToRight;
            descriptionToolbar.BackColor = Color.FromArgb(238, 241, 245);

            Button descriptionColorButton = CreateDescriptionToolButton("Color...");
            Button descriptionLineBreakButton = CreateDescriptionToolButton("Line");
            Button descriptionNormalFontButton = CreateDescriptionToolButton("Normal");
            Button descriptionSmallFontButton = CreateDescriptionToolButton("Small");
            Button descriptionTitleFontButton = CreateDescriptionToolButton("Title");

            ToolTip descriptionToolTip = new ToolTip();
            descriptionToolTip.SetToolTip(descriptionColorButton, "Insert FW color tag (^RRGGBB).");
            descriptionToolTip.SetToolTip(descriptionLineBreakButton, "Insert a line break.");
            descriptionToolTip.SetToolTip(descriptionNormalFontButton, "Switch to Raven's normal hint font (^O053).");
            descriptionToolTip.SetToolTip(descriptionSmallFontButton, "Switch to Raven's smaller emphasis font (^O005).");
            descriptionToolTip.SetToolTip(descriptionTitleFontButton, "Switch to the larger title hint font (^O057).");

            descriptionToolbar.Controls.Add(descriptionColorButton);
            descriptionToolbar.Controls.Add(descriptionLineBreakButton);
            descriptionToolbar.Controls.Add(descriptionNormalFontButton);
            descriptionToolbar.Controls.Add(descriptionSmallFontButton);
            descriptionToolbar.Controls.Add(descriptionTitleFontButton);

            descriptionLayout.Controls.Add(descriptionHeader, 0, 0);
            descriptionLayout.Controls.Add(previewLabel, 0, 1);
            descriptionLayout.Controls.Add(descriptionPreview, 0, 2);
            descriptionLayout.Controls.Add(sourceLabel, 0, 3);
            descriptionLayout.Controls.Add(descriptionToolbar, 0, 4);
            descriptionLayout.Controls.Add(descriptionEditor, 0, 5);
            descriptionTab.Controls.Add(descriptionLayout);

            rightTabs.TabPages.Add(valuesTab);
            rightTabs.TabPages.Add(equipmentTabModels);
            rightTabs.TabPages.Add(equipmentTabRefine);
            rightTabs.TabPages.Add(equipmentTabDecompose);
            rightTabs.TabPages.Add(descriptionTab);

            rightTabs.Margin = new Padding(0, 8, 0, 0);
            rightLayout.Controls.Add(rightTabs, 0, 3);
            rightLayout.SetColumnSpan(rightTabs, 2);
            rightPanel.Controls.Add(rightLayout);
            mainSplit.Panel2.Controls.Add(rightPanel);

            if (progressBar != null)
            {
                progressBar.Parent = root;
                progressBar.Dock = DockStyle.Fill;
                progressBar.Margin = new Padding(0);
                progressBar.Height = 8;
                progressBar.Visible = false;
            }

            root.Controls.Add(mainSplit, 0, 0);
            if (progressBar != null)
            {
                root.Controls.Add(progressBar, 0, 1);
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
                EquipmentTabs = rightTabs,
                EquipmentTabMain = equipmentTabMain,
                EquipmentTabModels = equipmentTabModels,
                EquipmentTabRefine = equipmentTabRefine,
                EquipmentTabDecompose = equipmentTabDecompose,
                EquipmentTabOther = equipmentTabOther,
                DescriptionTab = descriptionTab,
                DescriptionEditor = descriptionEditor,
                DescriptionPreview = descriptionPreview,
                DescriptionSaveButton = descriptionSaveButton,
                DescriptionStatusLabel = descriptionStatusLabel,
                DescriptionColorButton = descriptionColorButton,
                DescriptionLineBreakButton = descriptionLineBreakButton,
                DescriptionNormalFontButton = descriptionNormalFontButton,
                DescriptionSmallFontButton = descriptionSmallFontButton,
                DescriptionTitleFontButton = descriptionTitleFontButton,
                InlinePickIconButton = inlinePickIconButton,
                RawValueUpButton = rawValueUpButton,
                RawValueDownButton = rawValueDownButton,
                BackButton = backButton,
                ForwardButton = forwardButton,
                SearchSuggestionList = searchSuggestionList
            };
        }

        private static Label CreateCaption(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))),
                ForeColor = Color.FromArgb(83, 96, 112)
            };
        }

        private static Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(8, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))),
                BackColor = Color.FromArgb(225, 231, 238),
                ForeColor = Color.FromArgb(83, 96, 112)
            };
        }

        private static Button CreateNavButton(string text)
        {
            return new Button
            {
                Text = text,
                Width = 22,
                Height = 22,
                Margin = new Padding(0, 0, 4, 0),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                Enabled = false,
                BackColor = Color.FromArgb(232, 237, 243),
                ForeColor = Color.FromArgb(29, 36, 45)
            };
        }

        private static Button CreateStepperButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 4, 4),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                BackColor = Color.FromArgb(232, 237, 243),
                ForeColor = Color.FromArgb(29, 36, 45),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)))
            };
        }

        private static Button CreateDescriptionToolButton(string text)
        {
            return new Button
            {
                Text = text,
                AutoSize = false,
                Width = text.Length > 6 ? 74 : 58,
                Height = 24,
                Margin = new Padding(0, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                BackColor = Color.FromArgb(232, 237, 243),
                ForeColor = Color.FromArgb(29, 36, 45),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)))
            };
        }

        private static void MoveValuesInspectorToSelectedTab(TabControl tabs, Panel valuesInspectorPanel)
        {
            if (tabs == null || valuesInspectorPanel == null || tabs.SelectedTab == null)
            {
                return;
            }
            if (!(tabs.SelectedTab.Tag is EquipmentValuesTab))
            {
                return;
            }
            if (valuesInspectorPanel.Parent != tabs.SelectedTab)
            {
                tabs.SelectedTab.Controls.Add(valuesInspectorPanel);
            }
            valuesInspectorPanel.Dock = DockStyle.Fill;
            valuesInspectorPanel.BringToFront();
        }

        private static Label CreateSubsectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(8, 0, 0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0))),
                BackColor = Color.FromArgb(225, 231, 238),
                ForeColor = Color.FromArgb(83, 96, 112)
            };
        }

        private static void ApplyInspectorGridLayout(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeight = 30;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.RowTemplate.Height = 24;
            grid.RowTemplate.Resizable = DataGridViewTriState.False;
            grid.RowHeadersVisible = false;
            grid.RowHeadersWidth = 4;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);

            if (grid.Columns.Count >= 3)
            {
                grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[0].Width = 300;
                grid.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grid.Columns[1].Visible = false;
                grid.Columns[2].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                grid.Columns[2].MinimumWidth = 280;
            }
        }
    }
}
