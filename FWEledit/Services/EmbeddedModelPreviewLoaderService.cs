using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class EmbeddedModelPreviewLoaderService
    {
        private readonly PckEntryReaderService pckEntryReaderService;
        private static readonly object PfimBindingsSync = new object();
        private static bool pfimBindingsInitialized;
        private static MethodInfo pfimFromStreamMethod;
        private static Type pfimImageType;
        private static PropertyInfo pfimCompressedProperty;
        private static MethodInfo pfimDecompressMethod;
        private static PropertyInfo pfimWidthProperty;
        private static PropertyInfo pfimHeightProperty;
        private static PropertyInfo pfimStrideProperty;
        private static PropertyInfo pfimBitsPerPixelProperty;
        private static PropertyInfo pfimDataProperty;
        private static MethodInfo pfimDisposeMethod;
        private const int MaxGfxReferenceDepth = 6;
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
                    new string[0],
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
                    new string[0],
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
                if (string.Equals(modelExtension, ".gfx", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryBuildPreviewDataFromGfxReferences(
                        assetManager,
                        mappedModelPath,
                        package,
                        relativeEcm,
                        ecmText,
                        ecmBytes,
                        out previewData,
                        out error))
                    {
                        return true;
                    }

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        return false;
                    }
                }

                error = "SkinModelPath not found in .ecm file.";
                return false;
            }
            string[] addiSkinPaths = ExtractRepeatedFieldValues(ecmText, "AddiSkinPath");

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
            List<string> skiReferenceCandidates = new List<string>(8);
            if (!string.IsNullOrWhiteSpace(skiReference))
            {
                skiReferenceCandidates.Add(skiReference);
            }
            if (addiSkinPaths != null && addiSkinPaths.Length > 0)
            {
                for (int i = 0; i < addiSkinPaths.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(addiSkinPaths[i]))
                    {
                        skiReferenceCandidates.Add(addiSkinPaths[i]);
                    }
                }
            }
            if (skiReferenceCandidates.Count == 0)
            {
                skiReferenceCandidates.Add(Path.ChangeExtension(relativeSmd, ".ski"));
            }

            if (!TryReadSkiWithFallbackCandidates(
                assetManager,
                smdPackage,
                relativeSmd,
                skiReferenceCandidates,
                out skiPackage,
                out relativeSki,
                out skiBytes,
                out error))
            {
                return false;
            }

            if (!TryBuildPreviewDataFromResolvedSki(
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
                addiSkinPaths,
                out ModelPreviewMeshData basePreview,
                out error))
            {
                return false;
            }

            previewData = basePreview;

            if (ShouldTryEcmEffectMerge(basePreview))
            {
                if (TryBuildPreviewDataFromEcmEffectReferences(
                        assetManager,
                        mappedModelPath,
                        package,
                        relativeEcm,
                        ecmText,
                        ecmBytes,
                        basePreview,
                        out ModelPreviewMeshData enhancedPreview,
                        out string _))
                {
                    previewData = enhancedPreview;
                }
            }

            error = string.Empty;
            return true;
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

            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                error = "Invalid package entry path.";
                return false;
            }

            List<KeyValuePair<string, string>> readTargets = BuildPckReadTargets(package, normalizedRelative);
            string firstError = string.Empty;
            for (int i = 0; i < readTargets.Count; i++)
            {
                string probePackage = readTargets[i].Key ?? string.Empty;
                string probeRelative = readTargets[i].Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(probePackage) || string.IsNullOrWhiteSpace(probeRelative))
                {
                    continue;
                }

                if (pckEntryReaderService.TryReadFile(probePackage, probeRelative, out bytes, out error))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                {
                    firstError = error;
                }
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Entry not found."
                : firstError;
            return false;
        }

        private static List<KeyValuePair<string, string>> BuildPckReadTargets(string preferredPackage, string relativePath)
        {
            List<KeyValuePair<string, string>> targets = new List<KeyValuePair<string, string>>(32);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return targets;
            }

            string splitPackage = string.Empty;
            string splitRelative = string.Empty;
            if (!ModelPickerService.TrySplitModelPackagePath(normalizedRelative, out splitPackage, out splitRelative))
            {
                splitPackage = string.Empty;
                splitRelative = normalizedRelative;
            }

            List<string> packageCandidates = new List<string>(10);
            HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePackage(packageCandidates, seenPackages, preferredPackage);
            AddUniquePackage(packageCandidates, seenPackages, splitPackage);
            AddUniquePackage(packageCandidates, seenPackages, "models");
            AddUniquePackage(packageCandidates, seenPackages, "gfx");
            AddUniquePackage(packageCandidates, seenPackages, "configs");
            AddUniquePackage(packageCandidates, seenPackages, "grasses");
            AddUniquePackage(packageCandidates, seenPackages, "litmodels");
            AddUniquePackage(packageCandidates, seenPackages, "moxing");
            AddUniquePackage(packageCandidates, seenPackages, "surfaces");
            AddUniquePackage(packageCandidates, seenPackages, string.Empty);

            List<string> relativeCandidates = new List<string>(8);
            HashSet<string> seenRelatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePathCandidate(relativeCandidates, seenRelatives, normalizedRelative);
            AddUniquePathCandidate(relativeCandidates, seenRelatives, splitRelative);

            bool hasConfigsPrefix = normalizedRelative.StartsWith("configs\\", StringComparison.OrdinalIgnoreCase);
            bool hasGfxPrefix = normalizedRelative.StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase);
            if (hasConfigsPrefix)
            {
                AddUniquePathCandidate(relativeCandidates, seenRelatives, NormalizeRelativePath(normalizedRelative.Substring("configs\\".Length)));
            }
            if (hasGfxPrefix)
            {
                AddUniquePathCandidate(relativeCandidates, seenRelatives, NormalizeRelativePath(normalizedRelative.Substring("gfx\\".Length)));
            }

            // Some gfx descriptors reference "folder\\file.smd", while the actual PCK entry
            // is stored as "models\\folder\\file.smd" (inside gfx.pck). Probe prefixed forms too.
            int baseRelativeCount = relativeCandidates.Count;
            for (int i = 0; i < baseRelativeCount; i++)
            {
                string rel = NormalizeRelativePath(relativeCandidates[i]);
                if (string.IsNullOrWhiteSpace(rel))
                {
                    continue;
                }

                if (!rel.StartsWith("models\\", StringComparison.OrdinalIgnoreCase))
                {
                    AddUniquePathCandidate(relativeCandidates, seenRelatives, "models\\" + rel);
                }
                if (!rel.StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase))
                {
                    AddUniquePathCandidate(relativeCandidates, seenRelatives, "gfx\\" + rel);
                }
                if (!rel.StartsWith("configs\\", StringComparison.OrdinalIgnoreCase))
                {
                    AddUniquePathCandidate(relativeCandidates, seenRelatives, "configs\\" + rel);
                }
            }

            for (int p = 0; p < packageCandidates.Count; p++)
            {
                string pkg = (packageCandidates[p] ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(pkg))
                {
                    continue;
                }

                for (int r = 0; r < relativeCandidates.Count; r++)
                {
                    string rel = NormalizeRelativePath(relativeCandidates[r]);
                    if (string.IsNullOrWhiteSpace(rel))
                    {
                        continue;
                    }

                    AddReadTarget(targets, seen, pkg, rel);

                    string pkgPrefix = pkg + "\\";
                    if (rel.StartsWith(pkgPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        AddReadTarget(targets, seen, pkg, NormalizeRelativePath(rel.Substring(pkgPrefix.Length)));
                    }
                }
            }

            return targets;
        }

        private static void AddReadTarget(
            List<KeyValuePair<string, string>> targets,
            HashSet<string> seen,
            string package,
            string relativePath)
        {
            if (targets == null || seen == null)
            {
                return;
            }

            string normalizedPackage = (package ?? string.Empty).Trim();
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedPackage) || string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return;
            }

            string key = (normalizedPackage + "|" + normalizedRelative).ToLowerInvariant();
            if (!seen.Add(key))
            {
                return;
            }

            targets.Add(new KeyValuePair<string, string>(normalizedPackage, normalizedRelative));
        }

        private bool TryReadSkiWithFallbackCandidates(
            AssetManager assetManager,
            string smdPackage,
            string relativeSmd,
            List<string> skiReferences,
            out string resolvedPackage,
            out string resolvedRelative,
            out byte[] skiBytes,
            out string error)
        {
            resolvedPackage = smdPackage;
            resolvedRelative = string.Empty;
            skiBytes = null;
            error = string.Empty;

            if (skiReferences == null || skiReferences.Count == 0)
            {
                return TryReadSkiWithFallback(
                    assetManager,
                    smdPackage,
                    relativeSmd,
                    string.Empty,
                    out resolvedPackage,
                    out resolvedRelative,
                    out skiBytes,
                    out error);
            }

            string firstError = string.Empty;
            for (int i = 0; i < skiReferences.Count; i++)
            {
                string reference = skiReferences[i];
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                if (TryReadSkiWithFallback(
                    assetManager,
                    smdPackage,
                    relativeSmd,
                    reference,
                    out resolvedPackage,
                    out resolvedRelative,
                    out skiBytes,
                    out error))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                {
                    firstError = error;
                }
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Failed to resolve .ski path for preview."
                : firstError;
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
            string[] additionalSkiReferences,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;
            string textureContextRelativePath = relativeEcm;
            if (string.IsNullOrWhiteSpace(textureContextRelativePath))
            {
                SplitPackagePath(sourceMappedPath, out string _, out string sourceRelativePath);
                textureContextRelativePath = sourceRelativePath;
            }
            if (string.IsNullOrWhiteSpace(textureContextRelativePath))
            {
                textureContextRelativePath = relativeSmd;
            }

            Dictionary<string, float[]> bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            float[][] bindMatricesBySkeletonBoneIndex = new float[0][];
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
                    TryParseBonBindMatrices(bonBytes, out bindMatricesByBoneName, out bindMatricesBySkeletonBoneIndex);
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
                textureContextRelativePath,
                skiBytes,
                bindMatricesByBoneName,
                bindMatricesBySkeletonBoneIndex,
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

            List<Vector3f> mergedVertices = new List<Vector3f>(vertices.Length + 2048);
            List<Vector2f> mergedUvs = new List<Vector2f>(uvs.Length + 2048);
            List<int> mergedIndices = new List<int>(indices.Length + 4096);
            List<int> mergedTriangleColors = new List<int>(triangleColors.Length + 4096);
            List<int> mergedTriangleTextureIndices = new List<int>(triangleTextureIndices.Length + 4096);
            List<PreviewTextureData> mergedTextures = new List<PreviewTextureData>((textures == null ? 0 : textures.Length) + 8);

            AppendParsedMeshSegment(
                mergedVertices,
                mergedUvs,
                mergedIndices,
                mergedTriangleColors,
                mergedTriangleTextureIndices,
                mergedTextures,
                vertices,
                uvs,
                indices,
                triangleColors,
                triangleTextureIndices,
                textures);

            if (additionalSkiReferences != null && additionalSkiReferences.Length > 0)
            {
                string primarySkiMappedPath = NormalizeRelativePath(BuildMappedPath(skiPackage, relativeSki));
                HashSet<string> seenSkiPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(primarySkiMappedPath))
                {
                    seenSkiPaths.Add(primarySkiMappedPath);
                }

                for (int i = 0; i < additionalSkiReferences.Length; i++)
                {
                    string addiRef = additionalSkiReferences[i];
                    if (string.IsNullOrWhiteSpace(addiRef))
                    {
                        continue;
                    }

                    if (!TryReadSkiWithFallback(
                        assetManager,
                        smdPackage,
                        relativeSmd,
                        addiRef,
                        out string addiPackage,
                        out string addiRelative,
                        out byte[] addiBytes,
                        out string _))
                    {
                        continue;
                    }

                    string addiMappedPath = NormalizeRelativePath(BuildMappedPath(addiPackage, addiRelative));
                    if (!string.IsNullOrWhiteSpace(addiMappedPath) && !seenSkiPaths.Add(addiMappedPath))
                    {
                        continue;
                    }

                    if (!TryParseSkiMesh(
                        assetManager,
                        addiPackage,
                        addiRelative,
                        textureContextRelativePath,
                        addiBytes,
                        bindMatricesByBoneName,
                        bindMatricesBySkeletonBoneIndex,
                        out Vector3f[] addiVertices,
                        out Vector2f[] addiUvs,
                        out int[] addiIndices,
                        out int[] addiTriangleColors,
                        out int[] addiTriangleTextureIndices,
                        out PreviewTextureData[] addiTextures,
                        out string _))
                    {
                        continue;
                    }

                    AppendParsedMeshSegment(
                        mergedVertices,
                        mergedUvs,
                        mergedIndices,
                        mergedTriangleColors,
                        mergedTriangleTextureIndices,
                        mergedTextures,
                        addiVertices,
                        addiUvs,
                        addiIndices,
                        addiTriangleColors,
                        addiTriangleTextureIndices,
                        addiTextures);
                }
            }

            int[] finalTriangleColors = mergedTriangleColors.ToArray();
            ApplyOrgColorTintToTriangleColorsInPlace(finalTriangleColors, orgColorArgb);

            previewData = new ModelPreviewMeshData
            {
                SourceMappedPath = sourceMappedPath ?? string.Empty,
                EcmPath = BuildMappedPath(ecmPackage, relativeEcm),
                EcmAbsolutePath = ResolveAbsolutePath(assetManager, ecmPackage, relativeEcm),
                SkinModelPath = BuildMappedPath(smdPackage, relativeSmd),
                SkiPath = BuildMappedPath(skiPackage, relativeSki),
                SkiAbsolutePath = ResolveAbsolutePath(assetManager, skiPackage, relativeSki),
                Vertices = mergedVertices.ToArray(),
                UVs = mergedUvs.ToArray(),
                Indices = mergedIndices.ToArray(),
                TriangleColors = finalTriangleColors,
                TriangleTextureIndices = mergedTriangleTextureIndices.ToArray(),
                Textures = mergedTextures.ToArray(),
                SrcBlend = srcBlend,
                DestBlend = destBlend,
                EmissiveColorArgb = emissiveColorArgb,
                OrgColorArgb = orgColorArgb,
                ShaderFloats = shaderFloats ?? new float[0],
                OuterNum = outerNum
            };

            return true;
        }

        private static void AppendParsedMeshSegment(
            List<Vector3f> mergedVertices,
            List<Vector2f> mergedUvs,
            List<int> mergedIndices,
            List<int> mergedTriangleColors,
            List<int> mergedTriangleTextureIndices,
            List<PreviewTextureData> mergedTextures,
            Vector3f[] vertices,
            Vector2f[] uvs,
            int[] indices,
            int[] triangleColors,
            int[] triangleTextureIndices,
            PreviewTextureData[] textures)
        {
            if (mergedVertices == null
                || mergedUvs == null
                || mergedIndices == null
                || mergedTriangleColors == null
                || mergedTriangleTextureIndices == null
                || mergedTextures == null)
            {
                return;
            }

            Vector3f[] safeVertices = vertices ?? new Vector3f[0];
            Vector2f[] safeUvs = uvs ?? new Vector2f[0];
            int[] safeIndices = indices ?? new int[0];
            int[] safeTriangleColors = triangleColors ?? new int[0];
            int[] safeTriangleTextureIndices = triangleTextureIndices ?? new int[0];
            PreviewTextureData[] safeTextures = textures ?? new PreviewTextureData[0];

            int vertexBase = mergedVertices.Count;
            int textureBase = mergedTextures.Count;
            int triangleCount = safeIndices.Length / 3;

            mergedVertices.AddRange(safeVertices);
            if (safeUvs.Length == safeVertices.Length)
            {
                mergedUvs.AddRange(safeUvs);
            }
            else
            {
                for (int v = 0; v < safeVertices.Length; v++)
                {
                    mergedUvs.Add(new Vector2f(0f, 0f));
                }
            }

            for (int i = 0; i < safeIndices.Length; i++)
            {
                mergedIndices.Add(vertexBase + safeIndices[i]);
            }

            for (int t = 0; t < triangleCount; t++)
            {
                int color = unchecked((int)0xFFFFFFFF);
                if (t < safeTriangleColors.Length)
                {
                    color = safeTriangleColors[t];
                }
                mergedTriangleColors.Add(color);

                int textureIndex = -1;
                if (t < safeTriangleTextureIndices.Length)
                {
                    textureIndex = safeTriangleTextureIndices[t];
                }

                if (textureIndex >= 0)
                {
                    textureIndex += textureBase;
                }
                mergedTriangleTextureIndices.Add(textureIndex);
            }

            mergedTextures.AddRange(safeTextures);
        }

        private bool TryBuildPreviewDataFromEcmEffectReferences(
            AssetManager assetManager,
            string sourceMappedPath,
            string ecmPackage,
            string relativeEcm,
            string ecmText,
            byte[] ecmBytes,
            ModelPreviewMeshData basePreview,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = basePreview ?? new ModelPreviewMeshData();
            error = string.Empty;

            if (assetManager == null || basePreview == null)
            {
                return false;
            }

            List<string> references = ExtractModelReferenceCandidatesFromGfx(ecmText, ecmBytes);
            if (references == null || references.Count == 0)
            {
                return false;
            }

            HashSet<string> visitedGfx = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalizedSource = NormalizeRelativePath(sourceMappedPath);
            if (!string.IsNullOrWhiteSpace(normalizedSource))
            {
                visitedGfx.Add(normalizedSource);
            }

            HashSet<string> mergedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string baseKey = BuildPreviewMergeKey(basePreview);
            if (!string.IsNullOrWhiteSpace(baseKey))
            {
                mergedKeys.Add(baseKey);
            }

            List<Vector3f> mergedVertices = new List<Vector3f>(basePreview.VertexCount + 4096);
            List<Vector2f> mergedUvs = new List<Vector2f>((basePreview.UVs == null ? 0 : basePreview.UVs.Length) + 4096);
            List<int> mergedIndices = new List<int>(basePreview.Indices == null ? 4096 : basePreview.Indices.Length + 8192);
            List<int> mergedTriangleColors = new List<int>(basePreview.TriangleColors == null ? 4096 : basePreview.TriangleColors.Length + 8192);
            List<int> mergedTriangleTextureIndices = new List<int>(basePreview.TriangleTextureIndices == null ? 4096 : basePreview.TriangleTextureIndices.Length + 8192);
            List<PreviewTextureData> mergedTextures = new List<PreviewTextureData>((basePreview.Textures == null ? 0 : basePreview.Textures.Length) + 16);
            AppendParsedMeshSegment(
                mergedVertices,
                mergedUvs,
                mergedIndices,
                mergedTriangleColors,
                mergedTriangleTextureIndices,
                mergedTextures,
                basePreview.Vertices,
                basePreview.UVs,
                basePreview.Indices,
                basePreview.TriangleColors,
                basePreview.TriangleTextureIndices,
                basePreview.Textures);

            int mergedAdditional = 0;
            string firstError = string.Empty;
            ModelPreviewMeshData firstResolved = null;
            for (int i = 0; i < references.Count; i++)
            {
                string reference = references[i];
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                if (!TryBuildPreviewFromReferencedPath(
                    assetManager,
                    sourceMappedPath,
                    ecmPackage,
                    relativeEcm,
                    reference,
                    visitedGfx,
                    0,
                    out ModelPreviewMeshData resolvedData,
                    out string resolveError))
                {
                    if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(resolveError))
                    {
                        firstError = resolveError;
                    }
                    continue;
                }

                if (resolvedData == null || resolvedData.VertexCount <= 0 || resolvedData.TriangleCount <= 0)
                {
                    continue;
                }

                string key = BuildPreviewMergeKey(resolvedData);
                if (!mergedKeys.Add(key))
                {
                    continue;
                }

                if (firstResolved == null)
                {
                    firstResolved = resolvedData;
                }

                AppendParsedMeshSegment(
                    mergedVertices,
                    mergedUvs,
                    mergedIndices,
                    mergedTriangleColors,
                    mergedTriangleTextureIndices,
                    mergedTextures,
                    resolvedData.Vertices,
                    resolvedData.UVs,
                    resolvedData.Indices,
                    resolvedData.TriangleColors,
                    resolvedData.TriangleTextureIndices,
                    resolvedData.Textures);
                mergedAdditional++;
            }

            if (mergedAdditional <= 0 || mergedVertices.Count <= basePreview.VertexCount || mergedIndices.Count < 3)
            {
                if (!string.IsNullOrWhiteSpace(firstError))
                {
                    error = firstError;
                }
                return false;
            }

            ModelPreviewMeshData template = firstResolved ?? basePreview;
            previewData = new ModelPreviewMeshData
            {
                SourceMappedPath = basePreview.SourceMappedPath ?? sourceMappedPath ?? string.Empty,
                EcmPath = basePreview.EcmPath ?? string.Empty,
                EcmAbsolutePath = basePreview.EcmAbsolutePath ?? string.Empty,
                SkinModelPath = template.SkinModelPath ?? basePreview.SkinModelPath ?? string.Empty,
                SkiPath = template.SkiPath ?? basePreview.SkiPath ?? string.Empty,
                SkiAbsolutePath = template.SkiAbsolutePath ?? basePreview.SkiAbsolutePath ?? string.Empty,
                Vertices = mergedVertices.ToArray(),
                UVs = mergedUvs.ToArray(),
                Indices = mergedIndices.ToArray(),
                TriangleColors = mergedTriangleColors.ToArray(),
                TriangleTextureIndices = mergedTriangleTextureIndices.ToArray(),
                Textures = mergedTextures.ToArray(),
                SrcBlend = template.SrcBlend,
                DestBlend = template.DestBlend,
                EmissiveColorArgb = template.EmissiveColorArgb,
                OrgColorArgb = template.OrgColorArgb,
                ShaderFloats = template.ShaderFloats ?? new float[0],
                OuterNum = template.OuterNum
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

        private bool TryBuildPreviewDataFromGfxReferences(
            AssetManager assetManager,
            string sourceMappedPath,
            string gfxPackage,
            string relativeGfx,
            string gfxText,
            byte[] gfxBytes,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;

            List<string> candidates = ExtractModelReferenceCandidatesFromGfx(gfxText, gfxBytes);
            if (candidates.Count == 0)
            {
                error = "No model reference found in .gfx file.";
                return false;
            }

            HashSet<string> visitedGfx = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalizedSourcePath = NormalizeRelativePath(sourceMappedPath);
            if (!string.IsNullOrWhiteSpace(normalizedSourcePath))
            {
                visitedGfx.Add(normalizedSourcePath);
            }

            string firstError = string.Empty;
            int mergedModelCount = 0;
            HashSet<string> mergedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<Vector3f> mergedVertices = new List<Vector3f>(4096);
            List<Vector2f> mergedUvs = new List<Vector2f>(4096);
            List<int> mergedIndices = new List<int>(8192);
            List<int> mergedTriangleColors = new List<int>(8192);
            List<int> mergedTriangleTextureIndices = new List<int>(8192);
            List<PreviewTextureData> mergedTextures = new List<PreviewTextureData>(16);
            ModelPreviewMeshData firstResolved = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (TryBuildPreviewFromReferencedPath(
                    assetManager,
                    sourceMappedPath,
                    gfxPackage,
                    relativeGfx,
                    candidates[i],
                    visitedGfx,
                    0,
                    out ModelPreviewMeshData resolvedData,
                    out error))
                {
                    string mergeKey = BuildPreviewMergeKey(resolvedData);
                    if (!mergedKeys.Add(mergeKey))
                    {
                        continue;
                    }

                    if (firstResolved == null)
                    {
                        firstResolved = resolvedData;
                    }

                    AppendParsedMeshSegment(
                        mergedVertices,
                        mergedUvs,
                        mergedIndices,
                        mergedTriangleColors,
                        mergedTriangleTextureIndices,
                        mergedTextures,
                        resolvedData.Vertices,
                        resolvedData.UVs,
                        resolvedData.Indices,
                        resolvedData.TriangleColors,
                        resolvedData.TriangleTextureIndices,
                        resolvedData.Textures);
                    mergedModelCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                {
                    firstError = error;
                }
            }

            if (mergedModelCount > 0 && firstResolved != null && mergedVertices.Count > 0 && mergedIndices.Count >= 3)
            {
                previewData = new ModelPreviewMeshData
                {
                    SourceMappedPath = sourceMappedPath ?? string.Empty,
                    EcmPath = firstResolved.EcmPath ?? string.Empty,
                    EcmAbsolutePath = firstResolved.EcmAbsolutePath ?? string.Empty,
                    SkinModelPath = firstResolved.SkinModelPath ?? string.Empty,
                    SkiPath = firstResolved.SkiPath ?? string.Empty,
                    SkiAbsolutePath = firstResolved.SkiAbsolutePath ?? string.Empty,
                    Vertices = mergedVertices.ToArray(),
                    UVs = mergedUvs.ToArray(),
                    Indices = mergedIndices.ToArray(),
                    TriangleColors = mergedTriangleColors.ToArray(),
                    TriangleTextureIndices = mergedTriangleTextureIndices.ToArray(),
                    Textures = mergedTextures.ToArray(),
                    SrcBlend = firstResolved.SrcBlend,
                    DestBlend = firstResolved.DestBlend,
                    EmissiveColorArgb = firstResolved.EmissiveColorArgb,
                    OrgColorArgb = firstResolved.OrgColorArgb,
                    ShaderFloats = firstResolved.ShaderFloats ?? new float[0],
                    OuterNum = firstResolved.OuterNum
                };
                return true;
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Failed to resolve a previewable model from .gfx file."
                : firstError;
            return false;
        }

        private bool TryBuildPreviewFromReferencedPath(
            AssetManager assetManager,
            string sourceMappedPath,
            string basePackage,
            string baseRelativeFile,
            string referencePath,
            HashSet<string> visitedGfx,
            int depth,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;

            string normalizedReference = NormalizeRelativePath(referencePath);
            if (string.IsNullOrWhiteSpace(normalizedReference))
            {
                error = "Invalid model reference in .gfx file.";
                return false;
            }
            if (depth > MaxGfxReferenceDepth)
            {
                error = "Reached maximum .gfx reference depth while resolving preview model.";
                return false;
            }

            List<string> probes = new List<string>(4);
            string ext = (Path.GetExtension(normalizedReference) ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext))
            {
                probes.Add(normalizedReference + ".ecm");
                probes.Add(normalizedReference + ".smd");
                probes.Add(normalizedReference + ".ski");
                probes.Add(normalizedReference + ".gfx");
                probes.Add(normalizedReference + ".att");
                probes.Add(normalizedReference + ".sgc");
            }
            probes.Add(normalizedReference);

            bool fromGfxSpace = string.Equals(basePackage, "gfx", StringComparison.OrdinalIgnoreCase)
                || NormalizeRelativePath(sourceMappedPath).StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase);
            if (fromGfxSpace)
            {
                int baseProbeCount = probes.Count;
                for (int i = 0; i < baseProbeCount; i++)
                {
                    string candidate = NormalizeRelativePath(probes[i]);
                    if (string.IsNullOrWhiteSpace(candidate))
                    {
                        continue;
                    }
                    if (candidate.StartsWith("models\\", StringComparison.OrdinalIgnoreCase)
                        || candidate.StartsWith("gfx\\", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    probes.Add("models\\" + candidate);
                }
            }

            string normalizedSource = NormalizeRelativePath(sourceMappedPath);
            string firstError = string.Empty;
            for (int i = 0; i < probes.Count; i++)
            {
                string probe = probes[i];
                if (string.IsNullOrWhiteSpace(probe))
                {
                    continue;
                }

                ResolveReferencedPath(basePackage, baseRelativeFile, probe, out string targetPackage, out string targetRelative);
                if (string.IsNullOrWhiteSpace(targetRelative))
                {
                    continue;
                }

                string targetExt = (Path.GetExtension(targetRelative) ?? string.Empty).ToLowerInvariant();
                List<KeyValuePair<string, string>> targetLocations = BuildReferenceTargetCandidates(targetPackage, targetRelative);
                for (int t = 0; t < targetLocations.Count; t++)
                {
                    string probePackage = targetLocations[t].Key ?? string.Empty;
                    string probeRelative = targetLocations[t].Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(probeRelative))
                    {
                        continue;
                    }

                    string mappedTarget = BuildMappedPath(probePackage, probeRelative);
                    if (string.IsNullOrWhiteSpace(mappedTarget))
                    {
                        continue;
                    }

                    string normalizedTarget = NormalizeRelativePath(mappedTarget);
                    if (!string.IsNullOrWhiteSpace(normalizedSource)
                        && string.Equals(normalizedTarget, normalizedSource, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (IsEffectDescriptorExtension(targetExt))
                    {
                        if (visitedGfx != null && !visitedGfx.Add(normalizedTarget))
                        {
                            continue;
                        }

                        if (!TryReadModelFile(assetManager, probePackage, probeRelative, out byte[] gfxBytes, out string readError))
                        {
                            if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(readError))
                            {
                                firstError = readError;
                            }
                            continue;
                        }

                        if (!TryDecodeText(gfxBytes, out string gfxText))
                        {
                            if (string.IsNullOrWhiteSpace(firstError))
                            {
                                firstError = "Failed to decode referenced .gfx file: " + mappedTarget;
                            }
                            continue;
                        }

                        List<string> nestedReferences = ExtractModelReferenceCandidatesFromGfx(gfxText, gfxBytes);
                        if (nestedReferences.Count == 0)
                        {
                            continue;
                        }

                        int nestedMergedCount = 0;
                        HashSet<string> nestedMergedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        List<Vector3f> nestedMergedVertices = new List<Vector3f>(4096);
                        List<Vector2f> nestedMergedUvs = new List<Vector2f>(4096);
                        List<int> nestedMergedIndices = new List<int>(8192);
                        List<int> nestedMergedTriangleColors = new List<int>(8192);
                        List<int> nestedMergedTriangleTextureIndices = new List<int>(8192);
                        List<PreviewTextureData> nestedMergedTextures = new List<PreviewTextureData>(16);
                        ModelPreviewMeshData firstNestedResolved = null;
                        for (int n = 0; n < nestedReferences.Count; n++)
                        {
                            if (TryBuildPreviewFromReferencedPath(
                                assetManager,
                                sourceMappedPath,
                                probePackage,
                                probeRelative,
                                nestedReferences[n],
                                visitedGfx,
                                depth + 1,
                                out ModelPreviewMeshData nestedResolvedData,
                                out error))
                            {
                                string nestedMergeKey = BuildPreviewMergeKey(nestedResolvedData);
                                if (!nestedMergedKeys.Add(nestedMergeKey))
                                {
                                    continue;
                                }

                                if (firstNestedResolved == null)
                                {
                                    firstNestedResolved = nestedResolvedData;
                                }

                                AppendParsedMeshSegment(
                                    nestedMergedVertices,
                                    nestedMergedUvs,
                                    nestedMergedIndices,
                                    nestedMergedTriangleColors,
                                    nestedMergedTriangleTextureIndices,
                                    nestedMergedTextures,
                                    nestedResolvedData.Vertices,
                                    nestedResolvedData.UVs,
                                    nestedResolvedData.Indices,
                                    nestedResolvedData.TriangleColors,
                                    nestedResolvedData.TriangleTextureIndices,
                                    nestedResolvedData.Textures);
                                nestedMergedCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                            {
                                firstError = error;
                            }
                        }

                        if (nestedMergedCount > 0
                            && firstNestedResolved != null
                            && nestedMergedVertices.Count > 0
                            && nestedMergedIndices.Count >= 3)
                        {
                            previewData = new ModelPreviewMeshData
                            {
                                SourceMappedPath = sourceMappedPath ?? string.Empty,
                                EcmPath = firstNestedResolved.EcmPath ?? string.Empty,
                                EcmAbsolutePath = firstNestedResolved.EcmAbsolutePath ?? string.Empty,
                                SkinModelPath = firstNestedResolved.SkinModelPath ?? string.Empty,
                                SkiPath = firstNestedResolved.SkiPath ?? string.Empty,
                                SkiAbsolutePath = firstNestedResolved.SkiAbsolutePath ?? string.Empty,
                                Vertices = nestedMergedVertices.ToArray(),
                                UVs = nestedMergedUvs.ToArray(),
                                Indices = nestedMergedIndices.ToArray(),
                                TriangleColors = nestedMergedTriangleColors.ToArray(),
                                TriangleTextureIndices = nestedMergedTriangleTextureIndices.ToArray(),
                                Textures = nestedMergedTextures.ToArray(),
                                SrcBlend = firstNestedResolved.SrcBlend,
                                DestBlend = firstNestedResolved.DestBlend,
                                EmissiveColorArgb = firstNestedResolved.EmissiveColorArgb,
                                OrgColorArgb = firstNestedResolved.OrgColorArgb,
                                ShaderFloats = firstNestedResolved.ShaderFloats ?? new float[0],
                                OuterNum = firstNestedResolved.OuterNum
                            };
                            return true;
                        }

                        continue;
                    }

                    if (TryLoadPreviewMesh(assetManager, mappedTarget, out previewData, out error))
                    {
                        return true;
                    }

                    // Some FX chains point to intermediate text descriptors with non-standard
                    // extensions; try parsing them recursively before failing.
                    if (visitedGfx != null && !visitedGfx.Add(normalizedTarget))
                    {
                        continue;
                    }

                    if (TryReadModelFile(assetManager, probePackage, probeRelative, out byte[] nestedBytes, out string nestedReadError)
                        && TryDecodeText(nestedBytes, out string nestedText))
                    {
                        List<string> nestedReferences = ExtractModelReferenceCandidatesFromGfx(nestedText, nestedBytes);
                        int nestedMergedCount = 0;
                        HashSet<string> nestedMergedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        List<Vector3f> nestedMergedVertices = new List<Vector3f>(4096);
                        List<Vector2f> nestedMergedUvs = new List<Vector2f>(4096);
                        List<int> nestedMergedIndices = new List<int>(8192);
                        List<int> nestedMergedTriangleColors = new List<int>(8192);
                        List<int> nestedMergedTriangleTextureIndices = new List<int>(8192);
                        List<PreviewTextureData> nestedMergedTextures = new List<PreviewTextureData>(16);
                        ModelPreviewMeshData firstNestedResolved = null;
                        for (int n = 0; n < nestedReferences.Count; n++)
                        {
                            if (TryBuildPreviewFromReferencedPath(
                                assetManager,
                                sourceMappedPath,
                                probePackage,
                                probeRelative,
                                nestedReferences[n],
                                visitedGfx,
                                depth + 1,
                                out ModelPreviewMeshData nestedResolvedData,
                                out error))
                            {
                                string nestedMergeKey = BuildPreviewMergeKey(nestedResolvedData);
                                if (!nestedMergedKeys.Add(nestedMergeKey))
                                {
                                    continue;
                                }

                                if (firstNestedResolved == null)
                                {
                                    firstNestedResolved = nestedResolvedData;
                                }

                                AppendParsedMeshSegment(
                                    nestedMergedVertices,
                                    nestedMergedUvs,
                                    nestedMergedIndices,
                                    nestedMergedTriangleColors,
                                    nestedMergedTriangleTextureIndices,
                                    nestedMergedTextures,
                                    nestedResolvedData.Vertices,
                                    nestedResolvedData.UVs,
                                    nestedResolvedData.Indices,
                                    nestedResolvedData.TriangleColors,
                                    nestedResolvedData.TriangleTextureIndices,
                                    nestedResolvedData.Textures);
                                nestedMergedCount++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                            {
                                firstError = error;
                            }
                        }

                        if (nestedMergedCount > 0
                            && firstNestedResolved != null
                            && nestedMergedVertices.Count > 0
                            && nestedMergedIndices.Count >= 3)
                        {
                            previewData = new ModelPreviewMeshData
                            {
                                SourceMappedPath = sourceMappedPath ?? string.Empty,
                                EcmPath = firstNestedResolved.EcmPath ?? string.Empty,
                                EcmAbsolutePath = firstNestedResolved.EcmAbsolutePath ?? string.Empty,
                                SkinModelPath = firstNestedResolved.SkinModelPath ?? string.Empty,
                                SkiPath = firstNestedResolved.SkiPath ?? string.Empty,
                                SkiAbsolutePath = firstNestedResolved.SkiAbsolutePath ?? string.Empty,
                                Vertices = nestedMergedVertices.ToArray(),
                                UVs = nestedMergedUvs.ToArray(),
                                Indices = nestedMergedIndices.ToArray(),
                                TriangleColors = nestedMergedTriangleColors.ToArray(),
                                TriangleTextureIndices = nestedMergedTriangleTextureIndices.ToArray(),
                                Textures = nestedMergedTextures.ToArray(),
                                SrcBlend = firstNestedResolved.SrcBlend,
                                DestBlend = firstNestedResolved.DestBlend,
                                EmissiveColorArgb = firstNestedResolved.EmissiveColorArgb,
                                OrgColorArgb = firstNestedResolved.OrgColorArgb,
                                ShaderFloats = firstNestedResolved.ShaderFloats ?? new float[0],
                                OuterNum = firstNestedResolved.OuterNum
                            };
                            return true;
                        }
                    }
                    else if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(nestedReadError))
                    {
                        firstError = nestedReadError;
                    }

                    if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                    {
                        firstError = error;
                    }
                }
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Failed to resolve referenced preview model from .gfx file."
                : firstError;
            return false;
        }

        private static List<KeyValuePair<string, string>> BuildReferenceTargetCandidates(string package, string relativePath)
        {
            List<KeyValuePair<string, string>> candidates = new List<KeyValuePair<string, string>>(6);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddTargetCandidate(candidates, seen, package, relativePath);
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return candidates;
            }

            bool looksLikeConfigPrefixed = normalizedRelative.StartsWith("configs\\", StringComparison.OrdinalIgnoreCase);
            string withoutConfigsPrefix = looksLikeConfigPrefixed
                ? NormalizeRelativePath(normalizedRelative.Substring("configs\\".Length))
                : normalizedRelative;

            if (string.Equals(package, "configs", StringComparison.OrdinalIgnoreCase) || looksLikeConfigPrefixed)
            {
                AddTargetCandidate(candidates, seen, "gfx", withoutConfigsPrefix);
                AddTargetCandidate(candidates, seen, "gfx", normalizedRelative);
            }

            if (string.Equals(package, "gfx", StringComparison.OrdinalIgnoreCase))
            {
                AddTargetCandidate(candidates, seen, "configs", withoutConfigsPrefix);
                AddTargetCandidate(candidates, seen, "configs", normalizedRelative);
            }

            AddTargetCandidate(candidates, seen, string.Empty, withoutConfigsPrefix);
            AddTargetCandidate(candidates, seen, string.Empty, normalizedRelative);
            return candidates;
        }

        private static void AddTargetCandidate(
            List<KeyValuePair<string, string>> candidates,
            HashSet<string> seen,
            string package,
            string relativePath)
        {
            if (candidates == null || seen == null)
            {
                return;
            }

            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return;
            }

            string normalizedPackage = (package ?? string.Empty).Trim();
            string key = (normalizedPackage + "|" + normalizedRelative).ToLowerInvariant();
            if (!seen.Add(key))
            {
                return;
            }

            candidates.Add(new KeyValuePair<string, string>(normalizedPackage, normalizedRelative));
        }

        private static string BuildPreviewMergeKey(ModelPreviewMeshData previewData)
        {
            if (previewData == null)
            {
                return "null";
            }

            string primary = NormalizeRelativePath(previewData.SkiPath);
            if (!string.IsNullOrWhiteSpace(primary))
            {
                return "ski|" + primary.ToLowerInvariant();
            }

            string fallback = NormalizeRelativePath(previewData.SkinModelPath);
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return "skin|" + fallback.ToLowerInvariant();
            }

            return "mesh|" + previewData.VertexCount.ToString(CultureInfo.InvariantCulture)
                + "|" + previewData.TriangleCount.ToString(CultureInfo.InvariantCulture);
        }

        private static bool ShouldTryEcmEffectMerge(ModelPreviewMeshData basePreview)
        {
            if (basePreview == null)
            {
                return false;
            }

            int triCount = basePreview.TriangleCount;
            int vertCount = basePreview.VertexCount;
            int texCount = basePreview.Textures == null ? 0 : basePreview.Textures.Length;

            if (triCount <= 0 || vertCount <= 0)
            {
                return true;
            }

            // Placeholder / proxy meshes (like 2-triangle planes) should try effect-chain merge.
            if (triCount <= 64 || vertCount <= 128)
            {
                return true;
            }

            // Small and low-texture previews are likely incomplete skin placeholders.
            if (triCount <= 512 && texCount <= 2)
            {
                return true;
            }

            return false;
        }

        private static List<string> ExtractModelReferenceCandidatesFromGfx(string text, byte[] bytes)
        {
            List<string> candidates = new List<string>(16);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string[] directFields = new string[]
            {
                "SkinModelPath",
                "ModelPath",
                "ModelFile",
                "FileModel",
                "FilePath",
                "EcmPath",
                "SmdPath",
                "SkiPath",
                "FxFilePath",
                "GfxPath",
                "GfxFile",
                "AttPath",
                "AttFile",
                "SgcPath",
                "SgcFile",
                "Path"
            };

            for (int i = 0; i < directFields.Length; i++)
            {
                string fieldName = directFields[i];
                if (TryExtractFieldValue(text, fieldName, out string fieldPath))
                {
                    AddUniquePathCandidate(candidates, seen, fieldPath);
                }

                string[] repeated = ExtractRepeatedFieldValues(text, fieldName);
                for (int r = 0; r < repeated.Length; r++)
                {
                    AddUniquePathCandidate(candidates, seen, repeated[r]);
                }
            }

            string[] extensions = new string[] { ".ecm", ".smd", ".ski", ".gfx", ".att", ".sgc" };
            for (int i = 0; i < extensions.Length; i++)
            {
                string ext = extensions[i];
                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        MatchCollection matches = Regex.Matches(
                            text,
                            @"([^\0\r\n\t:]+" + Regex.Escape(ext) + ")",
                            RegexOptions.IgnoreCase);
                        for (int m = 0; m < matches.Count; m++)
                        {
                            string raw = matches[m].Groups[1].Value;
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(raw));
                        }
                    }
                    catch
                    {
                    }
                }

                if (TryExtractPathByRawByteScan(bytes, ext, out string rawPath))
                {
                    AddUniquePathCandidate(candidates, seen, rawPath);
                }
            }

            return candidates;
        }

        private static bool IsEffectDescriptorExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return string.Equals(extension, ".gfx", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".att", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".sgc", StringComparison.OrdinalIgnoreCase);
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

        private static string[] ExtractRepeatedFieldValues(string text, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(fieldName))
            {
                return new string[0];
            }

            List<string> values = new List<string>(4);
            string[] lines = text.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            string prefix = fieldName + ":";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i] ?? string.Empty;
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon < 0)
                {
                    continue;
                }

                string candidate = line.Substring(colon + 1).Trim().Trim('"');
                candidate = NormalizeRelativePath(candidate);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    values.Add(candidate);
                }
            }

            return values.ToArray();
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
            string textureContextRelativePath,
            byte[] bytes,
            Dictionary<string, float[]> bindMatricesByBoneName,
            float[][] bindMatricesBySkeletonBoneIndex,
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
                    if (!TryReadSkiFileHeader(br, out SkiFileHeader header, out error))
                    {
                        return false;
                    }

                    string[] skiBoneNames = new string[Math.Max(0, header.NumSkinBone)];
                    if (header.Version >= 9)
                    {
                        for (int i = 0; i < skiBoneNames.Length; i++)
                        {
                            if (!TryReadLengthPrefixedString(br, gbk, 8192, out string boneName))
                            {
                                error = "Invalid bone table in .ski.";
                                return false;
                            }

                            skiBoneNames[i] = boneName ?? string.Empty;
                        }
                    }

                    string[] textureNames = new string[Math.Max(0, header.NumTexture)];
                    for (int i = 0; i < textureNames.Length; i++)
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 8192, out string textureName))
                        {
                            error = "Invalid texture table in .ski.";
                            return false;
                        }
                        textureNames[i] = textureName ?? string.Empty;
                    }

                    SkiMaterial[] materials = new SkiMaterial[Math.Max(0, header.NumMaterial)];
                    for (int i = 0; i < materials.Length; i++)
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
                        textureContextRelativePath,
                        textureNames);
                    float[][] skinMatricesByBoneIndex = BuildSkinMatricesByBoneIndex(skiBoneNames, bindMatricesByBoneName);

                    List<Vector3f> allVertices = new List<Vector3f>(4096);
                    List<Vector2f> allUvs = new List<Vector2f>(4096);
                    List<int> allIndices = new List<int>(8192);
                    List<int> allTriangleColors = new List<int>(8192);
                    List<int> allTriangleTextureIndices = new List<int>(8192);
                    string parseError = string.Empty;

                    bool TryReadSkinMeshBlock()
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 4096, out _))
                        {
                            parseError = "Invalid mesh name in .ski.";
                            return false;
                        }

                        if (!TryEnsureRemaining(br, 16))
                        {
                            parseError = "Unexpected end of skin mesh metadata in .ski.";
                            return false;
                        }

                        int texIndex = br.ReadInt32();
                        int matIndex = br.ReadInt32();
                        int vertexCount = br.ReadInt32();
                        int indexCount = br.ReadInt32();

                        if (vertexCount < 0 || indexCount < 0)
                        {
                            parseError = "Invalid skin mesh counts in .ski.";
                            return false;
                        }
                        if (vertexCount == 0 || indexCount < 3)
                        {
                            return true;
                        }
                        if (vertexCount > 500000 || indexCount > 5000000)
                        {
                            parseError = "Skin mesh is too large for preview.";
                            return false;
                        }

                        Color meshColor = ResolveMeshColor(texIndex, matIndex, materials, textureDataByIndex);
                        int meshTextureIndex = ResolveMeshTextureIndex(texIndex, textureNames);

                        long skinVertexBytes = (long)vertexCount * 48L;
                        if (!TryEnsureRemaining(br, skinVertexBytes))
                        {
                            parseError = "Unexpected end of skin vertex data in .ski.";
                            return false;
                        }

                        int baseVertex = allVertices.Count;
                        for (int v = 0; v < vertexCount; v++)
                        {
                            float px = br.ReadSingle();
                            float py = br.ReadSingle();
                            float pz = br.ReadSingle();
                            br.ReadSingle(); // weight 0
                            br.ReadSingle(); // weight 1
                            br.ReadSingle(); // weight 2
                            br.ReadUInt32(); // packed matrix indices
                            br.ReadSingle(); // normal x
                            br.ReadSingle(); // normal y
                            br.ReadSingle(); // normal z
                            float tu = br.ReadSingle();
                            float tv = br.ReadSingle();

                            if (float.IsNaN(px) || float.IsNaN(py) || float.IsNaN(pz)
                                || float.IsInfinity(px) || float.IsInfinity(py) || float.IsInfinity(pz))
                            {
                                parseError = "Invalid vertex data in .ski.";
                                return false;
                            }

                            allVertices.Add(new Vector3f(-px, py, pz));
                            allUvs.Add(new Vector2f(tu, tv));
                        }

                        long indexBytes = (long)indexCount * 2L;
                        if (!TryEnsureRemaining(br, indexBytes))
                        {
                            parseError = "Unexpected end of skin index data in .ski.";
                            return false;
                        }

                        int[] rawIndices = new int[indexCount];
                        for (int i = 0; i < indexCount; i++)
                        {
                            rawIndices[i] = br.ReadUInt16();
                        }

                        int faceCount = indexCount / 3;
                        for (int f = 0; f < faceCount; f++)
                        {
                            int i0 = baseVertex + rawIndices[(f * 3) + 0];
                            int i1 = baseVertex + rawIndices[(f * 3) + 1];
                            int i2 = baseVertex + rawIndices[(f * 3) + 2];

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

                        return true;
                    }

                    bool TryReadRigidMeshBlock()
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 4096, out _))
                        {
                            parseError = "Invalid rigid mesh name in .ski.";
                            return false;
                        }

                        if (!TryEnsureRemaining(br, 20))
                        {
                            parseError = "Unexpected end of rigid mesh metadata in .ski.";
                            return false;
                        }

                        int boneIndex = br.ReadInt32();
                        int texIndex = br.ReadInt32();
                        int matIndex = br.ReadInt32();
                        int vertexCount = br.ReadInt32();
                        int indexCount = br.ReadInt32();

                        if (vertexCount < 0 || indexCount < 0)
                        {
                            parseError = "Invalid rigid mesh counts in .ski.";
                            return false;
                        }
                        if (vertexCount == 0 || indexCount < 3)
                        {
                            return true;
                        }
                        if (vertexCount > 500000 || indexCount > 5000000)
                        {
                            parseError = "Rigid mesh is too large for preview.";
                            return false;
                        }

                        Color meshColor = ResolveMeshColor(texIndex, matIndex, materials, textureDataByIndex);
                        int meshTextureIndex = ResolveMeshTextureIndex(texIndex, textureNames);

                        long rigidVertexBytes = (long)vertexCount * 32L;
                        if (!TryEnsureRemaining(br, rigidVertexBytes))
                        {
                            parseError = "Unexpected end of rigid vertex data in .ski.";
                            return false;
                        }

                        int baseVertex = allVertices.Count;
                        for (int v = 0; v < vertexCount; v++)
                        {
                            float px = br.ReadSingle();
                            float py = br.ReadSingle();
                            float pz = br.ReadSingle();
                            br.ReadSingle(); // normal x
                            br.ReadSingle(); // normal y
                            br.ReadSingle(); // normal z
                            float tu = br.ReadSingle();
                            float tv = br.ReadSingle();

                            Vector3f sourcePoint = new Vector3f(px, py, pz);
                            float[] rigidMatrix = null;
                            if (boneIndex >= 0
                                && bindMatricesBySkeletonBoneIndex != null
                                && boneIndex < bindMatricesBySkeletonBoneIndex.Length
                                && bindMatricesBySkeletonBoneIndex[boneIndex] != null
                                && bindMatricesBySkeletonBoneIndex[boneIndex].Length >= 16)
                            {
                                rigidMatrix = bindMatricesBySkeletonBoneIndex[boneIndex];
                            }
                            else if (boneIndex >= 0
                                && boneIndex < skinMatricesByBoneIndex.Length
                                && skinMatricesByBoneIndex[boneIndex] != null
                                && skinMatricesByBoneIndex[boneIndex].Length >= 16)
                            {
                                rigidMatrix = skinMatricesByBoneIndex[boneIndex];
                            }

                            if (rigidMatrix != null && rigidMatrix.Length >= 16)
                            {
                                sourcePoint = TransformPointRowVector(sourcePoint, rigidMatrix);
                            }

                            allVertices.Add(new Vector3f(-sourcePoint.X, sourcePoint.Y, sourcePoint.Z));
                            allUvs.Add(new Vector2f(tu, tv));
                        }

                        long indexBytes = (long)indexCount * 2L;
                        if (!TryEnsureRemaining(br, indexBytes))
                        {
                            parseError = "Unexpected end of rigid index data in .ski.";
                            return false;
                        }

                        int[] rawIndices = new int[indexCount];
                        for (int i = 0; i < indexCount; i++)
                        {
                            rawIndices[i] = br.ReadUInt16();
                        }

                        int faceCount = indexCount / 3;
                        for (int f = 0; f < faceCount; f++)
                        {
                            int i0 = baseVertex + rawIndices[(f * 3) + 0];
                            int i1 = baseVertex + rawIndices[(f * 3) + 1];
                            int i2 = baseVertex + rawIndices[(f * 3) + 2];

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

                        return true;
                    }

                    for (int i = 0; i < header.NumSkinMesh; i++)
                    {
                        if (!TryReadSkinMeshBlock())
                        {
                            error = parseError;
                            return false;
                        }
                    }

                    for (int i = 0; i < header.NumRigidMesh; i++)
                    {
                        if (!TryReadRigidMeshBlock())
                        {
                            error = parseError;
                            return false;
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

        private static bool TryReadSkiFileHeader(BinaryReader br, out SkiFileHeader header, out string error)
        {
            header = new SkiFileHeader();
            error = string.Empty;

            if (br == null)
            {
                error = "Invalid .ski reader.";
                return false;
            }

            if (!TryEnsureRemaining(br, 8))
            {
                error = "Invalid .ski header.";
                return false;
            }

            string prefix = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (string.Equals(prefix, "MOXB", StringComparison.Ordinal))
            {
                string flags = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (!string.Equals(flags, "IKSA", StringComparison.Ordinal))
                {
                    error = "Unsupported .ski identity: " + flags;
                    return false;
                }
            }
            else if (string.Equals(prefix, "IKSA", StringComparison.Ordinal))
            {
                // No MOXB file marker, raw ASKI stream.
            }
            else
            {
                error = "Unsupported .ski magic: " + prefix;
                return false;
            }

            if (!TryEnsureRemaining(br, 100))
            {
                error = "Truncated .ski header.";
                return false;
            }

            header.Version = br.ReadInt32();
            header.NumSkinMesh = Math.Max(0, br.ReadInt32());
            header.NumRigidMesh = Math.Max(0, br.ReadInt32());
            header.NumMorphSkinMesh = Math.Max(0, br.ReadInt32());
            header.NumMorphRigidMesh = Math.Max(0, br.ReadInt32());
            header.NumTexture = Math.Max(0, br.ReadInt32());
            header.NumMaterial = Math.Max(0, br.ReadInt32());
            header.NumSkinBone = Math.Max(0, br.ReadInt32());
            header.MinWeight = br.ReadSingle();
            header.NumSkeletonBone = Math.Max(0, br.ReadInt32());
            header.NumSuppleMesh = Math.Max(0, br.ReadInt32());
            header.NumMuscleMesh = Math.Max(0, br.ReadInt32());
            br.ReadBytes(52);

            if (header.Version <= 0 || header.Version > 64)
            {
                error = "Unsupported .ski version: " + header.Version;
                return false;
            }

            return true;
        }

        private static bool TryReadMaterialBlock(BinaryReader br, out SkiMaterial material)
        {
            material = new SkiMaterial();
            if (br == null)
            {
                return false;
            }

            if (!TryReadNullTerminatedString(br, 2048, out string name))
            {
                return false;
            }

            // A3DMaterial::Load (binary):
            // "MATERIAL: <name>\0" + Ambient + Diffuse + Emissive + Specular + Power + bool.
            const int colorBytes = 16 * 4;
            if (!TryEnsureRemaining(br, colorBytes + 4 + 1))
            {
                return false;
            }

            float ambientR = br.ReadSingle();
            float ambientG = br.ReadSingle();
            float ambientB = br.ReadSingle();
            float ambientA = br.ReadSingle();

            float diffuseR = br.ReadSingle();
            float diffuseG = br.ReadSingle();
            float diffuseB = br.ReadSingle();
            float diffuseA = br.ReadSingle();

            br.ReadSingle(); // emissive r
            br.ReadSingle(); // emissive g
            br.ReadSingle(); // emissive b
            br.ReadSingle(); // emissive a

            br.ReadSingle(); // specular r
            br.ReadSingle(); // specular g
            br.ReadSingle(); // specular b
            br.ReadSingle(); // specular a

            br.ReadSingle(); // power
            byte twoSided = br.ReadByte();

            material.Name = name ?? string.Empty;
            material.BaseColor = BuildMaterialBaseColor(diffuseR, diffuseG, diffuseB, ambientR, ambientG, ambientB);
            material.DiffuseAlpha = Clamp01(diffuseA);
            material.IsTwoSided = twoSided != 0;
            return true;
        }

        private static bool TryReadNullTerminatedString(BinaryReader br, int maxLength, out string value)
        {
            value = string.Empty;
            if (br == null || maxLength <= 0)
            {
                return false;
            }

            List<byte> buffer = new List<byte>(Math.Min(maxLength, 256));
            for (int i = 0; i < maxLength; i++)
            {
                if (!TryEnsureRemaining(br, 1))
                {
                    return false;
                }

                byte b = br.ReadByte();
                if (b == 0)
                {
                    value = Encoding.ASCII.GetString(buffer.ToArray());
                    return true;
                }

                buffer.Add(b);
            }

            return false;
        }

        private static Color BuildMaterialBaseColor(
            float diffuseR,
            float diffuseG,
            float diffuseB,
            float ambientR,
            float ambientG,
            float ambientB)
        {
            int r = FloatToByte(diffuseR);
            int g = FloatToByte(diffuseG);
            int b = FloatToByte(diffuseB);

            if (r == 0 && g == 0 && b == 0)
            {
                // Some files store weak diffuse and stronger ambient. Use ambient as fallback.
                r = FloatToByte(ambientR);
                g = FloatToByte(ambientG);
                b = FloatToByte(ambientB);
            }

            if (r == 0 && g == 0 && b == 0)
            {
                return Color.FromArgb(255, 255, 255, 255);
            }

            return Color.FromArgb(255, r, g, b);
        }

        private static void ApplyOrgColorTintToTriangleColorsInPlace(int[] triangleColors, int orgColorArgb)
        {
            if (triangleColors == null || triangleColors.Length == 0)
            {
                return;
            }

            int tintR = (orgColorArgb >> 16) & 0xFF;
            int tintG = (orgColorArgb >> 8) & 0xFF;
            int tintB = orgColorArgb & 0xFF;
            int tintA = (orgColorArgb >> 24) & 0xFF;
            if (tintR == 255 && tintG == 255 && tintB == 255 && tintA == 255)
            {
                return;
            }

            for (int i = 0; i < triangleColors.Length; i++)
            {
                int src = triangleColors[i];
                int sa = (src >> 24) & 0xFF;
                int sr = (src >> 16) & 0xFF;
                int sg = (src >> 8) & 0xFF;
                int sb = src & 0xFF;

                int da = (sa * tintA) / 255;
                int dr = (sr * tintR) / 255;
                int dg = (sg * tintG) / 255;
                int db = (sb * tintB) / 255;
                triangleColors[i] = (da << 24) | (dr << 16) | (dg << 8) | db;
            }
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
            string textureContextRelativePath,
            string[] textureNames)
        {
            Dictionary<int, PreviewTextureData> result = new Dictionary<int, PreviewTextureData>();
            if (textureNames == null || textureNames.Length == 0)
            {
                return result;
            }

            TextureAlphaPolicy alphaPolicy = ResolveTextureAlphaPolicy(relativeSkiPath);

            for (int i = 0; i < textureNames.Length; i++)
            {
                string texture = textureNames[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(texture))
                {
                    continue;
                }

                PreviewTextureData textureData;
                if (TryLoadTextureData(assetManager, skiPackage, relativeSkiPath, textureContextRelativePath, texture, alphaPolicy, out textureData))
                {
                    result[i] = textureData;
                }
            }

            if (IsMountRelativeSkiPath(relativeSkiPath))
            {
                RepairMountTextureSetInPlace(result);
            }

            return result;
        }

        private static void RepairMountTextureSetInPlace(Dictionary<int, PreviewTextureData> textures)
        {
            if (textures == null || textures.Count < 2)
            {
                return;
            }

            List<int> keys = new List<int>(textures.Keys);
            keys.Sort();
            for (int k = 0; k < keys.Count; k++)
            {
                int key = keys[k];
                if (!textures.TryGetValue(key, out PreviewTextureData primary)
                    || primary == null
                    || !primary.IsValid
                    || primary.Pixels == null
                    || primary.Pixels.Length == 0)
                {
                    continue;
                }

                bool primaryCutout = primary.UsesAlphaAsOpacity;
                bool primaryOpaqueAux = !primaryCutout && ShouldFillHiddenTexelsForOpaqueAuxAlpha(primary.Pixels);
                if (!primaryCutout && !primaryOpaqueAux)
                {
                    continue;
                }

                PreviewTextureData partner = null;
                for (int p = 0; p < keys.Count; p++)
                {
                    int otherKey = keys[p];
                    if (otherKey == key)
                    {
                        continue;
                    }

                    if (textures.TryGetValue(otherKey, out PreviewTextureData other)
                        && other != null
                        && other.IsValid
                        && other.Pixels != null
                        && other.Pixels.Length == primary.Pixels.Length
                        && other.Width == primary.Width
                        && other.Height == primary.Height)
                    {
                        if (primaryCutout && !other.UsesAlphaAsOpacity)
                        {
                            continue;
                        }

                        partner = other;
                        break;
                    }
                }

                if (partner == null)
                {
                    continue;
                }

                int[] dst = primary.Pixels;
                int[] src = partner.Pixels;
                bool changed = false;
                for (int i = 0; i < dst.Length; i++)
                {
                    int d = dst[i];
                    int da = (d >> 24) & 0xFF;
                    int dr = (d >> 16) & 0xFF;
                    int dg = (d >> 8) & 0xFF;
                    int db = d & 0xFF;

                    // Hidden black texels on mounts are often placeholders; recover from partner layer.
                    if (da > 20 || (dr + dg + db) > 24)
                    {
                        continue;
                    }

                    int s = src[i];
                    int sa = (s >> 24) & 0xFF;
                    int sr = (s >> 16) & 0xFF;
                    int sg = (s >> 8) & 0xFF;
                    int sb = s & 0xFF;
                    if (sa <= 8 || (sr + sg + sb) <= 24)
                    {
                        continue;
                    }

                    dst[i] = (da << 24) | (sr << 16) | (sg << 8) | sb;
                    changed = true;
                }

                if (changed)
                {
                    primary.AverageColorArgb = ComputeAverageColorArgb(primary.Pixels);
                }
            }
        }

        private static TextureAlphaPolicy ResolveTextureAlphaPolicy(string relativeSkiPath)
        {
            string normalized = NormalizeRelativePath(relativeSkiPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return TextureAlphaPolicy.Default;
            }

            string lower = normalized.ToLowerInvariant();
            if (lower.Contains("weapons\\") || lower.Contains("\\weapons\\"))
            {
                return TextureAlphaPolicy.PreferOpaque;
            }

            if (lower.Contains("wings\\")
                || lower.Contains("\\wings\\")
                || lower.Contains("wing\\")
                || lower.Contains("\\wing\\")
                || lower.Contains("翅膀"))
            {
                return TextureAlphaPolicy.PreferCutout;
            }

            if (IsMountRelativeSkiPath(lower))
            {
                // Mount atlases in FW frequently encode auxiliary data in alpha.
                // Keep them opaque and apply dedicated RGB recovery/fill steps.
                return TextureAlphaPolicy.PreferMountOpaque;
            }

            return TextureAlphaPolicy.Default;
        }

        private static bool IsMountRelativeSkiPath(string relativeSkiPath)
        {
            string normalized = NormalizeRelativePath(relativeSkiPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            string lower = normalized.ToLowerInvariant();
            return lower.Contains("lms\\mounts\\")
                || lower.Contains("\\mounts\\")
                || lower.Contains("mount\\")
                || normalized.Contains("骑宠")
                || normalized.Contains("坐骑");
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

            return Color.FromArgb(255, materialColor.R, materialColor.G, materialColor.B);
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
            string textureContextRelativePath,
            string texturePath,
            TextureAlphaPolicy alphaPolicy,
            out PreviewTextureData textureData)
        {
            textureData = null;

            string[] candidates = BuildTexturePathCandidates(relativeSkiPath, textureContextRelativePath, texturePath);
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
                    && TryDecodeTextureData(texRelative, bytes, alphaPolicy, out PreviewTextureData decoded))
                {
                    decoded.Name = resolvedTexturePath;
                    textureData = decoded;
                    return true;
                }
            }

            if (TryLoadTextureFromSkiAbsoluteNeighbors(assetManager, skiPackage, relativeSkiPath, texturePath, alphaPolicy, out textureData))
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
            TextureAlphaPolicy alphaPolicy,
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
                    if (TryDecodeTextureData(absolute, bytes, alphaPolicy, out PreviewTextureData decoded))
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

        private static string[] BuildTexturePathCandidates(string relativeSkiPath, string textureContextRelativePath, string texturePath)
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

            string contextDir = string.Empty;
            try
            {
                contextDir = NormalizeRelativePath(Path.GetDirectoryName(NormalizeRelativePath(textureContextRelativePath)) ?? string.Empty);
            }
            catch
            {
                contextDir = string.Empty;
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
                    if (!string.IsNullOrWhiteSpace(contextDir))
                    {
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(contextDir + "\\" + candidate));
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(contextDir + "\\textures\\" + fileName));
                        AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(contextDir + "\\texture\\" + fileName));
                        if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                        {
                            AddUniquePathCandidate(candidates, seen, NormalizeRelativePath(contextDir + "\\tex_" + skiNameNoExt + "\\" + fileName));
                        }
                    }

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
            string normalized = NormalizeRelativePath(NormalizeExtractedPathCandidate(value));
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

        private static string NormalizeExtractedPathCandidate(string value)
        {
            string candidate = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            candidate = candidate.Trim('"', '\'').Trim();
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return string.Empty;
            }

            // Common descriptors may leak as "ModelPath: xxx.smd" or "GfxPath：xxx.gfx".
            int separatorIndex = -1;
            char[] separators = new char[] { ':', '：', '=' };
            for (int i = 0; i < separators.Length; i++)
            {
                int idx = candidate.IndexOf(separators[i]);
                if (idx > 0 && (separatorIndex < 0 || idx < separatorIndex))
                {
                    separatorIndex = idx;
                }
            }

            if (separatorIndex > 0 && separatorIndex < candidate.Length - 1)
            {
                string left = candidate.Substring(0, separatorIndex).Trim();
                string right = candidate.Substring(separatorIndex + 1).Trim().Trim('"', '\'').Trim();
                if (!string.IsNullOrWhiteSpace(right)
                    && left.IndexOf('\\') < 0
                    && left.IndexOf('/') < 0
                    && left.IndexOf('.') < 0)
                {
                    candidate = right;
                }
            }

            return candidate;
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
            TextureAlphaPolicy alphaPolicy,
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
                    if (!TryLoadDdsBitmapWithPfim(bytes, out bitmap))
                    {
                        bitmap = DDS.LoadImage(bytes, true);
                    }
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
                    // Keep source texture resolution (only downscale if extremely large).
                    const int maxSize = 4096;
                    using (Bitmap prepared = PrepareTextureBitmap(bitmap, maxSize))
                    {
                        int[] pixels = ExtractArgbPixels(prepared);
                        if (pixels == null || pixels.Length == 0)
                        {
                            return false;
                        }

                        TextureAlphaUsage alphaUsage = ComputeTextureAlphaUsage(relativePath, pixels, alphaPolicy);
                        if (alphaPolicy == TextureAlphaPolicy.PreferMountOpaque)
                        {
                            if (alphaUsage == TextureAlphaUsage.Cutout)
                            {
                                FillHiddenTexelsFromNeighborsInPlace(prepared.Width, prepared.Height, pixels);
                            }
                            else
                            {
                                if (ShouldRecoverRgbFromAuxAlpha(pixels, TextureAlphaUsage.Opaque, alphaPolicy))
                                {
                                    RecoverRgbFromAuxAlphaInPlace(
                                        pixels,
                                        alphaPolicy == TextureAlphaPolicy.PreferMountOpaque);
                                }

                                // Mount atlases often store auxiliary/spec data in alpha.
                                // Promote hidden texels and force opaque alpha for stable preview parity.
                                FillHiddenTexelsFromNeighborsInPlace(prepared.Width, prepared.Height, pixels);
                                ForceOpaqueAlphaInPlace(pixels);
                                alphaUsage = TextureAlphaUsage.Opaque;
                            }
                        }
                        else
                        {
                            if (ShouldRecoverRgbFromAuxAlpha(pixels, alphaUsage, alphaPolicy))
                            {
                                RecoverRgbFromAuxAlphaInPlace(
                                    pixels,
                                    alphaPolicy == TextureAlphaPolicy.PreferMountOpaque);
                            }

                            if (alphaPolicy == TextureAlphaPolicy.PreferCutout
                                && (alphaUsage == TextureAlphaUsage.Cutout || ShouldFillHiddenTexelsForOpaqueAuxAlpha(pixels)))
                            {
                                FillHiddenTexelsFromNeighborsInPlace(prepared.Width, prepared.Height, pixels);
                            }
                        }

                        data = new PreviewTextureData
                        {
                            Width = prepared.Width,
                            Height = prepared.Height,
                            Pixels = pixels,
                            AverageColorArgb = ComputeAverageColorArgb(pixels),
                            UsesAlphaAsOpacity = alphaUsage != TextureAlphaUsage.Opaque,
                            HasTransparency = alphaUsage == TextureAlphaUsage.Blend
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

        private static bool TryLoadDdsBitmapWithPfim(byte[] bytes, out Bitmap bitmap)
        {
            bitmap = null;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            if (!EnsurePfimBindings())
            {
                return false;
            }

            object image = null;
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(bytes, false);
                image = pfimFromStreamMethod.Invoke(null, new object[] { stream });
                if (image == null)
                {
                    return false;
                }

                if (pfimCompressedProperty != null && pfimDecompressMethod != null)
                {
                    bool compressed = false;
                    object compressedValue = pfimCompressedProperty.GetValue(image, null);
                    if (compressedValue is bool boolValue)
                    {
                        compressed = boolValue;
                    }
                    else if (compressedValue != null)
                    {
                        compressed = Convert.ToBoolean(compressedValue, CultureInfo.InvariantCulture);
                    }

                    if (compressed)
                    {
                        pfimDecompressMethod.Invoke(image, null);
                    }
                }

                int width = Convert.ToInt32(pfimWidthProperty.GetValue(image, null), CultureInfo.InvariantCulture);
                int height = Convert.ToInt32(pfimHeightProperty.GetValue(image, null), CultureInfo.InvariantCulture);
                int stride = Convert.ToInt32(pfimStrideProperty.GetValue(image, null), CultureInfo.InvariantCulture);
                int bitsPerPixel = Convert.ToInt32(pfimBitsPerPixelProperty.GetValue(image, null), CultureInfo.InvariantCulture);
                byte[] data = pfimDataProperty.GetValue(image, null) as byte[];
                if (width <= 0 || height <= 0 || stride <= 0 || data == null || data.Length == 0)
                {
                    return false;
                }

                PixelFormat sourceFormat = bitsPerPixel == 24
                    ? PixelFormat.Format24bppRgb
                    : PixelFormat.Format32bppArgb;

                Bitmap decoded = new Bitmap(width, height, sourceFormat);
                BitmapData decodedBits = decoded.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly,
                    sourceFormat);
                try
                {
                    int dstStride = Math.Abs(decodedBits.Stride);
                    int rowBytes = Math.Min(dstStride, stride);
                    for (int y = 0; y < height; y++)
                    {
                        IntPtr dst = IntPtr.Add(decodedBits.Scan0, y * decodedBits.Stride);
                        int srcOffset = y * stride;
                        if (srcOffset < 0 || srcOffset >= data.Length)
                        {
                            break;
                        }

                        int copyBytes = Math.Min(rowBytes, data.Length - srcOffset);
                        if (copyBytes <= 0)
                        {
                            break;
                        }

                        Marshal.Copy(data, srcOffset, dst, copyBytes);
                    }
                }
                finally
                {
                    decoded.UnlockBits(decodedBits);
                }

                if (sourceFormat == PixelFormat.Format32bppArgb)
                {
                    bitmap = decoded;
                    decoded = null;
                }
                else
                {
                    Bitmap argb = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(argb))
                    {
                        g.DrawImageUnscaled(decoded, 0, 0);
                    }
                    decoded.Dispose();
                    bitmap = argb;
                }

                return bitmap != null;
            }
            catch
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                    bitmap = null;
                }
                return false;
            }
            finally
            {
                if (image != null && pfimDisposeMethod != null)
                {
                    try
                    {
                        pfimDisposeMethod.Invoke(image, null);
                    }
                    catch
                    {
                    }
                }

                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        private static bool EnsurePfimBindings()
        {
            lock (PfimBindingsSync)
            {
                if (pfimBindingsInitialized)
                {
                    return pfimFromStreamMethod != null
                        && pfimImageType != null
                        && pfimWidthProperty != null
                        && pfimHeightProperty != null
                        && pfimStrideProperty != null
                        && pfimBitsPerPixelProperty != null
                        && pfimDataProperty != null;
                }

                pfimBindingsInitialized = true;
                try
                {
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
                    string[] candidates = new string[]
                    {
                        Path.Combine(baseDir, "Pfim.dll"),
                        Path.Combine(baseDir, "lib", "Pfim.dll"),
                        Path.Combine(Environment.CurrentDirectory ?? string.Empty, "Pfim.dll")
                    };

                    string pfimPath = string.Empty;
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(candidates[i]) && File.Exists(candidates[i]))
                        {
                            pfimPath = candidates[i];
                            break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(pfimPath))
                    {
                        return false;
                    }

                    Assembly assembly = Assembly.LoadFrom(pfimPath);
                    if (assembly == null)
                    {
                        return false;
                    }

                    Type pfimageType = assembly.GetType("Pfim.Pfimage");
                    Type iimageType = assembly.GetType("Pfim.IImage");
                    if (pfimageType == null || iimageType == null)
                    {
                        return false;
                    }

                    MethodInfo fromStream = null;
                    MethodInfo[] methods = pfimageType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (!string.Equals(method.Name, "FromStream", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 1 && typeof(Stream).IsAssignableFrom(parameters[0].ParameterType))
                        {
                            fromStream = method;
                            break;
                        }
                    }

                    if (fromStream == null)
                    {
                        return false;
                    }

                    pfimFromStreamMethod = fromStream;
                    pfimImageType = iimageType;
                    pfimCompressedProperty = iimageType.GetProperty("Compressed");
                    pfimDecompressMethod = iimageType.GetMethod("Decompress", Type.EmptyTypes);
                    pfimWidthProperty = iimageType.GetProperty("Width");
                    pfimHeightProperty = iimageType.GetProperty("Height");
                    pfimStrideProperty = iimageType.GetProperty("Stride");
                    pfimBitsPerPixelProperty = iimageType.GetProperty("BitsPerPixel");
                    pfimDataProperty = iimageType.GetProperty("Data");
                    pfimDisposeMethod = iimageType.GetMethod("Dispose", Type.EmptyTypes);
                }
                catch
                {
                    pfimFromStreamMethod = null;
                    pfimImageType = null;
                    pfimCompressedProperty = null;
                    pfimDecompressMethod = null;
                    pfimWidthProperty = null;
                    pfimHeightProperty = null;
                    pfimStrideProperty = null;
                    pfimBitsPerPixelProperty = null;
                    pfimDataProperty = null;
                    pfimDisposeMethod = null;
                }

                return pfimFromStreamMethod != null
                    && pfimImageType != null
                    && pfimWidthProperty != null
                    && pfimHeightProperty != null
                    && pfimStrideProperty != null
                    && pfimBitsPerPixelProperty != null
                    && pfimDataProperty != null;
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
            if (targetW == srcW && targetH == srcH)
            {
                // Keep original texel data; this matches the legacy model viewer behavior.
                Bitmap copy = new Bitmap(srcW, srcH, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(copy))
                {
                    g.Clear(Color.Transparent);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.DrawImage(source, new Rectangle(0, 0, srcW, srcH));
                }

                return copy;
            }

            Bitmap prepared = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(prepared))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
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

        private enum TextureAlphaUsage
        {
            Opaque = 0,
            Cutout = 1,
            Blend = 2
        }

        private static bool ShouldRecoverRgbFromAuxAlpha(
            int[] pixels,
            TextureAlphaUsage alphaUsage,
            TextureAlphaPolicy alphaPolicy)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            // Only recover when we intentionally render as opaque.
            if (alphaUsage != TextureAlphaUsage.Opaque)
            {
                return false;
            }

            // Recovery is mainly needed on mount atlases where alpha stores aux data.
            if (alphaPolicy != TextureAlphaPolicy.PreferCutout
                && alphaPolicy != TextureAlphaPolicy.PreferMountOpaque)
            {
                return false;
            }

            int total = pixels.Length;
            int lowAlphaCount = 0;
            int midAlphaCount = 0;
            int highAlphaCount = 0;
            int lowAlphaDarkCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int argb = pixels[i];
                int a = (argb >> 24) & 0xFF;
                if (a <= 64)
                {
                    lowAlphaCount++;
                    int r = (argb >> 16) & 0xFF;
                    int g = (argb >> 8) & 0xFF;
                    int b = argb & 0xFF;
                    int lum = (r + g + b) / 3;
                    if (lum <= 24)
                    {
                        lowAlphaDarkCount++;
                    }
                }
                else if (a < 224)
                {
                    midAlphaCount++;
                }
                else
                {
                    highAlphaCount++;
                }
            }

            float lowRatio = lowAlphaCount / (float)Math.Max(1, total);
            float midRatio = midAlphaCount / (float)Math.Max(1, total);
            float highRatio = highAlphaCount / (float)Math.Max(1, total);
            float lowDarkRatio = lowAlphaDarkCount / (float)Math.Max(1, lowAlphaCount);

            // Normal aux-alpha recovery case: enough high-alpha anchors and clearly-dark low-alpha region.
            bool anchoredAuxAlpha = lowRatio >= 0.25f
                && (lowRatio + midRatio) >= 0.55f
                && highRatio >= 0.10f
                && highRatio <= 0.72f
                && lowDarkRatio >= 0.55f;
            if (anchoredAuxAlpha)
            {
                return true;
            }

            // Some mount atlases have almost no near-255 alpha, but still carry darkened RGB in low alpha.
            // Allow a softer recovery path to avoid black body patches without forcing opacity cutout.
            bool sparseHighAuxAlpha = lowRatio >= 0.20f
                && midRatio >= 0.35f
                && highRatio <= 0.02f
                && lowDarkRatio >= 0.90f;
            return sparseHighAuxAlpha;
        }

        private static void RecoverRgbFromAuxAlphaInPlace(int[] pixels, bool aggressive)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                int argb = pixels[i];
                int a = (argb >> 24) & 0xFF;
                if (a <= 0)
                {
                    continue;
                }

                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;

                // Un-premultiply compensation:
                // - default: conservative to avoid over-bright artifacts on non-mount assets
                // - aggressive: mounts often store heavily darkened aux-alpha RGB
                float denom = Math.Max(aggressive ? 36f : 108f, a);
                float scale = 255f / denom;
                if (scale <= 1.0001f)
                {
                    continue;
                }

                float maxScale = aggressive ? 4.10f : 1.85f;
                if (aggressive && a <= 24)
                {
                    maxScale = 6.00f;
                }
                if (scale > maxScale)
                {
                    scale = maxScale;
                }

                int rr = (int)(r * scale);
                int gg = (int)(g * scale);
                int bb = (int)(b * scale);
                if (rr > 255) rr = 255;
                if (gg > 255) gg = 255;
                if (bb > 255) bb = 255;

                pixels[i] = (a << 24) | (rr << 16) | (gg << 8) | bb;
            }
        }

        private static bool ShouldFillHiddenTexelsForOpaqueAuxAlpha(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            int total = pixels.Length;
            int lowAlphaCount = 0;
            int lowDarkCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int argb = pixels[i];
                int a = (argb >> 24) & 0xFF;
                if (a > 12)
                {
                    continue;
                }

                lowAlphaCount++;
                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;
                if ((r + g + b) <= 36)
                {
                    lowDarkCount++;
                }
            }

            if (lowAlphaCount <= 0)
            {
                return false;
            }

            float lowAlphaRatio = lowAlphaCount / (float)Math.Max(1, total);
            float lowDarkRatio = lowDarkCount / (float)Math.Max(1, lowAlphaCount);
            return lowAlphaRatio >= 0.10f && lowDarkRatio >= 0.75f;
        }

        private static void ForceOpaqueAlphaInPlace(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return;
            }

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] |= unchecked((int)0xFF000000);
            }
        }

        private static TextureAlphaUsage ComputeTextureAlphaUsage(
            string texturePath,
            int[] pixels,
            TextureAlphaPolicy alphaPolicy)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return TextureAlphaUsage.Opaque;
            }

            int total = pixels.Length;
            int fullyTransparentCount = 0;
            int lowAlphaCount = 0;
            int fullyOpaqueCount = 0;
            int translucentCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int alpha = (pixels[i] >> 24) & 0xFF;
                if (alpha <= 48)
                {
                    lowAlphaCount++;
                }

                if (alpha >= 248)
                {
                    fullyOpaqueCount++;
                    continue;
                }

                if (alpha <= 8)
                {
                    fullyTransparentCount++;
                    continue;
                }

                translucentCount++;
            }

            if (translucentCount <= 0)
            {
                return fullyTransparentCount > 0
                    ? TextureAlphaUsage.Cutout
                    : TextureAlphaUsage.Opaque;
            }

            float transparentRatio = fullyTransparentCount / (float)Math.Max(1, total);
            float lowAlphaRatio = lowAlphaCount / (float)Math.Max(1, total);
            float opaqueRatio = fullyOpaqueCount / (float)Math.Max(1, total);
            float translucentRatio = translucentCount / (float)Math.Max(1, total);

            if (alphaPolicy == TextureAlphaPolicy.PreferOpaque)
            {
                // Weapons are safer as opaque in FW data; alpha is commonly used as data channel.
                return TextureAlphaUsage.Opaque;
            }

            if (alphaPolicy == TextureAlphaPolicy.PreferMountOpaque)
            {
                return IsLikelyOpacityCutoutTextureForMount(pixels)
                    ? TextureAlphaUsage.Cutout
                    : TextureAlphaUsage.Opaque;
            }

            if (alphaPolicy == TextureAlphaPolicy.PreferCutout)
            {
                // Mount assets are mixed:
                // - some atlases store opacity cutout (manes/hair/feathers/cards),
                // - others use alpha as auxiliary/spec data.
                // Decide per texture instead of forcing one global mode.
                return IsLikelyOpacityCutoutTextureForMount(pixels)
                    ? TextureAlphaUsage.Cutout
                    : TextureAlphaUsage.Opaque;
            }

            // Conservative policy:
            // 1) Only use alpha as opacity for clearly cutout-like textures (mostly binary alpha).
            // 2) Never auto-enable full blend from texture statistics (too many FW assets use alpha as data).
            // This avoids ghosted / missing-body artifacts on weapons and mounts.

            if (transparentRatio < 0.0035f && lowAlphaRatio < 0.03f)
            {
                return TextureAlphaUsage.Opaque;
            }

            // If alpha is broadly mid-range, it's usually data (spec/gloss), not opacity.
            if (translucentRatio > 0.22f)
            {
                return TextureAlphaUsage.Opaque;
            }

            // If there are almost no strong opaque texels, avoid cutout classification.
            if (opaqueRatio < 0.05f)
            {
                return TextureAlphaUsage.Opaque;
            }

            bool binaryLeaningAtlas = translucentRatio <= 0.35f && transparentRatio >= 0.10f && opaqueRatio >= 0.10f;
            if (binaryLeaningAtlas)
            {
                return TextureAlphaUsage.Cutout;
            }

            bool mostlyBinaryAlpha = translucentRatio <= 0.12f;
            bool hasMeaningfulCutoutCoverage = (transparentRatio >= 0.0035f || lowAlphaRatio >= 0.05f) && opaqueRatio >= 0.08f;
            if (mostlyBinaryAlpha && hasMeaningfulCutoutCoverage)
            {
                return TextureAlphaUsage.Cutout;
            }

            return TextureAlphaUsage.Opaque;
        }

        private static bool IsLikelyOpacityCutoutTextureForMount(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
            {
                return false;
            }

            int total = pixels.Length;
            int lowAlphaCount = 0;
            int highAlphaCount = 0;
            int midAlphaCount = 0;
            int lowAlphaDarkCount = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                int argb = pixels[i];
                int a = (argb >> 24) & 0xFF;
                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;
                int lum = (r + g + b) / 3;

                if (a <= 8)
                {
                    lowAlphaCount++;
                    if (lum <= 30)
                    {
                        lowAlphaDarkCount++;
                    }
                }
                else if (a >= 224)
                {
                    highAlphaCount++;
                }
                else
                {
                    midAlphaCount++;
                }
            }

            float lowRatio = lowAlphaCount / (float)Math.Max(1, total);
            float highRatio = highAlphaCount / (float)Math.Max(1, total);
            float midRatio = midAlphaCount / (float)Math.Max(1, total);
            float lowDarkRatio = lowAlphaDarkCount / (float)Math.Max(1, lowAlphaCount);

            // Mount textures are frequently authored as binary/near-binary masks, but hidden texels
            // are not always dark (many use bright placeholder RGB). Keep this permissive.
            bool binaryMaskLike = lowRatio >= 0.012f
                && highRatio >= 0.12f
                && midRatio <= 0.30f;
            if (binaryMaskLike)
            {
                return true;
            }

            bool broadMaskLike = lowRatio >= 0.035f
                && highRatio >= 0.10f
                && midRatio <= 0.40f;
            if (broadMaskLike)
            {
                return true;
            }

            // Keep an extra strict-dark gate as fallback for older atlases.
            return lowRatio >= 0.015f
                && highRatio >= 0.18f
                && midRatio <= 0.30f
                && lowDarkRatio >= 0.40f;
        }

        private static void FillHiddenTexelsFromNeighborsInPlace(int width, int height, int[] pixels)
        {
            if (pixels == null || pixels.Length == 0 || width <= 0 || height <= 0)
            {
                return;
            }

            int total = width * height;
            if (pixels.Length < total)
            {
                return;
            }

            int[] snapshot = new int[total];
            for (int pass = 0; pass < 8; pass++)
            {
                Buffer.BlockCopy(pixels, 0, snapshot, 0, total * sizeof(int));
                bool changed = false;

                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int idx = row + x;
                        int argb = snapshot[idx];
                        int a = (argb >> 24) & 0xFF;
                        int r = (argb >> 16) & 0xFF;
                        int g = (argb >> 8) & 0xFF;
                        int b = argb & 0xFF;

                        // Fill only texels that are effectively hidden and very dark.
                        if (a > 8 || (r + g + b) > 12)
                        {
                            continue;
                        }

                        int sumR = 0;
                        int sumG = 0;
                        int sumB = 0;
                        int count = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            int ny = y + oy;
                            if (ny < 0 || ny >= height)
                            {
                                continue;
                            }

                            int nrow = ny * width;
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                {
                                    continue;
                                }

                                int nx = x + ox;
                                if (nx < 0 || nx >= width)
                                {
                                    continue;
                                }

                                int n = snapshot[nrow + nx];
                                int na = (n >> 24) & 0xFF;
                                if (na <= 24)
                                {
                                    continue;
                                }

                                int nr = (n >> 16) & 0xFF;
                                int ng = (n >> 8) & 0xFF;
                                int nb = n & 0xFF;
                                if ((nr + ng + nb) <= 18)
                                {
                                    continue;
                                }

                                sumR += nr;
                                sumG += ng;
                                sumB += nb;
                                count++;
                            }
                        }

                        if (count <= 0)
                        {
                            continue;
                        }

                        int rr = sumR / count;
                        int gg = sumG / count;
                        int bb = sumB / count;
                        pixels[idx] = (a << 24) | (rr << 16) | (gg << 8) | bb;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }
        }

        private static bool TryParseBonBindMatrices(
            byte[] bonBytes,
            out Dictionary<string, float[]> bindMatricesByBoneName,
            out float[][] bindMatricesBySkeletonBoneIndex)
        {
            bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            bindMatricesBySkeletonBoneIndex = new float[0][];
            if (bonBytes == null || bonBytes.Length < 200)
            {
                return false;
            }

            Encoding gbk = Encoding.GetEncoding("GBK");
            try
            {
                using (MemoryStream stream = new MemoryStream(bonBytes))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    // A3DSKELETONFILEHEADER (pack(1))
                    // dwFlags, dwVersion, iNumBone, iNumJoint, iNumHook, iAnimStart, iAnimEnd, iAnimFPS, reserved[64]
                    if (!TryEnsureRemaining(br, 8 + (7 * 4) + 64))
                    {
                        return false;
                    }

                    uint flags = br.ReadUInt32();
                    uint version = br.ReadUInt32();
                    int numBones = br.ReadInt32();
                    br.ReadInt32(); // num joints
                    br.ReadInt32(); // num hooks
                    br.ReadInt32(); // anim start
                    br.ReadInt32(); // anim end
                    br.ReadInt32(); // anim fps
                    br.ReadBytes(64); // reserved

                    if (flags != 0x41534B45u || version > 6u || numBones <= 0 || numBones > 4096)
                    {
                        return false;
                    }

                    List<float[]> orderedMatrices = new List<float[]>(numBones);
                    for (int i = 0; i < numBones; i++)
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 8192, out string boneName))
                        {
                            return false;
                        }

                        // A3DBone::BONEDATA (pack(1))
                        if (!TryEnsureRemaining(br, 1 + (3 * 4) + (16 * 4) + (16 * 4)))
                        {
                            return false;
                        }

                        br.ReadByte();      // byFlags
                        br.ReadInt32();     // iParent
                        br.ReadInt32();     // iFirstJoint
                        int childCount = br.ReadInt32(); // iNumChild
                        if (childCount < 0 || childCount > 2048)
                        {
                            return false;
                        }

                        br.ReadBytes(16 * 4); // matRelative
                        float[] bindMatrix = new float[16];
                        for (int m = 0; m < 16; m++)
                        {
                            bindMatrix[m] = br.ReadSingle(); // matBoneInit
                        }

                        if (!string.IsNullOrWhiteSpace(boneName) && !bindMatricesByBoneName.ContainsKey(boneName))
                        {
                            bindMatricesByBoneName[boneName] = bindMatrix;
                        }

                        orderedMatrices.Add(bindMatrix);

                        long childBytes = (long)childCount * 4L;
                        if (!TryEnsureRemaining(br, childBytes))
                        {
                            return false;
                        }
                        br.ReadBytes((int)childBytes); // children indices
                    }

                    bindMatricesBySkeletonBoneIndex = orderedMatrices.ToArray();
                    return bindMatricesByBoneName.Count > 0 || bindMatricesBySkeletonBoneIndex.Length > 0;
                }
            }
            catch
            {
                bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
                bindMatricesBySkeletonBoneIndex = new float[0][];
                return false;
            }
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
                || !TryDecodeTextureData(relativePath, bytes, TextureAlphaPolicy.Default, out PreviewTextureData texture)
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

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private struct SkiFileHeader
        {
            public int Version;
            public int NumSkinMesh;
            public int NumRigidMesh;
            public int NumMorphSkinMesh;
            public int NumMorphRigidMesh;
            public int NumTexture;
            public int NumMaterial;
            public int NumSkinBone;
            public float MinWeight;
            public int NumSkeletonBone;
            public int NumSuppleMesh;
            public int NumMuscleMesh;
        }

        private struct SkiMaterial
        {
            public string Name;
            public Color BaseColor;
            public float DiffuseAlpha;
            public bool IsTwoSided;
        }

        private enum TextureAlphaPolicy
        {
            Default = 0,
            PreferCutout = 1,
            PreferOpaque = 2,
            PreferMountOpaque = 3
        }
    }
}
