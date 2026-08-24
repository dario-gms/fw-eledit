using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace FWEledit
{
    public sealed class EditableTitleDefinition
    {
        public int Id { get; set; }
        public string TitleText { get; set; }
        public string AccentHex { get; set; }
        public string Description { get; set; }
        public string[] AddonDescriptions { get; set; }
        public string IconPath { get; set; }
        public bool IsGraphicTitle { get; set; }
        public bool ShowGraphicInChat { get; set; }
        public string CategoryPathKey { get; set; }
        public string CategoryDisplay { get; set; }
    }

    public sealed class TitleCategoryOption
    {
        public string PathKey { get; set; }
        public string Display { get; set; }
        public int TitleCount { get; set; }
    }

    public static class TitleDefinitionCatalog
    {
        public const int TargetListIndex = -3;

        private static readonly object SyncRoot = new object();
        private const string LuaStringPattern = "(?:\"(?<value>(?:\\\\.|[^\"])*)\"|'(?<singleValue>(?:\\\\.|[^'])*)'|\\[(?<equals>=*)\\[(?<longValue>.*?)\\]\\k<equals>\\])";
        private static readonly Regex EntryRegex = new Regex(
            "title_definition\\[\\s*\"?(?<id>\\d+)\"?\\s*\\]\\s*=\\s*\\{(?<body>.*?)\\}",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex NoteRegex = new Regex(
            "note\\s*=\\s*" + LuaStringPattern,
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex DescriptionRegex = new Regex(
            "desc\\s*=\\s*" + LuaStringPattern,
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex AddonDescriptionRegex = new Regex(
            "addon_desc\\d+\\s*=\\s*" + LuaStringPattern,
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex IconBlockRegex = new Regex(
            "title_definition\\[\\s*\"?(?<id>\\d+)\"?\\s*\\]\\.icon\\s*=\\s*\\{(?<body>.*?)\\}",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex IconImageRegex = new Regex(
            "(?<![A-Za-z0-9_])image\\s*=\\s*" + LuaStringPattern,
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex BeforeNameRegex = new Regex(
            "title_definition\\[\\s*\"?(?<id>\\d+)\"?\\s*\\]\\.beforename\\s*=\\s*(?<value>\\d+)",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex DefinitionAssignmentStartRegex = new Regex(
            "title_definition\\s*\\[\\s*(?:\"(?<id>\\d+)\"|'(?<id>\\d+)'|(?<id>\\d+))\\s*\\]\\s*=\\s*\\{",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex AggregateDefinitionStartRegex = new Regex(
            "title_definition\\s*=\\s*\\{",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex AggregateDefinitionEntryStartRegex = new Regex(
            "\\[\\s*(?:\"(?<id>\\d+)\"|'(?<id>\\d+)'|(?<id>\\d+))\\s*\\]\\s*=\\s*\\{",
            RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex LeadingColorRegex = new Regex(
            "\\^(?<hex>[0-9a-fA-F]{6})",
            RegexOptions.Compiled);
        private static readonly string[] TitleDefinitionLuaCandidates = new string[]
        {
            "config\\title_def_u.lua",
            "config\\Title_Def_U.lua",
            "config\\title_def.lua",
            "title_def_u.lua",
            "title\\title_def_u.lua"
        };
        private static string cachedGameRoot = string.Empty;
        private static string cachedLuaText = string.Empty;
        private static string cachedLoadStatus = string.Empty;
        private static bool cacheInitialized;
        private static List<ItemReferenceOption> cachedOptions = new List<ItemReferenceOption>();
        private static Dictionary<int, ItemReferenceOption> cachedById = new Dictionary<int, ItemReferenceOption>();
        private static Dictionary<string, ItemReferenceOption> cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<int, EditableTitleDefinition> cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();
        private static List<object> cachedTitleDir = new List<object>();
        private static List<TitleCategoryOption> cachedCategories = new List<TitleCategoryOption>();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_open(string pckFile);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_close();

        [DllImport("pckdll_x64.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr pck_getFileEntryByPath(string pathInPck);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong pck_getFileSizeInEntry(IntPtr entry);

        [DllImport("pckdll_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int pck_GetSingleFileData(IntPtr entry, byte[] buffer, UIntPtr sizeOfBuffer);

        public static List<ItemReferenceOption> BuildOptions()
        {
            EnsureCache();
            lock (SyncRoot)
            {
                return CloneOptions(cachedOptions);
            }
        }

        public static bool TryGetOptionById(int id, out ItemReferenceOption option)
        {
            option = null;
            if (id <= 0)
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                return cachedById.TryGetValue(id, out option);
            }
        }

        public static bool TryGetOptionByName(string name, out ItemReferenceOption option)
        {
            option = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                return cachedByName.TryGetValue(name.Trim(), out option);
            }
        }

        public static bool TryGetEditableDefinition(int id, out EditableTitleDefinition definition)
        {
            definition = null;
            if (id <= 0)
            {
                return false;
            }

            EnsureCache();
            lock (SyncRoot)
            {
                if (!cachedDefinitions.TryGetValue(id, out EditableTitleDefinition cached))
                {
                    return false;
                }

                definition = CloneDefinition(cached);
                return definition != null;
            }
        }

        public static List<EditableTitleDefinition> BuildEditableDefinitions()
        {
            EnsureCache();
            lock (SyncRoot)
            {
                List<EditableTitleDefinition> definitions = new List<EditableTitleDefinition>();
                foreach (EditableTitleDefinition definition in cachedDefinitions.Values)
                {
                    EditableTitleDefinition clone = CloneDefinition(definition);
                    if (clone != null)
                    {
                        definitions.Add(clone);
                    }
                }

                definitions.Sort(delegate (EditableTitleDefinition left, EditableTitleDefinition right)
                {
                    int leftId = left != null ? left.Id : 0;
                    int rightId = right != null ? right.Id : 0;
                    return leftId.CompareTo(rightId);
                });
                return definitions;
            }
        }

        public static List<TitleCategoryOption> BuildCategoryOptions()
        {
            EnsureCache();
            lock (SyncRoot)
            {
                List<TitleCategoryOption> categories = new List<TitleCategoryOption>();
                foreach (TitleCategoryOption category in cachedCategories)
                {
                    if (category == null)
                    {
                        continue;
                    }

                    categories.Add(new TitleCategoryOption
                    {
                        PathKey = category.PathKey ?? string.Empty,
                        Display = category.Display ?? string.Empty,
                        TitleCount = category.TitleCount
                    });
                }

                return categories;
            }
        }

        public static string GetLoadStatus()
        {
            EnsureCache();
            lock (SyncRoot)
            {
                return cachedLoadStatus ?? string.Empty;
            }
        }

        public static void InvalidateCache()
        {
            lock (SyncRoot)
            {
                cacheInitialized = false;
                cachedGameRoot = string.Empty;
                cachedLuaText = string.Empty;
                cachedLoadStatus = string.Empty;
                cachedOptions = new List<ItemReferenceOption>();
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();
                cachedTitleDir = new List<object>();
                cachedCategories = new List<TitleCategoryOption>();
            }
        }

        public static bool SaveEditableDefinition(EditableTitleDefinition definition, AssetManager assetManager, out string error)
        {
            return SaveEditableDefinitions(new List<EditableTitleDefinition> { definition }, new List<int>(), assetManager, out error);
        }

        public static bool SaveEditableDefinitions(
            IEnumerable<EditableTitleDefinition> definitions,
            IEnumerable<int> deletedIds,
            AssetManager assetManager,
            out string error)
        {
            error = string.Empty;
            if (assetManager == null)
            {
                error = "Asset manager is not available.";
                return false;
            }

            EnsureCache();

            string updatedLuaText;
            lock (SyncRoot)
            {
                updatedLuaText = cachedLuaText ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(updatedLuaText))
            {
                error = "Unable to load title definitions from script.pck.";
                return false;
            }

            if (deletedIds != null)
            {
                foreach (int deletedId in deletedIds)
                {
                    if (deletedId <= 0)
                    {
                        continue;
                    }

                    string entryPattern = BuildTitleEntryPattern(deletedId);
                    updatedLuaText = Regex.Replace(updatedLuaText, entryPattern + "\\s*", string.Empty, RegexOptions.Singleline);
                    updatedLuaText = RemoveIconBlock(updatedLuaText, deletedId);
                    updatedLuaText = RemoveBeforeNameLine(updatedLuaText, deletedId);
                }
            }

            if (definitions != null)
            {
                foreach (EditableTitleDefinition definition in definitions)
                {
                    if (definition == null || definition.Id <= 0)
                    {
                        error = "Invalid title definition.";
                        return false;
                    }

                    string updatedBlock = BuildDefinitionBlock(definition);
                    string pattern = BuildTitleEntryPattern(definition.Id);
                    if (Regex.IsMatch(updatedLuaText, pattern, RegexOptions.Singleline))
                    {
                        updatedLuaText = Regex.Replace(updatedLuaText, pattern, updatedBlock, RegexOptions.Singleline);
                    }
                    else
                    {
                        int insertAt = updatedLuaText.IndexOf("title_dir = {", StringComparison.Ordinal);
                        updatedLuaText = insertAt >= 0
                            ? updatedLuaText.Insert(insertAt, updatedBlock + Environment.NewLine + Environment.NewLine)
                            : updatedLuaText + Environment.NewLine + Environment.NewLine + updatedBlock + Environment.NewLine;
                    }

                    updatedLuaText = UpsertIconBlock(updatedLuaText, definition);
                }
            }

            List<object> workingTitleDir;
            lock (SyncRoot)
            {
                workingTitleDir = CloneTitleDir(cachedTitleDir);
            }
            if (workingTitleDir.Count > 0)
            {
                if (deletedIds != null)
                {
                    foreach (int deletedId in deletedIds)
                    {
                        RemoveTitleIdFromTitleDir(workingTitleDir, deletedId);
                    }
                }

                if (definitions != null)
                {
                    foreach (EditableTitleDefinition definition in definitions)
                    {
                        if (definition == null || definition.Id <= 0)
                        {
                            continue;
                        }

                        RemoveTitleIdFromTitleDir(workingTitleDir, definition.Id);
                        if (!string.IsNullOrWhiteSpace(definition.CategoryPathKey))
                        {
                            AddTitleIdToCategory(workingTitleDir, definition.CategoryPathKey, definition.Id);
                        }
                    }
                }

                updatedLuaText = ReplaceTitleDir(updatedLuaText, workingTitleDir);
            }

            return WriteLuaTextToScript(updatedLuaText, assetManager, out error);
        }

        private static bool WriteLuaTextToScript(string updatedLuaText, AssetManager assetManager, out string error)
        {
            error = string.Empty;
            string tempSourcePath = Path.Combine(Path.GetTempPath(), "FWEledit", "lua-cache", "title_def_u_edit.lua");
            Directory.CreateDirectory(Path.GetDirectoryName(tempSourcePath) ?? Path.GetTempPath());
            File.WriteAllText(tempSourcePath, updatedLuaText, Encoding.GetEncoding("GBK"));

            string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-stage", "script-title");
            try
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, true);
                }
            }
            catch
            { }

            string configDirectory = Path.Combine(stagingRoot, "config");
            Directory.CreateDirectory(configDirectory);
            string targetPath = Path.Combine(configDirectory, "title_def_u.lua");

            if (!CompileLuaFile(tempSourcePath, targetPath, out error))
            {
                return false;
            }

            string compiledLuaText = DecompileLuaFile(targetPath);
            Dictionary<int, string> compiledIconPathsById = ParseGraphicIconPaths(compiledLuaText);
            Dictionary<int, bool> compiledBeforeNameById = ParseGraphicBeforeNameFlags(compiledLuaText);
            List<object> compiledTitleDir = ParseTitleDir(compiledLuaText);
            Dictionary<int, EditableTitleDefinition> compiledDefinitions = ParseEditableDefinitions(compiledLuaText, compiledIconPathsById, compiledBeforeNameById);
            if (string.IsNullOrWhiteSpace(compiledLuaText) || compiledDefinitions.Count == 0)
            {
                error = "The compiled title_def_u.lua could not be read back. Save was cancelled so script.pck stays unchanged.";
                return false;
            }

            if (!assetManager.ImportStagedPackageAssets("script", stagingRoot, out string applySummary))
            {
                error = string.IsNullOrWhiteSpace(applySummary)
                    ? "Unable to update script.pck."
                    : applySummary;
                return false;
            }

            InvalidateCache();
            string savedLuaText = LoadTitleDefinitionLua(AssetManager.GameRootPath);
            Dictionary<int, string> savedIconPathsById = ParseGraphicIconPaths(savedLuaText);
            Dictionary<int, bool> savedBeforeNameById = ParseGraphicBeforeNameFlags(savedLuaText);
            List<object> titleDir = ParseTitleDir(savedLuaText);
            Dictionary<int, EditableTitleDefinition> updatedDefinitions = ParseEditableDefinitions(savedLuaText, savedIconPathsById, savedBeforeNameById);
            if (string.IsNullOrWhiteSpace(savedLuaText) || updatedDefinitions.Count == 0)
            {
                TryRestoreScriptBackup(assetManager);
                error = "script.pck was updated but title_def_u.lua could not be loaded back. The previous script.pck backup was restored.";
                return false;
            }

            List<TitleCategoryOption> categories = BuildTitleCategoryOptions(titleDir);
            ApplyCategoriesToDefinitions(updatedDefinitions, titleDir, categories);
            List<ItemReferenceOption> updatedOptions = BuildOptionsFromDefinitions(updatedDefinitions);
            lock (SyncRoot)
            {
                cachedLuaText = savedLuaText;
                cachedDefinitions = updatedDefinitions;
                cachedTitleDir = titleDir;
                cachedCategories = categories;
                cachedOptions = updatedOptions;
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cachedOptions.Count; i++)
                {
                    ItemReferenceOption option = cachedOptions[i];
                    if (option == null)
                    {
                        continue;
                    }

                    if (!cachedById.ContainsKey(option.Id))
                    {
                        cachedById.Add(option.Id, option);
                    }
                    if (!string.IsNullOrWhiteSpace(option.Name) && !cachedByName.ContainsKey(option.Name))
                    {
                        cachedByName.Add(option.Name, option);
                    }
                }
            }

            return true;
        }

        private static void TryRestoreScriptBackup(AssetManager assetManager)
        {
            try
            {
                string gameRoot = AssetManager.GameRootPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(gameRoot))
                {
                    return;
                }

                string resources = Path.Combine(gameRoot, "resources");
                string scriptPck = Path.Combine(resources, "script.pck");
                string backupPck = scriptPck + ".bak";
                if (File.Exists(backupPck))
                {
                    File.Copy(backupPck, scriptPck, true);
                }

                string scriptPkx = Path.Combine(resources, "script.pkx");
                string backupPkx = scriptPkx + ".bak";
                if (File.Exists(backupPkx))
                {
                    File.Copy(backupPkx, scriptPkx, true);
                }

                PckEntryReaderService.InvalidatePackageGlobally("script");
                InvalidateCache();
            }
            catch
            {
            }
        }

        public static string FormatDisplay(string rawValue)
        {
            int id;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id <= 0)
            {
                return rawValue ?? string.Empty;
            }

            ItemReferenceOption option;
            if (TryGetOptionById(id, out option) && option != null && !string.IsNullOrWhiteSpace(option.Name))
            {
                return option.Name;
            }

            return rawValue ?? string.Empty;
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int numericValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            ItemReferenceOption option;
            if (TryGetOptionByName(trimmed, out option) && option != null)
            {
                return option.Id.ToString(CultureInfo.InvariantCulture);
            }

            int separator = trimmed.IndexOf(" - ", StringComparison.OrdinalIgnoreCase);
            if (separator > 0)
            {
                string leadingId = trimmed.Substring(0, separator).Trim();
                if (int.TryParse(leadingId, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
                {
                    return numericValue.ToString(CultureInfo.InvariantCulture);
                }
            }

            return trimmed;
        }

        private static void EnsureCache()
        {
            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            lock (SyncRoot)
            {
                if (cacheInitialized && string.Equals(cachedGameRoot, gameRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                cachedGameRoot = gameRoot;
                cacheInitialized = true;
                cachedLuaText = string.Empty;
                cachedOptions = new List<ItemReferenceOption>();
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                cachedDefinitions = new Dictionary<int, EditableTitleDefinition>();
                cachedTitleDir = new List<object>();
                cachedCategories = new List<TitleCategoryOption>();
            }

            string loadStatus;
            string luaText = LoadTitleDefinitionLua(gameRoot, out loadStatus);
            Dictionary<int, string> iconPathsById = ParseGraphicIconPaths(luaText);
            Dictionary<int, bool> beforeNameById = ParseGraphicBeforeNameFlags(luaText);
            List<object> titleDir = ParseTitleDir(luaText);
            Dictionary<int, EditableTitleDefinition> parsedDefinitions = ParseEditableDefinitions(luaText, iconPathsById, beforeNameById);
            if (string.IsNullOrWhiteSpace(loadStatus))
            {
                loadStatus = parsedDefinitions.Count > 0
                    ? "Loaded title_def_u.lua from script.pck."
                    : "Loaded Lua data from script.pck, but no title_definition entries were recognized.";
            }
            List<TitleCategoryOption> parsedCategories = BuildTitleCategoryOptions(titleDir);
            ApplyCategoriesToDefinitions(parsedDefinitions, titleDir, parsedCategories);
            List<ItemReferenceOption> parsedOptions = BuildOptionsFromDefinitions(parsedDefinitions);

            lock (SyncRoot)
            {
                cachedLuaText = luaText ?? string.Empty;
                cachedLoadStatus = loadStatus ?? string.Empty;
                cachedDefinitions = parsedDefinitions;
                cachedTitleDir = titleDir;
                cachedCategories = parsedCategories;
                cachedOptions = parsedOptions;
                cachedById = new Dictionary<int, ItemReferenceOption>();
                cachedByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cachedOptions.Count; i++)
                {
                    ItemReferenceOption option = cachedOptions[i];
                    if (option == null)
                    {
                        continue;
                    }

                    if (!cachedById.ContainsKey(option.Id))
                    {
                        cachedById.Add(option.Id, option);
                    }
                    if (!string.IsNullOrWhiteSpace(option.Name) && !cachedByName.ContainsKey(option.Name))
                    {
                        cachedByName.Add(option.Name, option);
                    }
                }
            }
        }

        private static string LoadTitleDefinitionLua(string gameRoot)
        {
            string ignored;
            return LoadTitleDefinitionLua(gameRoot, out ignored);
        }

        private static string LoadTitleDefinitionLua(string gameRoot, out string status)
        {
            status = string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                status = "Game root is not configured.";
                return string.Empty;
            }

            List<string> attempts = new List<string>();
            try
            {
                PckEntryReaderService reader = new PckEntryReaderService();
                byte[] payload;
                string error;
                for (int i = 0; i < TitleDefinitionLuaCandidates.Length; i++)
                {
                    string candidate = TitleDefinitionLuaCandidates[i];
                    if (reader.TryReadFile("script", candidate, out payload, out error)
                        && payload != null
                        && payload.Length > 0)
                    {
                        string tempDirectory = Path.Combine(Path.GetTempPath(), "FWEledit", "lua-cache");
                        Directory.CreateDirectory(tempDirectory);
                        string sourcePath = Path.Combine(tempDirectory, "script_pck_" + candidate.Replace('\\', '_').Replace('/', '_'));
                        File.WriteAllBytes(sourcePath, payload);
                        string decodeStatus;
                        string luaText = DecodeOrDecompileLuaFile(sourcePath, payload, out decodeStatus);
                        if (!string.IsNullOrWhiteSpace(luaText))
                        {
                            status = "Loaded " + candidate + " from script.pck. " + decodeStatus;
                            return luaText;
                        }

                        attempts.Add(candidate + ": " + decodeStatus);
                    }
                    else if (!string.IsNullOrWhiteSpace(error))
                    {
                        attempts.Add(candidate + ": " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                attempts.Add("managed script.pck reader: " + ex.Message);
            }

            try
            {
                byte[] payload;
                string entryPath;
                string winPckError;
                if (TryReadTitleDefinitionLuaWithWinPck(gameRoot, out payload, out entryPath, out winPckError)
                    && payload != null
                    && payload.Length > 0)
                {
                    string tempDirectory = Path.Combine(Path.GetTempPath(), "FWEledit", "lua-cache");
                    Directory.CreateDirectory(tempDirectory);
                    string sourcePath = Path.Combine(tempDirectory, "winpck_" + entryPath.Replace('\\', '_').Replace('/', '_'));
                    File.WriteAllBytes(sourcePath, payload);
                    string decodeStatus;
                    string luaText = DecodeOrDecompileLuaFile(sourcePath, payload, out decodeStatus);
                    if (!string.IsNullOrWhiteSpace(luaText))
                    {
                        status = "Loaded " + entryPath + " from script.pck using WinPCK. " + decodeStatus;
                        return luaText;
                    }

                    attempts.Add(entryPath + " via WinPCK: " + decodeStatus);
                }
                else if (!string.IsNullOrWhiteSpace(winPckError))
                {
                    attempts.Add("WinPCK reader: " + winPckError);
                }
            }
            catch (Exception ex)
            {
                attempts.Add("WinPCK reader: " + ex.Message);
            }

            status = attempts.Count > 0
                ? "Unable to load title definitions. " + string.Join(" | ", attempts.ToArray())
                : "Unable to load title definitions from script.pck.";
            return string.Empty;
        }

        private static bool TryReadTitleDefinitionLuaWithWinPck(string gameRoot, out byte[] payload, out string entryPath, out string error)
        {
            payload = null;
            entryPath = string.Empty;
            error = string.Empty;
            string scriptPck = Path.Combine(gameRoot ?? string.Empty, "resources", "script.pck");
            if (!File.Exists(scriptPck))
            {
                error = "script.pck was not found: " + scriptPck;
                return false;
            }

            string dllDirectory = FindWinPckDllDirectory();
            if (string.IsNullOrWhiteSpace(dllDirectory))
            {
                error = "pckdll_x64.dll was not found.";
                return false;
            }

            SetDllDirectory(dllDirectory);
            int openResult = pck_open(scriptPck);
            if (openResult != 0)
            {
                error = "WinPCK could not open script.pck. Code: " + openResult.ToString(CultureInfo.InvariantCulture);
                return false;
            }

            try
            {
                for (int i = 0; i < TitleDefinitionLuaCandidates.Length; i++)
                {
                    string candidate = TitleDefinitionLuaCandidates[i];
                    IntPtr entry = pck_getFileEntryByPath(candidate);
                    if (entry == IntPtr.Zero)
                    {
                        continue;
                    }

                    ulong size = pck_getFileSizeInEntry(entry);
                    if (size == 0 || size > int.MaxValue)
                    {
                        error = "Invalid size for " + candidate + ".";
                        return false;
                    }

                    byte[] buffer = new byte[(int)size];
                    int readResult = pck_GetSingleFileData(entry, buffer, new UIntPtr(size));
                    if (readResult != 0)
                    {
                        error = "WinPCK could not read " + candidate + ". Code: " + readResult.ToString(CultureInfo.InvariantCulture);
                        return false;
                    }

                    payload = buffer;
                    entryPath = candidate;
                    return true;
                }

                error = "None of the known title definition paths were found.";
                return false;
            }
            finally
            {
                pck_close();
            }
        }

        private static string FindWinPckDllDirectory()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string[] roots = new string[]
            {
                assemblyDirectory,
                AssetManager.WorkspaceRootPath,
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory),
                Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)),
                Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)))
            };

            for (int i = 0; i < roots.Length; i++)
            {
                string root = roots[i];
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }

                string[] candidates = new string[]
                {
                    Path.Combine(root, "tools", "FWPck", "pckdll_x64.dll"),
                    Path.Combine(root, "FWPck", "pckdll_x64.dll"),
                    Path.Combine(root, "pckdll_x64.dll")
                };
                for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
                {
                    string candidate = candidates[candidateIndex];
                    if (File.Exists(candidate))
                    {
                        return Path.GetDirectoryName(candidate);
                    }
                }
            }

            return string.Empty;
        }

        private static Dictionary<int, EditableTitleDefinition> ParseEditableDefinitions(
            string luaText,
            Dictionary<int, string> iconPathsById,
            Dictionary<int, bool> beforeNameById)
        {
            Dictionary<int, EditableTitleDefinition> definitions = new Dictionary<int, EditableTitleDefinition>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return definitions;
            }
            MatchCollection matches = DefinitionAssignmentStartRegex.Matches(luaText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || id <= 0)
                {
                    continue;
                }

                int braceStart = luaText.IndexOf('{', match.Index + match.Length - 1);
                int braceEnd = braceStart >= 0 ? FindMatchingBrace(luaText, braceStart) : -1;
                if (braceStart < 0 || braceEnd <= braceStart)
                {
                    continue;
                }

                string body = luaText.Substring(braceStart + 1, braceEnd - braceStart - 1);
                AddEditableDefinition(definitions, id, body, iconPathsById, beforeNameById);
            }

            if (definitions.Count == 0)
            {
                ParseAggregateEditableDefinitions(luaText, definitions, iconPathsById, beforeNameById);
            }

            return definitions;
        }

        private static void ParseAggregateEditableDefinitions(
            string luaText,
            Dictionary<int, EditableTitleDefinition> definitions,
            Dictionary<int, string> iconPathsById,
            Dictionary<int, bool> beforeNameById)
        {
            if (string.IsNullOrWhiteSpace(luaText) || definitions == null)
            {
                return;
            }

            Match tableMatch = AggregateDefinitionStartRegex.Match(luaText);
            if (!tableMatch.Success)
            {
                return;
            }

            int tableBraceStart = luaText.IndexOf('{', tableMatch.Index + tableMatch.Length - 1);
            int tableBraceEnd = tableBraceStart >= 0 ? FindMatchingBrace(luaText, tableBraceStart) : -1;
            if (tableBraceStart < 0 || tableBraceEnd <= tableBraceStart)
            {
                return;
            }

            string tableBody = luaText.Substring(tableBraceStart + 1, tableBraceEnd - tableBraceStart - 1);
            MatchCollection matches = AggregateDefinitionEntryStartRegex.Matches(tableBody);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || id <= 0)
                {
                    continue;
                }

                int braceStart = tableBody.IndexOf('{', match.Index + match.Length - 1);
                int braceEnd = braceStart >= 0 ? FindMatchingBrace(tableBody, braceStart) : -1;
                if (braceStart < 0 || braceEnd <= braceStart)
                {
                    continue;
                }

                string body = tableBody.Substring(braceStart + 1, braceEnd - braceStart - 1);
                AddEditableDefinition(definitions, id, body, iconPathsById, beforeNameById);
            }
        }

        private static void AddEditableDefinition(
            Dictionary<int, EditableTitleDefinition> definitions,
            int id,
            string body,
            Dictionary<int, string> iconPathsById,
            Dictionary<int, bool> beforeNameById)
        {
            if (definitions == null || id <= 0)
            {
                return;
            }

            string rawNote = ExtractFieldRaw(body, NoteRegex);
            string note = DecodeScriptLiteralPreserveFormatting(rawNote);
            string description = DecodeScriptLiteralPreserveFormatting(ExtractFieldRaw(body, DescriptionRegex));
            string iconPath;
            iconPathsById.TryGetValue(id, out iconPath);
            bool showGraphicInChat;
            beforeNameById.TryGetValue(id, out showGraphicInChat);
            string accentHex = ExtractLeadingColor(rawNote);
            EditableTitleDefinition definition = new EditableTitleDefinition
            {
                Id = id,
                AccentHex = accentHex,
                TitleText = StripLeadingColorCode(note),
                Description = description,
                AddonDescriptions = new string[5],
                IconPath = iconPath ?? string.Empty,
                IsGraphicTitle = !string.IsNullOrWhiteSpace(iconPath),
                ShowGraphicInChat = showGraphicInChat
            };

            for (int addonIndex = 1; addonIndex <= 5; addonIndex++)
            {
                definition.AddonDescriptions[addonIndex - 1] = DecodeScriptLiteralPreserveFormatting(
                    ExtractNamedStringFieldRaw(body, "addon_desc" + addonIndex.ToString(CultureInfo.InvariantCulture)));
            }

            definitions[id] = definition;
        }

        private static List<ItemReferenceOption> BuildOptionsFromDefinitions(Dictionary<int, EditableTitleDefinition> definitions)
        {
            List<ItemReferenceOption> options = new List<ItemReferenceOption>();
            if (definitions != null)
            {
                foreach (KeyValuePair<int, EditableTitleDefinition> pair in definitions)
                {
                    ItemReferenceOption option = BuildDisplayOption(pair.Value);
                    if (option != null)
                    {
                        options.Add(option);
                    }
                }
            }

            options.Sort(delegate (ItemReferenceOption left, ItemReferenceOption right)
            {
                string leftName = left != null ? left.Name ?? string.Empty : string.Empty;
                string rightName = right != null ? right.Name ?? string.Empty : string.Empty;
                int compare = string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
                if (compare != 0)
                {
                    return compare;
                }

                int leftId = left != null ? left.Id : 0;
                int rightId = right != null ? right.Id : 0;
                return leftId.CompareTo(rightId);
            });

            return options;
        }

        private static ItemReferenceOption BuildDisplayOption(EditableTitleDefinition definition)
        {
            if (definition == null || definition.Id <= 0)
            {
                return null;
            }

            string noteRaw = ComposeNoteRaw(definition);
            string label = CleanScriptText(noteRaw);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Title " + definition.Id.ToString(CultureInfo.InvariantCulture);
            }

            List<string> addonDescriptions = new List<string>();
            if (definition.AddonDescriptions != null)
            {
                for (int i = 0; i < definition.AddonDescriptions.Length; i++)
                {
                    string display = CleanScriptText(definition.AddonDescriptions[i]);
                    if (!string.IsNullOrWhiteSpace(display))
                    {
                        addonDescriptions.Add(display);
                    }
                }
            }

            string description = CleanScriptText(definition.Description);
            return new ItemReferenceOption
            {
                ListIndex = TargetListIndex,
                ElementIndex = -1,
                Id = definition.Id,
                Name = label,
                ListName = "Title definitions",
                IconKey = definition.IsGraphicTitle ? definition.IconPath ?? string.Empty : string.Empty,
                Quality = -1,
                Description = BuildTitleDetails(description, addonDescriptions, definition.IconPath, definition.AccentHex, definition.CategoryDisplay),
                SecondaryText = BuildSecondaryText(description, addonDescriptions, definition.IsGraphicTitle, definition.AccentHex, definition.CategoryDisplay),
                AccentHex = definition.AccentHex ?? string.Empty,
                Kind = definition.IsGraphicTitle ? "Graphic title" : "Title"
            };
        }

        private static Dictionary<int, string> ParseGraphicIconPaths(string luaText)
        {
            Dictionary<int, string> iconPaths = new Dictionary<int, string>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return iconPaths;
            }

            MatchCollection matches = IconBlockRegex.Matches(luaText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || id <= 0)
                {
                    continue;
                }

                string iconPath = DecodeScriptLiteralPreserveFormatting(ExtractFieldRaw(match.Groups["body"].Value ?? string.Empty, IconImageRegex));
                if (string.IsNullOrWhiteSpace(iconPath))
                {
                    continue;
                }

                iconPaths[id] = NormalizeImagePath(iconPath);
            }

            return iconPaths;
        }

        private static Dictionary<int, bool> ParseGraphicBeforeNameFlags(string luaText)
        {
            Dictionary<int, bool> flags = new Dictionary<int, bool>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return flags;
            }

            MatchCollection matches = BeforeNameRegex.Matches(luaText);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                int id;
                int value;
                if (match == null
                    || !int.TryParse(match.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                    || !int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                    || id <= 0)
                {
                    continue;
                }

                flags[id] = value != 0;
            }

            return flags;
        }

        private static List<object> ParseTitleDir(string luaText)
        {
            List<object> titleDir = new List<object>();
            if (string.IsNullOrWhiteSpace(luaText))
            {
                return titleDir;
            }

            int start = luaText.IndexOf("title_dir", StringComparison.Ordinal);
            if (start < 0)
            {
                return titleDir;
            }

            int braceStart = luaText.IndexOf('{', start);
            if (braceStart < 0)
            {
                return titleDir;
            }

            int braceEnd = FindMatchingBrace(luaText, braceStart);
            if (braceEnd <= braceStart)
            {
                return titleDir;
            }

            string block = luaText.Substring(braceStart, braceEnd - braceStart + 1);
            List<string> tokens = TokenizeLuaTable(block);
            int position = 0;
            object parsed = ParseLuaTable(tokens, ref position);
            List<object> parsedList = parsed as List<object>;
            return parsedList ?? titleDir;
        }

        private static List<TitleCategoryOption> BuildTitleCategoryOptions(List<object> titleDir)
        {
            List<TitleCategoryOption> categories = new List<TitleCategoryOption>();
            WalkTitleDirCategories(titleDir, string.Empty, string.Empty, categories, null);
            categories.Sort(delegate (TitleCategoryOption left, TitleCategoryOption right)
            {
                return string.Compare(left != null ? left.Display : string.Empty, right != null ? right.Display : string.Empty, StringComparison.OrdinalIgnoreCase);
            });
            return categories;
        }

        private static void ApplyCategoriesToDefinitions(
            Dictionary<int, EditableTitleDefinition> definitions,
            List<object> titleDir,
            List<TitleCategoryOption> categories)
        {
            if (definitions == null || titleDir == null)
            {
                return;
            }

            Dictionary<string, string> displayByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (categories != null)
            {
                foreach (TitleCategoryOption category in categories)
                {
                    if (category != null && !displayByPath.ContainsKey(category.PathKey ?? string.Empty))
                    {
                        displayByPath.Add(category.PathKey ?? string.Empty, category.Display ?? string.Empty);
                    }
                }
            }

            Dictionary<int, string> pathByTitle = new Dictionary<int, string>();
            WalkTitleDirCategories(titleDir, string.Empty, string.Empty, null, pathByTitle);
            foreach (KeyValuePair<int, EditableTitleDefinition> pair in definitions)
            {
                string pathKey;
                if (pathByTitle.TryGetValue(pair.Key, out pathKey))
                {
                    pair.Value.CategoryPathKey = pathKey;
                    string display;
                    pair.Value.CategoryDisplay = displayByPath.TryGetValue(pathKey, out display) ? display : string.Empty;
                }
                else
                {
                    pair.Value.CategoryPathKey = string.Empty;
                    pair.Value.CategoryDisplay = "! Uncategorized";
                }
            }
        }

        private static void WalkTitleDirCategories(
            List<object> node,
            string pathKey,
            string parentDisplay,
            List<TitleCategoryOption> categories,
            Dictionary<int, string> pathByTitle)
        {
            if (node == null)
            {
                return;
            }

            string categoryName = node.Count > 0 ? StripLeadingColorCode(node[0] as string ?? string.Empty) : string.Empty;
            string display = !string.IsNullOrWhiteSpace(categoryName)
                ? (string.IsNullOrWhiteSpace(parentDisplay) ? categoryName : parentDisplay + " > " + categoryName)
                : parentDisplay;
            if (!string.IsNullOrWhiteSpace(display) && categories != null)
            {
                categories.Add(new TitleCategoryOption
                {
                    PathKey = pathKey ?? string.Empty,
                    Display = display,
                    TitleCount = CountDirectTitleIds(node)
                });
            }

            for (int i = 0; i < node.Count; i++)
            {
                object item = node[i];
                if (item is int)
                {
                    int titleId = (int)item;
                    if (pathByTitle != null && !pathByTitle.ContainsKey(titleId))
                    {
                        pathByTitle.Add(titleId, pathKey ?? string.Empty);
                    }
                }
                else
                {
                    List<object> child = item as List<object>;
                    if (child != null)
                    {
                        string childPath = string.IsNullOrWhiteSpace(pathKey)
                            ? i.ToString(CultureInfo.InvariantCulture)
                            : pathKey + "." + i.ToString(CultureInfo.InvariantCulture);
                        WalkTitleDirCategories(child, childPath, display, categories, pathByTitle);
                    }
                }
            }
        }

        private static int CountDirectTitleIds(List<object> node)
        {
            int count = 0;
            if (node == null)
            {
                return count;
            }

            for (int i = 0; i < node.Count; i++)
            {
                if (node[i] is int)
                {
                    count++;
                }
            }

            return count;
        }

        private static string ReplaceTitleDir(string luaText, List<object> titleDir)
        {
            if (string.IsNullOrWhiteSpace(luaText) || titleDir == null || titleDir.Count == 0)
            {
                return luaText ?? string.Empty;
            }

            int start = luaText.IndexOf("title_dir", StringComparison.Ordinal);
            if (start < 0)
            {
                return luaText;
            }

            int braceStart = luaText.IndexOf('{', start);
            if (braceStart < 0)
            {
                return luaText;
            }

            int braceEnd = FindMatchingBrace(luaText, braceStart);
            if (braceEnd <= braceStart)
            {
                return luaText;
            }

            string serialized = "title_dir = " + SerializeLuaNode(titleDir, 1);
            return luaText.Substring(0, start) + serialized + luaText.Substring(braceEnd + 1);
        }

        private static void RemoveTitleIdFromTitleDir(List<object> node, int titleId)
        {
            if (node == null || titleId <= 0)
            {
                return;
            }

            for (int i = node.Count - 1; i >= 0; i--)
            {
                if (node[i] is int && (int)node[i] == titleId)
                {
                    node.RemoveAt(i);
                }
                else
                {
                    List<object> child = node[i] as List<object>;
                    if (child != null)
                    {
                        RemoveTitleIdFromTitleDir(child, titleId);
                    }
                }
            }
        }

        private static bool AddTitleIdToCategory(List<object> titleDir, string pathKey, int titleId)
        {
            List<object> node = FindCategoryByPath(titleDir, pathKey);
            if (node == null)
            {
                return false;
            }

            node.Add(titleId);
            return true;
        }

        private static List<object> FindCategoryByPath(List<object> titleDir, string pathKey)
        {
            if (titleDir == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(pathKey))
            {
                return titleDir;
            }

            List<object> current = titleDir;
            string[] parts = pathKey.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                int index;
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out index)
                    || index < 0
                    || index >= current.Count)
                {
                    return null;
                }

                current = current[index] as List<object>;
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static List<object> CloneTitleDir(List<object> source)
        {
            List<object> clone = new List<object>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                List<object> child = source[i] as List<object>;
                if (child != null)
                {
                    clone.Add(CloneTitleDir(child));
                }
                else
                {
                    clone.Add(source[i]);
                }
            }

            return clone;
        }

        private static int FindMatchingBrace(string text, int braceStart)
        {
            int depth = 0;
            char stringQuote = '\0';
            for (int i = braceStart; i < text.Length; i++)
            {
                char current = text[i];
                if (stringQuote != '\0')
                {
                    if (current == stringQuote && (i == 0 || text[i - 1] != '\\'))
                    {
                        stringQuote = '\0';
                    }

                    continue;
                }

                if (current == '"' || current == '\'')
                {
                    stringQuote = current;
                    continue;
                }

                if (current == '{')
                {
                    depth++;
                }
                else if (current == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static List<string> TokenizeLuaTable(string text)
        {
            List<string> tokens = new List<string>();
            for (int i = 0; i < text.Length;)
            {
                char c = text[i];
                if (c == '-' && i + 1 < text.Length && text[i + 1] == '-')
                {
                    while (i < text.Length && text[i] != '\n')
                    {
                        i++;
                    }
                    continue;
                }

                if (char.IsWhiteSpace(c) || c == ',')
                {
                    i++;
                    continue;
                }

                if (c == '{' || c == '}')
                {
                    tokens.Add(c.ToString());
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    StringBuilder builder = new StringBuilder();
                    i++;
                    while (i < text.Length)
                    {
                        if (text[i] == '"' && (i == 0 || text[i - 1] != '\\'))
                        {
                            i++;
                            break;
                        }

                        builder.Append(text[i]);
                        i++;
                    }

                    tokens.Add("\"" + builder.ToString() + "\"");
                    continue;
                }

                if (char.IsDigit(c) || (c == '-' && i + 1 < text.Length && char.IsDigit(text[i + 1])))
                {
                    int start = i;
                    i++;
                    while (i < text.Length && char.IsDigit(text[i]))
                    {
                        i++;
                    }

                    tokens.Add(text.Substring(start, i - start));
                    continue;
                }

                i++;
            }

            return tokens;
        }

        private static object ParseLuaTable(List<string> tokens, ref int position)
        {
            List<object> items = new List<object>();
            if (tokens == null || position >= tokens.Count || tokens[position] != "{")
            {
                return items;
            }

            position++;
            while (position < tokens.Count && tokens[position] != "}")
            {
                string token = tokens[position];
                if (token == "{")
                {
                    items.Add(ParseLuaTable(tokens, ref position));
                }
                else if (token.StartsWith("\"", StringComparison.Ordinal))
                {
                    items.Add(token.Length >= 2 ? token.Substring(1, token.Length - 2) : string.Empty);
                    position++;
                }
                else
                {
                    int number;
                    if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    {
                        items.Add(number);
                    }
                    position++;
                }
            }

            if (position < tokens.Count && tokens[position] == "}")
            {
                position++;
            }

            return items;
        }

        private static string SerializeLuaNode(object node, int indent)
        {
            List<object> list = node as List<object>;
            if (list == null)
            {
                if (node is int)
                {
                    return ((int)node).ToString(CultureInfo.InvariantCulture);
                }

                return "\"" + EscapeLuaString(node as string ?? string.Empty) + "\"";
            }

            string pad = new string(' ', indent * 2);
            string outer = new string(' ', Math.Max(0, (indent - 1) * 2));
            StringBuilder builder = new StringBuilder();
            builder.Append("{").Append(Environment.NewLine);
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(pad)
                    .Append(SerializeLuaNode(list[i], indent + 1));
                if (i < list.Count - 1)
                {
                    builder.Append(",");
                }
                builder.Append(Environment.NewLine);
            }
            builder.Append(outer).Append("}");
            return builder.ToString();
        }

        private static string BuildTitleDetails(string description, List<string> addonDescriptions, string iconPath, string accentHex, string categoryDisplay)
        {
            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(description))
            {
                lines.Add(description.Trim());
            }

            if (!string.IsNullOrWhiteSpace(categoryDisplay))
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Category: " + categoryDisplay);
            }

            if (!string.IsNullOrWhiteSpace(accentHex))
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Title color: #" + accentHex.Trim().TrimStart('#').ToUpperInvariant());
            }

            if (!string.IsNullOrWhiteSpace(iconPath))
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Graphic title");
                lines.Add("Asset: " + iconPath);
            }

            if (addonDescriptions != null && addonDescriptions.Count > 0)
            {
                if (lines.Count > 0)
                {
                    lines.Add(string.Empty);
                }

                lines.Add("Bonuses:");
                for (int i = 0; i < addonDescriptions.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                    {
                        lines.Add("- " + addonDescriptions[i]);
                    }
                }
            }

            return string.Join(Environment.NewLine, lines.ToArray()).Trim();
        }

        private static string BuildSecondaryText(string description, List<string> addonDescriptions, bool isGraphicTitle, string accentHex, string categoryDisplay)
        {
            string colorLabel = string.IsNullOrWhiteSpace(accentHex)
                ? string.Empty
                : "Color #" + accentHex.Trim().TrimStart('#').ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(categoryDisplay) && !categoryDisplay.StartsWith("!", StringComparison.Ordinal))
            {
                colorLabel = string.IsNullOrWhiteSpace(colorLabel)
                    ? categoryDisplay
                    : colorLabel + " | " + categoryDisplay;
            }

            if (addonDescriptions != null)
            {
                for (int i = 0; i < addonDescriptions.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                    {
                        if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
                        {
                            return "Graphic title | " + colorLabel + " | " + addonDescriptions[i];
                        }

                        if (isGraphicTitle)
                        {
                            return "Graphic title | " + addonDescriptions[i];
                        }

                        return !string.IsNullOrWhiteSpace(colorLabel)
                            ? colorLabel + " | " + addonDescriptions[i]
                            : addonDescriptions[i];
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
                {
                    return "Graphic title | " + colorLabel + " | " + description;
                }

                if (isGraphicTitle)
                {
                    return "Graphic title | " + description;
                }

                return !string.IsNullOrWhiteSpace(colorLabel)
                    ? colorLabel + " | " + description
                    : description;
            }

            if (isGraphicTitle && !string.IsNullOrWhiteSpace(colorLabel))
            {
                return "Graphic title | " + colorLabel;
            }

            if (isGraphicTitle)
            {
                return "Graphic title";
            }

            return !string.IsNullOrWhiteSpace(colorLabel)
                ? colorLabel
                : "Title";
        }

        private static string ExtractLeadingColor(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            Match match = LeadingColorRegex.Match(rawValue);
            if (!match.Success)
            {
                return string.Empty;
            }

            return (match.Groups["hex"].Value ?? string.Empty).Trim();
        }

        private static string ExtractFieldRaw(string body, Regex regex)
        {
            if (string.IsNullOrWhiteSpace(body) || regex == null)
            {
                return string.Empty;
            }

            Match match = regex.Match(body);
            return ExtractLuaStringValue(match);
        }

        private static string ExtractFieldValue(string body, Regex regex)
        {
            return CleanScriptText(ExtractFieldRaw(body, regex));
        }

        private static List<string> ExtractFieldValues(string body, Regex regex)
        {
            List<string> values = new List<string>();
            if (string.IsNullOrWhiteSpace(body) || regex == null)
            {
                return values;
            }

            MatchCollection matches = regex.Matches(body);
            for (int i = 0; i < matches.Count; i++)
            {
                string value = CleanScriptText(ExtractLuaStringValue(matches[i]));
                if (!string.IsNullOrWhiteSpace(value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static string ExtractNamedStringFieldRaw(string body, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            Match match = Regex.Match(
                body,
                Regex.Escape(fieldName) + "\\s*=\\s*" + LuaStringPattern,
                RegexOptions.Singleline);
            return ExtractLuaStringValue(match);
        }

        private static string ExtractLuaStringValue(Match match)
        {
            if (match == null || !match.Success)
            {
                return string.Empty;
            }

            Group quoted = match.Groups["value"];
            if (quoted != null && quoted.Success)
            {
                return quoted.Value ?? string.Empty;
            }

            Group singleQuoted = match.Groups["singleValue"];
            if (singleQuoted != null && singleQuoted.Success)
            {
                return singleQuoted.Value ?? string.Empty;
            }

            Group longValue = match.Groups["longValue"];
            if (longValue != null && longValue.Success)
            {
                return longValue.Value ?? string.Empty;
            }

            return string.Empty;
        }

        private static string StripLeadingColorCode(string value)
        {
            string cleaned = value ?? string.Empty;
            cleaned = LeadingColorRegex.Replace(cleaned, string.Empty, 1);
            return cleaned.Trim();
        }

        private static string ComposeNoteRaw(EditableTitleDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            string titleText = (definition.TitleText ?? string.Empty).Trim();
            string accentHex = (definition.AccentHex ?? string.Empty).Trim().TrimStart('#');
            if (accentHex.Length == 6)
            {
                return "^" + accentHex.ToUpperInvariant() + titleText;
            }

            return titleText;
        }

        private static string BuildDefinitionBlock(EditableTitleDefinition definition)
        {
            string idText = definition.Id.ToString(CultureInfo.InvariantCulture);
            StringBuilder builder = new StringBuilder();
            builder.Append("title_definition[\"").Append(idText).Append("\"] = {").Append(Environment.NewLine);
            builder.Append("  id = ").Append(idText).Append(",").Append(Environment.NewLine);
            builder.Append("  note = \"").Append(EscapeLuaString(ComposeNoteRaw(definition))).Append("\",").Append(Environment.NewLine);

            string[] addonDescriptions = definition.AddonDescriptions ?? new string[0];
            for (int i = 0; i < addonDescriptions.Length && i < 5; i++)
            {
                if (!string.IsNullOrWhiteSpace(addonDescriptions[i]))
                {
                    builder.Append("  addon_desc")
                        .Append((i + 1).ToString(CultureInfo.InvariantCulture))
                        .Append(" = \"")
                        .Append(EscapeLuaString(addonDescriptions[i]))
                        .Append("\",")
                        .Append(Environment.NewLine);
                }
            }

            builder.Append("  desc = \"")
                .Append(EscapeLuaString(definition.Description ?? string.Empty))
                .Append("\"")
                .Append(Environment.NewLine)
                .Append("}");
            return builder.ToString();
        }

        private static string BuildTitleEntryPattern(int id)
        {
            return "title_definition\\[\\s*\"?" + id.ToString(CultureInfo.InvariantCulture) + "\"?\\s*\\]\\s*=\\s*\\{.*?\\}";
        }

        private static string BuildIconEntryPattern(int id)
        {
            return "title_definition\\[\\s*\"?" + id.ToString(CultureInfo.InvariantCulture) + "\"?\\s*\\]\\.icon\\s*=\\s*\\{.*?\\}";
        }

        private static string BuildBeforeNameEntryPattern(int id)
        {
            return "\\s*title_definition\\[\\s*\"?" + id.ToString(CultureInfo.InvariantCulture) + "\"?\\s*\\]\\.beforename\\s*=\\s*\\d+\\s*";
        }

        private static int FindIconBlockStart(string luaText, int id)
        {
            if (string.IsNullOrWhiteSpace(luaText) || id <= 0)
            {
                return -1;
            }

            string idText = id.ToString(CultureInfo.InvariantCulture);
            string[] needles =
            {
                "title_definition[\"" + idText + "\"].icon",
                "title_definition[" + idText + "].icon"
            };

            for (int i = 0; i < needles.Length; i++)
            {
                int start = luaText.IndexOf(needles[i], StringComparison.Ordinal);
                if (start >= 0)
                {
                    return start;
                }
            }

            return -1;
        }

        private static bool TryFindIconBlockRange(string luaText, int id, out int start, out int endExclusive)
        {
            start = -1;
            endExclusive = -1;

            start = FindIconBlockStart(luaText, id);
            if (start < 0)
            {
                return false;
            }

            int braceStart = luaText.IndexOf('{', start);
            if (braceStart < 0)
            {
                return false;
            }

            int braceEnd = FindMatchingBrace(luaText, braceStart);
            if (braceEnd <= braceStart)
            {
                return false;
            }

            endExclusive = braceEnd + 1;
            while (true)
            {
                while (endExclusive < luaText.Length && char.IsWhiteSpace(luaText[endExclusive]))
                {
                    endExclusive++;
                }

                if (endExclusive >= luaText.Length || luaText[endExclusive] != ',')
                {
                    break;
                }

                int next = endExclusive + 1;
                while (next < luaText.Length && char.IsWhiteSpace(luaText[next]))
                {
                    next++;
                }

                if (next >= luaText.Length || luaText[next] != '{')
                {
                    break;
                }

                int chainedBraceEnd = FindMatchingBrace(luaText, next);
                if (chainedBraceEnd <= next)
                {
                    break;
                }

                endExclusive = chainedBraceEnd + 1;
            }

            while (endExclusive < luaText.Length && char.IsWhiteSpace(luaText[endExclusive]))
            {
                endExclusive++;
            }

            return true;
        }

        private static string RemoveIconBlock(string luaText, int id)
        {
            string updatedLuaText = luaText ?? string.Empty;
            int start;
            int endExclusive;
            if (!TryFindIconBlockRange(updatedLuaText, id, out start, out endExclusive))
            {
                return Regex.Replace(updatedLuaText, BuildIconEntryPattern(id) + "\\s*", string.Empty, RegexOptions.Singleline);
            }

            return updatedLuaText.Remove(start, endExclusive - start);
        }

        private static string RemoveBeforeNameLine(string luaText, int id)
        {
            string updatedLuaText = luaText ?? string.Empty;
            return Regex.Replace(
                updatedLuaText,
                BuildBeforeNameEntryPattern(id),
                Environment.NewLine,
                RegexOptions.Singleline);
        }

        private static string UpsertIconBlock(string luaText, EditableTitleDefinition definition)
        {
            if (definition == null)
            {
                return luaText ?? string.Empty;
            }

            string updatedLuaText = luaText ?? string.Empty;
            string pattern = BuildIconEntryPattern(definition.Id);
            string iconPath = NormalizeImagePath(definition.IconPath);
            if (!definition.IsGraphicTitle || string.IsNullOrWhiteSpace(iconPath))
            {
                updatedLuaText = RemoveIconBlock(updatedLuaText, definition.Id);
                return RemoveBeforeNameLine(updatedLuaText, definition.Id);
            }

            updatedLuaText = RemoveBeforeNameLine(updatedLuaText, definition.Id);

            string idText = definition.Id.ToString(CultureInfo.InvariantCulture);
            StringBuilder iconBlock = new StringBuilder();
            if (definition.ShowGraphicInChat)
            {
                iconBlock.Append("title_definition[\"").Append(idText).Append("\"].beforename = 1").Append(Environment.NewLine);
            }
            iconBlock.Append("title_definition[\"").Append(idText).Append("\"].icon = {").Append(Environment.NewLine);
            iconBlock.Append("  {").Append(Environment.NewLine);
            iconBlock.Append("    imageplace = \"0\",").Append(Environment.NewLine);
            iconBlock.Append("    image = \"").Append(EscapeLuaString(iconPath)).Append("\",").Append(Environment.NewLine);
            iconBlock.Append("    imagenum = \"0\",").Append(Environment.NewLine);
            iconBlock.Append("    imagetime = \"0\"").Append(Environment.NewLine);
            iconBlock.Append("  }").Append(Environment.NewLine);
            iconBlock.Append("}");

            int start;
            int endExclusive;
            if (TryFindIconBlockRange(updatedLuaText, definition.Id, out start, out endExclusive))
            {
                return updatedLuaText.Substring(0, start)
                    + iconBlock
                    + updatedLuaText.Substring(endExclusive);
            }

            if (Regex.IsMatch(updatedLuaText, pattern, RegexOptions.Singleline))
            {
                return Regex.Replace(updatedLuaText, pattern, iconBlock.ToString(), RegexOptions.Singleline);
            }

            int insertAt = updatedLuaText.IndexOf("title_dir = {", StringComparison.Ordinal);
            if (insertAt >= 0)
            {
                return updatedLuaText.Insert(insertAt, iconBlock + Environment.NewLine + Environment.NewLine);
            }

            return updatedLuaText + Environment.NewLine + Environment.NewLine + iconBlock + Environment.NewLine;
        }

        private static string EscapeLuaString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\r\n", "\\r")
                .Replace("\n", "\\r")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\"", "\\\"")
                .Replace("%", "%%");
        }

        private static string NormalizeImagePath(string value)
        {
            string normalized = (value ?? string.Empty).Trim().Replace('/', '\\');
            while (normalized.Contains("\\\\"))
            {
                normalized = normalized.Replace("\\\\", "\\");
            }

            return normalized;
        }

        private static string DecodeOrDecompileLuaFile(string sourcePath, byte[] payload)
        {
            string ignored;
            return DecodeOrDecompileLuaFile(sourcePath, payload, out ignored);
        }

        private static string DecodeOrDecompileLuaFile(string sourcePath, byte[] payload, out string status)
        {
            string plainText;
            byte[] sourcePayload = payload;
            if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
            {
                try
                {
                    sourcePayload = File.ReadAllBytes(sourcePath);
                }
                catch
                {
                    sourcePayload = payload;
                }
            }

            if (!IsLuaBytecode(sourcePayload) && TryDecodePlainLuaText(sourcePayload, out plainText))
            {
                status = "Decoded as plain Lua text.";
                return plainText;
            }

            string error;
            string decompiled = DecompileLuaFile(sourcePath, out error);
            status = string.IsNullOrWhiteSpace(error)
                ? "Decompiled Lua bytecode."
                : error;
            return decompiled;
        }

        private static bool TryDecodePlainLuaText(byte[] payload, out string luaText)
        {
            luaText = string.Empty;
            if (payload == null || payload.Length == 0)
            {
                return false;
            }

            if (IsLuaBytecode(payload))
            {
                return false;
            }

            Encoding[] encodings = new Encoding[]
            {
                new UTF8Encoding(false, false),
                Encoding.GetEncoding("GBK"),
                Encoding.Default
            };

            for (int i = 0; i < encodings.Length; i++)
            {
                try
                {
                    string decoded = encodings[i].GetString(payload);
                    if (LooksLikeLuaSourceText(decoded)
                        && decoded.IndexOf("title_definition", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        luaText = decoded.TrimStart('\uFEFF');
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool LooksLikeLuaSourceText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (trimmed.Length == 0 || trimmed[0] == '\x1B')
            {
                return false;
            }

            int inspected = Math.Min(text.Length, 4096);
            int controlCount = 0;
            for (int i = 0; i < inspected; i++)
            {
                char current = text[i];
                if (current == '\0')
                {
                    return false;
                }

                if (char.IsControl(current)
                    && current != '\r'
                    && current != '\n'
                    && current != '\t')
                {
                    controlCount++;
                }
            }

            return controlCount <= Math.Max(2, inspected / 100);
        }

        private static bool IsLuaBytecode(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
            {
                return false;
            }

            for (int i = 0; i <= payload.Length - 4 && i < 32; i++)
            {
                if (payload[i] == 0x1B
                    && payload[i + 1] == (byte)'L'
                    && payload[i + 2] == (byte)'u'
                    && payload[i + 3] == (byte)'a')
                {
                    return true;
                }
            }

            return false;
        }

        private static string DecompileLuaFile(string sourcePath)
        {
            string ignored;
            return DecompileLuaFile(sourcePath, out ignored);
        }

        private static string DecompileLuaFile(string sourcePath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "Lua source file was not found.";
                return string.Empty;
            }

            string unluacJar = FindBundledUnluacJar();
            if (string.IsNullOrWhiteSpace(unluacJar) || !File.Exists(unluacJar))
            {
                error = "Bundled unluac.jar was not found.";
                return string.Empty;
            }

            string workingDirectory = Path.GetDirectoryName(unluacJar) ?? string.Empty;
            string processError;
            string output = ExecuteProcessCaptureOutput(
                ResolveJavaExecutable(),
                "-jar \"" + unluacJar + "\" \"" + sourcePath + "\"",
                workingDirectory,
                out processError);
            if (string.IsNullOrWhiteSpace(output))
            {
                error = string.IsNullOrWhiteSpace(processError)
                    ? "unluac produced no output."
                    : processError;
            }

            return output;
        }

        private static string ResolveJavaExecutable()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string baseDirectory = !string.IsNullOrWhiteSpace(assemblyDirectory)
                ? assemblyDirectory
                : (AppDomain.CurrentDomain.BaseDirectory ?? string.Empty);
            string[] candidates = new string[]
            {
                Path.Combine(baseDirectory, "tools", "java", "bin", "java.exe"),
                Path.Combine(baseDirectory, "jre", "bin", "java.exe"),
                Path.Combine(baseDirectory, "java", "bin", "java.exe")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return "java";
        }

        private static string FindBundledUnluacJar()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string baseDirectory = !string.IsNullOrWhiteSpace(assemblyDirectory)
                ? assemblyDirectory
                : (AppDomain.CurrentDomain.BaseDirectory ?? string.Empty);
            string[] directCandidates = new[]
            {
                Path.Combine(baseDirectory, "tools", "lua", "unluac.jar"),
                Path.Combine(baseDirectory, "unluac.jar")
            };

            for (int i = 0; i < directCandidates.Length; i++)
            {
                if (File.Exists(directCandidates[i]))
                {
                    return directCandidates[i];
                }
            }

            string current = baseDirectory;
            for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                string solutionCandidate = Path.Combine(current, "FWEledit.sln");
                if (File.Exists(solutionCandidate))
                {
                    string repoCandidate = Path.Combine(current, "tools", "lua", "unluac.jar");
                    if (File.Exists(repoCandidate))
                    {
                        return repoCandidate;
                    }
                }

                DirectoryInfo parentInfo = Directory.GetParent(current);
                current = parentInfo != null ? parentInfo.FullName : string.Empty;
            }

            return string.Empty;
        }

        private static bool CompileLuaFile(string sourcePath, string outputPath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "The Lua source file was not found.";
                return false;
            }

            string luacPath = FindBundledLuacExecutable();
            if (string.IsNullOrWhiteSpace(luacPath) || !File.Exists(luacPath))
            {
                error = "luac.exe was not found.";
                return false;
            }

            string workingDirectory = Path.GetDirectoryName(luacPath) ?? string.Empty;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = luacPath,
                Arguments = "-o \"" + outputPath + "\" \"" + sourcePath + "\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        error = "Unable to start luac.exe.";
                        return false;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        error = "luac.exe timed out.";
                        return false;
                    }

                    if (process.ExitCode != 0)
                    {
                        error = string.IsNullOrWhiteSpace(stdError)
                            ? (string.IsNullOrWhiteSpace(output) ? "luac.exe failed." : output.Trim())
                            : stdError.Trim();
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            return File.Exists(outputPath);
        }

        private static string FindBundledLuacExecutable()
        {
            string assemblyDirectory = string.Empty;
            try
            {
                assemblyDirectory = Path.GetDirectoryName(typeof(TitleDefinitionCatalog).Assembly.Location) ?? string.Empty;
            }
            catch
            {
                assemblyDirectory = string.Empty;
            }

            string baseDirectory = !string.IsNullOrWhiteSpace(assemblyDirectory)
                ? assemblyDirectory
                : (AppDomain.CurrentDomain.BaseDirectory ?? string.Empty);
            string[] directCandidates = new[]
            {
                Path.Combine(baseDirectory, "tools", "lua", "luac.exe"),
                Path.Combine(baseDirectory, "luac.exe")
            };

            for (int i = 0; i < directCandidates.Length; i++)
            {
                if (File.Exists(directCandidates[i]))
                {
                    return directCandidates[i];
                }
            }

            string current = baseDirectory;
            for (int depth = 0; depth < 8 && !string.IsNullOrWhiteSpace(current); depth++)
            {
                string solutionCandidate = Path.Combine(current, "FWEledit.sln");
                if (File.Exists(solutionCandidate))
                {
                    string repoCandidate = Path.Combine(current, "tools", "lua", "luac.exe");
                    if (File.Exists(repoCandidate))
                    {
                        return repoCandidate;
                    }
                }

                DirectoryInfo parentInfo = Directory.GetParent(current);
                current = parentInfo != null ? parentInfo.FullName : string.Empty;
            }

            return string.Empty;
        }

        private static string ExecuteProcessCaptureOutput(string fileName, string arguments, string workingDirectory)
        {
            string ignored;
            return ExecuteProcessCaptureOutput(fileName, arguments, workingDirectory, out ignored);
        }

        private static string ExecuteProcessCaptureOutput(string fileName, string arguments, string workingDirectory, out string error)
        {
            error = string.Empty;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? string.Empty : workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        error = "Unable to start " + fileName + ".";
                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    if (!process.HasExited)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                        }

                        error = fileName + " timed out while decompiling Lua.";
                        return string.Empty;
                    }

                    if (process.ExitCode != 0)
                    {
                        error = string.IsNullOrWhiteSpace(stderr)
                            ? fileName + " exited with code " + process.ExitCode.ToString(CultureInfo.InvariantCulture) + "."
                            : stderr.Trim();
                        return string.Empty;
                    }

                    return DecodeLuaEscapedUtf8(output);
                }
            }
            catch (Exception ex)
            {
                error = "Unable to start " + fileName + ": " + ex.Message;
                return string.Empty;
            }
        }

        private static string CleanScriptText(string text)
        {
            string cleaned = DecodeLuaEscapedUtf8(text ?? string.Empty);
            try
            {
                cleaned = Extensions.ColorClean(cleaned);
            }
            catch
            {
            }

            cleaned = NormalizeScriptLineEndings(cleaned)
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("%%", "%")
                .Trim();

            while (cleaned.StartsWith("%s", StringComparison.Ordinal))
            {
                cleaned = cleaned.Substring(2).TrimStart();
            }

            return cleaned;
        }

        private static string DecodeScriptLiteralPreserveFormatting(string text)
        {
            string cleaned = DecodeLuaEscapedUtf8(text ?? string.Empty);
            cleaned = NormalizeScriptLineEndings(cleaned)
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("%%", "%");

            return cleaned.Trim();
        }

        private static string NormalizeScriptLineEndings(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\r", "\n")
                .Replace("\\n", "\n")
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\n", Environment.NewLine);
        }

        private static string DecodeLuaEscapedUtf8(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return Regex.Replace(
                text,
                "(?:\\\\\\d{1,3})+",
                delegate (Match match)
                {
                    try
                    {
                        MatchCollection digits = Regex.Matches(match.Value, "\\\\(\\d{1,3})");
                        byte[] bytes = new byte[digits.Count];
                        for (int i = 0; i < digits.Count; i++)
                        {
                            bytes[i] = byte.Parse(digits[i].Groups[1].Value, CultureInfo.InvariantCulture);
                        }

                        try
                        {
                            return new UTF8Encoding(false, true).GetString(bytes);
                        }
                        catch
                        {
                            return Encoding.GetEncoding("GBK").GetString(bytes);
                        }
                    }
                    catch
                    {
                        return match.Value;
                    }
                });
        }

        private static List<ItemReferenceOption> CloneOptions(List<ItemReferenceOption> source)
        {
            List<ItemReferenceOption> clone = new List<ItemReferenceOption>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ItemReferenceOption option = source[i];
                if (option == null)
                {
                    continue;
                }

                clone.Add(new ItemReferenceOption
                {
                    ListIndex = option.ListIndex,
                    ElementIndex = option.ElementIndex,
                    Id = option.Id,
                    Name = option.Name,
                    ListName = option.ListName,
                    IconKey = option.IconKey,
                    Quality = option.Quality,
                    Description = option.Description,
                    SecondaryText = option.SecondaryText,
                    AccentHex = option.AccentHex,
                    Kind = option.Kind
                });
            }

            return clone;
        }

        private static EditableTitleDefinition CloneDefinition(EditableTitleDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            string[] addonDescriptions = definition.AddonDescriptions != null
                ? (string[])definition.AddonDescriptions.Clone()
                : new string[5];
            if (addonDescriptions.Length < 5)
            {
                Array.Resize(ref addonDescriptions, 5);
            }

            return new EditableTitleDefinition
            {
                Id = definition.Id,
                TitleText = definition.TitleText,
                AccentHex = definition.AccentHex,
                Description = definition.Description,
                AddonDescriptions = addonDescriptions,
                IconPath = definition.IconPath,
                IsGraphicTitle = definition.IsGraphicTitle,
                ShowGraphicInChat = definition.ShowGraphicInChat,
                CategoryPathKey = definition.CategoryPathKey ?? string.Empty,
                CategoryDisplay = definition.CategoryDisplay ?? string.Empty
            };
        }
    }
}
