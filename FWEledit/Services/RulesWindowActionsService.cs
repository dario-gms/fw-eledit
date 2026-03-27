using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class RulesWindowActionsService
    {
        public void PrepareForLoad(DataGridView fieldsGrid, DataGridView valuesGrid, ComboBox listCombo, DataGridViewColumn col1, DataGridViewColumn col2, DataGridViewColumn col7, DataGridViewColumn col8)
        {
            if (fieldsGrid != null)
            {
                fieldsGrid.Rows.Clear();
            }
            if (valuesGrid != null)
            {
                valuesGrid.Rows.Clear();
            }
            if (col1 != null) col1.HeaderText = "Base Fields";
            if (col2 != null) col2.HeaderText = "Recent Fields";
            if (col7 != null) col7.HeaderText = "Base Values";
            if (col8 != null) col8.HeaderText = "Recent Values";
            if (listCombo != null)
            {
                listCombo.Items.Clear();
            }
        }

        public void LoadCollection(
            RulesWindowViewModel viewModel,
            string filePath,
            bool isBase,
            ref ColorProgressBar.ColorProgressBar progressBar,
            TextBox baseFile,
            TextBox baseVersion,
            TextBox recentFile,
            TextBox recentVersion)
        {
            if (viewModel == null || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            if (isBase)
            {
                viewModel.BaseCollection = viewModel.LoadCollection(filePath, ref progressBar);
                if (baseFile != null)
                {
                    baseFile.Text = filePath;
                }
                if (baseVersion != null && viewModel.BaseCollection != null)
                {
                    baseVersion.Text = viewModel.BaseCollection.Version.ToString();
                }
            }
            else
            {
                viewModel.RecentCollection = viewModel.LoadCollection(filePath, ref progressBar);
                if (recentFile != null)
                {
                    recentFile.Text = filePath;
                }
                if (recentVersion != null && viewModel.RecentCollection != null)
                {
                    recentVersion.Text = viewModel.RecentCollection.Version.ToString();
                }
            }
        }

        public void PopulateListsIfReady(RulesWindowViewModel viewModel, ComboBox listCombo)
        {
            if (viewModel == null || listCombo == null)
            {
                return;
            }
            if (viewModel.BaseCollection == null || viewModel.RecentCollection == null)
            {
                return;
            }
            if (viewModel.BaseCollection.Lists.Length == 0 || viewModel.RecentCollection.Lists.Length == 0)
            {
                return;
            }

            viewModel.ResetRules();
            listCombo.Items.Clear();
            for (int l = 0; l < viewModel.RecentCollection.Lists.Length; l++)
            {
                listCombo.Items.Add("[" + l + "]: " + viewModel.RecentCollection.Lists[l].listName + " (" + viewModel.RecentCollection.Lists[l].elementValues.Length + ")");
            }
        }

        public void ApplyListSelection(
            RulesWindowViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn col1,
            DataGridViewColumn col2,
            DataGridViewColumn col7,
            DataGridViewColumn col8,
            CheckBox removeList,
            RadioButton baseOffset,
            RadioButton recentOffset,
            TextBox baseOffsetText,
            TextBox recentOffsetText)
        {
            if (viewModel == null || viewModel.BaseCollection == null || viewModel.RecentCollection == null || viewModel.Rules == null)
            {
                return;
            }

            if (fieldsGrid != null) fieldsGrid.Rows.Clear();
            if (valuesGrid != null) valuesGrid.Rows.Clear();
            if (col1 != null) col1.HeaderText = "Base Fields";
            if (col2 != null) col2.HeaderText = "Recent Fields";
            if (col7 != null) col7.HeaderText = "Base Values";
            if (col8 != null) col8.HeaderText = "Recent Values";

            int lb = listIndex;
            int lr = listIndex;
            if (viewModel.BaseCollection.Version < 191 && viewModel.RecentCollection.Version >= 191 && viewModel.BaseCollection.ConversationListIndex <= lb)
            {
                lb = lb + 1;
            }

            if (lr <= -1)
            {
                return;
            }

            if (removeList != null)
            {
                removeList.Checked = viewModel.Rules[lr].RemoveList;
            }

            if (viewModel.BaseCollection.Lists.Length > lb && viewModel.RecentCollection.ConversationListIndex != lr)
            {
                if (col1 != null) col1.HeaderText = "Base Fields (" + viewModel.BaseCollection.Lists[lb].elementFields.Length + ")";
                if (col2 != null) col2.HeaderText = "Recent Fields (" + viewModel.RecentCollection.Lists[lr].elementFields.Length + ")";

                int baseFieldIndex = 0;
                int recentFieldIndex = 0;

                if (viewModel.Rules[lr].ReplaceOffset)
                {
                    if (baseOffset != null) baseOffset.Checked = true;
                    if (recentOffset != null) recentOffset.Checked = false;
                }
                else
                {
                    if (baseOffset != null) baseOffset.Checked = false;
                    if (recentOffset != null) recentOffset.Checked = true;
                }

                if (baseOffsetText != null)
                {
                    baseOffsetText.Text = viewModel.BaseCollection.GetOffset(lb);
                }
                if (recentOffsetText != null)
                {
                    recentOffsetText.Text = viewModel.RecentCollection.GetOffset(lr);
                }

                for (int f = 0; f < viewModel.RecentCollection.Lists[lr].elementFields.Length; f++)
                {
                    if (viewModel.Rules[lr].RemoveValues[f])
                    {
                        baseFieldIndex--;
                    }

                    string baseType = string.Empty;
                    if (!viewModel.Rules[lr].RemoveValues[f] && baseFieldIndex < viewModel.BaseCollection.Lists[lb].elementFields.Length)
                    {
                        baseType = viewModel.BaseCollection.Lists[lb].elementTypes[baseFieldIndex];
                    }

                    string recentType = viewModel.RecentCollection.Lists[lr].elementTypes[recentFieldIndex];
                    string mismatch = viewModel.CountMismatches(lb, lr, baseFieldIndex, recentFieldIndex).ToString();
                    string removeValue = viewModel.Rules[lr].RemoveValues[f].ToString();

                    if (fieldsGrid != null)
                    {
                        fieldsGrid.Rows.Add(new string[] { baseType, recentType, mismatch, removeValue, "Details" });
                        fieldsGrid.Rows[fieldsGrid.Rows.Count - 1].HeaderCell.Value = f.ToString();
                    }

                    baseFieldIndex++;
                    recentFieldIndex++;
                }
            }
        }

        public void ToggleRemoveList(RulesWindowViewModel viewModel, int listIndex, bool remove)
        {
            if (viewModel == null || viewModel.Rules == null || listIndex < 0)
            {
                return;
            }

            viewModel.Rules[listIndex].RemoveList = remove;
        }

        public void ToggleOffset(RulesWindowViewModel viewModel, int listIndex, bool baseOffset)
        {
            if (viewModel == null || viewModel.BaseCollection == null || viewModel.Rules == null || listIndex < 0)
            {
                return;
            }

            if (baseOffset)
            {
                viewModel.Rules[listIndex].ReplaceOffset = true;
                viewModel.Rules[listIndex].Offset = viewModel.BaseCollection.GetOffset(listIndex);
            }
            else
            {
                viewModel.Rules[listIndex].ReplaceOffset = false;
                viewModel.Rules[listIndex].Offset = string.Empty;
            }
        }

        public void HandleFieldClick(
            RulesWindowViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn col7,
            DataGridViewColumn col8,
            int rowIndex,
            int columnIndex)
        {
            if (viewModel == null || viewModel.BaseCollection == null || viewModel.RecentCollection == null || viewModel.Rules == null)
            {
                return;
            }

            int lb = listIndex;
            if (viewModel.BaseCollection.Version < 191 && viewModel.RecentCollection.Version >= 191 && viewModel.BaseCollection.ConversationListIndex <= lb)
            {
                lb = lb + 1;
            }

            if (listIndex > -1 && columnIndex == 3 && rowIndex > -1)
            {
                if (fieldsGrid != null && fieldsGrid.FirstDisplayedCell != null)
                {
                    int displayRow = fieldsGrid.FirstDisplayedCell.RowIndex;
                    int displayCol = fieldsGrid.FirstDisplayedCell.ColumnIndex;
                    viewModel.Rules[listIndex].RemoveValues[rowIndex] = Convert.ToBoolean(fieldsGrid.Rows[rowIndex].Cells[columnIndex].EditedFormattedValue);
                    if (fieldsGrid.Rows.Count > 0)
                    {
                        fieldsGrid.FirstDisplayedCell = fieldsGrid.Rows[displayRow].Cells[displayCol];
                    }
                }
                return;
            }

            if (listIndex > -1 && columnIndex == 4 && rowIndex > -1)
            {
                if (valuesGrid != null)
                {
                    valuesGrid.Rows.Clear();
                }
                if (col7 != null) col7.HeaderText = "Base Values";
                if (col8 != null) col8.HeaderText = "Recent Values";

                int baseFieldIndex = 0;
                int recentFieldIndex = 0;

                for (int f = 0; f < rowIndex; f++)
                {
                    if (viewModel.Rules[listIndex].RemoveValues[f])
                    {
                        baseFieldIndex--;
                    }
                    baseFieldIndex++;
                    recentFieldIndex++;
                }

                if (recentFieldIndex < viewModel.RecentCollection.Lists[listIndex].elementFields.Length)
                {
                    if (col7 != null) col7.HeaderText = "Base Values (" + viewModel.BaseCollection.Lists[lb].elementValues.Length + ")";
                    if (col8 != null) col8.HeaderText = "Recent Values (" + viewModel.RecentCollection.Lists[listIndex].elementValues.Length + ")";

                    for (int i = 0; i < viewModel.RecentCollection.Lists[listIndex].elementValues.Length; i++)
                    {
                        string baseValue = string.Empty;
                        string recentValue = viewModel.RecentCollection.GetValue(listIndex, i, recentFieldIndex);
                        bool mismatched = false;

                        if (i < viewModel.BaseCollection.Lists[lb].elementValues.Length
                            && baseFieldIndex < viewModel.BaseCollection.Lists[lb].elementFields.Length
                            && !viewModel.Rules[listIndex].RemoveValues[recentFieldIndex])
                        {
                            baseValue = viewModel.BaseCollection.GetValue(lb, i, baseFieldIndex);
                            mismatched = baseValue != recentValue;
                        }

                        if (valuesGrid != null)
                        {
                            valuesGrid.Rows.Add(new string[] { baseValue, recentValue });
                            if (mismatched)
                            {
                                valuesGrid.Rows[valuesGrid.Rows.Count - 1].DefaultCellStyle.BackColor = Color.Salmon;
                            }
                            valuesGrid.Rows[valuesGrid.Rows.Count - 1].HeaderCell.Value = i.ToString();
                        }
                    }
                }
            }
        }

        public void ShowRules(RulesWindowViewModel viewModel)
        {
            if (viewModel == null || viewModel.Rules == null || viewModel.BaseCollection == null || viewModel.RecentCollection == null)
            {
                return;
            }

            string message = viewModel.BuildRulesMessage();
            new DebugWindow("Rules v" + viewModel.RecentCollection.Version.ToString() + " -> v" + viewModel.BaseCollection.Version.ToString(), message);
        }

        public void ImportRules(RulesWindowViewModel viewModel, string filePath)
        {
            if (viewModel == null || viewModel.Rules == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            viewModel.ImportRules(filePath);
        }

        public void ExportRules(RulesWindowViewModel viewModel, string filePath)
        {
            if (viewModel == null || viewModel.Rules == null || viewModel.BaseCollection == null || viewModel.RecentCollection == null || string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            viewModel.ExportRules(filePath);
        }
    }
}
