using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class NpcGenMapPreviewService
    {
        private const int MaxPreviewSide = 1536;
        private static readonly bool ClientPackageLookupEnabled = false;

        public NpcGenMapPreviewData BuildPreview(
            NpcGenData data,
            ISessionService sessionService,
            NpcGenEntityLookupService entityLookupService,
            string mapDisplayName,
            int initialAreaIndex = -1,
            int initialEntryIndex = -1)
        {
            NpcGenMapPreviewData preview = new NpcGenMapPreviewData();
            preview.MapName = string.IsNullOrWhiteSpace(mapDisplayName) ? "(unknown map)" : mapDisplayName;
            preview.GridSize = 1f;
            preview.InitialAreaIndex = initialAreaIndex;
            preview.InitialEntryIndex = initialEntryIndex;

            string mapDirectory = ResolveMapDirectory(data);
            preview.MapDirectory = mapDirectory;
            TerrainPreviewConfig config = LoadTerrainConfig(mapDirectory);
            RectangleF bounds = BuildWorldBounds(config, data);
            preview.WorldBounds = bounds;
            preview.GridSize = config.GridSize > 0f ? config.GridSize : 1f;
            string backgroundSource;
            bool fitBackgroundToSpawns;
            bool hasBackgroundWorldBounds;
            RectangleF backgroundWorldBounds;
            string resolvedMapName;
            PointF? focusPoint = TryGetAreaFocusPoint(data, initialAreaIndex);
            preview.HeightMapImage = TryBuildClientMapImage(
                data,
                sessionService,
                entityLookupService,
                mapDirectory,
                mapDisplayName,
                focusPoint,
                out backgroundSource,
                out fitBackgroundToSpawns,
                out hasBackgroundWorldBounds,
                out backgroundWorldBounds,
                out resolvedMapName)
                ?? TryBuildHeightMapImage(mapDirectory, config);
            if (!string.IsNullOrWhiteSpace(resolvedMapName))
            {
                preview.MapName = resolvedMapName;
            }
            if (preview.HeightMapImage != null && hasBackgroundWorldBounds)
            {
                preview.WorldBounds = backgroundWorldBounds;
            }
            else if (fitBackgroundToSpawns && preview.HeightMapImage != null)
            {
                preview.WorldBounds = BuildSpawnFittedWorldBounds(data, preview.HeightMapImage, bounds);
            }
            preview.Markers.AddRange(BuildMarkers(data, sessionService, entityLookupService));

            if (!string.IsNullOrWhiteSpace(backgroundSource))
            {
                preview.Status = string.Format(
                    CultureInfo.InvariantCulture,
                    "Client map loaded from {0}; {1:N0} spawn markers",
                    backgroundSource,
                    preview.Markers.Count);
            }
            else if (preview.HeightMapImage != null)
            {
                preview.Status = string.Format(
                    CultureInfo.InvariantCulture,
                    "Heightmap loaded from {0}; {1:N0} spawn markers",
                    Path.Combine(mapDirectory ?? string.Empty, "map"),
                    preview.Markers.Count);
            }
            else
            {
                preview.Status = string.Format(
                    CultureInfo.InvariantCulture,
                    "No heightmap found; showing coordinate grid and {0:N0} spawn markers",
                    preview.Markers.Count);
            }

            if (hasBackgroundWorldBounds && preview.Markers.Count > 0)
            {
                int inside = CountMarkersInsideBounds(preview.Markers, preview.WorldBounds);
                if (inside < preview.Markers.Count / 2)
                {
                    preview.Status += string.Format(
                        CultureInfo.InvariantCulture,
                        " | Warning: only {0:N0}/{1:N0} markers are inside this map area; npcgen.data may belong to another instance",
                        inside,
                        preview.Markers.Count);
                }
            }

            return preview;
        }

        private static Bitmap TryBuildClientMapImage(
            NpcGenData data,
            ISessionService sessionService,
            NpcGenEntityLookupService entityLookupService,
            string mapDirectory,
            string mapDisplayName,
            PointF? focusPoint,
            out string source,
            out bool fitToSpawns,
            out bool hasWorldBounds,
            out RectangleF worldBounds,
            out string resolvedMapName)
        {
            source = string.Empty;
            fitToSpawns = false;
            hasWorldBounds = false;
            worldBounds = RectangleF.Empty;
            resolvedMapName = string.Empty;
            if (!ClientPackageLookupEnabled)
            {
                return null;
            }

            if (sessionService == null || sessionService.Database == null)
            {
                return null;
            }

            AssetManager assetManager = sessionService.AssetManager ?? new AssetManager(sessionService);
            try
            {
                assetManager.EnsureWorkspaceReady();
                assetManager.EnsurePackageExtracted("script");
                assetManager.EnsurePackageExtracted("surfaces");
                assetManager.EnsurePathDataLoaded();
            }
            catch
            {
            }

            CacheSave database = sessionService.Database;
            if (database.pathById == null || database.pathById.Count == 0)
            {
                return null;
            }

            List<ClientMapImageCandidate> candidates = new List<ClientMapImageCandidate>();
            InstanceMapMatch instanceMatch = TryFindInstanceMapMatch(assetManager, data);
            bool isTechnicalMap = IsPaddedInstanceMap(mapDirectory, mapDisplayName);
            bool isWorldMap = IsWorldMapCode(mapDirectory, mapDisplayName);
            bool suppressLooseClientCandidates = isTechnicalMap && instanceMatch == null;
            AddWorldMapCodeImageCandidate(candidates, mapDirectory, mapDisplayName, focusPoint);
            AddKnownTechnicalMapImageCandidate(candidates, assetManager, data, mapDirectory, mapDisplayName);
            AddTechnicalCodeInstanceMapCandidates(candidates, assetManager, data, mapDirectory, mapDisplayName);
            if (!isTechnicalMap && !isWorldMap)
            {
                AddSpawnMatchedInstanceMapCandidates(candidates, assetManager, data);
            }
            if (!suppressLooseClientCandidates)
            {
                AddElementMapImageCandidates(candidates, sessionService.ListCollection, database, data, entityLookupService, instanceMatch, mapDirectory, mapDisplayName);
                AddPathDataMapImageCandidates(candidates, database, mapDirectory, mapDisplayName);
                AddPckIndexMapImageCandidates(candidates, assetManager, mapDirectory, mapDisplayName);
            }

            foreach (ClientMapImageCandidate candidate in candidates
                .Where(item => item != null && item.Score > 0 && !string.IsNullOrWhiteSpace(item.MappedPath))
                .OrderByDescending(item => item.HasWorldBounds)
                .ThenByDescending(item => item.Score))
            {
                ClientMapImageCandidate effectiveCandidate = TryPromoteInstanceMapCandidate(assetManager, data, candidate) ?? candidate;
                Bitmap bitmap = TryLoadMappedBitmap(assetManager, effectiveCandidate.MappedPath);
                if (bitmap == null || bitmap.Width < 128 || bitmap.Height < 128)
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                    continue;
                }

                source = string.IsNullOrWhiteSpace(effectiveCandidate.Source)
                    ? effectiveCandidate.MappedPath
                    : effectiveCandidate.Source + " -> " + effectiveCandidate.MappedPath;
                fitToSpawns = effectiveCandidate.FitToSpawns;
                hasWorldBounds = effectiveCandidate.HasWorldBounds;
                worldBounds = effectiveCandidate.WorldBounds;
                resolvedMapName = effectiveCandidate.DisplayName;
                return bitmap;
            }

            return null;
        }

        private static void AddElementMapImageCandidates(
            List<ClientMapImageCandidate> candidates,
            eListCollection listCollection,
            CacheSave database,
            NpcGenData data,
            NpcGenEntityLookupService entityLookupService,
            InstanceMapMatch instanceMatch,
            string mapDirectory,
            string mapDisplayName)
        {
            if (candidates == null || listCollection == null || listCollection.Lists == null || database == null || database.pathById == null)
            {
                return;
            }

            List<string> mapTokens = BuildMapSearchTokens(data, mapDirectory, mapDisplayName);
            List<string> spawnNameTokens = BuildSpawnNameTokens(data, listCollection, database, entityLookupService);
            bool preferSpawnNameMatches = IsPaddedInstanceMap(mapDirectory, mapDisplayName)
                && !HasResolvedMapDisplayName(mapDisplayName);
            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null || list.elementFields == null || list.elementValues == null)
                {
                    continue;
                }

                int mapImageIndex = GetFieldIndex(list.elementFields, "dynamic_instance_map_image");
                if (mapImageIndex < 0)
                {
                    continue;
                }

                int nameIndex = GetFieldIndex(list.elementFields, "name");
                int imageIndex = GetFieldIndex(list.elementFields, "file_image");
                int sceneImageIndex = GetFieldIndex(list.elementFields, "dynamic_instance_scene_image");
                int descIndex = GetFieldIndex(list.elementFields, "desc");
                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    string rowName = nameIndex >= 0 ? listCollection.GetValue(listIndex, rowIndex, nameIndex) : string.Empty;
                    int nameScore = ScoreMapName(rowName, mapTokens);
                    string rowText = rowName + " " + (descIndex >= 0 ? listCollection.GetValue(listIndex, rowIndex, descIndex) : string.Empty);
                    int spawnScore = ScoreTextAgainstSpawnNames(rowText, spawnNameTokens);
                    if (preferSpawnNameMatches && spawnScore > 0)
                    {
                        spawnScore += 140;
                    }

                    int score = Math.Max(nameScore, spawnScore);
                    AddInstanceMatchedPathIdCandidate(
                        candidates,
                        database,
                        listCollection.GetValue(listIndex, rowIndex, mapImageIndex),
                        rowName,
                        instanceMatch,
                        230,
                        list.listName + " instance map");
                    if (score <= 0)
                    {
                        continue;
                    }

                    AddPathIdCandidate(candidates, database, listCollection.GetValue(listIndex, rowIndex, mapImageIndex), score + 60, list.listName + " map image", rowName);
                    if (imageIndex >= 0)
                    {
                        AddPathIdCandidate(candidates, database, listCollection.GetValue(listIndex, rowIndex, imageIndex), score + 20, list.listName + " image", rowName);
                    }
                    if (sceneImageIndex >= 0)
                    {
                        AddPathIdCandidate(candidates, database, listCollection.GetValue(listIndex, rowIndex, sceneImageIndex), score + 10, list.listName + " scene image", rowName);
                    }
                }
            }
        }

        private static void AddTechnicalCodeInstanceMapCandidates(
            List<ClientMapImageCandidate> candidates,
            AssetManager assetManager,
            NpcGenData data,
            string mapDirectory,
            string mapDisplayName)
        {
            int instanceId;
            if (candidates == null
                || assetManager == null
                || !TryResolveTechnicalInstanceId(mapDirectory, mapDisplayName, out instanceId))
            {
                return;
            }

            List<PointF> points = BuildSpawnPoints(data);
            string[] scripts = GetInstanceMapScriptNames();
            for (int scriptIndex = 0; scriptIndex < scripts.Length; scriptIndex++)
            {
                foreach (string text in ReadScriptTexts(assetManager, scripts[scriptIndex]))
                {
                    List<InstanceMapMatch> matches = ParseInstanceMapMatches(text);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        InstanceMapMatch match = matches[i];
                        if (match == null
                            || match.InstanceId != instanceId
                            || string.IsNullOrWhiteSpace(match.MidMap))
                        {
                            continue;
                        }

                        int inside = points.Count == 0 ? 0 : CountPointsInside(points, match.RectMap);
                        if (!HasReliableMapBounds(points, match.RectMap, inside))
                        {
                            continue;
                        }

                        int areaPenalty = (int)Math.Min(99, Math.Abs(match.RectMap.Width * match.RectMap.Height) / 100000f);
                        candidates.Add(new ClientMapImageCandidate
                        {
                            MappedPath = "surfaces\\" + NormalizeAssetPath(match.MidMap),
                            Source = scripts[scriptIndex] + " map code " + GetTechnicalMapToken(mapDirectory, mapDisplayName),
                            FitToSpawns = false,
                            HasWorldBounds = true,
                            WorldBounds = match.RectMap,
                            DisplayName = string.IsNullOrWhiteSpace(mapDisplayName) ? match.DisplayName : mapDisplayName,
                            Score = 1200 + Math.Min(400, inside * 10) - areaPenalty
                        });
                    }
                }
            }
        }

        private static void AddWorldMapCodeImageCandidate(
            List<ClientMapImageCandidate> candidates,
            string mapDirectory,
            string mapDisplayName,
            PointF? focusPoint)
        {
            if (candidates == null)
            {
                return;
            }

            string token = NormalizeWorldMapToken(GetTechnicalMapToken(mapDirectory, mapDisplayName));
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            RectangleF bounds;
            bool hasBounds = TryGetClientPrecinctMapBounds(token, mapDirectory, out bounds);
            candidates.Add(new ClientMapImageCandidate
            {
                MappedPath = "surfaces\\midmaps\\" + token + ".dds",
                Source = hasBounds ? "map code " + token + " with precinct.clt bounds" : "map code " + token,
                FitToSpawns = false,
                HasWorldBounds = hasBounds,
                WorldBounds = bounds,
                DisplayName = mapDisplayName,
                Score = 1800
            });
        }

        private static bool TryGetClientPrecinctMapBounds(string token, string mapDirectory, out RectangleF bounds)
        {
            bounds = RectangleF.Empty;
            string precinctPath = TryResolveClientMapMetadataPath(token, mapDirectory, "precinct.clt");
            if (string.IsNullOrWhiteSpace(precinctPath))
            {
                return false;
            }

            try
            {
                string[] lines = File.ReadAllLines(precinctPath, Encoding.Unicode);
                float minX = float.MaxValue;
                float minZ = float.MaxValue;
                float maxX = float.MinValue;
                float maxZ = float.MinValue;
                bool hasPoint = false;

                for (int i = 0; i < lines.Length - 6; i++)
                {
                    string nameLine = (lines[i] ?? string.Empty).Trim();
                    if (nameLine.Length < 2 || nameLine[0] != '"' || nameLine[nameLine.Length - 1] != '"')
                    {
                        continue;
                    }

                    string[] headerParts = SplitWhitespace(lines[i + 1]);
                    int pointCount;
                    if (headerParts.Length < 2
                        || !int.TryParse(headerParts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out pointCount)
                        || pointCount <= 0)
                    {
                        continue;
                    }

                    int firstPointLine = i + 6;
                    int lastPointLine = Math.Min(lines.Length, firstPointLine + pointCount);
                    for (int j = firstPointLine; j < lastPointLine; j++)
                    {
                        string[] pointParts = SplitWhitespace(lines[j]);
                        float x;
                        float z;
                        if (pointParts.Length < 3
                            || !float.TryParse(pointParts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                            || !float.TryParse(pointParts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                        {
                            continue;
                        }

                        minX = Math.Min(minX, x);
                        minZ = Math.Min(minZ, z);
                        maxX = Math.Max(maxX, x);
                        maxZ = Math.Max(maxZ, z);
                        hasPoint = true;
                    }
                }

                if (!hasPoint || maxX <= minX || maxZ <= minZ)
                {
                    return false;
                }

                bounds = RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string TryResolveClientMapMetadataPath(string token, string mapDirectory, string fileName)
        {
            string normalized = NormalizeWorldMapToken(token);
            if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            string localPath = string.IsNullOrWhiteSpace(mapDirectory)
                ? string.Empty
                : Path.Combine(mapDirectory, fileName);
            if (!string.IsNullOrWhiteSpace(localPath) && File.Exists(localPath))
            {
                return localPath;
            }

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return string.Empty;
            }

            string clientPath = Path.Combine(gameRoot, "maps", normalized, fileName);
            return File.Exists(clientPath) ? clientPath : string.Empty;
        }

        private static string[] SplitWhitespace(string value)
        {
            return (value ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void AddKnownTechnicalMapImageCandidate(
            List<ClientMapImageCandidate> candidates,
            AssetManager assetManager,
            NpcGenData data,
            string mapDirectory,
            string mapDisplayName)
        {
            if (candidates == null)
            {
                return;
            }

            string token = NormalizeTechnicalMapToken(GetTechnicalMapToken(mapDirectory, mapDisplayName));
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            string midMap;
            if (!TryGetKnownTechnicalMidMap(token, out midMap))
            {
                return;
            }

            int instanceId;
            RectangleF bounds = RectangleF.Empty;
            bool hasBounds = TryResolveTechnicalInstanceId(mapDirectory, mapDisplayName, out instanceId)
                && TryFindKnownTechnicalMapBounds(assetManager, data, instanceId, midMap, out bounds);

            candidates.Add(new ClientMapImageCandidate
            {
                MappedPath = "surfaces\\" + NormalizeAssetPath(midMap),
                Source = hasBounds ? "built-in map code " + token + " with script bounds" : "built-in map code " + token,
                FitToSpawns = !hasBounds,
                HasWorldBounds = hasBounds,
                WorldBounds = bounds,
                DisplayName = mapDisplayName,
                Score = 650
            });
        }

        private static bool TryFindKnownTechnicalMapBounds(
            AssetManager assetManager,
            NpcGenData data,
            int instanceId,
            string midMap,
            out RectangleF bounds)
        {
            bounds = RectangleF.Empty;
            if (assetManager == null || instanceId <= 0 || string.IsNullOrWhiteSpace(midMap))
            {
                return false;
            }

            string normalizedMidMap = NormalizeAssetPath(midMap);
            List<PointF> points = BuildSpawnPoints(data);
            string[] scripts = GetInstanceMapScriptNames();
            InstanceMapMatch best = null;
            for (int scriptIndex = 0; scriptIndex < scripts.Length; scriptIndex++)
            {
                foreach (string text in ReadScriptTexts(assetManager, scripts[scriptIndex]))
                {
                    List<InstanceMapMatch> matches = ParseInstanceMapMatches(text);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        InstanceMapMatch match = matches[i];
                        if (match == null
                            || match.InstanceId != instanceId
                            || !string.Equals(NormalizeAssetPath(match.MidMap), normalizedMidMap, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        int inside = points.Count == 0 ? 0 : CountPointsInside(points, match.RectMap);
                        if (!HasReliableMapBounds(points, match.RectMap, inside))
                        {
                            continue;
                        }

                        match.Score = inside * 1000 - (int)Math.Min(999, Math.Abs(match.RectMap.Width * match.RectMap.Height) / 1000f);
                        if (best == null || match.Score > best.Score)
                        {
                            best = match;
                        }
                    }
                }
            }

            if (best == null)
            {
                return false;
            }

            bounds = best.RectMap;
            return true;
        }

        private static void AddSpawnMatchedInstanceMapCandidates(
            List<ClientMapImageCandidate> candidates,
            AssetManager assetManager,
            NpcGenData data)
        {
            if (candidates == null || assetManager == null || data == null)
            {
                return;
            }

            List<PointF> points = BuildSpawnPoints(data);
            if (points.Count == 0)
            {
                return;
            }

            string[] scripts = GetInstanceMapScriptNames();
            for (int scriptIndex = 0; scriptIndex < scripts.Length; scriptIndex++)
            {
                foreach (string text in ReadScriptTexts(assetManager, scripts[scriptIndex]))
                {
                    List<InstanceMapMatch> matches = ParseInstanceMapMatches(text);
                    for (int i = 0; i < matches.Count; i++)
                    {
                        InstanceMapMatch match = matches[i];
                        if (match == null || string.IsNullOrWhiteSpace(match.MidMap))
                        {
                            continue;
                        }

                        if (IsGenericWorldMidMap(match.MidMap))
                        {
                            continue;
                        }

                        int inside = CountPointsInside(points, match.RectMap);
                        if (!HasStrongSpawnCoverage(points, inside)
                            || !HasReliableMapBounds(points, match.RectMap, inside))
                        {
                            continue;
                        }

                        int missing = points.Count - inside;
                        int areaPenalty = (int)Math.Min(500, Math.Abs(match.RectMap.Width * match.RectMap.Height) / 1000f);
                        candidates.Add(new ClientMapImageCandidate
                        {
                            MappedPath = "surfaces\\" + NormalizeAssetPath(match.MidMap),
                            Source = scripts[scriptIndex] + " spawn bounds",
                            FitToSpawns = false,
                            HasWorldBounds = true,
                            WorldBounds = match.RectMap,
                            DisplayName = match.DisplayName,
                            Score = 980 + inside * 25 - missing * 20 - areaPenalty
                        });
                    }
                }
            }
        }

        private static bool TryGetKnownTechnicalMidMap(string token, out string midMap)
        {
            midMap = string.Empty;
            switch (token)
            {
                case "a01":
                    midMap = "midmaps\\f4.dds";
                    return true;
                case "d01":
                    midMap = "midmaps\\s12.1.dds";
                    return true;
                case "d02":
                    midMap = "midmaps\\s12.2.dds";
                    return true;
                case "d03":
                    midMap = "midmaps\\s12.3.dds";
                    return true;
                case "d04":
                    midMap = "midmaps\\f17.dds";
                    return true;
                case "d05":
                    midMap = "midmaps\\f15.dds";
                    return true;
                case "d06":
                    midMap = "midmaps\\f18.dds";
                    return true;
                case "d07":
                    midMap = "midmaps\\f16.dds";
                    return true;
                case "k00":
                    midMap = "midmaps\\g1.dds";
                    return true;
                case "r01":
                case "r02":
                case "r03":
                    midMap = "midmaps\\f35.dds";
                    return true;
                case "r04":
                case "r05":
                case "r06":
                    midMap = "midmaps\\f38_4.dds";
                    return true;
                default:
                    return false;
            }
        }

        private static void AddInstanceMatchedPathIdCandidate(
            List<ClientMapImageCandidate> candidates,
            CacheSave database,
            string rawPathId,
            string rowName,
            InstanceMapMatch instanceMatch,
            int score,
            string source)
        {
            int pathId;
            string mappedPath;
            if (candidates == null
                || database == null
                || database.pathById == null
                || instanceMatch == null
                || string.IsNullOrWhiteSpace(instanceMatch.AssetNameToken)
                || !int.TryParse((rawPathId ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pathId)
                || pathId <= 0
                || !database.pathById.TryGetValue(pathId, out mappedPath)
                || !IsSupportedMapImagePath(mappedPath)
                || NormalizeSearchText(mappedPath).IndexOf(NormalizeSearchText(instanceMatch.AssetNameToken), StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            candidates.Add(new ClientMapImageCandidate
            {
                MappedPath = mappedPath,
                Source = source + " " + pathId.ToString(CultureInfo.InvariantCulture),
                FitToSpawns = IsInstanceMapImagePath(mappedPath),
                DisplayName = string.IsNullOrWhiteSpace(rowName) ? instanceMatch.DisplayName : rowName.Trim(),
                Score = score
            });
        }

        private static void AddPathDataMapImageCandidates(
            List<ClientMapImageCandidate> candidates,
            CacheSave database,
            string mapDirectory,
            string mapDisplayName)
        {
            if (candidates == null || database == null || database.pathById == null)
            {
                return;
            }

            List<string> mapTokens = BuildMapSearchTokens(null, mapDirectory, mapDisplayName);
            foreach (KeyValuePair<int, string> item in database.pathById)
            {
                string mappedPath = item.Value ?? string.Empty;
                if (!IsSupportedMapImagePath(mappedPath) || !IsLikelyMapImagePath(mappedPath))
                {
                    continue;
                }

                int score = ScoreMapPath(mappedPath, mapTokens);
                if (score > 0)
                {
                    candidates.Add(new ClientMapImageCandidate
                    {
                        MappedPath = mappedPath,
                        Source = "path.data " + item.Key.ToString(CultureInfo.InvariantCulture),
                        FitToSpawns = IsInstanceMapImagePath(mappedPath),
                        Score = score + ScorePackageMapImagePreference(mappedPath)
                    });
                }
            }
        }

        private static void AddPckIndexMapImageCandidates(
            List<ClientMapImageCandidate> candidates,
            AssetManager assetManager,
            string mapDirectory,
            string mapDisplayName)
        {
            if (candidates == null || assetManager == null)
            {
                return;
            }

            List<string> mapTokens = BuildMapSearchTokens(null, mapDirectory, mapDisplayName);
            string[] packageNames = { "interfaces", "surfaces", "textures", "configs" };
            for (int i = 0; i < packageNames.Length; i++)
            {
                List<string> entries;
                if (!assetManager.TryEnumeratePckIndexEntries(packageNames[i], out entries) || entries == null)
                {
                    continue;
                }

                for (int j = 0; j < entries.Count; j++)
                {
                    string entry = entries[j] ?? string.Empty;
                    if (!IsSupportedMapImagePath(entry) || !IsLikelyMapImagePath(entry))
                    {
                        continue;
                    }

                    int score = ScoreMapPath(entry, mapTokens);
                    if (score <= 0)
                    {
                        continue;
                    }

                    candidates.Add(new ClientMapImageCandidate
                    {
                        MappedPath = packageNames[i] + "\\" + entry.Trim().Replace('/', '\\').Trim('\\'),
                        Source = packageNames[i] + ".pck",
                        FitToSpawns = IsInstanceMapImagePath(entry),
                        Score = score + ScorePackageMapImagePreference(entry)
                    });
                }
            }
        }

        private static void AddPathIdCandidate(
            List<ClientMapImageCandidate> candidates,
            CacheSave database,
            string rawPathId,
            int score,
            string source,
            string displayName = null)
        {
            int pathId;
            string mappedPath;
            if (candidates == null
                || database == null
                || database.pathById == null
                || !int.TryParse((rawPathId ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out pathId)
                || pathId <= 0
                || !database.pathById.TryGetValue(pathId, out mappedPath)
                || !IsSupportedMapImagePath(mappedPath))
            {
                return;
            }

            candidates.Add(new ClientMapImageCandidate
            {
                MappedPath = mappedPath,
                Source = source + " " + pathId.ToString(CultureInfo.InvariantCulture),
                FitToSpawns = IsInstanceMapImagePath(mappedPath),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
                Score = score
            });
        }

        private static ClientMapImageCandidate TryPromoteInstanceMapCandidate(
            AssetManager assetManager,
            NpcGenData data,
            ClientMapImageCandidate candidate)
        {
            if (assetManager == null
                || data == null
                || candidate == null
                || string.IsNullOrWhiteSpace(candidate.MappedPath)
                || !IsInstanceMapImagePath(candidate.MappedPath))
            {
                return null;
            }

            string assetToken = ExtractInstanceMapNameToken(candidate.MappedPath);
            if (string.IsNullOrWhiteSpace(assetToken))
            {
                return null;
            }

            List<PointF> points = BuildSpawnPoints(data);
            if (points.Count == 0)
            {
                return null;
            }

            string[] scripts = GetInstanceMapScriptNames();
            InstanceMapMatch best = null;
            for (int i = 0; i < scripts.Length; i++)
            {
                foreach (string text in ReadScriptTexts(assetManager, scripts[i]))
                {
                    List<InstanceMapMatch> matches = ParseInstanceMapMatches(text);
                    for (int j = 0; j < matches.Count; j++)
                    {
                        InstanceMapMatch match = matches[j];
                        if (match == null
                            || string.IsNullOrWhiteSpace(match.MidMap)
                            || !InstanceNameMatchesAssetToken(match.DisplayName, assetToken)
                            || IsGenericWorldMidMap(match.MidMap))
                        {
                            continue;
                        }

                        int inside = CountPointsInside(points, match.RectMap);
                        if (!HasReliableMapBounds(points, match.RectMap, inside))
                        {
                            continue;
                        }

                        match.Score = inside * 1000 - (int)Math.Min(999, Math.Abs(match.RectMap.Width * match.RectMap.Height) / 1000f);
                        if (best == null || match.Score > best.Score)
                        {
                            best = match;
                        }
                    }
                }
            }

            if (best == null)
            {
                return null;
            }

            return new ClientMapImageCandidate
            {
                MappedPath = "surfaces\\" + NormalizeAssetPath(best.MidMap),
                Source = candidate.Source + " promoted by instance.lua",
                FitToSpawns = false,
                HasWorldBounds = true,
                WorldBounds = best.RectMap,
                DisplayName = string.IsNullOrWhiteSpace(candidate.DisplayName) ? best.DisplayName : candidate.DisplayName,
                Score = candidate.Score + 80
            };
        }

        private static bool HasReliableMapBounds(List<PointF> points, RectangleF rect, int inside)
        {
            if (points == null || points.Count == 0 || inside <= 0 || rect.Width <= 0f || rect.Height <= 0f)
            {
                return false;
            }

            int requiredInside = Math.Max(3, points.Count / 3);
            if (inside < requiredInside)
            {
                return false;
            }

            RectangleF markerBounds = CalculatePointBounds(points);
            if (markerBounds.Width <= 0f || markerBounds.Height <= 0f)
            {
                return true;
            }

            float markerWidth = Math.Max(16f, markerBounds.Width);
            float markerHeight = Math.Max(16f, markerBounds.Height);
            float markerArea = markerWidth * markerHeight;
            float rectArea = rect.Width * rect.Height;

            return rectArea <= markerArea * 25f
                && rect.Width <= markerWidth * 8f
                && rect.Height <= markerHeight * 8f;
        }

        private static bool HasStrongSpawnCoverage(List<PointF> points, int inside)
        {
            if (points == null || points.Count == 0 || inside <= 0)
            {
                return false;
            }

            int requiredInside = Math.Max(3, (int)Math.Ceiling(points.Count * 0.8f));
            return inside >= requiredInside;
        }

        private static List<PointF> BuildSpawnPoints(NpcGenData data)
        {
            List<PointF> points = new List<PointF>();
            if (data == null)
            {
                return points;
            }

            points.AddRange(data.Areas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.ResourceAreas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.DynamicObjects.Select(item => new PointF(item.Position.X, item.Position.Z)));
            return points;
        }

        private static PointF? TryGetAreaFocusPoint(NpcGenData data, int areaIndex)
        {
            if (data == null || areaIndex < 0 || areaIndex >= data.Areas.Count)
            {
                return null;
            }

            PointF3 position = data.Areas[areaIndex].Position;
            return new PointF(position.X, position.Z);
        }

        private static string ExtractInstanceMapNameToken(string mappedPath)
        {
            string normalizedPath = (mappedPath ?? string.Empty).Replace('/', '\\');
            string name = Path.GetFileNameWithoutExtension(normalizedPath);
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            name = name.Replace("_地图", string.Empty)
                .Replace("_地圖", string.Empty)
                .Replace("_map", string.Empty)
                .Replace("地图", string.Empty)
                .Replace("地圖", string.Empty)
                .Trim();
            return NormalizeSearchText(name);
        }

        private static bool InstanceNameMatchesAssetToken(string instanceName, string assetToken)
        {
            string name = NormalizeSearchText(instanceName);
            string token = NormalizeSearchText(assetToken);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            name = Regex.Replace(name, @"\d+$", string.Empty);
            return token.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsGenericWorldMidMap(string midMap)
        {
            string fileName = Path.GetFileNameWithoutExtension((midMap ?? string.Empty).Replace('/', '\\'));
            return Regex.IsMatch(fileName ?? string.Empty, @"^s\d", RegexOptions.IgnoreCase);
        }

        private static Bitmap TryLoadMappedBitmap(AssetManager assetManager, string mappedPath)
        {
            if (assetManager == null || string.IsNullOrWhiteSpace(mappedPath))
            {
                return null;
            }

            try
            {
                string resolved = assetManager.ResolveResourcePath(mappedPath);
                if (!string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved))
                {
                    return TryLoadBitmap(File.ReadAllBytes(resolved), mappedPath);
                }
            }
            catch
            {
            }

            string normalized = NormalizeAssetPath(mappedPath);
            int separator = normalized.IndexOf('\\');
            if (separator <= 0 || separator >= normalized.Length - 1)
            {
                return null;
            }

            string package = normalized.Substring(0, separator);
            string relative = normalized.Substring(separator + 1);
            string extracted = TryFindExtractedPackageResource(package, relative);
            if (!string.IsNullOrWhiteSpace(extracted) && File.Exists(extracted))
            {
                return TryLoadBitmap(File.ReadAllBytes(extracted), mappedPath);
            }

            byte[] payload;
            string error;
            if (assetManager.TryReadPackageEntry(package, relative, out payload, out error))
            {
                return TryLoadBitmap(payload, mappedPath);
            }

            return TryLoadNumberedSurfaceBitmap(assetManager, package, relative);
        }

        private static Bitmap TryLoadNumberedSurfaceBitmap(AssetManager assetManager, string package, string relative)
        {
            if (assetManager == null
                || !string.Equals((package ?? string.Empty).Trim(), "surfaces", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(relative))
            {
                return null;
            }

            string normalizedRelative = NormalizeAssetPath(relative);
            for (int i = 1; i <= 9; i++)
            {
                string numberedPackage = i.ToString(CultureInfo.InvariantCulture) + "surfaces";
                string extracted = TryFindExtractedPackageResource(numberedPackage, normalizedRelative);
                if (!string.IsNullOrWhiteSpace(extracted) && File.Exists(extracted))
                {
                    Bitmap extractedBitmap = TryLoadBitmap(File.ReadAllBytes(extracted), numberedPackage + "\\" + normalizedRelative);
                    if (extractedBitmap != null)
                    {
                        return extractedBitmap;
                    }
                }

                try
                {
                    byte[] payload;
                    string error;
                    if (assetManager.TryReadPackageEntry(numberedPackage, normalizedRelative, out payload, out error))
                    {
                        Bitmap bitmap = TryLoadBitmap(payload, numberedPackage + "\\" + normalizedRelative);
                        if (bitmap != null)
                        {
                            return bitmap;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static string TryFindExtractedPackageResource(string package, string relative)
        {
            string normalizedPackage = (package ?? string.Empty).Trim().TrimEnd('\\', '/');
            string normalizedRelative = NormalizeAssetPath(relative);
            if (string.IsNullOrWhiteSpace(normalizedPackage) || string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return string.Empty;
            }

            string[] roots = BuildPackageExtractionRoots(normalizedPackage);
            for (int i = 0; i < roots.Length; i++)
            {
                string path = Path.Combine(roots[i], normalizedRelative);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private static string[] BuildPackageExtractionRoots(string package)
        {
            List<string> roots = new List<string>();
            string packageFolder = package + ".pck.files";
            AddPackageExtractionRoot(roots, Path.Combine(AssetManager.WorkspaceRootPath ?? string.Empty, "resources", packageFolder));

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            AddPackageExtractionRoot(roots, Path.Combine(gameRoot, "resources", packageFolder));
            return roots.ToArray();
        }

        private static void AddExtractedWorkspacePackageRoots(List<string> roots, string workspaceRoot, string packageFolder)
        {
            if (roots == null
                || string.IsNullOrWhiteSpace(workspaceRoot)
                || string.IsNullOrWhiteSpace(packageFolder)
                || !Directory.Exists(workspaceRoot))
            {
                return;
            }

            try
            {
                DirectoryInfo[] directories = new DirectoryInfo(workspaceRoot).GetDirectories();
                Array.Sort(directories, (a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
                for (int i = 0; i < directories.Length; i++)
                {
                    AddPackageExtractionRoot(roots, Path.Combine(directories[i].FullName, "resources", packageFolder));
                }
            }
            catch
            {
            }
        }

        private static void AddPackageExtractionRoot(List<string> roots, string path)
        {
            if (roots == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string normalized;
            try
            {
                normalized = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }

            if (Directory.Exists(normalized) && !roots.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                roots.Add(normalized);
            }
        }

        private static Bitmap TryLoadBitmap(byte[] payload, string path)
        {
            if (payload == null || payload.Length == 0)
            {
                return null;
            }

            try
            {
                string extension = Path.GetExtension(path) ?? string.Empty;
                if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    return DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.UNKNOWN)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT3)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT5)
                        ?? DDS.LoadImage(payload, true, DDSReader.Utils.PixelFormat.DXT1);
                }
                if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
                {
                    return new TgaImageService().TryLoad(payload);
                }

                using (MemoryStream stream = new MemoryStream(payload, false))
                using (Image image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<NpcGenMapMarker> BuildMarkers(
            NpcGenData data,
            ISessionService sessionService,
            NpcGenEntityLookupService entityLookupService)
        {
            if (data == null)
            {
                yield break;
            }

            eListCollection listCollection = sessionService != null ? sessionService.ListCollection : null;
            CacheSave database = sessionService != null ? sessionService.Database : null;

            for (int i = 0; i < data.Areas.Count; i++)
            {
                NpcGenArea area = data.Areas[i];
                if (area.Entries.Count == 0)
                {
                    yield return new NpcGenMapMarker
                    {
                        AreaIndex = i,
                        EntryIndex = -1,
                        Id = 0,
                        Name = "Empty spawn group",
                        Kind = area.AreaType == 1 ? "Monster" : "NPC",
                        Position = area.Position,
                        Extents = area.Extents,
                        Count = 0,
                        Color = Color.FromArgb(130, 130, 130)
                    };
                    continue;
                }

                for (int entryIndex = 0; entryIndex < area.Entries.Count; entryIndex++)
                {
                    NpcGenEntry entry = area.Entries[entryIndex];
                    int id = entry != null ? entry.Id : 0;
                    NpcGenEntityInfo info = entityLookupService != null
                        ? entityLookupService.Resolve(listCollection, database, id)
                        : null;
                    yield return new NpcGenMapMarker
                    {
                        AreaIndex = i,
                        EntryIndex = entryIndex,
                        Id = id,
                        Name = ResolveName(info, id),
                        Kind = area.AreaType == 1 ? "Monster" : "NPC",
                        Position = area.Position,
                        Extents = area.Extents,
                        Count = entry != null ? entry.Num : 0,
                        Icon = info != null ? info.Icon : null,
                        Color = area.AreaType == 1 ? Color.FromArgb(245, 138, 71) : Color.FromArgb(73, 169, 245)
                    };
                }
            }

            for (int i = 0; i < data.ResourceAreas.Count; i++)
            {
                NpcGenResourceArea area = data.ResourceAreas[i];
                NpcGenResourceEntry firstEntry = area.Entries.FirstOrDefault();
                int id = firstEntry != null ? firstEntry.TemplateId : 0;
                yield return new NpcGenMapMarker
                {
                    AreaIndex = i,
                    EntryIndex = 0,
                    Id = id,
                    Name = id == 0 ? "Resource area" : "Resource " + id.ToString(CultureInfo.InvariantCulture),
                    Kind = "Resource",
                    Position = area.Position,
                    Extents = new PointF3(area.ExtX, area.ExtY, area.ExtZ),
                    Count = area.Entries.Count,
                    Color = Color.FromArgb(98, 201, 91)
                };
            }

            for (int i = 0; i < data.DynamicObjects.Count; i++)
            {
                NpcGenDynamicObject item = data.DynamicObjects[i];
                yield return new NpcGenMapMarker
                {
                    AreaIndex = i,
                    EntryIndex = 0,
                    Id = item.DynamicObjectId,
                    Name = "Dynamic object " + item.DynamicObjectId.ToString(CultureInfo.InvariantCulture),
                    Kind = "Dynamic",
                    Position = item.Position,
                    Extents = new PointF3(4f, 4f, 4f),
                    Count = 1,
                    Color = Color.FromArgb(218, 190, 82)
                };
            }
        }

        private static string ResolveName(NpcGenEntityInfo info, int id)
        {
            if (info != null && !string.IsNullOrWhiteSpace(info.Name))
            {
                return info.Name;
            }
            return id == 0 ? "NONE!" : "ID " + id.ToString(CultureInfo.InvariantCulture);
        }

        private static RectangleF BuildWorldBounds(TerrainPreviewConfig config, NpcGenData data)
        {
            float worldWidth = config.AreaWidth > 0 && config.GridSize > 0f
                ? config.AreaWidth * Math.Max(1, config.Columns) * config.GridSize
                : 0f;
            float worldHeight = config.AreaHeight > 0 && config.GridSize > 0f
                ? config.AreaHeight * Math.Max(1, config.Rows) * config.GridSize
                : 0f;

            if (worldWidth > 0f && worldHeight > 0f)
            {
                RectangleF cfgBounds = new RectangleF(-worldWidth / 2f, -worldHeight / 2f, worldWidth, worldHeight);
                return ExpandToIncludeMarkers(cfgBounds, data, 32f);
            }

            RectangleF markerBounds = CalculateMarkerBounds(data);
            if (markerBounds.Width <= 0f || markerBounds.Height <= 0f)
            {
                return new RectangleF(-512f, -512f, 1024f, 1024f);
            }
            return Inflate(markerBounds, 64f);
        }

        private static RectangleF BuildSpawnFittedWorldBounds(NpcGenData data, Bitmap background, RectangleF fallback)
        {
            RectangleF markerBounds = CalculateMarkerBounds(data);
            if (markerBounds.Width <= 0f || markerBounds.Height <= 0f || background == null || background.Width <= 0 || background.Height <= 0)
            {
                return fallback;
            }

            float padding = Math.Max(48f, Math.Max(markerBounds.Width, markerBounds.Height) * 0.18f);
            RectangleF fitted = Inflate(markerBounds, padding);
            float imageAspect = (float)background.Width / Math.Max(1, background.Height);
            return FitBoundsToAspect(fitted, imageAspect);
        }

        private static RectangleF FitBoundsToAspect(RectangleF bounds, float targetAspect)
        {
            if (bounds.Width <= 0f || bounds.Height <= 0f || targetAspect <= 0f)
            {
                return bounds;
            }

            float currentAspect = bounds.Width / bounds.Height;
            float centerX = bounds.Left + bounds.Width / 2f;
            float centerY = bounds.Top + bounds.Height / 2f;
            if (currentAspect < targetAspect)
            {
                float width = bounds.Height * targetAspect;
                return new RectangleF(centerX - width / 2f, bounds.Top, width, bounds.Height);
            }

            float height = bounds.Width / targetAspect;
            return new RectangleF(bounds.Left, centerY - height / 2f, bounds.Width, height);
        }

        private static RectangleF ExpandToIncludeMarkers(RectangleF bounds, NpcGenData data, float padding)
        {
            RectangleF markerBounds = CalculateMarkerBounds(data);
            if (markerBounds.Width <= 0f || markerBounds.Height <= 0f)
            {
                return bounds;
            }

            float left = Math.Min(bounds.Left, markerBounds.Left - padding);
            float top = Math.Min(bounds.Top, markerBounds.Top - padding);
            float right = Math.Max(bounds.Right, markerBounds.Right + padding);
            float bottom = Math.Max(bounds.Bottom, markerBounds.Bottom + padding);
            return RectangleF.FromLTRB(left, top, right, bottom);
        }

        private static RectangleF CalculateMarkerBounds(NpcGenData data)
        {
            if (data == null)
            {
                return RectangleF.Empty;
            }

            List<PointF> points = new List<PointF>();
            points.AddRange(data.Areas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.ResourceAreas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.DynamicObjects.Select(item => new PointF(item.Position.X, item.Position.Z)));
            if (points.Count == 0)
            {
                return RectangleF.Empty;
            }

            float minX = points.Min(p => p.X);
            float maxX = points.Max(p => p.X);
            float minZ = points.Min(p => p.Y);
            float maxZ = points.Max(p => p.Y);
            return RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
        }

        private static RectangleF CalculatePointBounds(List<PointF> points)
        {
            if (points == null || points.Count == 0)
            {
                return RectangleF.Empty;
            }

            float minX = points.Min(p => p.X);
            float maxX = points.Max(p => p.X);
            float minZ = points.Min(p => p.Y);
            float maxZ = points.Max(p => p.Y);
            return RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
        }

        private static RectangleF Inflate(RectangleF rect, float padding)
        {
            rect.Inflate(padding, padding);
            if (rect.Width < 64f)
            {
                rect.Inflate((64f - rect.Width) / 2f, 0f);
            }
            if (rect.Height < 64f)
            {
                rect.Inflate(0f, (64f - rect.Height) / 2f);
            }
            return rect;
        }

        private static string ResolveMapDirectory(NpcGenData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.FilePath))
            {
                return string.Empty;
            }
            string directory = Path.GetDirectoryName(data.FilePath);
            return directory ?? string.Empty;
        }

        private static TerrainPreviewConfig LoadTerrainConfig(string mapDirectory)
        {
            TerrainPreviewConfig config = new TerrainPreviewConfig();
            config.Rows = 1;
            config.Columns = 1;
            config.GridSize = 1f;

            string path = string.IsNullOrWhiteSpace(mapDirectory)
                ? string.Empty
                : Path.Combine(mapDirectory, "terrain.cfg");
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return config;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split('=');
                if (parts.Length < 2)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[1].Trim();
                int intValue;
                float floatValue;
                if (string.Equals(key, "nRow", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                {
                    config.Rows = intValue;
                }
                else if (string.Equals(key, "nCol", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                {
                    config.Columns = intValue;
                }
                else if (string.Equals(key, "nAreaWidth", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                {
                    config.AreaWidth = intValue;
                }
                else if (string.Equals(key, "nAreaHeight", StringComparison.OrdinalIgnoreCase) && int.TryParse(value, out intValue))
                {
                    config.AreaHeight = intValue;
                }
                else if (string.Equals(key, "vGridSize", StringComparison.OrdinalIgnoreCase)
                    && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                {
                    config.GridSize = floatValue;
                }
            }

            return config;
        }

        private static Bitmap TryBuildHeightMapImage(string mapDirectory, TerrainPreviewConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mapDirectory))
                {
                    return null;
                }

                string hmapDirectory = Path.Combine(mapDirectory, "map");
                if (!Directory.Exists(hmapDirectory))
                {
                    return null;
                }

                string[] hmapFiles = Directory.GetFiles(hmapDirectory, "*.hmap")
                    .OrderBy(path => ExtractLeadingNumber(Path.GetFileNameWithoutExtension(path)))
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (hmapFiles.Length == 0)
                {
                    return null;
                }

                int tileColumns = Math.Max(1, config.Columns);
                int tileRows = Math.Max(1, config.Rows);
                if (hmapFiles.Length < tileColumns * tileRows)
                {
                    tileColumns = 1;
                    tileRows = 1;
                }

                byte[] bytes = File.ReadAllBytes(hmapFiles[0]);
                int side = (int)Math.Sqrt(bytes.Length);
                if (side < 16 || side * side != bytes.Length)
                {
                    return null;
                }

                int tilePreviewSide = Math.Max(16, Math.Min(MaxPreviewSide / Math.Max(tileColumns, tileRows), side));
                Bitmap bitmap = new Bitmap(tilePreviewSide * tileColumns, tilePreviewSide * tileRows, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.FromArgb(33, 36, 34));
                }

                for (int tile = 0; tile < tileColumns * tileRows; tile++)
                {
                    byte[] tileBytes = tile == 0 ? bytes : File.ReadAllBytes(hmapFiles[tile]);
                    int tileSide = (int)Math.Sqrt(tileBytes.Length);
                    if (tileSide < 16 || tileSide * tileSide != tileBytes.Length)
                    {
                        continue;
                    }

                    int tileX = tile % tileColumns;
                    int tileY = tile / tileColumns;
                    for (int y = 0; y < tilePreviewSide; y++)
                    {
                        int srcY = y * tileSide / tilePreviewSide;
                        for (int x = 0; x < tilePreviewSide; x++)
                        {
                            int srcX = x * tileSide / tilePreviewSide;
                            int index = Math.Min(tileBytes.Length - 1, srcY * tileSide + srcX);
                            int h = tileBytes[index];
                            int right = tileBytes[Math.Min(tileBytes.Length - 1, srcY * tileSide + Math.Min(tileSide - 1, srcX + 1))];
                            int down = tileBytes[Math.Min(tileBytes.Length - 1, Math.Min(tileSide - 1, srcY + 1) * tileSide + srcX)];
                            int shade = ClampToByte(74 + (h / 2) + ((h - right) + (h - down)));
                            Color color = BuildTerrainColor(h, shade);
                            bitmap.SetPixel(tileX * tilePreviewSide + x, tileY * tilePreviewSide + y, color);
                        }
                    }
                }
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static InstanceMapMatch TryFindInstanceMapMatch(AssetManager assetManager, NpcGenData data)
        {
            if (assetManager == null || data == null)
            {
                return null;
            }

            RectangleF markerBounds = CalculateMarkerBounds(data);
            if (markerBounds.Width <= 0f || markerBounds.Height <= 0f)
            {
                return null;
            }

            List<PointF> points = new List<PointF>();
            points.AddRange(data.Areas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.ResourceAreas.Select(area => new PointF(area.Position.X, area.Position.Z)));
            points.AddRange(data.DynamicObjects.Select(item => new PointF(item.Position.X, item.Position.Z)));
            if (points.Count == 0)
            {
                return null;
            }

            string[] scripts = GetInstanceMapScriptNames();
            InstanceMapMatch best = null;
            for (int i = 0; i < scripts.Length; i++)
            {
                foreach (string text in ReadScriptTexts(assetManager, scripts[i]))
                {
                    List<InstanceMapMatch> matches = ParseInstanceMapMatches(text);
                    for (int j = 0; j < matches.Count; j++)
                    {
                        InstanceMapMatch match = matches[j];
                        if (match == null
                            || string.IsNullOrWhiteSpace(match.MidMap)
                            || IsGenericWorldMidMap(match.MidMap))
                        {
                            continue;
                        }

                        int inside = CountPointsInside(points, match.RectMap);
                        if (!HasStrongSpawnCoverage(points, inside)
                            || !HasReliableMapBounds(points, match.RectMap, inside))
                        {
                            continue;
                        }

                        match.Score = inside * 1000 - (int)Math.Min(999, Math.Abs(match.RectMap.Width * match.RectMap.Height) / 1000f);
                        if (best == null || match.Score > best.Score)
                        {
                            best = match;
                        }
                    }
                }
            }

            return best;
        }

        private static bool TryReadScriptText(AssetManager assetManager, string scriptName, out string text)
        {
            text = string.Empty;
            foreach (string candidate in ReadScriptTexts(assetManager, scriptName))
            {
                text = candidate;
                return true;
            }

            return false;
        }

        private static string[] GetInstanceMapScriptNames()
        {
            return new[]
            {
                "instancedynamic.lua",
                "instance.lua",
                "instanceworld.lua",
                "instancebattle.lua"
            };
        }

        private static IEnumerable<string> ReadScriptTexts(AssetManager assetManager, string scriptName)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            string[] candidates = BuildScriptPathCandidates(scriptName);
            if (assetManager != null)
            {
                foreach (string text in ReadPackagedScriptTexts(assetManager, candidates))
                {
                    if (seen.Add(text))
                    {
                        yield return text;
                    }
                }

                foreach (string text in ReadIndexedScriptTexts(assetManager, scriptName))
                {
                    if (seen.Add(text))
                    {
                        yield return text;
                    }
                }
            }

            foreach (string text in ReadLooseScriptTexts(candidates))
            {
                if (seen.Add(text))
                {
                    yield return text;
                }
            }
        }

        private static IEnumerable<string> ReadPackagedScriptTexts(AssetManager assetManager, string[] candidates)
        {
            if (assetManager == null || candidates == null)
            {
                yield break;
            }

            string[] packages = { "script", "configs" };
            for (int packageIndex = 0; packageIndex < packages.Length; packageIndex++)
            {
                for (int i = 0; i < candidates.Length; i++)
                {
                    byte[] payload;
                    string error;
                    if (assetManager.TryReadPackageEntry(packages[packageIndex], candidates[i], out payload, out error)
                        && payload != null
                        && payload.Length > 0)
                    {
                        string text = DecodeScriptText(payload);
                        if (LooksLikeInstanceScript(text))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }

        private static bool TryReadIndexedScriptText(AssetManager assetManager, string scriptName, out string text)
        {
            text = string.Empty;
            foreach (string candidate in ReadIndexedScriptTexts(assetManager, scriptName))
            {
                text = candidate;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> ReadIndexedScriptTexts(AssetManager assetManager, string scriptName)
        {
            if (assetManager == null || string.IsNullOrWhiteSpace(scriptName))
            {
                yield break;
            }

            List<string> entries;
            string[] packages = { "script", "configs" };
            for (int packageIndex = 0; packageIndex < packages.Length; packageIndex++)
            {
                if (!assetManager.TryEnumeratePckIndexEntries(packages[packageIndex], out entries) || entries == null)
                {
                    continue;
                }

                string targetName = Path.GetFileName(NormalizeAssetPath(scriptName));
                for (int i = 0; i < entries.Count; i++)
                {
                    string entry = NormalizeAssetPath(entries[i]);
                    if (!string.Equals(Path.GetFileName(entry), targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    byte[] payload;
                    string error;
                    if (assetManager.TryReadPackageEntry(packages[packageIndex], entry, out payload, out error)
                        && payload != null
                        && payload.Length > 0)
                    {
                        string candidate = DecodeScriptText(payload);
                        if (LooksLikeInstanceScript(candidate))
                        {
                            yield return candidate;
                        }
                    }
                }
            }
        }

        private static string[] BuildScriptPathCandidates(string scriptName)
        {
            List<string> candidates = new List<string>();
            string normalized = NormalizeAssetPath(scriptName);
            AddScriptPathCandidate(candidates, normalized);
            AddScriptPathCandidate(candidates, Path.Combine("config", normalized));
            AddScriptPathCandidate(candidates, Path.Combine("script", normalized));
            AddScriptPathCandidate(candidates, Path.Combine("data", "script", normalized));
            return candidates.ToArray();
        }

        private static void AddScriptPathCandidate(List<string> candidates, string path)
        {
            string normalized = NormalizeAssetPath(path);
            if (!string.IsNullOrWhiteSpace(normalized) && !candidates.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(normalized);
            }
        }

        private static bool TryReadLooseScriptText(string[] scriptCandidates, out string text)
        {
            text = string.Empty;
            foreach (string candidate in ReadLooseScriptTexts(scriptCandidates))
            {
                text = candidate;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> ReadLooseScriptTexts(string[] scriptCandidates)
        {
            string[] roots = BuildScriptExtractionRoots();
            for (int i = 0; i < roots.Length; i++)
            {
                for (int j = 0; j < scriptCandidates.Length; j++)
                {
                    string path = Path.Combine(roots[i], scriptCandidates[j] ?? string.Empty);
                    string text;
                    if (TryReadLooseScriptPath(path, out text))
                    {
                        yield return text;
                    }
                }

                for (int j = 0; j < scriptCandidates.Length; j++)
                {
                    string targetName = Path.GetFileName(scriptCandidates[j] ?? string.Empty);
                    if (string.IsNullOrWhiteSpace(targetName))
                    {
                        continue;
                    }

                    string[] paths =
                    {
                        Path.Combine(roots[i], "config", targetName),
                        Path.Combine(roots[i], "script", "config", targetName),
                        Path.Combine(roots[i], "data", "script", "config", targetName)
                    };
                    for (int k = 0; k < paths.Length; k++)
                    {
                        string text;
                        if (TryReadLooseScriptPath(paths[k], out text))
                        {
                            yield return text;
                        }
                    }
                }
            }

            foreach (string text in FindLooseScriptsByFileName(scriptCandidates))
            {
                yield return text;
            }
        }

        private static bool TryFindLooseScriptByFileName(string[] scriptCandidates, out string text)
        {
            text = string.Empty;
            foreach (string candidate in FindLooseScriptsByFileName(scriptCandidates))
            {
                text = candidate;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> FindLooseScriptsByFileName(string[] scriptCandidates)
        {
            if (scriptCandidates == null || scriptCandidates.Length == 0)
            {
                yield break;
            }

            HashSet<string> targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scriptCandidates.Length; i++)
            {
                string targetName = Path.GetFileName(scriptCandidates[i] ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(targetName))
                {
                    targetNames.Add(targetName);
                }
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] roots =
            {
                Path.Combine(AssetManager.WorkspaceRootPath ?? string.Empty, "resources", "script.pck.files"),
                Path.Combine(AssetManager.GameRootPath ?? string.Empty, "resources", "script.pck.files"),
                Path.Combine(AssetManager.GameRootPath ?? string.Empty, "resources", "script"),
                Path.Combine(AssetManager.GameRootPath ?? string.Empty, "script")
            };

            for (int i = 0; i < roots.Length; i++)
            {
                string root = roots[i];
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                List<string> filesToRead = new List<string>();
                try
                {
                    foreach (string targetName in targetNames)
                    {
                        string[] files = Directory.GetFiles(root, targetName, SearchOption.AllDirectories);
                        Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                        for (int j = 0; j < files.Length; j++)
                        {
                            filesToRead.Add(files[j]);
                        }
                    }
                }
                catch
                {
                }

                for (int j = 0; j < filesToRead.Count; j++)
                {
                    string text;
                    if (TryReadLooseScriptPath(filesToRead[j], out text))
                    {
                        yield return text;
                    }
                }
            }
        }

        private static bool TryReadLooseScriptPath(string path, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                byte[] payload = File.ReadAllBytes(path);
                string candidate = DecodeScriptText(payload);
                if (LooksLikeInstanceScript(candidate))
                {
                    text = candidate;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static string[] BuildScriptExtractionRoots()
        {
            List<string> roots = new List<string>();
            AddScriptExtractionRoot(roots, Path.Combine(AssetManager.WorkspaceRootPath ?? string.Empty, "resources", "script.pck.files"));

            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            AddScriptExtractionRoot(roots, Path.Combine(gameRoot, "resources", "script.pck.files"));
            AddScriptExtractionRoot(roots, Path.Combine(gameRoot, "resources", "script"));
            AddScriptExtractionRoot(roots, Path.Combine(gameRoot, "script"));
            return roots.ToArray();
        }

        private static void AddExtractedWorkspaceScriptRoots(List<string> roots, string workspaceRoot)
        {
            if (roots == null || string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            {
                return;
            }

            try
            {
                DirectoryInfo[] directories = new DirectoryInfo(workspaceRoot).GetDirectories();
                Array.Sort(directories, (a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));
                for (int i = 0; i < directories.Length; i++)
                {
                    AddScriptExtractionRoot(roots, Path.Combine(directories[i].FullName, "resources", "script.pck.files"));
                }
            }
            catch
            {
            }
        }

        private static void AddScriptExtractionRoot(List<string> roots, string path)
        {
            if (roots == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string normalized;
            try
            {
                normalized = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }

            if (Directory.Exists(normalized) && !roots.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                roots.Add(normalized);
            }
        }

        private static bool LooksLikeInstanceScript(string text)
        {
            return !string.IsNullOrWhiteSpace(text)
                && Regex.IsMatch(text, @"Instance\s*\[\s*\d+\s*\]\s*=", RegexOptions.IgnoreCase)
                && Regex.IsMatch(text, @"rectMap\s*=\s*\{", RegexOptions.IgnoreCase)
                && Regex.IsMatch(text, @"MidMap\s*=\s*[""']", RegexOptions.IgnoreCase);
        }

        private static string BuildWorkspaceId(string gameRoot)
        {
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return "default";
            }

            string normalized = gameRoot.Trim().ToLowerInvariant();
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(normalized));
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private static string DecodeScriptText(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
            {
                return string.Empty;
            }

            string bytecodeMapText;
            if (TryBuildLuaBytecodeMapText(payload, out bytecodeMapText))
            {
                return bytecodeMapText;
            }

            if (payload.Length >= 2 && payload[0] == 0xFF && payload[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(payload);
            }
            if (payload.Length >= 3 && payload[0] == 0xEF && payload[1] == 0xBB && payload[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(payload);
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(payload);
            }
            catch
            {
            }

            return Encoding.GetEncoding(936).GetString(payload);
        }

        private static bool TryBuildLuaBytecodeMapText(byte[] payload, out string text)
        {
            text = string.Empty;
            if (payload == null
                || payload.Length < 12
                || payload[0] != 0x1B
                || payload[1] != (byte)'L'
                || payload[2] != (byte)'u'
                || payload[3] != (byte)'a')
            {
                return false;
            }

            try
            {
                LuaBytecodeReader reader = new LuaBytecodeReader(payload);
                List<LuaConstantToken> tokens = new List<LuaConstantToken>();
                if (!reader.TryRead(tokens) || tokens.Count == 0)
                {
                    return false;
                }

                List<InstanceMapMatch> matches = BuildLuaBytecodeMapMatches(tokens);
                if (matches.Count == 0)
                {
                    return false;
                }

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < matches.Count; i++)
                {
                    InstanceMapMatch match = matches[i];
                    sb.Append("Instance[")
                        .Append(match.InstanceId.ToString(CultureInfo.InvariantCulture))
                        .AppendLine("] = {");
                    sb.Append("name = \"")
                        .Append(EscapeLuaPseudoString(match.DisplayName))
                        .AppendLine("\",");
                    sb.Append("rectMap = {")
                        .Append(match.RectMap.Left.ToString("R", CultureInfo.InvariantCulture))
                        .Append(", ")
                        .Append(match.RectMap.Bottom.ToString("R", CultureInfo.InvariantCulture))
                        .Append(", ")
                        .Append(match.RectMap.Right.ToString("R", CultureInfo.InvariantCulture))
                        .Append(", ")
                        .Append(match.RectMap.Top.ToString("R", CultureInfo.InvariantCulture))
                        .AppendLine("},");
                    sb.Append("MidMap = \"")
                        .Append(EscapeLuaPseudoString(match.MidMap))
                        .AppendLine("\"");
                    sb.AppendLine("}");
                }

                text = sb.ToString();
                return true;
            }
            catch
            {
                text = string.Empty;
                return false;
            }
        }

        private static List<InstanceMapMatch> BuildLuaBytecodeMapMatches(List<LuaConstantToken> tokens)
        {
            List<InstanceMapMatch> matches = new List<InstanceMapMatch>();
            if (tokens == null || tokens.Count == 0)
            {
                return matches;
            }

            int previousMidMapIndex = -1;
            int currentInstanceId = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                string midMap = tokens[i].StringValue;
                if (string.IsNullOrWhiteSpace(midMap)
                    || !midMap.Replace('/', '\\').StartsWith("midmaps\\", StringComparison.OrdinalIgnoreCase)
                    || !midMap.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int[] rectIndices = FindPreviousNumberIndices(tokens, i, 4);
                if (rectIndices.Length != 4)
                {
                    previousMidMapIndex = i;
                    continue;
                }

                int rectStart = rectIndices[0];
                int inferredInstanceId = InferLuaBytecodeInstanceId(tokens, previousMidMapIndex + 1, rectStart);
                if (inferredInstanceId > 0)
                {
                    currentInstanceId = inferredInstanceId;
                }

                if (currentInstanceId <= 0)
                {
                    previousMidMapIndex = i;
                    continue;
                }

                float left = (float)tokens[rectIndices[0]].NumberValue.Value;
                float top = (float)tokens[rectIndices[1]].NumberValue.Value;
                float right = (float)tokens[rectIndices[2]].NumberValue.Value;
                float bottom = (float)tokens[rectIndices[3]].NumberValue.Value;
                RectangleF rect = RectangleF.FromLTRB(
                    Math.Min(left, right),
                    Math.Min(top, bottom),
                    Math.Max(left, right),
                    Math.Max(top, bottom));
                if (!LooksLikeWorldRect(rect))
                {
                    previousMidMapIndex = i;
                    continue;
                }

                string displayName = TryFindLuaBytecodeDisplayName(tokens, previousMidMapIndex + 1, rectStart);
                matches.Add(new InstanceMapMatch
                {
                    InstanceId = currentInstanceId,
                    DisplayName = string.IsNullOrWhiteSpace(displayName)
                        ? "Instance " + currentInstanceId.ToString(CultureInfo.InvariantCulture)
                        : displayName,
                    AssetNameToken = displayName,
                    MidMap = midMap,
                    RectMap = rect
                });

                previousMidMapIndex = i;
            }

            return matches;
        }

        private static bool LooksLikeWorldRect(RectangleF rect)
        {
            if (rect.Width <= 0f || rect.Height <= 0f)
            {
                return false;
            }

            float area = rect.Width * rect.Height;
            if (area <= 1f)
            {
                return false;
            }

            return true;
        }

        private static int[] FindPreviousNumberIndices(List<LuaConstantToken> tokens, int beforeIndex, int count)
        {
            List<int> indices = new List<int>();
            for (int i = beforeIndex - 1; i >= 0 && indices.Count < count; i--)
            {
                if (tokens[i].NumberValue.HasValue)
                {
                    indices.Add(i);
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private static int InferLuaBytecodeInstanceId(List<LuaConstantToken> tokens, int start, int end)
        {
            int instanceId = 0;
            int safeStart = Math.Max(0, start);
            int safeEnd = Math.Min(tokens != null ? tokens.Count : 0, Math.Max(start, end));
            for (int i = safeStart; i < safeEnd; i++)
            {
                double value;
                if (!tokens[i].NumberValue.HasValue)
                {
                    continue;
                }

                value = tokens[i].NumberValue.Value;
                int rounded = (int)Math.Round(value);
                if (Math.Abs(value - rounded) > 0.000001d)
                {
                    continue;
                }

                if (rounded >= 200 && rounded <= 699)
                {
                    instanceId = rounded;
                }
            }

            return instanceId;
        }

        private static string TryFindLuaBytecodeDisplayName(List<LuaConstantToken> tokens, int start, int end)
        {
            int safeStart = Math.Max(0, start);
            int safeEnd = Math.Min(tokens != null ? tokens.Count : 0, Math.Max(start, end));
            for (int i = safeEnd - 1; i >= safeStart; i--)
            {
                string value = tokens[i].StringValue;
                if (string.IsNullOrWhiteSpace(value)
                    || value.IndexOf('\\') >= 0
                    || value.IndexOf('/') >= 0
                    || value.Equals("path", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("rectMap", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("MidMap", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("MiniMap", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("loadingImage", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("detailTexture", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return value.Trim();
            }

            return string.Empty;
        }

        private static string EscapeLuaPseudoString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static List<InstanceMapMatch> ParseInstanceMapMatches(string text)
        {
            List<InstanceMapMatch> results = new List<InstanceMapMatch>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return results;
            }

            for (int index = 0; index < text.Length;)
            {
                Match header = Regex.Match(text.Substring(index), @"Instance\s*\[\s*(?<id>\d+)\s*\]\s*=");
                if (!header.Success)
                {
                    break;
                }

                int headerIndex = index + header.Index;
                int openBrace = text.IndexOf('{', headerIndex + header.Length);
                if (openBrace < 0)
                {
                    break;
                }

                int closeBrace = FindMatchingBrace(text, openBrace);
                if (closeBrace < 0)
                {
                    break;
                }

                int instanceId;
                if (!int.TryParse(header.Groups["id"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out instanceId))
                {
                    instanceId = 0;
                }

                string body = text.Substring(openBrace + 1, closeBrace - openBrace - 1);
                string name = ExtractQuotedField(body, "name");
                MatchCollection rectMatches = Regex.Matches(body, @"rectMap\s*=\s*\{\s*(?<left>-?\d+(?:\.\d+)?)\s*,\s*(?<top>-?\d+(?:\.\d+)?)\s*,\s*(?<right>-?\d+(?:\.\d+)?)\s*,\s*(?<bottom>-?\d+(?:\.\d+)?)\s*\}", RegexOptions.IgnoreCase);
                MatchCollection midMapMatches = Regex.Matches(body, @"MidMap\s*=\s*(?<quote>[""'])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>", RegexOptions.IgnoreCase);
                int count = Math.Min(rectMatches.Count, midMapMatches.Count);
                for (int i = 0; i < count; i++)
                {
                    RectangleF rect;
                    if (!TryParseRectMap(rectMatches[i], out rect))
                    {
                        continue;
                    }

                    string midMap = UnescapeLuaString(midMapMatches[i].Groups["value"].Value);
                    if (string.IsNullOrWhiteSpace(midMap))
                    {
                        continue;
                    }

                    results.Add(new InstanceMapMatch
                    {
                        InstanceId = instanceId,
                        DisplayName = name,
                        AssetNameToken = name,
                        MidMap = midMap,
                        RectMap = rect
                    });
                }

                index = closeBrace + 1;
            }

            return results;
        }

        private static string ExtractQuotedField(string body, string fieldName)
        {
            Match match = Regex.Match(
                body ?? string.Empty,
                @"(?:^|[,;\r\n])\s*" + Regex.Escape(fieldName) + @"\s*=\s*(?<quote>[""'])(?<value>(?:\\.|(?!\k<quote>).)*)\k<quote>",
                RegexOptions.IgnoreCase);
            return match.Success ? UnescapeLuaString(match.Groups["value"].Value).Trim() : string.Empty;
        }

        private static int FindMatchingBrace(string text, int openBrace)
        {
            int depth = 0;
            bool inString = false;
            char quote = '\0';
            for (int i = openBrace; i < text.Length; i++)
            {
                char ch = text[i];
                if (inString)
                {
                    if (ch == '\\')
                    {
                        i++;
                        continue;
                    }
                    if (ch == quote)
                    {
                        inString = false;
                    }
                    continue;
                }

                if (ch == '"' || ch == '\'')
                {
                    inString = true;
                    quote = ch;
                    continue;
                }
                if (ch == '{')
                {
                    depth++;
                }
                else if (ch == '}')
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

        private static bool TryParseRectMap(Match match, out RectangleF rect)
        {
            rect = RectangleF.Empty;
            float left;
            float top;
            float right;
            float bottom;
            if (match == null
                || !float.TryParse(match.Groups["left"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out left)
                || !float.TryParse(match.Groups["top"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out top)
                || !float.TryParse(match.Groups["right"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out right)
                || !float.TryParse(match.Groups["bottom"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out bottom))
            {
                return false;
            }

            float minX = Math.Min(left, right);
            float maxX = Math.Max(left, right);
            float minZ = Math.Min(top, bottom);
            float maxZ = Math.Max(top, bottom);
            rect = RectangleF.FromLTRB(minX, minZ, maxX, maxZ);
            return rect.Width > 0f && rect.Height > 0f;
        }

        private static int CountPointsInside(List<PointF> points, RectangleF rect)
        {
            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (rect.Contains(points[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountMarkersInsideBounds(List<NpcGenMapMarker> markers, RectangleF bounds)
        {
            if (markers == null || bounds.Width <= 0f || bounds.Height <= 0f)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < markers.Count; i++)
            {
                NpcGenMapMarker marker = markers[i];
                if (marker != null && bounds.Contains(marker.Position.X, marker.Position.Z))
                {
                    count++;
                }
            }

            return count;
        }

        private static string UnescapeLuaString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\\"", "\"")
                .Replace("\\'", "'")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\t", "\t");
        }

        private static List<string> BuildSpawnNameTokens(
            NpcGenData data,
            eListCollection listCollection,
            CacheSave database,
            NpcGenEntityLookupService entityLookupService)
        {
            List<string> tokens = new List<string>();
            if (data == null || entityLookupService == null)
            {
                return tokens;
            }

            for (int i = 0; i < data.Areas.Count; i++)
            {
                NpcGenArea area = data.Areas[i];
                for (int j = 0; j < area.Entries.Count; j++)
                {
                    NpcGenEntry entry = area.Entries[j];
                    if (entry == null || entry.Id <= 0)
                    {
                        continue;
                    }

                    NpcGenEntityInfo info = entityLookupService.Resolve(listCollection, database, entry.Id);
                    AddSpawnNameTokens(tokens, ResolveName(info, entry.Id));
                }
            }

            return tokens;
        }

        private static void AddSpawnNameTokens(List<string> tokens, string name)
        {
            if (tokens == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            string[] parts = name.Split(new[] { ' ', '\t', '-', '_', '\'', '"', '[', ']', '(', ')', '/', '\\', ',', '.', ':' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = NormalizeSearchText(parts[i]);
                if (token.Length >= 5
                    && !IsCommonSpawnToken(token)
                    && !tokens.Contains(token))
                {
                    tokens.Add(token);
                }
            }
        }

        private static bool IsCommonSpawnToken(string token)
        {
            switch (token)
            {
                case "monster":
                case "common":
                case "spawn":
                case "point":
                case "resource":
                case "reward":
                case "teleporter":
                case "entrance":
                    return true;
                default:
                    return false;
            }
        }

        private static int ScoreTextAgainstSpawnNames(string text, List<string> spawnNameTokens)
        {
            string normalizedText = NormalizeSearchText(text);
            if (string.IsNullOrWhiteSpace(normalizedText) || spawnNameTokens == null || spawnNameTokens.Count == 0)
            {
                return 0;
            }

            int matches = 0;
            for (int i = 0; i < spawnNameTokens.Count; i++)
            {
                string token = spawnNameTokens[i];
                if (!string.IsNullOrWhiteSpace(token)
                    && normalizedText.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches++;
                }
            }

            if (matches == 0)
            {
                return 0;
            }

            return Math.Min(95, 45 + (matches - 1) * 10);
        }

        private static List<string> BuildMapSearchTokens(NpcGenData data, string mapDirectory, string mapDisplayName)
        {
            List<string> tokens = new List<string>();
            AddToken(tokens, Path.GetFileName((mapDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));

            string display = mapDisplayName ?? string.Empty;
            AddToken(tokens, display);
            int parenStart = display.LastIndexOf('(');
            int parenEnd = display.LastIndexOf(')');
            if (parenStart >= 0 && parenEnd > parenStart)
            {
                AddToken(tokens, display.Substring(parenStart + 1, parenEnd - parenStart - 1));
                display = display.Substring(0, parenStart).Trim();
            }

            string[] parts = display.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                AddToken(tokens, parts[i]);
            }

            AddToken(tokens, data != null ? Path.GetDirectoryName(data.FilePath) : string.Empty);
            return tokens;
        }

        private static bool IsPaddedInstanceMap(string mapDirectory, string mapDisplayName)
        {
            string folder = Path.GetFileName((mapDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (IsPaddedInstanceToken(folder))
            {
                return true;
            }

            Match match = Regex.Match(mapDisplayName ?? string.Empty, @"\((?<token>[A-Za-z]+\d+)\)");
            return match.Success && IsPaddedInstanceToken(match.Groups["token"].Value);
        }

        private static bool IsWorldMapCode(string mapDirectory, string mapDisplayName)
        {
            return !string.IsNullOrWhiteSpace(NormalizeWorldMapToken(GetTechnicalMapToken(mapDirectory, mapDisplayName)));
        }

        private static bool TryResolveTechnicalInstanceId(string mapDirectory, string mapDisplayName, out int instanceId)
        {
            instanceId = 0;
            string token = GetTechnicalMapToken(mapDirectory, mapDisplayName);
            if (TryGetKnownTechnicalInstanceId(token, out instanceId))
            {
                return true;
            }

            Match match = Regex.Match(token ?? string.Empty, @"^(?<prefix>[A-Za-z]+)0*(?<number>\d+)$");
            int number;
            if (!match.Success
                || !int.TryParse(match.Groups["number"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return false;
            }

            string prefix = match.Groups["prefix"].Value.ToLowerInvariant();
            if (prefix == "i" && number >= 1)
            {
                instanceId = 200 + number;
                return true;
            }

            if (prefix == "d" && number >= 1)
            {
                instanceId = 215 + number;
                return true;
            }

            if (prefix == "a")
            {
                if (number >= 1 && number <= 6)
                {
                    instanceId = 209 + number;
                    return true;
                }

                if (number == 7)
                {
                    instanceId = 223;
                    return true;
                }
            }

            if (prefix == "k")
            {
                if (number == 0)
                {
                    instanceId = 200;
                    return true;
                }

                if (number == 3)
                {
                    instanceId = 235;
                    return true;
                }
            }

            if (prefix == "r")
            {
                if (number >= 1 && number <= 3)
                {
                    instanceId = 249 + number;
                    return true;
                }

                if (number >= 4 && number <= 6)
                {
                    instanceId = 249 + number;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetKnownTechnicalInstanceId(string token, out int instanceId)
        {
            instanceId = 0;
            switch (NormalizeTechnicalMapToken(token))
            {
                case "a01":
                    instanceId = 210;
                    return true;
                case "a02":
                    instanceId = 211;
                    return true;
                case "a03":
                    instanceId = 212;
                    return true;
                case "a05":
                    instanceId = 214;
                    return true;
                case "a06":
                    instanceId = 215;
                    return true;
                case "a08":
                    instanceId = 224;
                    return true;
                case "a09":
                    instanceId = 225;
                    return true;
                case "a10":
                    instanceId = 226;
                    return true;
                case "a14":
                    instanceId = 231;
                    return true;
                case "a19":
                    instanceId = 237;
                    return true;
                case "a21":
                    instanceId = 242;
                    return true;
                default:
                    return false;
            }
        }

        private static string GetTechnicalMapToken(string mapDirectory, string mapDisplayName)
        {
            string folder = Path.GetFileName((mapDirectory ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (Regex.IsMatch(folder ?? string.Empty, @"^[A-Za-z]+\d+$", RegexOptions.IgnoreCase))
            {
                return folder;
            }

            Match match = Regex.Match(mapDisplayName ?? string.Empty, @"\((?<token>[A-Za-z]+\d+)\)");
            if (match.Success)
            {
                return match.Groups["token"].Value;
            }

            return string.Empty;
        }

        private static string NormalizeTechnicalMapToken(string token)
        {
            Match match = Regex.Match((token ?? string.Empty).Trim(), @"^(?<prefix>[A-Za-z]+)0*(?<number>\d+)$");
            int number;
            if (!match.Success
                || !int.TryParse(match.Groups["number"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
            {
                return string.Empty;
            }

            string prefix = match.Groups["prefix"].Value.ToLowerInvariant();
            if (prefix == "k" && number == 0)
            {
                return "k00";
            }

            return prefix + number.ToString("00", CultureInfo.InvariantCulture);
        }

        private static string NormalizeWorldMapToken(string token)
        {
            Match match = Regex.Match((token ?? string.Empty).Trim(), @"^[sS]0*(?<number>\d+)$");
            int number;
            if (!match.Success
                || !int.TryParse(match.Groups["number"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
                || number < 0)
            {
                return string.Empty;
            }

            return "s" + number.ToString(CultureInfo.InvariantCulture);
        }

        private static bool HasResolvedMapDisplayName(string mapDisplayName)
        {
            string value = (mapDisplayName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (Regex.IsMatch(value, @"^[A-Za-z]+\d+$", RegexOptions.IgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsPaddedInstanceToken(string value)
        {
            return Regex.IsMatch(value ?? string.Empty, @"^[iadrk]0\d+$", RegexOptions.IgnoreCase);
        }

        private static void AddToken(List<string> tokens, string value)
        {
            string normalized = NormalizeSearchText(value);
            if (string.IsNullOrWhiteSpace(normalized) || tokens.Contains(normalized))
            {
                return;
            }

            tokens.Add(normalized);
        }

        private static int ScoreMapName(string name, List<string> tokens)
        {
            string normalizedName = NormalizeSearchText(name);
            if (string.IsNullOrWhiteSpace(normalizedName) || tokens == null || tokens.Count == 0)
            {
                return 0;
            }

            int best = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (string.Equals(normalizedName, token, StringComparison.OrdinalIgnoreCase))
                {
                    best = Math.Max(best, 120);
                }
                else if (normalizedName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0
                    || token.IndexOf(normalizedName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    best = Math.Max(best, token.Length <= 3 ? 25 : 70);
                }
            }

            return best;
        }

        private static int ScoreMapPath(string mappedPath, List<string> tokens)
        {
            string normalizedPath = NormalizeSearchText(mappedPath);
            string lowerPath = (mappedPath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedPath) || tokens == null || tokens.Count == 0)
            {
                return 0;
            }

            int best = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (normalizedPath.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int score = token.Length <= 3 ? 35 : 65;
                    if (normalizedPath.IndexOf("map", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 25;
                    }
                    if (normalizedPath.IndexOf("world", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 15;
                    }
                    if (lowerPath.IndexOf("midmaps\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 30;
                    }
                    if (lowerPath.IndexOf("maploading\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score -= 35;
                    }
                    if (lowerPath.IndexOf("minimaps\\", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score -= 20;
                    }
                    best = Math.Max(best, score);
                }
            }

            return best;
        }

        private static int ScorePackageMapImagePreference(string path)
        {
            string lowerPath = (path ?? string.Empty).Replace('/', '\\').ToLowerInvariant();
            if (lowerPath.IndexOf("midmaps\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (lowerPath.EndsWith("_h.dds", StringComparison.OrdinalIgnoreCase)
                    || lowerPath.EndsWith("_b.dds", StringComparison.OrdinalIgnoreCase)
                    || lowerPath.IndexOf("_alpha.", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return 55;
                }

                return 120;
            }
            if (lowerPath.IndexOf("minimaps\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 5;
            }
            if (lowerPath.IndexOf("maploading\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return -25;
            }

            return 0;
        }

        private static string NormalizeSearchText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string fileName = value.Trim().Replace('/', '\\');
            fileName = fileName.Trim('\\');
            int mapsIndex = fileName.IndexOf("maps\\", StringComparison.OrdinalIgnoreCase);
            if (mapsIndex >= 0)
            {
                fileName = fileName.Substring(mapsIndex + 5);
            }

            return new string(fileName
                .ToLowerInvariant()
                .Where(ch => char.IsLetterOrDigit(ch))
                .ToArray());
        }

        private static string NormalizeAssetPath(string value)
        {
            return (value ?? string.Empty).Trim().Trim('"', '\'').Replace('/', '\\').Trim('\\');
        }

        private static bool IsSupportedMapImagePath(string mappedPath)
        {
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                return false;
            }

            string extension = Path.GetExtension(mappedPath);
            return string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyMapImagePath(string mappedPath)
        {
            string lowerPath = (mappedPath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();
            return lowerPath.IndexOf("midmaps\\", StringComparison.OrdinalIgnoreCase) >= 0
                || lowerPath.IndexOf("minimaps\\", StringComparison.OrdinalIgnoreCase) >= 0
                || lowerPath.IndexOf("maploading\\", StringComparison.OrdinalIgnoreCase) >= 0
                || lowerPath.IndexOf("worldmaps\\", StringComparison.OrdinalIgnoreCase) >= 0
                || lowerPath.IndexOf("instance\\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsInstanceMapImagePath(string mappedPath)
        {
            string lowerPath = (mappedPath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();
            return lowerPath.IndexOf("instance\\", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int GetFieldIndex(string[] fields, string name)
        {
            if (fields == null || string.IsNullOrWhiteSpace(name))
            {
                return -1;
            }

            string normalizedName = NormalizeFieldName(name);
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(NormalizeFieldName(fields[i]), normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string NormalizeFieldName(string value)
        {
            return (value ?? string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        }

        private static int ExtractLeadingNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return int.MaxValue;
            }

            int i = 0;
            while (i < value.Length && char.IsDigit(value[i]))
            {
                i++;
            }

            int number;
            return i > 0 && int.TryParse(value.Substring(0, i), out number) ? number : int.MaxValue;
        }

        private static int ClampToByte(int value)
        {
            if (value < 0)
            {
                return 0;
            }
            if (value > 255)
            {
                return 255;
            }
            return value;
        }

        private static Color BuildTerrainColor(int height, int shade)
        {
            int softHeight = ClampToByte(52 + height / 5);
            int red = ClampToByte((shade + softHeight) / 2);
            int green = ClampToByte((shade + softHeight + 12) / 2);
            int blue = ClampToByte((shade + softHeight - 8) / 2);
            return Color.FromArgb(255, red, green, blue);
        }

        private sealed class TerrainPreviewConfig
        {
            public int Rows;
            public int Columns;
            public int AreaWidth;
            public int AreaHeight;
            public float GridSize;
        }

        private sealed class ClientMapImageCandidate
        {
            public string MappedPath { get; set; }
            public string Source { get; set; }
            public string DisplayName { get; set; }
            public bool FitToSpawns { get; set; }
            public bool HasWorldBounds { get; set; }
            public RectangleF WorldBounds { get; set; }
            public int Score { get; set; }
        }

        private sealed class InstanceMapMatch
        {
            public int InstanceId { get; set; }
            public string DisplayName { get; set; }
            public string AssetNameToken { get; set; }
            public string MidMap { get; set; }
            public RectangleF RectMap { get; set; }
            public int Score { get; set; }
        }

        private sealed class LuaConstantToken
        {
            public string StringValue { get; set; }
            public double? NumberValue { get; set; }
        }

        private sealed class LuaBytecodeReader
        {
            private readonly byte[] data;
            private int offset;
            private bool littleEndian = true;
            private int intSize = 4;
            private int sizeTSize = 4;
            private int instructionSize = 4;
            private int numberSize = 8;

            public LuaBytecodeReader(byte[] data)
            {
                this.data = data ?? new byte[0];
            }

            public bool TryRead(List<LuaConstantToken> tokens)
            {
                if (tokens == null || data.Length < 12)
                {
                    return false;
                }

                offset = 0;
                if (ReadByte() != 0x1B
                    || ReadByte() != (byte)'L'
                    || ReadByte() != (byte)'u'
                    || ReadByte() != (byte)'a')
                {
                    return false;
                }

                int version = ReadByte();
                ReadByte(); // format
                littleEndian = ReadByte() != 0;
                intSize = ReadByte();
                sizeTSize = ReadByte();
                instructionSize = ReadByte();
                numberSize = ReadByte();
                ReadByte(); // integral flag
                if (version != 0x51
                    || intSize <= 0
                    || intSize > 8
                    || sizeTSize <= 0
                    || sizeTSize > 8
                    || instructionSize <= 0
                    || instructionSize > 8
                    || (numberSize != 4 && numberSize != 8))
                {
                    return false;
                }

                ReadFunction(tokens, 0);
                return true;
            }

            private void ReadFunction(List<LuaConstantToken> tokens, int depth)
            {
                if (depth > 64)
                {
                    throw new InvalidDataException("Lua bytecode nesting is too deep.");
                }

                ReadString();
                ReadInt();
                ReadInt();
                ReadByte();
                ReadByte();
                ReadByte();
                ReadByte();

                int instructionCount = ReadSafeCount();
                Skip(checked(instructionCount * instructionSize));

                int constantCount = ReadSafeCount();
                for (int i = 0; i < constantCount; i++)
                {
                    int type = ReadByte();
                    switch (type)
                    {
                        case 0:
                            break;
                        case 1:
                            ReadByte();
                            break;
                        case 3:
                            tokens.Add(new LuaConstantToken { NumberValue = ReadNumber() });
                            break;
                        case 4:
                            tokens.Add(new LuaConstantToken { StringValue = ReadString() });
                            break;
                        default:
                            throw new InvalidDataException("Unknown Lua constant type.");
                    }
                }

                int protoCount = ReadSafeCount();
                for (int i = 0; i < protoCount; i++)
                {
                    ReadFunction(tokens, depth + 1);
                }

                int lineInfoCount = ReadSafeCount();
                Skip(checked(lineInfoCount * intSize));

                int localCount = ReadSafeCount();
                for (int i = 0; i < localCount; i++)
                {
                    ReadString();
                    ReadInt();
                    ReadInt();
                }

                int upvalueCount = ReadSafeCount();
                for (int i = 0; i < upvalueCount; i++)
                {
                    ReadString();
                }
            }

            private int ReadSafeCount()
            {
                long value = ReadInteger(intSize);
                if (value < 0 || value > 1000000)
                {
                    throw new InvalidDataException("Invalid Lua bytecode count.");
                }

                return (int)value;
            }

            private int ReadInt()
            {
                return (int)ReadInteger(intSize);
            }

            private long ReadSizeT()
            {
                return ReadInteger(sizeTSize);
            }

            private long ReadInteger(int size)
            {
                Ensure(size);
                long value = 0;
                if (littleEndian)
                {
                    for (int i = 0; i < size; i++)
                    {
                        value |= ((long)data[offset + i]) << (8 * i);
                    }
                }
                else
                {
                    for (int i = 0; i < size; i++)
                    {
                        value = (value << 8) | data[offset + i];
                    }
                }

                offset += size;
                return value;
            }

            private double ReadNumber()
            {
                Ensure(numberSize);
                byte[] buffer = new byte[numberSize];
                Buffer.BlockCopy(data, offset, buffer, 0, numberSize);
                offset += numberSize;
                if (BitConverter.IsLittleEndian != littleEndian)
                {
                    Array.Reverse(buffer);
                }

                return numberSize == 8
                    ? BitConverter.ToDouble(buffer, 0)
                    : BitConverter.ToSingle(buffer, 0);
            }

            private string ReadString()
            {
                long length = ReadSizeT();
                if (length <= 0)
                {
                    return string.Empty;
                }
                if (length > data.Length - offset)
                {
                    throw new EndOfStreamException();
                }

                int byteLength = (int)length - 1;
                byte[] bytes = new byte[Math.Max(0, byteLength)];
                if (byteLength > 0)
                {
                    Buffer.BlockCopy(data, offset, bytes, 0, byteLength);
                }
                offset += (int)length;

                try
                {
                    return new UTF8Encoding(false, true).GetString(bytes);
                }
                catch
                {
                    return Encoding.GetEncoding(936).GetString(bytes);
                }
            }

            private int ReadByte()
            {
                Ensure(1);
                return data[offset++];
            }

            private void Skip(int bytes)
            {
                if (bytes < 0)
                {
                    throw new InvalidDataException();
                }

                Ensure(bytes);
                offset += bytes;
            }

            private void Ensure(int bytes)
            {
                if (bytes < 0 || offset + bytes > data.Length)
                {
                    throw new EndOfStreamException();
                }
            }
        }
    }
}
