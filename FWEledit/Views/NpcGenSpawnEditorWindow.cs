using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcGenSpawnEditorWindow : Form
    {
        private readonly NpcGenData data;
        private readonly NpcGenController controller;
        private readonly ISessionService sessionService;
        private readonly NpcGenEntityLookupService entityLookupService;
        private readonly NpcGenScriptReferenceService scriptReferenceService = new NpcGenScriptReferenceService();
        private readonly NpcGenFileService fileService = new NpcGenFileService();

        private DataGridView linkedGrid;
        private DataGridView rulesGrid;
        private Label summaryLabel;
        private Label diagnosticLabel;
        private NumericUpDown realmBox;
        private Button applyScriptButton;
        private Button applyControllerButton;
        private Button allRealmsButton;
        private Button saveNpcGenButton;
        private Button openFileButton;

        public NpcGenSpawnEditorWindow(
            NpcGenData data,
            NpcGenController controller,
            ISessionService sessionService,
            NpcGenEntityLookupService entityLookupService)
        {
            this.data = data;
            this.controller = controller;
            this.sessionService = sessionService;
            this.entityLookupService = entityLookupService;

            Text = string.Format(CultureInfo.InvariantCulture, "Spawn Editor - Controller {0} / Trigger {1}", controller.Id, controller.ControllerId);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(980, 640);
            Size = new Size(1120, 720);
            BuildUi();
            ApplyDarkTheme(this);
            LoadSpawnContext();
        }

        private void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            Controls.Add(root);

            TableLayoutPanel header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            summaryLabel = new Label { Dock = DockStyle.Fill, Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            diagnosticLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            header.Controls.Add(summaryLabel, 0, 0);
            header.Controls.Add(diagnosticLabel, 0, 1);
            root.Controls.Add(header, 0, 0);

            GroupBox linkedGroup = new GroupBox { Text = "Spawns controlled by this controller", Dock = DockStyle.Fill };
            linkedGrid = CreateGrid();
            linkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Kind", Width = 92 });
            linkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "No", Width = 60 });
            linkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", Width = 90 });
            linkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            linkedGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Count", Width = 70 });
            linkedGroup.Controls.Add(linkedGrid);
            root.Controls.Add(linkedGroup, 0, 1);

            GroupBox rulesGroup = new GroupBox { Text = "Realm rules", Dock = DockStyle.Fill };
            rulesGrid = CreateGrid();
            rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Source", Width = 160 });
            rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Realm", Width = 120 });
            rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "File", Width = 120 });
            rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Line", Width = 56 });
            rulesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Rule / Code", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            rulesGrid.SelectionChanged += rulesGrid_SelectionChanged;
            rulesGroup.Controls.Add(rulesGrid);
            root.Controls.Add(rulesGroup, 0, 2);

            TableLayoutPanel footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            realmBox = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 32, Value = 5 };
            applyScriptButton = new Button { Text = "Apply to Lua", Dock = DockStyle.Fill, Enabled = false };
            applyControllerButton = new Button { Text = "Set ctrl realm", Dock = DockStyle.Fill };
            allRealmsButton = new Button { Text = "Ctrl all realms", Dock = DockStyle.Fill };
            saveNpcGenButton = new Button { Text = "Save npcgen", Dock = DockStyle.Fill };
            openFileButton = new Button { Text = "Open file", Dock = DockStyle.Right, Width = 118, Enabled = false };
            applyScriptButton.Click += applyScriptButton_Click;
            applyControllerButton.Click += applyControllerButton_Click;
            allRealmsButton.Click += allRealmsButton_Click;
            saveNpcGenButton.Click += saveNpcGenButton_Click;
            openFileButton.Click += openFileButton_Click;
            footer.Controls.Add(new Label { Text = "Realm:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            footer.Controls.Add(realmBox, 1, 0);
            footer.Controls.Add(applyScriptButton, 2, 0);
            footer.Controls.Add(applyControllerButton, 3, 0);
            footer.Controls.Add(allRealmsButton, 4, 0);
            footer.Controls.Add(saveNpcGenButton, 5, 0);
            footer.Controls.Add(openFileButton, 6, 0);
            root.Controls.Add(footer, 0, 3);
        }

        private void LoadSpawnContext()
        {
            linkedGrid.Rows.Clear();
            foreach (NpcGenArea area in GetControlledAreas(controller.Id))
            {
                NpcGenEntry first = area.Entries.FirstOrDefault();
                int id = first != null ? first.Id : 0;
                NpcGenEntityInfo info = ResolveEntity(id);
                linkedGrid.Rows.Add("NPC group", data.Areas.IndexOf(area), id, string.IsNullOrWhiteSpace(info.Name) ? "NONE!" : info.Name, area.Entries.Count);
            }

            rulesGrid.Rows.Clear();
            List<SpawnRealmRule> rules = BuildRealmRules();
            foreach (SpawnRealmRule rule in rules)
            {
                int row = rulesGrid.Rows.Add(rule.Source, rule.RealmText, rule.FileName, rule.LineNumber > 0 ? rule.LineNumber.ToString(CultureInfo.InvariantCulture) : string.Empty, rule.Code);
                rulesGrid.Rows[row].Tag = rule;
                if (rule.Kind == SpawnRealmRuleKind.ScriptRealm)
                {
                    rulesGrid.Rows[row].DefaultCellStyle.Font = new Font(rulesGrid.Font, FontStyle.Bold);
                }
            }

            SpawnRealmRule effective = rules.FirstOrDefault(rule => rule.Kind == SpawnRealmRuleKind.ScriptRealm)
                ?? rules.FirstOrDefault(rule => rule.Kind == SpawnRealmRuleKind.TemplateMask)
                ?? rules.FirstOrDefault(rule => rule.Kind == SpawnRealmRuleKind.ControllerMask);
            summaryLabel.Text = effective == null
                ? "Effective realm: unknown"
                : "Effective realm: " + effective.RealmText + " (" + effective.Source + ")";
            diagnosticLabel.Text = BuildDiagnostic(rules);
        }

        private List<SpawnRealmRule> BuildRealmRules()
        {
            List<SpawnRealmRule> rules = new List<SpawnRealmRule>();
            rules.Add(new SpawnRealmRule
            {
                Kind = SpawnRealmRuleKind.ControllerMask,
                Source = "Controller mask",
                RealmText = FormatMask(controller.ZoneMask),
                Code = "Zone mask = " + controller.ZoneMask.ToString(CultureInfo.InvariantCulture)
            });

            AddTemplateRules(rules);

            List<NpcGenScriptReferenceInfo> directReferences = scriptReferenceService.FindReferences(
                data.FilePath,
                controller.ControllerId,
                GetEntityIdsForController(controller.Id));
            AddScriptRules(rules, "Direct script", directReferences);
            AddParentScriptRules(rules, directReferences);

            return rules;
        }

        private void AddTemplateRules(List<SpawnRealmRule> rules)
        {
            if (sessionService == null)
            {
                return;
            }

            foreach (int entityId in GetEntityIdsForController(controller.Id))
            {
                NpcGenEntityInfo info = ResolveEntity(entityId);
                if (info == null || !info.ZoneMask.HasValue)
                {
                    continue;
                }

                rules.Add(new SpawnRealmRule
                {
                    Kind = SpawnRealmRuleKind.TemplateMask,
                    Source = "Template " + entityId.ToString(CultureInfo.InvariantCulture),
                    RealmText = FormatMask(info.ZoneMask.Value),
                    Code = "server_zone_mask = " + info.ZoneMask.Value.ToString(CultureInfo.InvariantCulture)
                });
            }
        }

        private void AddParentScriptRules(List<SpawnRealmRule> rules, List<NpcGenScriptReferenceInfo> directReferences)
        {
            if (sessionService == null)
            {
                return;
            }

            HashSet<string> emitted = new HashSet<string>(rules.Select(BuildRuleKey), StringComparer.OrdinalIgnoreCase);
            foreach (int aiScriptId in directReferences
                .Select(reference => TryGetAiScriptId(reference.FileName))
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .Distinct())
            {
                HashSet<int> ownerIds = new HashSet<int>(entityLookupService
                    .FindByAiScript(sessionService.ListCollection, sessionService.Database, aiScriptId)
                    .Select(info => info.Id));

                foreach (string scriptPath in directReferences
                    .Where(reference => TryGetAiScriptId(reference.FileName) == aiScriptId)
                    .Select(reference => reference.FilePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    foreach (int registeredId in FindRegisteredNameIds(scriptPath))
                    {
                        ownerIds.Add(registeredId);
                        rules.Add(new SpawnRealmRule
                        {
                            Kind = SpawnRealmRuleKind.Diagnostic,
                            Source = "AI owner hint",
                            RealmText = "n/a",
                            FileName = Path.GetFileName(scriptPath),
                            FilePath = scriptPath,
                            Code = "RegisterNameID(" + registeredId.ToString(CultureInfo.InvariantCulture) + ")"
                        });
                    }
                }

                foreach (NpcGenArea ownerArea in data.Areas.Where(area => area.Entries.Any(entry => ownerIds.Contains(entry.Id))))
                {
                    NpcGenController parentController = FindControllerByLinkId(ownerArea.ControllerId);
                    if (parentController == null || parentController.Id == controller.Id)
                    {
                        continue;
                    }

                    rules.Add(new SpawnRealmRule
                    {
                        Kind = SpawnRealmRuleKind.Diagnostic,
                        Source = "Parent controller",
                        RealmText = "n/a",
                        Code = string.Format(CultureInfo.InvariantCulture, "Link {0} / Trigger {1}", parentController.Id, parentController.ControllerId)
                    });

                    List<NpcGenScriptReferenceInfo> parentReferences = scriptReferenceService.FindReferences(
                        data.FilePath,
                        parentController.ControllerId,
                        ownerArea.Entries.Select(entry => entry.Id));
                    foreach (SpawnRealmRule rule in BuildScriptRules("Parent script", parentReferences))
                    {
                        if (emitted.Add(BuildRuleKey(rule)))
                        {
                            rules.Add(rule);
                        }
                    }
                }
            }
        }

        private void AddScriptRules(List<SpawnRealmRule> rules, string source, List<NpcGenScriptReferenceInfo> references)
        {
            foreach (SpawnRealmRule rule in BuildScriptRules(source, references))
            {
                rules.Add(rule);
            }
        }

        private static IEnumerable<SpawnRealmRule> BuildScriptRules(string source, IEnumerable<NpcGenScriptReferenceInfo> references)
        {
            foreach (NpcGenScriptReferenceInfo reference in references ?? Enumerable.Empty<NpcGenScriptReferenceInfo>())
            {
                int conditionLineNumber;
                string conditionCode;
                int? realm = FindRealmCondition(reference, out conditionLineNumber, out conditionCode);
                if (!realm.HasValue)
                {
                    continue;
                }

                yield return new SpawnRealmRule
                {
                    Kind = SpawnRealmRuleKind.ScriptRealm,
                    Source = source,
                    Realm = realm,
                    RealmText = "Realm " + realm.Value.ToString(CultureInfo.InvariantCulture),
                    FileName = reference.FileName,
                    FilePath = reference.FilePath,
                    LineNumber = conditionLineNumber,
                    Code = conditionCode
                };
            }
        }

        private void applyScriptButton_Click(object sender, EventArgs e)
        {
            SpawnRealmRule rule = GetSelectedRule();
            if (rule == null || rule.Kind != SpawnRealmRuleKind.ScriptRealm || string.IsNullOrWhiteSpace(rule.FilePath))
            {
                return;
            }

            Encoding encoding;
            string[] lines = ReadScriptLines(rule.FilePath, out encoding);
            int index = rule.LineNumber - 1;
            if (index < 0 || index >= lines.Length)
            {
                return;
            }

            string original = lines[index];
            string updated = Regex.Replace(
                original,
                @"(?<name>serverid|zoneid)\s*={2}\s*\d+",
                match => match.Groups["name"].Value + " == " + ((int)realmBox.Value).ToString(CultureInfo.InvariantCulture),
                RegexOptions.IgnoreCase);
            if (string.Equals(original, updated, StringComparison.Ordinal))
            {
                MessageBox.Show(this, "Selected line does not contain a direct serverid/zoneid comparison.", "Spawn Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            WriteScriptLines(rule.FilePath, lines.Select((line, i) => i == index ? updated : line).ToArray(), encoding);
            LoadSpawnContext();
        }

        private void applyControllerButton_Click(object sender, EventArgs e)
        {
            controller.ZoneMask = 1L << ((int)realmBox.Value - 1);
            LoadSpawnContext();
        }

        private void allRealmsButton_Click(object sender, EventArgs e)
        {
            controller.ZoneMask = -1;
            LoadSpawnContext();
        }

        private void saveNpcGenButton_Click(object sender, EventArgs e)
        {
            fileService.Save(data, data.FilePath);
            MessageBox.Show(this, "npcgen.data saved.", "Spawn Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void openFileButton_Click(object sender, EventArgs e)
        {
            SpawnRealmRule rule = GetSelectedRule();
            if (rule == null || string.IsNullOrWhiteSpace(rule.FilePath) || !File.Exists(rule.FilePath))
            {
                return;
            }

            Process.Start(new ProcessStartInfo { FileName = rule.FilePath, UseShellExecute = true });
        }

        private void rulesGrid_SelectionChanged(object sender, EventArgs e)
        {
            SpawnRealmRule rule = GetSelectedRule();
            bool hasFile = rule != null && !string.IsNullOrWhiteSpace(rule.FilePath) && File.Exists(rule.FilePath);
            openFileButton.Enabled = hasFile;
            applyScriptButton.Enabled = rule != null && rule.Kind == SpawnRealmRuleKind.ScriptRealm && hasFile;
            if (rule != null && rule.Realm.HasValue)
            {
                realmBox.Value = Math.Max(realmBox.Minimum, Math.Min(realmBox.Maximum, rule.Realm.Value));
            }
        }

        private SpawnRealmRule GetSelectedRule()
        {
            return rulesGrid.CurrentRow == null ? null : rulesGrid.CurrentRow.Tag as SpawnRealmRule;
        }

        private string BuildDiagnostic(List<SpawnRealmRule> rules)
        {
            if (rules.Any(rule => rule.Kind == SpawnRealmRuleKind.ScriptRealm))
            {
                return "A script realm rule was found. Change the selected Lua line or set the controller mask below.";
            }
            if (rules.Any(rule => rule.Kind == SpawnRealmRuleKind.Diagnostic))
            {
                return "Chain loaded, but no direct serverid/zoneid comparison was found on the activation lines.";
            }
            return "No script chain was found. This spawn currently depends on controller/template masks only.";
        }

        private IEnumerable<NpcGenArea> GetControlledAreas(int controllerId)
        {
            return data == null ? Enumerable.Empty<NpcGenArea>() : data.Areas.Where(area => area.ControllerId == controllerId);
        }

        private IEnumerable<int> GetEntityIdsForController(int controllerId)
        {
            return GetControlledAreas(controllerId)
                .SelectMany(area => area.Entries)
                .Select(entry => entry.Id)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
        }

        private NpcGenController FindControllerByLinkId(int linkId)
        {
            return data == null ? null : data.Controllers.FirstOrDefault(item => item.Id == linkId);
        }

        private NpcGenEntityInfo ResolveEntity(int id)
        {
            return entityLookupService.Resolve(sessionService != null ? sessionService.ListCollection : null, sessionService != null ? sessionService.Database : null, id);
        }

        private static IEnumerable<int> FindRegisteredNameIds(string scriptPath)
        {
            if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            {
                return Enumerable.Empty<int>();
            }

            List<int> ids = new List<int>();
            foreach (string line in File.ReadLines(scriptPath))
            {
                Match match = Regex.Match(line, @"RegisterNameID\s*\(\s*(?<id>\d+)\s*\)", RegexOptions.IgnoreCase);
                int id;
                if (match.Success && int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                {
                    ids.Add(id);
                }
            }

            return ids.Distinct().ToArray();
        }

        private static int? ExtractRealmFromHint(string hint)
        {
            if (string.IsNullOrWhiteSpace(hint))
            {
                return null;
            }

            Match match = Regex.Match(hint, @"realm condition:\s*(?<realm>\d+)", RegexOptions.IgnoreCase);
            int realm;
            return match.Success && int.TryParse(match.Groups["realm"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out realm)
                ? (int?)realm
                : null;
        }

        private static int? FindRealmCondition(NpcGenScriptReferenceInfo reference, out int lineNumber, out string code)
        {
            lineNumber = reference != null ? reference.LineNumber : 0;
            code = reference != null ? reference.Code : string.Empty;
            int? hintedRealm = ExtractRealmFromHint(reference != null ? reference.Hint : string.Empty);
            if (reference == null || string.IsNullOrWhiteSpace(reference.FilePath) || !File.Exists(reference.FilePath))
            {
                return hintedRealm;
            }

            Encoding encoding;
            string[] lines = ReadScriptLines(reference.FilePath, out encoding);
            int start = Math.Max(0, reference.LineNumber - 13);
            int end = Math.Min(lines.Length - 1, reference.LineNumber - 1);
            for (int i = end; i >= start; i--)
            {
                Match match = Regex.Match(lines[i], @"(?:serverid|zoneid)\s*={2}\s*(?<realm>\d+)", RegexOptions.IgnoreCase);
                int realm;
                if (match.Success && int.TryParse(match.Groups["realm"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out realm))
                {
                    lineNumber = i + 1;
                    code = lines[i].Trim();
                    return realm;
                }
            }

            return hintedRealm;
        }

        private static string[] ReadScriptLines(string path, out Encoding encoding)
        {
            encoding = DetectScriptEncoding(path);
            return File.ReadAllLines(path, encoding);
        }

        private static void WriteScriptLines(string path, string[] lines, Encoding encoding)
        {
            File.WriteAllLines(path, lines, encoding ?? DetectScriptEncoding(path));
        }

        private static Encoding DetectScriptEncoding(string path)
        {
            byte[] bom = File.ReadAllBytes(path).Take(3).ToArray();
            if (bom.Length >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                return new UTF8Encoding(true);
            }
            if (bom.Length >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
            {
                return Encoding.Unicode;
            }
            if (bom.Length >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }

            try
            {
                return Encoding.GetEncoding("GBK");
            }
            catch
            {
                return Encoding.Default;
            }
        }

        private static int? TryGetAiScriptId(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            Match match = Regex.Match(fileName, @"^ai(?<id>\d+)\.lua$", RegexOptions.IgnoreCase);
            int id;
            return match.Success && int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                ? (int?)id
                : null;
        }

        private static string FormatMask(long mask)
        {
            if (mask == -1)
            {
                return "All realms";
            }
            if (mask == 0)
            {
                return "No realm";
            }

            return string.Join(
                ", ",
                Enumerable.Range(1, 32)
                    .Where(realm => (mask & (1L << (realm - 1))) != 0)
                    .Select(realm => "Realm " + realm.ToString(CultureInfo.InvariantCulture))
                    .ToArray());
        }

        private static string BuildRuleKey(SpawnRealmRule rule)
        {
            return rule == null
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}", rule.Kind, rule.FilePath, rule.LineNumber, rule.Code);
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.FromArgb(17, 21, 26);
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.Dock = DockStyle.Fill;
            grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            grid.GridColor = Color.FromArgb(45, 52, 61);
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 28;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            return grid;
        }

        private static void ApplyDarkTheme(Control root)
        {
            root.BackColor = Color.FromArgb(13, 17, 22);
            root.ForeColor = Color.White;
            foreach (Control child in root.Controls)
            {
                ApplyDarkTheme(child);
                DataGridView grid = child as DataGridView;
                if (grid != null)
                {
                    grid.DefaultCellStyle.BackColor = Color.FromArgb(17, 21, 26);
                    grid.DefaultCellStyle.ForeColor = Color.White;
                    grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(69, 117, 157);
                    grid.DefaultCellStyle.SelectionForeColor = Color.White;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(31, 37, 45);
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
                    grid.EnableHeadersVisualStyles = false;
                }
            }
        }

        private enum SpawnRealmRuleKind
        {
            ControllerMask,
            TemplateMask,
            ScriptRealm,
            Diagnostic
        }

        private sealed class SpawnRealmRule
        {
            public SpawnRealmRuleKind Kind { get; set; }
            public string Source { get; set; }
            public int? Realm { get; set; }
            public string RealmText { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
            public string Code { get; set; }
        }
    }
}
