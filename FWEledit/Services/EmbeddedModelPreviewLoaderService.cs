using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class EmbeddedModelPreviewLoaderService
    {
        private readonly PckEntryReaderService pckEntryReaderService;
        private static readonly float[] IdentitySkinMatrix = new float[]
        {
            1f, 0f, 0f, 0f,
            0f, 1f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f
        };

        public EmbeddedModelPreviewLoaderService(PckEntryReaderService pckEntryReaderService)
        {
            this.pckEntryReaderService = pckEntryReaderService ?? new PckEntryReaderService();
        }

        public bool TryLoadPreviewMesh(
            AssetManager assetManager,
            string mappedModelPath,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;

            if (assetManager == null)
            {
                error = "Model preview unavailable.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(mappedModelPath))
            {
                error = "Invalid model path.";
                return false;
            }

            string package;
            string relativeModelPath;
            SplitPackagePath(mappedModelPath, out package, out relativeModelPath);
            if (string.IsNullOrWhiteSpace(package) || string.IsNullOrWhiteSpace(relativeModelPath))
            {
                error = "Invalid model path mapping: " + mappedModelPath;
                return false;
            }

            string modelExtension = (Path.GetExtension(relativeModelPath) ?? string.Empty).ToLowerInvariant();
            if (string.Equals(modelExtension, ".ski", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadModelFile(assetManager, package, relativeModelPath, out byte[] directSkiBytes, out error))
                {
                    return false;
                }

                return TryBuildPreviewDataFromResolvedSki(
                    assetManager,
                    mappedModelPath,
                    string.Empty,
                    string.Empty,
                    package,
                    relativeModelPath,
                    package,
                    relativeModelPath,
                    Path.ChangeExtension(relativeModelPath, ".bon"),
                    package,
                    relativeModelPath,
                    directSkiBytes,
                    5,
                    6,
                    0,
                    unchecked((int)0xFFFFFFFF),
                    0,
                    new float[0],
                    out previewData,
                    out error);
            }

            if (string.Equals(modelExtension, ".smd", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadModelFile(assetManager, package, relativeModelPath, out byte[] smdBytesDirect, out error))
                {
                    return false;
                }

                string skiReferenceDirect;
                if (!TryExtractReferencedFileFromSmd(smdBytesDirect, ".ski", out skiReferenceDirect))
                {
                    skiReferenceDirect = Path.ChangeExtension(relativeModelPath, ".ski");
                    if (string.IsNullOrWhiteSpace(skiReferenceDirect))
                    {
                        skiReferenceDirect = string.Empty;
                    }
                }

                string bonReferenceDirect;
                if (!TryExtractReferencedFileFromSmd(smdBytesDirect, ".bon", out bonReferenceDirect))
                {
                    bonReferenceDirect = Path.ChangeExtension(relativeModelPath, ".bon");
                    if (string.IsNullOrWhiteSpace(bonReferenceDirect))
                    {
                        bonReferenceDirect = string.Empty;
                    }
                }

                if (!TryReadSkiWithFallback(
                    assetManager,
                    package,
                    relativeModelPath,
                    skiReferenceDirect,
                    out string skiPackageDirect,
                    out string relativeSkiDirect,
                    out byte[] skiBytesDirect,
                    out error))
                {
                    return false;
                }

                return TryBuildPreviewDataFromResolvedSki(
                    assetManager,
                    mappedModelPath,
                    string.Empty,
                    string.Empty,
                    package,
                    relativeModelPath,
                    package,
                    relativeModelPath,
                    bonReferenceDirect,
                    skiPackageDirect,
                    relativeSkiDirect,
                    skiBytesDirect,
                    5,
                    6,
                    0,
                    unchecked((int)0xFFFFFFFF),
                    0,
                    new float[0],
                    out previewData,
                    out error);
            }

            string relativeEcm = relativeModelPath;
            byte[] ecmBytes;
            if (!TryReadModelFile(assetManager, package, relativeEcm, out ecmBytes, out error))
            {
                return false;
            }

            string ecmText;
            if (!TryDecodeText(ecmBytes, out ecmText))
            {
                error = "Failed to decode .ecm file content.";
                return false;
            }

            int srcBlend = ParseIntFieldOrDefault(ecmText, "SrcBlend", 5);
            int destBlend = ParseIntFieldOrDefault(ecmText, "DestBlend", 6);
            int emissiveColorArgb = ParseColorFieldOrDefault(ecmText, "EmissiveCol", 0);
            int orgColorArgb = ParseColorFieldOrDefault(ecmText, "OrgColor", unchecked((int)0xFFFFFFFF));
            int outerNum = ParseIntFieldOrDefault(ecmText, "OuterNum", 0);
            float[] shaderFloats = ParseFloatParameters(ecmText, outerNum);

            string skinModelPath;
            if (!TryExtractFieldValue(ecmText, "SkinModelPath", out skinModelPath))
            {
                error = "SkinModelPath not found in .ecm file.";
                return false;
            }

            string smdPackage;
            string relativeSmd;
            ResolveReferencedPath(package, relativeEcm, skinModelPath, out smdPackage, out relativeSmd);
            if (string.IsNullOrWhiteSpace(relativeSmd))
            {
                error = "Invalid SkinModelPath: " + skinModelPath;
                return false;
            }

            string skiReference;
            string bonReference = string.Empty;
            byte[] smdBytes = null;
            if (string.Equals(Path.GetExtension(relativeSmd), ".ski", StringComparison.OrdinalIgnoreCase))
            {
                skiReference = relativeSmd;
                bonReference = Path.ChangeExtension(relativeSmd, ".bon");
            }
            else
            {
                if (!TryReadModelFile(assetManager, smdPackage, relativeSmd, out smdBytes, out error))
                {
                    return false;
                }

                if (!TryExtractReferencedFileFromSmd(smdBytes, ".ski", out skiReference))
                {
                    skiReference = Path.ChangeExtension(relativeSmd, ".ski");
                    if (string.IsNullOrWhiteSpace(skiReference))
                    {
                        skiReference = string.Empty;
                    }
                }

                if (!TryExtractReferencedFileFromSmd(smdBytes, ".bon", out bonReference))
                {
                    bonReference = Path.ChangeExtension(relativeSmd, ".bon");
                    if (string.IsNullOrWhiteSpace(bonReference))
                    {
                        bonReference = string.Empty;
                    }
                }
            }

            string skiPackage;
            string relativeSki;
            byte[] skiBytes;
            if (!TryReadSkiWithFallback(
                assetManager,
                smdPackage,
                relativeSmd,
                skiReference,
                out skiPackage,
                out relativeSki,
                out skiBytes,
                out error))
            {
                return false;
            }

            return TryBuildPreviewDataFromResolvedSki(
                assetManager,
                mappedModelPath,
                package,
                relativeEcm,
                smdPackage,
                relativeSmd,
                smdPackage,
                relativeSmd,
                bonReference,
                skiPackage,
                relativeSki,
                skiBytes,
                srcBlend,
                destBlend,
                emissiveColorArgb,
                orgColorArgb,
                outerNum,
                shaderFloats,
                out previewData,
                out error);
        }

        private static void SplitPackagePath(string mappedPath, out string package, out string relative)
        {
            package = "models";
            relative = NormalizeRelativePath(mappedPath);
            if (string.IsNullOrWhiteSpace(relative))
            {
                return;
            }

            string pkg;
            string rel;
            if (ModelPickerService.TrySplitModelPackagePath(relative, out pkg, out rel))
            {
                package = pkg;
                relative = rel;
                return;
            }

            if (relative.StartsWith("models\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "models";
                relative = relative.Substring("models\\".Length);
            }
            else if (relative.StartsWith("litmodels\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "litmodels";
                relative = relative.Substring("litmodels\\".Length);
            }
            else if (relative.StartsWith("moxing\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "moxing";
                relative = relative.Substring("moxing\\".Length);
            }
        }

        private static string BuildMappedPath(string package, string relative)
        {
            string safePackage = (package ?? string.Empty).Trim();
            string safeRelative = NormalizeRelativePath(relative);
            if (string.IsNullOrWhiteSpace(safePackage))
            {
                return safeRelative;
            }
            if (string.IsNullOrWhiteSpace(safeRelative))
            {
                return safePackage;
            }
            return safePackage + "\\" + safeRelative;
        }

        private static string ResolveAbsolutePath(AssetManager assetManager, string package, string relative)
        {
            if (assetManager == null)
            {
                return string.Empty;
            }

            string safeRelative = NormalizeRelativePath(relative);
            if (string.IsNullOrWhiteSpace(safeRelative))
            {
                return string.Empty;
            }

            string safePackage = (package ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(safePackage))
            {
                try
                {
                    string root = assetManager.GetExtractedPackageRoot(safePackage);
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        string candidate = Path.Combine(root, safeRelative);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }
                catch
                { }
            }

            string mapped = BuildMappedPath(safePackage, safeRelative);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                string resolvedMapped = assetManager.ResolveResourcePathNoExtract(mapped);
                if (!string.IsNullOrWhiteSpace(resolvedMapped) && File.Exists(resolvedMapped))
                {
                    return resolvedMapped;
                }
            }

            string resolvedRelative = assetManager.ResolveResourcePathNoExtract(safeRelative);
            if (!string.IsNullOrWhiteSpace(resolvedRelative) && File.Exists(resolvedRelative))
            {
                return resolvedRelative;
            }

            return string.Empty;
        }

        private static void ResolveReferencedPath(
            string basePackage,
            string baseRelativeFile,
            string referencedPath,
            out string package,
            out string relative)
        {
            package = basePackage;
            relative = string.Empty;

            string normalized = NormalizeRelativePath(referencedPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            string refPackage;
            string refRelative;
            if (ModelPickerService.TrySplitModelPackagePath(normalized, out refPackage, out refRelative))
            {
                package = refPackage;
                relative = refRelative;
                return;
            }

            if (normalized.StartsWith("models\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "models";
                relative = normalized.Substring("models\\".Length);
                return;
            }
            if (normalized.StartsWith("litmodels\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "litmodels";
                relative = normalized.Substring("litmodels\\".Length);
                return;
            }
            if (normalized.StartsWith("moxing\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "moxing";
                relative = normalized.Substring("moxing\\".Length);
                return;
            }

            if (normalized.IndexOf('\\') >= 0)
            {
                relative = normalized;
                return;
            }

            string baseDir = string.Empty;
            try
            {
                baseDir = Path.GetDirectoryName(NormalizeRelativePath(baseRelativeFile)) ?? string.Empty;
            }
            catch
            {
                baseDir = string.Empty;
            }

            relative = string.IsNullOrWhiteSpace(baseDir)
                ? normalized
                : NormalizeRelativePath(baseDir + "\\" + normalized);
        }

        private bool TryReadModelFile(
            AssetManager assetManager,
            string package,
            string relativePath,
            out byte[] bytes,
            out string error)
        {
            bytes = null;
            error = string.Empty;

            string mappedPath = BuildMappedPath(package, relativePath);
            string absolute = assetManager.ResolveResourcePathNoExtract(mappedPath);
            if (!string.IsNullOrWhiteSpace(absolute) && File.Exists(absolute))
            {
                try
                {
                    bytes = File.ReadAllBytes(absolute);
                    return true;
                }
                catch (Exception ex)
                {
                    error = "Failed to read resource file: " + ex.Message;
                    return false;
                }
            }

            string absoluteRelative = assetManager.ResolveResourcePathNoExtract(relativePath);
            if (!string.IsNullOrWhiteSpace(absoluteRelative) && File.Exists(absoluteRelative))
            {
                try
                {
                    bytes = File.ReadAllBytes(absoluteRelative);
                    return true;
                }
                catch (Exception ex)
                {
                    error = "Failed to read resource file: " + ex.Message;
                    return false;
                }
            }

            if (pckEntryReaderService.TryReadFile(package, relativePath, out bytes, out error))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                error = "Resource file not found: " + mappedPath;
            }

            return false;
        }

        private bool TryBuildPreviewDataFromResolvedSki(
            AssetManager assetManager,
            string sourceMappedPath,
            string ecmPackage,
            string relativeEcm,
            string smdPackage,
            string relativeSmd,
            string bonBasePackage,
            string bonBaseRelative,
            string bonReference,
            string skiPackage,
            string relativeSki,
            byte[] skiBytes,
            int srcBlend,
            int destBlend,
            int emissiveColorArgb,
            int orgColorArgb,
            int outerNum,
            float[] shaderFloats,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;

            Dictionary<string, float[]> bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(bonReference))
            {
                bonReference = Path.ChangeExtension(relativeSki, ".bon");
            }
            if (!string.IsNullOrWhiteSpace(bonReference))
            {
                string bonPackage;
                string relativeBon;
                ResolveReferencedPath(bonBasePackage, bonBaseRelative, bonReference, out bonPackage, out relativeBon);
                if (!string.IsNullOrWhiteSpace(relativeBon)
                    && TryReadModelFile(assetManager, bonPackage, relativeBon, out byte[] bonBytes, out string _))
                {
                    TryParseBonBindMatrices(bonBytes, out bindMatricesByBoneName);
                }
            }

            Vector3f[] vertices;
            Vector2f[] uvs;
            int[] indices;
            int[] triangleColors;
            int[] triangleTextureIndices;
            PreviewTextureData[] textures;
            if (!TryParseSkiMesh(
                assetManager,
                skiPackage,
                relativeSki,
                skiBytes,
                bindMatricesByBoneName,
                out vertices,
                out uvs,
                out indices,
                out triangleColors,
                out triangleTextureIndices,
                out textures,
                out error))
            {
                return false;
            }

            previewData = new ModelPreviewMeshData
            {
                SourceMappedPath = sourceMappedPath ?? string.Empty,
                EcmPath = BuildMappedPath(ecmPackage, relativeEcm),
                EcmAbsolutePath = ResolveAbsolutePath(assetManager, ecmPackage, relativeEcm),
                SkinModelPath = BuildMappedPath(smdPackage, relativeSmd),
                SkiPath = BuildMappedPath(skiPackage, relativeSki),
                SkiAbsolutePath = ResolveAbsolutePath(assetManager, skiPackage, relativeSki),
                Vertices = vertices,
                UVs = uvs,
                Indices = indices,
                TriangleColors = triangleColors,
                TriangleTextureIndices = triangleTextureIndices,
                Textures = textures,
                SrcBlend = srcBlend,
                DestBlend = destBlend,
                EmissiveColorArgb = emissiveColorArgb,
                OrgColorArgb = orgColorArgb,
                ShaderFloats = shaderFloats ?? new float[0],
                OuterNum = outerNum
            };

            return true;
        }

        private bool TryReadSkiWithFallback(
            AssetManager assetManager,
            string smdPackage,
            string relativeSmd,
            string skiReference,
            out string resolvedPackage,
            out string resolvedRelative,
            out byte[] skiBytes,
            out string error)
        {
            resolvedPackage = smdPackage;
            resolvedRelative = string.Empty;
            skiBytes = null;
            error = string.Empty;

            string[] skiPathCandidates = BuildSkiPathCandidates(relativeSmd, skiReference);
            if (skiPathCandidates == null || skiPathCandidates.Length == 0)
            {
                error = "Failed to resolve .ski path for preview.";
                return false;
            }

            List<string> packageFallbacks = new List<string>(8);
            HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePackage(packageFallbacks, seenPackages, smdPackage);
            AddUniquePackage(packageFallbacks, seenPackages, "grasses");
            AddUniquePackage(packageFallbacks, seenPackages, "models");
            AddUniquePackage(packageFallbacks, seenPackages, "litmodels");
            AddUniquePackage(packageFallbacks, seenPackages, "moxing");
            AddUniquePackage(packageFallbacks, seenPackages, "surfaces");
            AddUniquePackage(packageFallbacks, seenPackages, string.Empty);

            string lastProbeError = string.Empty;
            for (int i = 0; i < skiPathCandidates.Length; i++)
            {
                string candidate = skiPathCandidates[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string basePackage;
                string baseRelative;
                ResolveReferencedPath(smdPackage, relativeSmd, candidate, out basePackage, out baseRelative);
                if (string.IsNullOrWhiteSpace(baseRelative))
                {
                    continue;
                }

                for (int p = 0; p < packageFallbacks.Count; p++)
                {
                    string probePackage = packageFallbacks[p];
                    if (p == 0)
                    {
                        probePackage = basePackage;
                    }

                    if (TryReadModelFile(assetManager, probePackage, baseRelative, out skiBytes, out string probeError))
                    {
                        resolvedPackage = probePackage;
                        resolvedRelative = baseRelative;
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(lastProbeError) && !string.IsNullOrWhiteSpace(probeError))
                    {
                        lastProbeError = probeError;
                    }
                }
            }

            error = string.IsNullOrWhiteSpace(lastProbeError)
                ? "Failed to resolve .ski path for preview."
                : lastProbeError;
            return false;
        }

        private static string[] BuildSkiPathCandidates(string relativeSmd, string skiReference)
        {
            List<string> candidates = new List<string>(12);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string normalizedSmd = NormalizeRelativePath(relativeSmd);
            string normalizedRef = NormalizeRelativePath(skiReference);

            AddUniquePathCandidate(candidates, seen, normalizedRef);

            if (!string.IsNullOrWhiteSpace(normalizedRef))
            {
                try
                {
                    AddUniquePathCandidate(candidates, seen, Path.ChangeExtension(normalizedRef, ".ski"));

                    string refName = NormalizeRelativePath(Path.GetFileName(normalizedRef) ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(refName))
                    {
                        AddUniquePathCandidate(candidates, seen, refName);

                        string smdDir = NormalizeRelativePath(Path.GetDirectoryName(normalizedSmd) ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(smdDir))
                        {
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(smdDir + "\\" + refName));
                        }
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedSmd))
            {
                try
                {
                    string fromSmdName = Path.ChangeExtension(normalizedSmd, ".ski");
                    AddUniquePathCandidate(candidates, seen, fromSmdName);

                    string smdDir = NormalizeRelativePath(Path.GetDirectoryName(normalizedSmd) ?? string.Empty);
                    string smdSkiFile = NormalizeRelativePath(Path.GetFileName(fromSmdName) ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(smdSkiFile))
                    {
                        AddUniquePathCandidate(candidates, seen, smdSkiFile);
                        if (!string.IsNullOrWhiteSpace(smdDir))
                        {
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(smdDir + "\\" + smdSkiFile));
                        }
                    }
                }
                catch
                {
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedSmd))
            {
                try
                {
                    string smdDir = NormalizeRelativePath(Path.GetDirectoryName(normalizedSmd) ?? string.Empty);
                    string smdDirName = NormalizeRelativePath(Path.GetFileName(smdDir) ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(smdDirName))
                    {
                        string dirNamedSki = smdDirName + ".ski";
                        AddUniquePathCandidate(candidates, seen, dirNamedSki);

                        if (!string.IsNullOrWhiteSpace(smdDir))
                        {
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(smdDir + "\\" + dirNamedSki));
                        }
                    }
                }
                catch
                {
                }
            }

            return candidates.ToArray();
        }

        private static bool TryDecodeText(byte[] bytes, out string text)
        {
            text = string.Empty;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            Encoding[] encodings = new Encoding[]
            {
                Encoding.GetEncoding("GBK"),
                Encoding.UTF8,
                Encoding.Unicode
            };

            for (int i = 0; i < encodings.Length; i++)
            {
                try
                {
                    string decoded = encodings[i].GetString(bytes);
                    if (!string.IsNullOrWhiteSpace(decoded))
                    {
                        text = decoded;
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryExtractFieldValue(string text, string fieldName, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string[] lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? string.Empty;
                if (!line.StartsWith(fieldName + ":", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon < 0)
                {
                    continue;
                }

                string candidate = line.Substring(colon + 1).Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    value = NormalizeRelativePath(candidate);
                    return true;
                }
            }

            return false;
        }

        private static int ParseIntFieldOrDefault(string text, string fieldName, int fallback)
        {
            if (!TryExtractFieldValue(text, fieldName, out string value))
            {
                return fallback;
            }

            if (TryParseInt(value, out int parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static int ParseColorFieldOrDefault(string text, string fieldName, int fallback)
        {
            if (!TryExtractFieldValue(text, fieldName, out string value))
            {
                return fallback;
            }

            if (TryParseColorArgb(value, out int argb))
            {
                return argb;
            }

            return fallback;
        }

        private static bool TryParseInt(string raw, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(trimmed.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
            }

            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            return int.TryParse(trimmed, out value);
        }

        private static bool TryParseColorArgb(string raw, out int argb)
        {
            argb = 0;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string trimmed = raw.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(2);
            }

            if (trimmed.Length > 0 && trimmed.Length <= 8
                && int.TryParse(trimmed, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int hex))
            {
                if (trimmed.Length <= 6)
                {
                    argb = unchecked((int)0xFF000000) | (hex & 0x00FFFFFF);
                }
                else
                {
                    argb = hex;
                }
                return true;
            }

            if (TryParseInt(trimmed, out int dec))
            {
                argb = dec;
                return true;
            }

            return false;
        }

        private static float[] ParseFloatParameters(string text, int preferredCount)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new float[0];
            }

            int maxCount = preferredCount > 0 ? preferredCount : 16;
            if (maxCount <= 0)
            {
                maxCount = 16;
            }

            List<float> values = new List<float>(Math.Min(maxCount, 16));
            string[] lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                if (values.Count >= maxCount)
                {
                    break;
                }

                string line = lines[i] ?? string.Empty;
                if (!line.StartsWith("Float:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon < 0)
                {
                    continue;
                }

                string raw = line.Substring(colon + 1).Trim();
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    values.Add(parsed);
                    continue;
                }

                if (float.TryParse(raw, out parsed))
                {
                    values.Add(parsed);
                }
            }

            return values.ToArray();
        }

        private static bool TryExtractReferencedFileFromSmd(byte[] smdBytes, string extension, out string value)
        {
            value = string.Empty;
            if (smdBytes == null || smdBytes.Length < 8 || string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            string extLower = extension.ToLowerInvariant();
            Encoding gbk = Encoding.GetEncoding("GBK");

            for (int pos = 0; pos <= smdBytes.Length - 4; pos++)
            {
                int len = BitConverter.ToInt32(smdBytes, pos);
                if (len <= 0 || len > 512 || pos + 4 + len > smdBytes.Length)
                {
                    continue;
                }

                string candidate;
                try
                {
                    candidate = gbk.GetString(smdBytes, pos + 4, len).Trim('\0', ' ', '\t', '\r', '\n');
                }
                catch
                {
                    continue;
                }

                if (!candidate.EndsWith(extLower, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                value = NormalizeRelativePath(candidate);
                return true;
            }

            try
            {
                string text = gbk.GetString(smdBytes);
                Match match = Regex.Match(text, @"([^\0\r\n\t]+\" + Regex.Escape(extension) + ")", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    value = NormalizeRelativePath(match.Groups[1].Value);
                    return true;
                }
            }
            catch
            {
            }

            try
            {
                string unicodeText = Encoding.Unicode.GetString(smdBytes);
                Match match = Regex.Match(unicodeText, @"([^\0\r\n\t]+\" + Regex.Escape(extension) + ")", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    value = NormalizeRelativePath(match.Groups[1].Value);
                    return true;
                }
            }
            catch
            {
            }

            if (TryExtractPathByRawByteScan(smdBytes, extension, out value))
            {
                return true;
            }

            return false;
        }

        private static bool TryExtractPathByRawByteScan(byte[] bytes, string extension, out string value)
        {
            value = string.Empty;
            if (bytes == null || bytes.Length == 0 || string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            string marker = extension.ToLowerInvariant();
            byte[] markerBytes = Encoding.ASCII.GetBytes(marker);
            if (markerBytes.Length == 0)
            {
                return false;
            }

            for (int i = 0; i <= bytes.Length - markerBytes.Length; i++)
            {
                bool match = true;
                for (int m = 0; m < markerBytes.Length; m++)
                {
                    byte b = bytes[i + m];
                    byte lower = (byte)char.ToLowerInvariant((char)b);
                    if (lower != markerBytes[m])
                    {
                        match = false;
                        break;
                    }
                }
                if (!match)
                {
                    continue;
                }

                int start = i - 1;
                while (start >= 0 && IsPathByte(bytes[start]))
                {
                    start--;
                }
                start++;

                int end = i + markerBytes.Length;
                while (end < bytes.Length && IsPathByte(bytes[end]))
                {
                    end++;
                }

                int len = end - start;
                if (len <= 0 || len > 1024)
                {
                    continue;
                }

                try
                {
                    string candidate = Encoding.GetEncoding("GBK").GetString(bytes, start, len).Trim('\0', ' ', '\t', '\r', '\n');
                    candidate = NormalizeRelativePath(candidate);
                    if (!string.IsNullOrWhiteSpace(candidate)
                        && candidate.EndsWith(marker, StringComparison.OrdinalIgnoreCase))
                    {
                        value = candidate;
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool IsPathByte(byte b)
        {
            if (b == 0 || b == 0xFF)
            {
                return false;
            }
            if (b < 0x20)
            {
                return false;
            }

            return true;
        }

        private static string NormalizeRelativePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string normalized = value
                .Replace('/', '\\')
                .Trim()
                .Trim('"')
                .Trim()
                .TrimStart('\\');

            return DecodeHashUnicodeEscapes(normalized);
        }

        private static string DecodeHashUnicodeEscapes(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            try
            {
                return Regex.Replace(input, @"#U([0-9a-fA-F]{4})", delegate(Match match)
                {
                    string hex = match.Groups[1].Value;
                    try
                    {
                        int code = Convert.ToInt32(hex, 16);
                        return char.ConvertFromUtf32(code);
                    }
                    catch
                    {
                        return match.Value;
                    }
                });
            }
            catch
            {
                return input;
            }
        }

        private bool TryParseSkiMesh(
            AssetManager assetManager,
            string skiPackage,
            string relativeSkiPath,
            byte[] bytes,
            Dictionary<string, float[]> bindMatricesByBoneName,
            out Vector3f[] vertices,
            out Vector2f[] uvs,
            out int[] indices,
            out int[] triangleColors,
            out int[] triangleTextureIndices,
            out PreviewTextureData[] textures,
            out string error)
        {
            vertices = new Vector3f[0];
            uvs = new Vector2f[0];
            indices = new int[0];
            triangleColors = new int[0];
            triangleTextureIndices = new int[0];
            textures = new PreviewTextureData[0];
            error = string.Empty;

            if (bytes == null || bytes.Length < 0x80)
            {
                error = "Invalid .ski file.";
                return false;
            }

            Encoding gbk = Encoding.GetEncoding("GBK");

            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    string header = Encoding.ASCII.GetString(br.ReadBytes(8));
                    if (!string.Equals(header, "MOXBIKSA", StringComparison.Ordinal))
                    {
                        error = "Unsupported ski header: " + header;
                        return false;
                    }

                    int skiType = br.ReadInt32();
                    int[] meshCountByType = new int[4];
                    meshCountByType[0] = br.ReadInt32();
                    meshCountByType[1] = br.ReadInt32();
                    meshCountByType[2] = br.ReadInt32();
                    meshCountByType[3] = br.ReadInt32();

                    int texCount = br.ReadInt32();
                    int matCount = br.ReadInt32();
                    int numBips = br.ReadInt32();
                    br.ReadInt32(); // unknown_2
                    br.ReadInt32(); // type_mask
                    br.ReadBytes(60); // zero block

                    int meshCount = 0;
                    for (int i = 0; i < meshCountByType.Length; i++)
                    {
                        meshCount += Math.Max(0, meshCountByType[i]);
                    }

                    if (meshCount <= 0)
                    {
                        error = "No meshes found in .ski.";
                        return false;
                    }
                    if (meshCountByType[2] > 0 || meshCountByType[3] > 0)
                    {
                        error = "Unsupported mesh vertex type in .ski (types 2/3).";
                        return false;
                    }

                    string[] skiBoneNames = new string[0];
                    if (skiType == 9)
                    {
                        skiBoneNames = new string[Math.Max(0, numBips)];
                        for (int i = 0; i < numBips; i++)
                        {
                            if (!TryReadLengthPrefixedString(br, gbk, 4096, out string boneName))
                            {
                                error = "Invalid bone table in .ski.";
                                return false;
                            }
                            skiBoneNames[i] = boneName ?? string.Empty;
                        }
                    }

                    string[] textureNames = new string[Math.Max(0, texCount)];
                    for (int i = 0; i < texCount; i++)
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 4096, out string textureName))
                        {
                            error = "Invalid texture table in .ski.";
                            return false;
                        }
                        textureNames[i] = textureName ?? string.Empty;
                    }

                    SkiMaterial[] materials = new SkiMaterial[Math.Max(0, matCount)];
                    for (int i = 0; i < matCount; i++)
                    {
                        if (!TryReadMaterialBlock(br, out materials[i]))
                        {
                            error = "Invalid material block in .ski.";
                            return false;
                        }
                    }

                    Dictionary<int, PreviewTextureData> textureDataByIndex = BuildTextureDataLookup(
                        assetManager,
                        skiPackage,
                        relativeSkiPath,
                        textureNames);
                    // NOTE:
                    // Some FW assets include BON data that does not map cleanly to the static
                    // preview geometry used by editor tools. Applying BON skinning here can
                    // severely distort meshes (collapsed/exploded look). Keep static SKI pose.
                    float[][] skinMatricesByBoneIndex = new float[0][];

                    List<Vector3f> allVertices = new List<Vector3f>(4096);
                    List<Vector2f> allUvs = new List<Vector2f>(4096);
                    List<int> allIndices = new List<int>(8192);
                    List<int> allTriangleColors = new List<int>(8192);
                    List<int> allTriangleTextureIndices = new List<int>(8192);

                    for (int currentVertexType = 0; currentVertexType <= 1; currentVertexType++)
                    {
                        int typedMeshCount = Math.Max(0, meshCountByType[currentVertexType]);
                        for (int meshIndex = 0; meshIndex < typedMeshCount; meshIndex++)
                        {
                            if (!TryReadLengthPrefixedString(br, gbk, 4096, out _))
                            {
                                error = "Invalid mesh name in .ski.";
                                return false;
                            }

                            if (!TryEnsureRemaining(br, 8))
                            {
                                error = "Unexpected end of mesh metadata in .ski.";
                                return false;
                            }

                            int texIndex = br.ReadInt32();
                            int matIndex = br.ReadInt32();
                            Color meshColor = ResolveMeshColor(texIndex, matIndex, materials, textureDataByIndex);
                            int meshTextureIndex = ResolveMeshTextureIndex(texIndex, textureNames);

                            if (currentVertexType == 1)
                            {
                                if (!TryEnsureRemaining(br, 4))
                                {
                                    error = "Unexpected end of extra mesh data in .ski.";
                                    return false;
                                }
                                br.ReadBytes(4);
                            }

                            if (!TryEnsureRemaining(br, 8))
                            {
                                error = "Unexpected end of mesh counts in .ski.";
                                return false;
                            }

                            uint vertexCount = br.ReadUInt32();
                            uint faceVertsCount = br.ReadUInt32();
                            if (vertexCount == 0 || faceVertsCount == 0 || faceVertsCount % 3 != 0)
                            {
                                continue;
                            }
                            if (vertexCount > 500000 || faceVertsCount > 5000000)
                            {
                                error = "Mesh is too large for preview.";
                                return false;
                            }

                            int baseVertex = allVertices.Count;

                            for (uint v = 0; v < vertexCount; v++)
                            {
                                if (!TryEnsureRemaining(br, 12))
                                {
                                    error = "Unexpected end of vertex position data in .ski.";
                                    return false;
                                }

                                float px = br.ReadSingle();
                                float py = br.ReadSingle();
                                float pz = br.ReadSingle();
                                float weight0 = 0f;
                                float weight1 = 0f;
                                float weight2 = 0f;
                                uint packedBoneIndices = 0;

                                if (currentVertexType == 0)
                                {
                                    if (!TryEnsureRemaining(br, 16))
                                    {
                                        error = "Unexpected end of vertex weight data in .ski.";
                                        return false;
                                    }

                                    weight0 = br.ReadSingle();
                                    weight1 = br.ReadSingle();
                                    weight2 = br.ReadSingle();
                                    packedBoneIndices = br.ReadUInt32();
                                }

                                if (!TryEnsureRemaining(br, 20))
                                {
                                    error = "Unexpected end of vertex normal/uv data in .ski.";
                                    return false;
                                }

                                br.ReadSingle();
                                br.ReadSingle();
                                br.ReadSingle();
                                float tu = br.ReadSingle();
                                float tv = br.ReadSingle();

                                Vector3f transformed = new Vector3f(px, py, pz);
                                if (currentVertexType == 0 && skinMatricesByBoneIndex != null && skinMatricesByBoneIndex.Length > 0)
                                {
                                    transformed = ApplyBoneSkinning(transformed, weight0, weight1, weight2, packedBoneIndices, skinMatricesByBoneIndex);
                                }

                                // Keep Y as up-axis (game-like orientation) so mounts/creatures
                                // don't appear vertically "standing" in preview.
                                allVertices.Add(new Vector3f(-transformed.X, transformed.Y, transformed.Z));
                                allUvs.Add(new Vector2f(tu, tv));
                            }

                            uint faceCount = faceVertsCount / 3;
                            long faceBytes = faceCount * 6L;
                            if (!TryEnsureRemaining(br, faceBytes))
                            {
                                error = "Unexpected end of face data in .ski.";
                                return false;
                            }

                            for (uint f = 0; f < faceCount; f++)
                            {
                                int i0 = baseVertex + br.ReadUInt16();
                                int i1 = baseVertex + br.ReadUInt16();
                                int i2 = baseVertex + br.ReadUInt16();

                                if (i0 < baseVertex || i1 < baseVertex || i2 < baseVertex)
                                {
                                    continue;
                                }
                                if (i0 >= allVertices.Count || i1 >= allVertices.Count || i2 >= allVertices.Count)
                                {
                                    continue;
                                }

                                allIndices.Add(i0);
                                allIndices.Add(i1);
                                allIndices.Add(i2);
                                allTriangleColors.Add(meshColor.ToArgb());
                                allTriangleTextureIndices.Add(meshTextureIndex);
                            }
                        }
                    }

                    if (allVertices.Count == 0 || allIndices.Count == 0)
                    {
                        error = "No previewable geometry found in .ski.";
                        return false;
                    }

                    vertices = allVertices.ToArray();
                    uvs = allUvs.Count == allVertices.Count ? allUvs.ToArray() : new Vector2f[0];
                    indices = allIndices.ToArray();
                    triangleColors = allTriangleColors.Count == (allIndices.Count / 3)
                        ? allTriangleColors.ToArray()
                        : new int[0];
                    triangleTextureIndices = allTriangleTextureIndices.Count == (allIndices.Count / 3)
                        ? allTriangleTextureIndices.ToArray()
                        : new int[0];
                    textures = BuildTextureArray(textureNames, textureDataByIndex);
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = "Failed to parse .ski: " + ex.Message;
                return false;
            }
        }

        private static bool TryReadMaterialBlock(BinaryReader br, out SkiMaterial material)
        {
            material = new SkiMaterial();
            if (br == null)
            {
                return false;
            }

            if (!TrySkipZeroTerminatedString(br, 512))
            {
                return false;
            }

            const int floatCount = 16;
            if (!TryEnsureRemaining(br, (floatCount * 4) + 4 + 1))
            {
                return false;
            }

            float[] values = new float[floatCount];
            for (int i = 0; i < floatCount; i++)
            {
                values[i] = br.ReadSingle();
            }

            br.ReadSingle(); // scale param
            br.ReadByte();   // is clothing

            material.BaseColor = BuildMaterialBaseColor(values);
            return true;
        }

        private static Color BuildMaterialBaseColor(float[] values)
        {
            if (values == null || values.Length < 3)
            {
                return Color.FromArgb(255, 205, 205, 205);
            }

            int r = FloatToByte(values[0]);
            int g = FloatToByte(values[1]);
            int b = FloatToByte(values[2]);
            if (r == 0 && g == 0 && b == 0)
            {
                r = 200;
                g = 200;
                b = 200;
            }

            return Color.FromArgb(255, r, g, b);
        }

        private static int FloatToByte(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 200;
            }

            float scaled = value <= 1.2f ? (value * 255f) : value;
            if (scaled < 0f) { scaled = 0f; }
            if (scaled > 255f) { scaled = 255f; }
            return (int)(scaled + 0.5f);
        }

        private Dictionary<int, PreviewTextureData> BuildTextureDataLookup(
            AssetManager assetManager,
            string skiPackage,
            string relativeSkiPath,
            string[] textureNames)
        {
            Dictionary<int, PreviewTextureData> result = new Dictionary<int, PreviewTextureData>();
            if (textureNames == null || textureNames.Length == 0)
            {
                return result;
            }

            for (int i = 0; i < textureNames.Length; i++)
            {
                string texture = textureNames[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(texture))
                {
                    continue;
                }

                PreviewTextureData textureData;
                if (TryLoadTextureData(assetManager, skiPackage, relativeSkiPath, texture, out textureData))
                {
                    result[i] = textureData;
                }
            }

            return result;
        }

        private Color ResolveMeshColor(
            int textureIndex,
            int materialIndex,
            SkiMaterial[] materials,
            Dictionary<int, PreviewTextureData> textureDataByIndex)
        {
            Color materialColor = Color.FromArgb(255, 200, 200, 200);
            if (materials != null && materialIndex >= 0 && materialIndex < materials.Length)
            {
                materialColor = materials[materialIndex].BaseColor;
            }

            Color textureColor = Color.White;
            if (textureDataByIndex != null
                && textureDataByIndex.TryGetValue(textureIndex, out PreviewTextureData found)
                && found != null
                && found.IsValid)
            {
                textureColor = Color.FromArgb(found.AverageColorArgb);
            }

            int r = (materialColor.R * textureColor.R) / 255;
            int g = (materialColor.G * textureColor.G) / 255;
            int b = (materialColor.B * textureColor.B) / 255;

            if (r < 20 && g < 20 && b < 20)
            {
                r = 120;
                g = 120;
                b = 120;
            }

            return Color.FromArgb(255, r, g, b);
        }

        private static int ResolveMeshTextureIndex(
            int textureIndex,
            string[] textureNames)
        {
            if (textureNames == null || textureNames.Length == 0)
            {
                return -1;
            }

            if (textureIndex >= 0 && textureIndex < textureNames.Length)
            {
                return textureIndex;
            }

            return -1;
        }

        private PreviewTextureData[] BuildTextureArray(
            string[] textureNames,
            Dictionary<int, PreviewTextureData> textureDataByIndex)
        {
            int count = textureNames == null ? 0 : textureNames.Length;
            if (count <= 0)
            {
                return new PreviewTextureData[0];
            }

            PreviewTextureData[] array = new PreviewTextureData[count];
            for (int i = 0; i < count; i++)
            {
                string name = textureNames[i] ?? string.Empty;
                array[i] = new PreviewTextureData
                {
                    Name = name
                };
            }

            if (textureDataByIndex == null || textureDataByIndex.Count == 0)
            {
                return array;
            }

            foreach (KeyValuePair<int, PreviewTextureData> entry in textureDataByIndex)
            {
                if (entry.Key < 0 || entry.Key >= count)
                {
                    continue;
                }

                array[entry.Key] = entry.Value;
            }

            return array;
        }

        private bool TryLoadTextureData(
            AssetManager assetManager,
            string skiPackage,
            string relativeSkiPath,
            string texturePath,
            out PreviewTextureData textureData)
        {
            textureData = null;

            string[] candidates = BuildTexturePathCandidates(relativeSkiPath, texturePath);
            for (int i = 0; i < candidates.Length; i++)
            {
                string texPackage;
                string texRelative;
                ResolveReferencedPath(skiPackage, relativeSkiPath, candidates[i], out texPackage, out texRelative);
                if (string.IsNullOrWhiteSpace(texRelative))
                {
                    continue;
                }

                if (TryReadTextureBytesWithPackageFallback(assetManager, texPackage, texRelative, out byte[] bytes, out string resolvedTexturePath)
                    && TryDecodeTextureData(texRelative, bytes, out PreviewTextureData decoded))
                {
                    decoded.Name = resolvedTexturePath;
                    textureData = decoded;
                    return true;
                }
            }

            if (TryLoadTextureFromSkiAbsoluteNeighbors(assetManager, skiPackage, relativeSkiPath, texturePath, out textureData))
            {
                return true;
            }

            return false;
        }

        private bool TryLoadTextureFromSkiAbsoluteNeighbors(
            AssetManager assetManager,
            string skiPackage,
            string relativeSkiPath,
            string texturePath,
            out PreviewTextureData textureData)
        {
            textureData = null;

            string skiAbsolute = ResolveAbsolutePath(assetManager, skiPackage, relativeSkiPath);
            if (string.IsNullOrWhiteSpace(skiAbsolute) || !File.Exists(skiAbsolute))
            {
                return false;
            }

            string skiDir = string.Empty;
            string skiParentDir = string.Empty;
            string skiNameNoExt = string.Empty;
            try
            {
                skiDir = Path.GetDirectoryName(skiAbsolute) ?? string.Empty;
                skiNameNoExt = Path.GetFileNameWithoutExtension(skiAbsolute) ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(skiDir))
                {
                    skiParentDir = Path.GetDirectoryName(skiDir) ?? string.Empty;
                }
            }
            catch
            {
                skiDir = string.Empty;
                skiParentDir = string.Empty;
                skiNameNoExt = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(skiDir))
            {
                return false;
            }

            string[] fileCandidates = BuildTextureFileCandidates(texturePath);
            if (fileCandidates.Length == 0)
            {
                return false;
            }

            List<string> absoluteCandidates = new List<string>(24);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fileCandidates.Length; i++)
            {
                string candidate = NormalizeRelativePath(fileCandidates[i]);
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string fileName = string.Empty;
                try
                {
                    fileName = Path.GetFileName(candidate) ?? string.Empty;
                }
                catch
                {
                    fileName = string.Empty;
                }

                AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiDir, candidate));

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiDir, "textures", fileName));
                    AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiDir, "texture", fileName));
                    if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                    {
                        AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiDir, "tex_" + skiNameNoExt, fileName));
                    }

                    if (!string.IsNullOrWhiteSpace(skiParentDir))
                    {
                        AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiParentDir, "textures", fileName));
                        AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiParentDir, "texture", fileName));
                        if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                        {
                            AddUniqueAbsolutePathCandidate(absoluteCandidates, seen, Path.Combine(skiParentDir, "tex_" + skiNameNoExt, fileName));
                        }
                    }
                }
            }

            for (int i = 0; i < absoluteCandidates.Count; i++)
            {
                string absolute = absoluteCandidates[i];
                if (!File.Exists(absolute))
                {
                    continue;
                }

                try
                {
                    byte[] bytes = File.ReadAllBytes(absolute);
                    if (TryDecodeTextureData(absolute, bytes, out PreviewTextureData decoded))
                    {
                        decoded.Name = absolute;
                        textureData = decoded;
                        return true;
                    }
                }
                catch
                {
                    // Ignore and continue fallback probes.
                }
            }

            return false;
        }

        private bool TryReadTextureBytesWithPackageFallback(
            AssetManager assetManager,
            string preferredPackage,
            string textureRelativePath,
            out byte[] bytes,
            out string resolvedPath)
        {
            bytes = null;
            resolvedPath = string.Empty;

            List<string> packageCandidates = new List<string>(8);
            HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePackage(packageCandidates, seenPackages, preferredPackage);
            AddUniquePackage(packageCandidates, seenPackages, "models");
            AddUniquePackage(packageCandidates, seenPackages, "litmodels");
            AddUniquePackage(packageCandidates, seenPackages, "moxing");
            AddUniquePackage(packageCandidates, seenPackages, "surfaces");
            AddUniquePackage(packageCandidates, seenPackages, "configs");
            AddUniquePackage(packageCandidates, seenPackages, string.Empty);

            for (int i = 0; i < packageCandidates.Count; i++)
            {
                string pkg = packageCandidates[i];
                if (TryReadModelFile(assetManager, pkg, textureRelativePath, out bytes, out string _))
                {
                    resolvedPath = BuildMappedPath(pkg, textureRelativePath);
                    return true;
                }
            }

            return false;
        }

        private static void AddUniquePackage(List<string> packages, HashSet<string> seen, string package)
        {
            string normalized = (package ?? string.Empty).Trim();
            if (seen.Contains(normalized))
            {
                return;
            }
            seen.Add(normalized);
            packages.Add(normalized);
        }

        private static string[] BuildTexturePathCandidates(string relativeSkiPath, string texturePath)
        {
            string[] fileCandidates = BuildTextureFileCandidates(texturePath);
            if (fileCandidates.Length == 0)
            {
                return fileCandidates;
            }

            string normalizedSki = NormalizeRelativePath(relativeSkiPath);
            string skiDir = string.Empty;
            string skiParentDir = string.Empty;
            string skiNameNoExt = string.Empty;
            try
            {
                skiDir = NormalizeRelativePath(Path.GetDirectoryName(normalizedSki) ?? string.Empty);
                skiNameNoExt = NormalizeRelativePath(Path.GetFileNameWithoutExtension(normalizedSki) ?? string.Empty);
                if (!string.IsNullOrWhiteSpace(skiDir))
                {
                    skiParentDir = NormalizeRelativePath(Path.GetDirectoryName(skiDir) ?? string.Empty);
                }
            }
            catch
            {
                skiDir = string.Empty;
                skiParentDir = string.Empty;
                skiNameNoExt = string.Empty;
            }

            List<string> candidates = new List<string>(32);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < fileCandidates.Length; i++)
            {
                string candidate = NormalizeRelativePath(fileCandidates[i]);
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                AddUniquePathCandidate(candidates, seen, candidate);

                string fileName = string.Empty;
                try
                {
                    fileName = NormalizeRelativePath(Path.GetFileName(candidate) ?? string.Empty);
                }
                catch
                {
                    fileName = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    AddUniquePathCandidate(candidates, seen, "textures\\" + fileName);
                    AddUniquePathCandidate(candidates, seen, "texture\\" + fileName);
                    if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                    {
                        AddUniquePathCandidate(candidates, seen, "tex_" + skiNameNoExt + "\\" + fileName);
                    }
                }

                if (!string.IsNullOrWhiteSpace(skiDir))
                {
                    AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiDir + "\\" + candidate));
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiDir + "\\textures\\" + fileName));
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiDir + "\\texture\\" + fileName));
                        if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                        {
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiDir + "\\tex_" + skiNameNoExt + "\\" + fileName));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(skiParentDir) && !string.IsNullOrWhiteSpace(fileName))
                {
                    AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiParentDir + "\\textures\\" + fileName));
                    AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiParentDir + "\\texture\\" + fileName));
                    if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                    {
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(skiParentDir + "\\tex_" + skiNameNoExt + "\\" + fileName));
                    }
                }
            }

            return candidates.ToArray();
        }

        private static void AddUniquePathCandidate(List<string> candidates, HashSet<string> seen, string value)
        {
            string normalized = NormalizeRelativePath(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }
            if (seen.Contains(normalized))
            {
                return;
            }

            seen.Add(normalized);
            candidates.Add(normalized);
        }

        private static void AddUniqueAbsolutePathCandidate(List<string> candidates, HashSet<string> seen, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            string normalized = value;
            try
            {
                normalized = Path.GetFullPath(value);
            }
            catch
            {
                normalized = value;
            }

            if (seen.Contains(normalized))
            {
                return;
            }

            seen.Add(normalized);
            candidates.Add(normalized);
        }

        private static string[] BuildTextureFileCandidates(string texturePath)
        {
            string normalized = NormalizeRelativePath(texturePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new string[0];
            }

            string ext = Path.GetExtension(normalized) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(ext))
            {
                return new string[] { normalized };
            }

            return new string[]
            {
                normalized + ".dds",
                normalized + ".tga",
                normalized + ".bmp",
                normalized + ".png",
                normalized + ".jpg",
                normalized + ".jpeg"
            };
        }

        private static bool TryDecodeTextureData(
            string relativePath,
            byte[] bytes,
            out PreviewTextureData data)
        {
            data = null;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            Bitmap bitmap = null;
            try
            {
                string ext = Path.GetExtension(relativePath) ?? string.Empty;
                if (string.Equals(ext, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    bitmap = DDS.LoadImage(bytes, true);
                }
                else
                {
                    using (MemoryStream ms = new MemoryStream(bytes))
                    using (Bitmap source = new Bitmap(ms))
                    {
                        bitmap = new Bitmap(source);
                    }
                }

                if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
                {
                    return false;
                }

                using (bitmap)
                {
                    const int maxSize = 1024;
                    using (Bitmap prepared = PrepareTextureBitmap(bitmap, maxSize))
                    {
                        int[] pixels = ExtractArgbPixels(prepared);
                        if (pixels == null || pixels.Length == 0)
                        {
                            return false;
                        }

                        data = new PreviewTextureData
                        {
                            Width = prepared.Width,
                            Height = prepared.Height,
                            Pixels = pixels,
                            AverageColorArgb = ComputeAverageColorArgb(pixels),
                            HasTransparency = ComputeHasTransparency(pixels)
                        };
                        return true;
                    }
                }
            }
            catch
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
                return false;
            }
        }

        private static Bitmap PrepareTextureBitmap(Bitmap source, int maxSize)
        {
            if (source == null)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            int srcW = Math.Max(1, source.Width);
            int srcH = Math.Max(1, source.Height);
            float scale = 1f;
            if (srcW > maxSize || srcH > maxSize)
            {
                float sx = maxSize / (float)srcW;
                float sy = maxSize / (float)srcH;
                scale = Math.Min(sx, sy);
            }

            int targetW = Math.Max(1, (int)Math.Round(srcW * scale));
            int targetH = Math.Max(1, (int)Math.Round(srcH * scale));
            Bitmap prepared = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(prepared))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, targetW, targetH));
            }

            return prepared;
        }

        private static int[] ExtractArgbPixels(Bitmap bitmap)
        {
            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return new int[0];
            }

            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = bitmap.Width;
                int height = bitmap.Height;
                int strideInts = data.Stride / 4;
                int[] raw = new int[strideInts * height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);

                int[] pixels = new int[width * height];
                for (int y = 0; y < height; y++)
                {
                    int srcOffset = y * strideInts;
                    int dstOffset = y * width;
                    Array.Copy(raw, srcOffset, pixels, dstOffset, width);
                }

                return pixels;
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static int ComputeAverageColorArgb(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return unchecked((int)0xFFFFFFFF);
            }

            long r = 0;
            long g = 0;
            long b = 0;
            long count = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                int argb = pixels[i];
                byte a = (byte)((argb >> 24) & 0xFF);
                if (a < 10)
                {
                    continue;
                }

                r += (argb >> 16) & 0xFF;
                g += (argb >> 8) & 0xFF;
                b += argb & 0xFF;
                count++;
            }

            if (count <= 0)
            {
                return unchecked((int)0xFFFFFFFF);
            }

            return Color.FromArgb(
                255,
                (int)(r / count),
                (int)(g / count),
                (int)(b / count)).ToArgb();
        }

        private static bool ComputeHasTransparency(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            // Treat textures with mostly binary alpha (0/255) as cutout/opaque for
            // depth-stable rendering. Only mark as transparent when there is enough
            // genuinely translucent coverage.
            int translucentCount = 0;
            int opaqueCount = 0;
            int translucentThreshold = Math.Max(32, pixels.Length / 200); // ~0.5%
            for (int i = 0; i < pixels.Length; i++)
            {
                int alpha = (pixels[i] >> 24) & 0xFF;
                if (alpha >= 248)
                {
                    opaqueCount++;
                    continue;
                }

                if (alpha <= 8)
                {
                    continue;
                }

                translucentCount++;
                if (translucentCount >= translucentThreshold)
                {
                    return true;
                }
            }

            if (opaqueCount <= 0)
            {
                return translucentCount > 0;
            }

            if (translucentCount >= (opaqueCount / 16)) // ~6.25%
            {
                return true;
            }

            return false;
        }

        private static bool TryParseBonBindMatrices(byte[] bonBytes, out Dictionary<string, float[]> bindMatricesByBoneName)
        {
            bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            if (bonBytes == null || bonBytes.Length < 200)
            {
                return false;
            }

            Encoding gbk = Encoding.GetEncoding("GBK");
            const int minEntryTail = 16 + 128;
            for (int offset = 0; offset <= bonBytes.Length - (4 + 1 + minEntryTail); offset++)
            {
                int nameLength = BitConverter.ToInt32(bonBytes, offset);
                if (nameLength <= 0 || nameLength >= 64)
                {
                    continue;
                }

                int nameOffset = offset + 4;
                if (nameOffset + nameLength + minEntryTail > bonBytes.Length)
                {
                    continue;
                }

                string boneName;
                try
                {
                    boneName = gbk.GetString(bonBytes, nameOffset, nameLength).Trim('\0');
                }
                catch
                {
                    continue;
                }

                if (!IsLikelyBoneName(boneName))
                {
                    continue;
                }

                int infoOffset = nameOffset + nameLength;
                int childCount = BitConverter.ToInt32(bonBytes, infoOffset + 12);
                if (childCount < 0 || childCount > 512)
                {
                    continue;
                }

                int nextOffset = infoOffset + 16 + 128 + (childCount * 4);
                if (nextOffset > bonBytes.Length)
                {
                    continue;
                }

                float[] bindMatrix = new float[16];
                Buffer.BlockCopy(bonBytes, infoOffset + 16, bindMatrix, 0, 16 * sizeof(float));
                if (!bindMatricesByBoneName.ContainsKey(boneName))
                {
                    bindMatricesByBoneName[boneName] = bindMatrix;
                }
            }

            return bindMatricesByBoneName.Count > 0;
        }

        private static bool IsLikelyBoneName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            if (value.IndexOf("_AnimJoint", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return value.StartsWith("Bip", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("Bone", StringComparison.OrdinalIgnoreCase);
        }

        private static float[][] BuildSkinMatricesByBoneIndex(
            string[] skiBoneNames,
            Dictionary<string, float[]> bindMatricesByBoneName)
        {
            if (skiBoneNames == null
                || skiBoneNames.Length == 0
                || bindMatricesByBoneName == null
                || bindMatricesByBoneName.Count == 0)
            {
                return new float[0][];
            }

            float[][] matrices = new float[skiBoneNames.Length][];
            for (int i = 0; i < skiBoneNames.Length; i++)
            {
                string boneName = skiBoneNames[i] ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(boneName)
                    && bindMatricesByBoneName.TryGetValue(boneName, out float[] found)
                    && found != null
                    && found.Length >= 16)
                {
                    matrices[i] = found;
                }
                else
                {
                    matrices[i] = IdentitySkinMatrix;
                }
            }

            return matrices;
        }

        private static Vector3f ApplyBoneSkinning(
            Vector3f point,
            float weight0,
            float weight1,
            float weight2,
            uint packedBoneIndices,
            float[][] skinMatricesByBoneIndex)
        {
            if (skinMatricesByBoneIndex == null || skinMatricesByBoneIndex.Length == 0)
            {
                return point;
            }

            float weight3 = 1f - weight0 - weight1 - weight2;
            if (weight3 < 0f)
            {
                weight3 = 0f;
            }

            int i0 = (int)(packedBoneIndices & 0xFFu);
            int i1 = (int)((packedBoneIndices >> 8) & 0xFFu);
            int i2 = (int)((packedBoneIndices >> 16) & 0xFFu);
            int i3 = (int)((packedBoneIndices >> 24) & 0xFFu);

            Vector3f accumulated = Vector3f.Zero;
            float totalWeight = 0f;
            AccumulateSkinnedPoint(ref accumulated, ref totalWeight, point, weight0, i0, skinMatricesByBoneIndex);
            AccumulateSkinnedPoint(ref accumulated, ref totalWeight, point, weight1, i1, skinMatricesByBoneIndex);
            AccumulateSkinnedPoint(ref accumulated, ref totalWeight, point, weight2, i2, skinMatricesByBoneIndex);
            AccumulateSkinnedPoint(ref accumulated, ref totalWeight, point, weight3, i3, skinMatricesByBoneIndex);

            if (totalWeight <= 0.000001f)
            {
                return point;
            }

            return accumulated / totalWeight;
        }

        private static void AccumulateSkinnedPoint(
            ref Vector3f accumulated,
            ref float totalWeight,
            Vector3f point,
            float weight,
            int boneIndex,
            float[][] skinMatricesByBoneIndex)
        {
            if (weight <= 0.000001f)
            {
                return;
            }

            float[] matrix = IdentitySkinMatrix;
            if (boneIndex >= 0
                && boneIndex < skinMatricesByBoneIndex.Length
                && skinMatricesByBoneIndex[boneIndex] != null
                && skinMatricesByBoneIndex[boneIndex].Length >= 16)
            {
                matrix = skinMatricesByBoneIndex[boneIndex];
            }

            Vector3f transformed = TransformPointRowVector(point, matrix);
            accumulated = new Vector3f(
                accumulated.X + (transformed.X * weight),
                accumulated.Y + (transformed.Y * weight),
                accumulated.Z + (transformed.Z * weight));
            totalWeight += weight;
        }

        private static Vector3f TransformPointRowVector(Vector3f point, float[] matrix)
        {
            if (matrix == null || matrix.Length < 16)
            {
                return point;
            }

            float x = point.X;
            float y = point.Y;
            float z = point.Z;
            return new Vector3f(
                (x * matrix[0]) + (y * matrix[4]) + (z * matrix[8]) + matrix[12],
                (x * matrix[1]) + (y * matrix[5]) + (z * matrix[9]) + matrix[13],
                (x * matrix[2]) + (y * matrix[6]) + (z * matrix[10]) + matrix[14]);
        }

        private bool TryComputeTextureAverageColor(
            AssetManager assetManager,
            string package,
            string relativePath,
            out Color color)
        {
            color = Color.White;
            if (!TryReadModelFile(assetManager, package, relativePath, out byte[] bytes, out string _)
                || !TryDecodeTextureData(relativePath, bytes, out PreviewTextureData texture)
                || texture == null
                || !texture.IsValid)
            {
                return false;
            }

            color = Color.FromArgb(texture.AverageColorArgb);
            return true;
        }

        private static bool TrySkipZeroTerminatedString(BinaryReader br, int maxBytes)
        {
            if (br == null || maxBytes <= 0)
            {
                return false;
            }

            for (int i = 0; i < maxBytes; i++)
            {
                if (!TryEnsureRemaining(br, 1))
                {
                    return false;
                }

                if (br.ReadByte() == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadLengthPrefixedString(BinaryReader br, Encoding enc, int maxLength, out string value)
        {
            value = string.Empty;
            if (br == null || enc == null)
            {
                return false;
            }

            if (!TryEnsureRemaining(br, 4))
            {
                return false;
            }

            int len = br.ReadInt32();
            if (len < 0 || len > maxLength)
            {
                return false;
            }

            if (!TryEnsureRemaining(br, len))
            {
                return false;
            }

            byte[] bytes = br.ReadBytes(len);
            if (bytes.Length != len)
            {
                return false;
            }

            value = enc.GetString(bytes).Trim('\0');
            return true;
        }

        private static bool TryEnsureRemaining(BinaryReader br, long requiredBytes)
        {
            if (br == null || br.BaseStream == null || requiredBytes < 0)
            {
                return false;
            }

            long remaining = br.BaseStream.Length - br.BaseStream.Position;
            return remaining >= requiredBytes;
        }

        private struct SkiMaterial
        {
            public Color BaseColor;
        }
    }
}
