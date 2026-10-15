using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
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
        private static readonly object ResourcePackageCacheSync = new object();
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
        private static string cachedResourcePackagesRoot = string.Empty;
        private static string cachedResourcePackagesSignature = string.Empty;
        private static List<string> cachedResourcePackages = new List<string>();
        private static Dictionary<string, string> crossPackagePathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> crossPackageMissCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        [ThreadStatic]
        private static HashSet<string> capturedDependencyPaths;
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

            if (TryLoadPreviewMeshFast(assetManager, mappedModelPath, package, relativeModelPath, out previewData, out error))
            {
                return true;
            }

            string modelExtension = (Path.GetExtension(relativeModelPath) ?? string.Empty).ToLowerInvariant();
            if (string.Equals(modelExtension, ".ski", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryReadDirectSkiWithFallback(assetManager, package, relativeModelPath, out string directSkiPackage, out string directSkiRelative, out byte[] directSkiBytes, out error))
                {
                    return false;
                }

                return TryBuildPreviewDataFromResolvedSki(
                    assetManager,
                    mappedModelPath,
                    string.Empty,
                    string.Empty,
                    directSkiPackage,
                    directSkiRelative,
                    directSkiPackage,
                    directSkiRelative,
                    Path.ChangeExtension(directSkiRelative, ".bon"),
                    directSkiPackage,
                    directSkiRelative,
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
                if (!TryReadModelFile(assetManager, package, relativeModelPath, out string directSmdPackage, out string directSmdRelative, out byte[] smdBytesDirect, out error))
                {
                    return false;
                }

                string skiReferenceDirect;
                if (!TryExtractReferencedFileFromSmd(smdBytesDirect, ".ski", out skiReferenceDirect))
                {
                    skiReferenceDirect = Path.ChangeExtension(directSmdRelative, ".ski");
                    if (string.IsNullOrWhiteSpace(skiReferenceDirect))
                    {
                        skiReferenceDirect = string.Empty;
                    }
                }

                string bonReferenceDirect;
                if (!TryExtractReferencedFileFromSmd(smdBytesDirect, ".bon", out bonReferenceDirect))
                {
                    bonReferenceDirect = Path.ChangeExtension(directSmdRelative, ".bon");
                    if (string.IsNullOrWhiteSpace(bonReferenceDirect))
                    {
                        bonReferenceDirect = string.Empty;
                    }
                }

                if (!TryReadSkiWithFallback(
                    assetManager,
                    directSmdPackage,
                    directSmdRelative,
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
                    directSmdPackage,
                    directSmdRelative,
                    directSmdPackage,
                    directSmdRelative,
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
            if (!TryReadModelFile(assetManager, package, relativeEcm, out string resolvedEcmPackage, out string resolvedRelativeEcm, out ecmBytes, out error))
            {
                return false;
            }
            package = resolvedEcmPackage;
            relativeEcm = resolvedRelativeEcm;

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
            TryPreferLocalVariantSkinModel(package, relativeEcm, smdPackage, relativeSmd, out smdPackage, out relativeSmd);
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
                if (!TryReadModelFile(assetManager, smdPackage, relativeSmd, out string resolvedSmdPackage, out string resolvedRelativeSmd, out smdBytes, out error))
                {
                    return false;
                }
                smdPackage = resolvedSmdPackage;
                relativeSmd = resolvedRelativeSmd;

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

            bool hasEffectDrivenVisuals = HasEffectDrivenVisualReferences(ecmText);
            bool shouldForceEffectMerge = hasEffectDrivenVisuals && IsLikelyBlackPlaceholderPreview(basePreview);
            if (ShouldTryEcmEffectMerge(basePreview) || shouldForceEffectMerge)
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

            if (hasEffectDrivenVisuals && IsLikelyBlackPlaceholderPreview(previewData))
            {
                PromoteBlackPlaceholderTexturesForPreview(previewData);
            }

            error = string.Empty;
            return true;
        }

        private bool TryLoadPreviewMeshFast(
            AssetManager assetManager,
            string mappedModelPath,
            string package,
            string relativeModelPath,
            out ModelPreviewMeshData previewData,
            out string error)
        {
            previewData = new ModelPreviewMeshData();
            error = string.Empty;

            string modelExtension = (Path.GetExtension(relativeModelPath) ?? string.Empty).ToLowerInvariant();
            if (string.Equals(modelExtension, ".ski", StringComparison.OrdinalIgnoreCase))
            {
                if (!pckEntryReaderService.TryReadFileFast(package, relativeModelPath, out byte[] skiBytes, out string resolvedSkiRelative, out string readError))
                {
                    error = readError;
                    return false;
                }

                return TryBuildPreviewDataFromResolvedSki(
                    assetManager,
                    mappedModelPath,
                    string.Empty,
                    string.Empty,
                    package,
                    resolvedSkiRelative,
                    package,
                    resolvedSkiRelative,
                    Path.ChangeExtension(resolvedSkiRelative, ".bon"),
                    package,
                    resolvedSkiRelative,
                    skiBytes,
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
                if (!pckEntryReaderService.TryReadFileFast(package, relativeModelPath, out byte[] smdBytes, out string resolvedSmdRelative, out string readError))
                {
                    error = readError;
                    return false;
                }

                string skiReference;
                if (!TryExtractReferencedFileFromSmd(smdBytes, ".ski", out skiReference))
                {
                    skiReference = Path.ChangeExtension(resolvedSmdRelative, ".ski");
                }

                string bonReference;
                if (!TryExtractReferencedFileFromSmd(smdBytes, ".bon", out bonReference))
                {
                    bonReference = Path.ChangeExtension(resolvedSmdRelative, ".bon");
                }

                if (!TryReadSkiWithFastFallback(package, resolvedSmdRelative, skiReference, out string resolvedSkiRelative, out byte[] skiBytes, out error))
                {
                    return false;
                }

                return TryBuildPreviewDataFromResolvedSki(
                    assetManager,
                    mappedModelPath,
                    string.Empty,
                    string.Empty,
                    package,
                    resolvedSmdRelative,
                    package,
                    resolvedSmdRelative,
                    bonReference,
                    package,
                    resolvedSkiRelative,
                    skiBytes,
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

            if (!string.Equals(modelExtension, ".ecm", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(modelExtension, ".gfx", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!pckEntryReaderService.TryReadFileFast(package, relativeModelPath, out byte[] ecmBytes, out string resolvedEcmRelative, out string ecmReadError))
            {
                error = ecmReadError;
                return false;
            }

            if (!TryDecodeText(ecmBytes, out string ecmText))
            {
                error = "Failed to decode .ecm file content.";
                return false;
            }

            if (!TryExtractFieldValue(ecmText, "SkinModelPath", out string skinModelPath) || string.IsNullOrWhiteSpace(skinModelPath))
            {
                error = string.Empty;
                return false;
            }

            int srcBlend = ParseIntFieldOrDefault(ecmText, "SrcBlend", 5);
            int destBlend = ParseIntFieldOrDefault(ecmText, "DestBlend", 6);
            int emissiveColorArgb = ParseColorFieldOrDefault(ecmText, "EmissiveCol", 0);
            int orgColorArgb = ParseColorFieldOrDefault(ecmText, "OrgColor", unchecked((int)0xFFFFFFFF));
            int outerNum = ParseIntFieldOrDefault(ecmText, "OuterNum", 0);
            float[] shaderFloats = ParseFloatParameters(ecmText, outerNum);
            string[] addiSkinPaths = ExtractRepeatedFieldValues(ecmText, "AddiSkinPath");

            ResolveReferencedPath(package, resolvedEcmRelative, skinModelPath, out string smdPackage, out string relativeSmd);
            TryPreferLocalVariantSkinModel(package, resolvedEcmRelative, smdPackage, relativeSmd, out smdPackage, out relativeSmd);
            if (string.IsNullOrWhiteSpace(relativeSmd) || !string.Equals(smdPackage, package, StringComparison.OrdinalIgnoreCase))
            {
                error = string.Empty;
                return false;
            }

            string resolvedSmdRelativeFast = relativeSmd;
            byte[] smdBytesFast = null;
            string skiReferenceFast = relativeSmd;
            string bonReferenceFast = Path.ChangeExtension(relativeSmd, ".bon");
            if (!string.Equals(Path.GetExtension(relativeSmd), ".ski", StringComparison.OrdinalIgnoreCase))
            {
                if (!pckEntryReaderService.TryReadFileFast(package, relativeSmd, out smdBytesFast, out resolvedSmdRelativeFast, out string smdReadError))
                {
                    error = smdReadError;
                    return false;
                }

                if (!TryExtractReferencedFileFromSmd(smdBytesFast, ".ski", out skiReferenceFast))
                {
                    skiReferenceFast = Path.ChangeExtension(resolvedSmdRelativeFast, ".ski");
                }
                if (!TryExtractReferencedFileFromSmd(smdBytesFast, ".bon", out bonReferenceFast))
                {
                    bonReferenceFast = Path.ChangeExtension(resolvedSmdRelativeFast, ".bon");
                }
            }

            List<string> skiReferenceCandidates = new List<string>(8);
            if (!string.IsNullOrWhiteSpace(skiReferenceFast))
            {
                skiReferenceCandidates.Add(skiReferenceFast);
            }
            if (addiSkinPaths != null)
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
                skiReferenceCandidates.Add(Path.ChangeExtension(resolvedSmdRelativeFast, ".ski"));
            }

            string resolvedSkiRelativeFast = string.Empty;
            byte[] skiBytesFast = null;
            for (int i = 0; i < skiReferenceCandidates.Count; i++)
            {
                if (TryReadSkiWithFastFallback(package, resolvedSmdRelativeFast, skiReferenceCandidates[i], out resolvedSkiRelativeFast, out skiBytesFast, out string _))
                {
                    break;
                }
            }

            if (skiBytesFast == null || string.IsNullOrWhiteSpace(resolvedSkiRelativeFast))
            {
                error = string.Empty;
                return false;
            }

            return TryBuildPreviewDataFromResolvedSki(
                assetManager,
                mappedModelPath,
                package,
                resolvedEcmRelative,
                package,
                resolvedSmdRelativeFast,
                package,
                resolvedSmdRelativeFast,
                bonReferenceFast,
                package,
                resolvedSkiRelativeFast,
                skiBytesFast,
                srcBlend,
                destBlend,
                emissiveColorArgb,
                orgColorArgb,
                outerNum,
                shaderFloats,
                addiSkinPaths,
                out previewData,
                out error);
        }

        private bool TryReadSkiWithFastFallback(
            string package,
            string relativeSmd,
            string skiReference,
            out string resolvedRelativeSki,
            out byte[] skiBytes,
            out string error)
        {
            resolvedRelativeSki = string.Empty;
            skiBytes = null;
            error = string.Empty;

            string[] candidates = BuildSkiPathCandidates(relativeSmd, skiReference);
            List<string> unresolvedCandidates = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                ResolveReferencedPath(package, relativeSmd, candidate, out string probePackage, out string probeRelative);
                if (!string.Equals(probePackage, package, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(probeRelative))
                {
                    continue;
                }

                unresolvedCandidates.Add(probeRelative);

                if (pckEntryReaderService.TryReadFileFast(package, probeRelative, out skiBytes, out resolvedRelativeSki, out string _))
                {
                    return true;
                }

                if (pckEntryReaderService.TryResolveSiblingByExtension(package, probeRelative, ".ski", out string siblingRelative, out string _)
                    && pckEntryReaderService.TryReadFileFast(package, siblingRelative, out skiBytes, out resolvedRelativeSki, out string _))
                {
                    return true;
                }
            }

            if (TryResolveNearestSkiByFileName(package, relativeSmd, unresolvedCandidates, out resolvedRelativeSki)
                && pckEntryReaderService.TryReadFileFast(package, resolvedRelativeSki, out skiBytes, out resolvedRelativeSki, out string _))
            {
                return true;
            }

            error = "Failed to resolve .ski path for preview.";
            return false;
        }

        public bool TryCollectDependencyPaths(
            AssetManager assetManager,
            string mappedModelPath,
            out List<string> mappedPaths,
            out string error)
        {
            mappedPaths = new List<string>();
            HashSet<string> previousCapture = capturedDependencyPaths;
            capturedDependencyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                bool ok = TryLoadPreviewMesh(assetManager, mappedModelPath, out ModelPreviewMeshData _, out error);
                mappedPaths = capturedDependencyPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                return ok || mappedPaths.Count > 0;
            }
            finally
            {
                capturedDependencyPaths = previousCapture;
            }
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
            else if (relative.StartsWith("shaders\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "shaders";
                relative = relative.Substring("shaders\\".Length);
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
            if (normalized.StartsWith("models2\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "models2";
                relative = normalized.Substring("models2\\".Length);
                return;
            }
            if (normalized.StartsWith("litmodels\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "litmodels";
                relative = normalized.Substring("litmodels\\".Length);
                return;
            }
            if (normalized.StartsWith("shaders\\", StringComparison.OrdinalIgnoreCase))
            {
                package = "shaders";
                relative = normalized.Substring("shaders\\".Length);
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
            return TryReadModelFile(
                assetManager,
                package,
                relativePath,
                out string _,
                out string _,
                out bytes,
                out error);
        }

        private bool TryReadModelFile(
            AssetManager assetManager,
            string package,
            string relativePath,
            out string resolvedPackage,
            out string resolvedRelativePath,
            out byte[] bytes,
            out string error)
        {
            bytes = null;
            error = string.Empty;
            resolvedPackage = string.Empty;
            resolvedRelativePath = string.Empty;

            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                error = "Invalid package entry path.";
                return false;
            }

            string firstError = string.Empty;
            string absolutePath = ResolveAbsolutePath(assetManager, package, normalizedRelative);
            if (!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))
            {
                try
                {
                    bytes = File.ReadAllBytes(absolutePath);
                    resolvedPackage = (package ?? string.Empty).Trim();
                    resolvedRelativePath = normalizedRelative;
                    CaptureDependencyPath(resolvedPackage, resolvedRelativePath);
                    return true;
                }
                catch (Exception ex)
                {
                    firstError = ex.Message;
                }
            }

            List<KeyValuePair<string, string>> readTargets = BuildPckReadTargets(package, normalizedRelative);
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
                    resolvedPackage = probePackage;
                    resolvedRelativePath = NormalizeRelativeForPackage(probePackage, probeRelative);
                    CaptureDependencyPath(resolvedPackage, resolvedRelativePath);
                    return true;
                }

                if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                {
                    firstError = error;
                }
            }

            if (TryReadModelFileFromAnyPackage(assetManager, readTargets, out resolvedPackage, out resolvedRelativePath, out bytes, out string crossPackageError))
            {
                error = string.Empty;
                return true;
            }
            if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(crossPackageError))
            {
                firstError = crossPackageError;
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Entry not found."
                : firstError;
            error = DecorateMissingDetachedReferenceError(normalizedRelative, error);
            return false;
        }

        private bool TryReadModelFileInPackage(
            AssetManager assetManager,
            string package,
            string relativePath,
            out byte[] bytes)
        {
            bytes = null;
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return false;
            }

            string absolutePath = ResolveAbsolutePath(assetManager, package, normalizedRelative);
            if (!string.IsNullOrWhiteSpace(absolutePath) && File.Exists(absolutePath))
            {
                try
                {
                    bytes = File.ReadAllBytes(absolutePath);
                    CaptureDependencyPath(package, normalizedRelative);
                    return true;
                }
                catch
                {
                }
            }

            List<KeyValuePair<string, string>> readTargets = BuildPckReadTargets(package, normalizedRelative);
            for (int i = 0; i < readTargets.Count; i++)
            {
                string probePackage = readTargets[i].Key ?? string.Empty;
                string probeRelative = readTargets[i].Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(probePackage) || string.IsNullOrWhiteSpace(probeRelative))
                {
                    continue;
                }

                if (pckEntryReaderService.TryReadFileFast(
                    probePackage,
                    probeRelative,
                    out bytes,
                    out string resolvedRelative,
                    out string _))
                {
                    CaptureDependencyPath(probePackage, resolvedRelative);
                    return true;
                }
            }

            bytes = null;
            return false;
        }

        private bool TryReadModelFileFromAnyPackage(
            AssetManager assetManager,
            List<KeyValuePair<string, string>> readTargets,
            out string resolvedPackage,
            out string resolvedRelativePath,
            out byte[] bytes,
            out string error)
        {
            resolvedPackage = string.Empty;
            resolvedRelativePath = string.Empty;
            bytes = null;
            error = string.Empty;

            if (readTargets == null || readTargets.Count == 0)
            {
                return false;
            }

            List<string> packages = GetAvailableResourcePackages();
            if (packages.Count == 0)
            {
                return false;
            }

            HashSet<string> tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < readTargets.Count; i++)
            {
                string relative = NormalizeRelativePath(readTargets[i].Value);
                if (string.IsNullOrWhiteSpace(relative))
                {
                    continue;
                }

                string cachedKey = BuildCrossPackageCacheKey(relative);
                if (IsKnownMissingCrossPackagePath(cachedKey))
                {
                    continue;
                }

                if (TryResolveCachedCrossPackagePath(cachedKey, out string cachedPackage, out string cachedRelative)
                    && !string.IsNullOrWhiteSpace(cachedPackage)
                    && !string.IsNullOrWhiteSpace(cachedRelative))
                {
                    string cachedTryKey = (cachedPackage + "|" + cachedRelative).ToLowerInvariant();
                    string cachedProbeError = string.Empty;
                    if (tried.Add(cachedTryKey)
                        && pckEntryReaderService.TryReadFile(cachedPackage, cachedRelative, out bytes, out cachedProbeError))
                    {
                        resolvedPackage = cachedPackage;
                        resolvedRelativePath = NormalizeRelativeForPackage(cachedPackage, cachedRelative);
                        CaptureDependencyPath(resolvedPackage, resolvedRelativePath);
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(cachedProbeError))
                    {
                        error = cachedProbeError;
                    }
                }

                for (int p = 0; p < packages.Count; p++)
                {
                    string package = packages[p];
                    if (string.IsNullOrWhiteSpace(package))
                    {
                        continue;
                    }

                    string key = (package + "|" + relative).ToLowerInvariant();
                    if (!tried.Add(key))
                    {
                        continue;
                    }

                    if (pckEntryReaderService.TryReadFile(package, relative, out bytes, out string probeError))
                    {
                        RememberCrossPackagePath(cachedKey, package, relative);
                        resolvedPackage = package;
                        resolvedRelativePath = NormalizeRelativeForPackage(package, relative);
                        CaptureDependencyPath(resolvedPackage, resolvedRelativePath);
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(error) && !string.IsNullOrWhiteSpace(probeError))
                    {
                        error = probeError;
                    }
                }

                RememberMissingCrossPackagePath(cachedKey);
            }

            return false;
        }

        private static void CaptureDependencyPath(string package, string relativePath)
        {
            if (capturedDependencyPaths == null)
            {
                return;
            }

            string normalizedPackage = (package ?? string.Empty).Trim();
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedPackage) || string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return;
            }

            capturedDependencyPaths.Add(normalizedPackage + "\\" + normalizedRelative);
        }

        private static List<string> GetAvailableResourcePackages()
        {
            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return new List<string>();
            }

            string resourcesRoot = Path.Combine(gameRoot, "resources");
            if (!Directory.Exists(resourcesRoot))
            {
                return new List<string>();
            }

            string resourcesSignature = resourcesRoot.Trim().ToLowerInvariant()
                + "|"
                + PckEntryReaderService.GetGlobalStateVersion().ToString();

            lock (ResourcePackageCacheSync)
            {
                if (string.Equals(cachedResourcePackagesRoot, resourcesRoot, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(cachedResourcePackagesSignature, resourcesSignature, StringComparison.Ordinal)
                    && cachedResourcePackages != null
                    && cachedResourcePackages.Count > 0)
                {
                    return new List<string>(cachedResourcePackages);
                }

                List<string> packages = new List<string>(16);
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    string[] pckFiles = Directory.GetFiles(resourcesRoot, "*.pck", SearchOption.TopDirectoryOnly);
                    for (int i = 0; i < pckFiles.Length; i++)
                    {
                        string packageName = Path.GetFileNameWithoutExtension(pckFiles[i]) ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(packageName) || !seen.Add(packageName))
                        {
                            continue;
                        }

                        packages.Add(packageName);
                    }
                }
                catch
                {
                }

                cachedResourcePackagesRoot = resourcesRoot;
                cachedResourcePackagesSignature = resourcesSignature;
                cachedResourcePackages = new List<string>(packages);
                crossPackagePathCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                crossPackageMissCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return packages;
            }
        }

        public static string GetResourcePackagesStateSignature()
        {
            string gameRoot = AssetManager.GameRootPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameRoot))
            {
                return string.Empty;
            }

            return gameRoot.Trim().ToLowerInvariant()
                + "|"
                + PckEntryReaderService.GetGlobalStateVersion().ToString();
        }

        private static string BuildCrossPackageCacheKey(string relativePath)
        {
            string normalized = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            string resourcesRoot = Path.Combine(AssetManager.GameRootPath ?? string.Empty, "resources");
            return (resourcesRoot + "|" + normalized).ToLowerInvariant();
        }

        private static bool TryResolveCachedCrossPackagePath(string cacheKey, out string package, out string relativePath)
        {
            package = string.Empty;
            relativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return false;
            }

            lock (ResourcePackageCacheSync)
            {
                if (!crossPackagePathCache.TryGetValue(cacheKey, out string cached) || string.IsNullOrWhiteSpace(cached))
                {
                    return false;
                }

                int sep = cached.IndexOf('|');
                if (sep <= 0 || sep >= cached.Length - 1)
                {
                    return false;
                }

                package = cached.Substring(0, sep);
                relativePath = cached.Substring(sep + 1);
                return !string.IsNullOrWhiteSpace(package) && !string.IsNullOrWhiteSpace(relativePath);
            }
        }

        private static bool IsKnownMissingCrossPackagePath(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return false;
            }

            lock (ResourcePackageCacheSync)
            {
                return crossPackageMissCache.Contains(cacheKey);
            }
        }

        private static void RememberCrossPackagePath(string cacheKey, string package, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return;
            }

            string normalizedPackage = (package ?? string.Empty).Trim();
            string normalizedRelative = NormalizeRelativeForPackage(package, relativePath);
            if (string.IsNullOrWhiteSpace(normalizedPackage) || string.IsNullOrWhiteSpace(normalizedRelative))
            {
                return;
            }

            lock (ResourcePackageCacheSync)
            {
                crossPackageMissCache.Remove(cacheKey);
                crossPackagePathCache[cacheKey] = normalizedPackage + "|" + normalizedRelative;
            }
        }

        private static void RememberMissingCrossPackagePath(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return;
            }

            lock (ResourcePackageCacheSync)
            {
                if (!crossPackagePathCache.ContainsKey(cacheKey))
                {
                    crossPackageMissCache.Add(cacheKey);
                }
            }
        }

        private static string NormalizeRelativeForPackage(string package, string relativePath)
        {
            string normalized = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return string.Empty;
            }

            string normalizedPackage = NormalizeRelativePath(package);
            if (!string.IsNullOrWhiteSpace(normalizedPackage))
            {
                string prefix = normalizedPackage + "\\";
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = NormalizeRelativePath(normalized.Substring(prefix.Length));
                }
            }

            return normalized;
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
            AddUniquePackage(packageCandidates, seenPackages, "models2");
            AddUniquePackage(packageCandidates, seenPackages, "gfx");
            AddUniquePackage(packageCandidates, seenPackages, "configs");
            AddUniquePackage(packageCandidates, seenPackages, "grasses");
            AddUniquePackage(packageCandidates, seenPackages, "litmodels");
            AddUniquePackage(packageCandidates, seenPackages, "moxing");
            AddUniquePackage(packageCandidates, seenPackages, "shaders");
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

            AddSpecialRelativeAliases(relativeCandidates, seenRelatives, normalizedRelative);
            AddSpecialRelativeAliases(relativeCandidates, seenRelatives, splitRelative);

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
                if (!rel.StartsWith("models2\\", StringComparison.OrdinalIgnoreCase))
                {
                    AddUniquePathCandidate(relativeCandidates, seenRelatives, "models2\\" + rel);
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
            PreviewSkeletonBone[] skeletonBones = new PreviewSkeletonBone[0];
            string[] animationPaths = BuildAnimationPathsFromSmd(assetManager, smdPackage, relativeSmd);
            string[] animationNames = BuildAnimationNames(animationPaths);
            PreviewAnimationClip[] animationClips = BuildAnimationClips(assetManager, smdPackage, relativeSmd, animationPaths, animationNames);
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
                    TryParseBonBindMatrices(bonBytes, out bindMatricesByBoneName, out bindMatricesBySkeletonBoneIndex, out skeletonBones);
                }
            }

            Vector3f[] vertices;
            Vector2f[] uvs;
            int[] indices;
            int[] triangleColors;
            int[] triangleTextureIndices;
            PreviewSkinVertexData[] skinVertices;
            int[] skinBoneSkeletonIndices;
            PreviewTextureData[] textures;
            bool hasSkinningData;
            int skinBoneCount;
            int skeletonBoneCount;
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
                out skinVertices,
                out skinBoneSkeletonIndices,
                out textures,
                out hasSkinningData,
                out skinBoneCount,
                out skeletonBoneCount,
                out error))
            {
                return false;
            }

            bool hasResolvedSkeleton = skeletonBones != null && skeletonBones.Length > 0;
            if (hasResolvedSkeleton)
            {
                skinVertices = RemapSkinVerticesToSkeletonIndices(skinVertices, skinBoneSkeletonIndices);
            }

            List<Vector3f> mergedVertices = new List<Vector3f>(vertices.Length + 2048);
            List<Vector2f> mergedUvs = new List<Vector2f>(uvs.Length + 2048);
            List<int> mergedIndices = new List<int>(indices.Length + 4096);
            List<int> mergedTriangleColors = new List<int>(triangleColors.Length + 4096);
            List<int> mergedTriangleTextureIndices = new List<int>(triangleTextureIndices.Length + 4096);
            List<PreviewSkinVertexData> mergedSkinVertices = new List<PreviewSkinVertexData>((skinVertices == null ? 0 : skinVertices.Length) + 2048);
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
            AppendSkinVertices(mergedSkinVertices, vertices, skinVertices);

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

                    addiRef = PreferLocalVariantAdditionalSki(ecmPackage, relativeEcm, addiRef);

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
                        out PreviewSkinVertexData[] addiSkinVertices,
                        out int[] addiSkinBoneSkeletonIndices,
                        out PreviewTextureData[] addiTextures,
                        out bool addiHasSkinningData,
                        out int addiSkinBoneCount,
                        out int addiSkeletonBoneCount,
                        out string _))
                    {
                        continue;
                    }


                    if (hasResolvedSkeleton)
                    {
                        addiSkinVertices = RemapSkinVerticesToSkeletonIndices(
                            addiSkinVertices,
                            addiSkinBoneSkeletonIndices);
                    }

                    if (IsSameMeshSurface(
                        vertices,
                        uvs,
                        indices,
                        triangleColors,
                        triangleTextureIndices,
                        textures,
                        addiVertices,
                        addiUvs,
                        addiIndices,
                        addiTriangleColors,
                        addiTriangleTextureIndices,
                        addiTextures))
                    {
                        if (HasMeaningfullyDifferentSkinning(skinVertices, addiSkinVertices))
                        {
                            ReplaceSkinVertices(mergedSkinVertices, 0, vertices.Length, addiSkinVertices);
                            hasSkinningData = hasSkinningData || addiHasSkinningData;
                            skinBoneCount = Math.Max(skinBoneCount, addiSkinBoneCount);
                            skeletonBoneCount = Math.Max(skeletonBoneCount, addiSkeletonBoneCount);
                        }
                        continue;
                    }

                    hasSkinningData = hasSkinningData || addiHasSkinningData;
                    skinBoneCount = Math.Max(skinBoneCount, addiSkinBoneCount);
                    skeletonBoneCount = Math.Max(skeletonBoneCount, addiSkeletonBoneCount);

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
                    AppendSkinVertices(mergedSkinVertices, addiVertices, addiSkinVertices);
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
                OuterNum = outerNum,
                HasSkinningData = hasSkinningData,
                SkinBoneCount = skinBoneCount,
                SkeletonBoneCount = skeletonBoneCount,
                AnimationNames = animationNames,
                AnimationPaths = animationPaths,
                AnimationClips = animationClips,
                SkinVertices = mergedSkinVertices.ToArray(),
                SkinBoneSkeletonIndices = hasResolvedSkeleton
                    ? BuildIdentityBoneIndices(skeletonBones.Length)
                    : (skinBoneSkeletonIndices ?? new int[0]),
                SkeletonBones = skeletonBones ?? new PreviewSkeletonBone[0]
            };

            return true;
        }

        private static PreviewSkinVertexData[] RemapSkinVerticesToSkeletonIndices(
            PreviewSkinVertexData[] skinVertices,
            int[] skinBoneSkeletonIndices)
        {
            PreviewSkinVertexData[] source = skinVertices ?? new PreviewSkinVertexData[0];
            if (source.Length == 0 || skinBoneSkeletonIndices == null || skinBoneSkeletonIndices.Length == 0)
            {
                return source;
            }

            PreviewSkinVertexData[] remapped = new PreviewSkinVertexData[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                PreviewSkinVertexData vertex = source[i];
                uint packed = vertex.PackedBoneIndices;
                uint mapped = 0u;
                for (int slot = 0; slot < 4; slot++)
                {
                    int skinBoneIndex = (int)((packed >> (slot * 8)) & 0xFFu);
                    int skeletonBoneIndex = skinBoneIndex >= 0 && skinBoneIndex < skinBoneSkeletonIndices.Length
                        ? skinBoneSkeletonIndices[skinBoneIndex]
                        : skinBoneIndex;
                    mapped |= ((uint)Math.Max(0, Math.Min(255, skeletonBoneIndex))) << (slot * 8);
                }

                remapped[i] = new PreviewSkinVertexData(
                    vertex.Weight0,
                    vertex.Weight1,
                    vertex.Weight2,
                    mapped);
            }
            return remapped;
        }

        private static int[] BuildIdentityBoneIndices(int count)
        {
            int safeCount = Math.Max(0, Math.Min(256, count));
            int[] indices = new int[safeCount];
            for (int i = 0; i < safeCount; i++)
            {
                indices[i] = i;
            }
            return indices;
        }

        private static bool IsSameMeshSurface(
            Vector3f[] firstVertices,
            Vector2f[] firstUvs,
            int[] firstIndices,
            int[] firstTriangleColors,
            int[] firstTriangleTextureIndices,
            PreviewTextureData[] firstTextures,
            Vector3f[] secondVertices,
            Vector2f[] secondUvs,
            int[] secondIndices,
            int[] secondTriangleColors,
            int[] secondTriangleTextureIndices,
            PreviewTextureData[] secondTextures)
        {
            if (firstVertices == null || secondVertices == null
                || firstVertices.Length != secondVertices.Length
                || !IntArraysEqual(firstIndices, secondIndices)
                || !IntArraysEqual(firstTriangleColors, secondTriangleColors)
                || !IntArraysEqual(firstTriangleTextureIndices, secondTriangleTextureIndices)
                || (firstTextures == null ? 0 : firstTextures.Length)
                    != (secondTextures == null ? 0 : secondTextures.Length))
            {
                return false;
            }

            Vector2f[] safeFirstUvs = firstUvs ?? new Vector2f[0];
            Vector2f[] safeSecondUvs = secondUvs ?? new Vector2f[0];
            if (safeFirstUvs.Length != safeSecondUvs.Length)
            {
                return false;
            }

            for (int i = 0; i < firstVertices.Length; i++)
            {
                Vector3f first = firstVertices[i];
                Vector3f second = secondVertices[i];
                if (first.X != second.X || first.Y != second.Y || first.Z != second.Z)
                {
                    return false;
                }
            }

            for (int i = 0; i < safeFirstUvs.Length; i++)
            {
                if (safeFirstUvs[i].X != safeSecondUvs[i].X
                    || safeFirstUvs[i].Y != safeSecondUvs[i].Y)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasMeaningfullyDifferentSkinning(
            PreviewSkinVertexData[] first,
            PreviewSkinVertexData[] second)
        {
            PreviewSkinVertexData[] safeFirst = first ?? new PreviewSkinVertexData[0];
            PreviewSkinVertexData[] safeSecond = second ?? new PreviewSkinVertexData[0];
            if (safeFirst.Length == 0 || safeFirst.Length != safeSecond.Length)
            {
                return false;
            }

            int differenceCount = 0;
            int requiredDifferences = Math.Max(8, safeFirst.Length / 100);
            for (int i = 0; i < safeFirst.Length; i++)
            {
                PreviewSkinVertexData firstVertex = safeFirst[i];
                PreviewSkinVertexData secondVertex = safeSecond[i];
                if (firstVertex.Weight0 == secondVertex.Weight0
                    && firstVertex.Weight1 == secondVertex.Weight1
                    && firstVertex.Weight2 == secondVertex.Weight2
                    && firstVertex.PackedBoneIndices == secondVertex.PackedBoneIndices)
                {
                    continue;
                }

                differenceCount++;
                if (differenceCount >= requiredDifferences)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ReplaceSkinVertices(
            List<PreviewSkinVertexData> mergedSkinVertices,
            int destinationStart,
            int vertexCount,
            PreviewSkinVertexData[] replacement)
        {
            if (mergedSkinVertices == null || vertexCount <= 0 || destinationStart < 0)
            {
                return;
            }

            PreviewSkinVertexData[] safeReplacement = replacement ?? new PreviewSkinVertexData[0];
            int availableDestination = Math.Max(0, mergedSkinVertices.Count - destinationStart);
            int replaceCount = Math.Min(vertexCount, Math.Min(safeReplacement.Length, availableDestination));
            for (int i = 0; i < replaceCount; i++)
            {
                mergedSkinVertices[destinationStart + i] = safeReplacement[i];
            }
        }

        private static bool IntArraysEqual(int[] first, int[] second)
        {
            int[] safeFirst = first ?? new int[0];
            int[] safeSecond = second ?? new int[0];
            if (safeFirst.Length != safeSecond.Length)
            {
                return false;
            }

            for (int i = 0; i < safeFirst.Length; i++)
            {
                if (safeFirst[i] != safeSecond[i])
                {
                    return false;
                }
            }

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

        private static void AppendSkinVertices(
            List<PreviewSkinVertexData> mergedSkinVertices,
            Vector3f[] vertices,
            PreviewSkinVertexData[] skinVertices)
        {
            if (mergedSkinVertices == null)
            {
                return;
            }

            Vector3f[] safeVertices = vertices ?? new Vector3f[0];
            PreviewSkinVertexData[] safeSkinVertices = skinVertices ?? new PreviewSkinVertexData[0];
            for (int i = 0; i < safeVertices.Length; i++)
            {
                mergedSkinVertices.Add(i < safeSkinVertices.Length
                    ? safeSkinVertices[i]
                    : new PreviewSkinVertexData(1f, 0f, 0f, 0u));
            }
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
                OuterNum = template.OuterNum,
                HasSkinningData = template.HasSkinningData || basePreview.HasSkinningData,
                SkinBoneCount = Math.Max(template.SkinBoneCount, basePreview.SkinBoneCount),
                SkeletonBoneCount = Math.Max(template.SkeletonBoneCount, basePreview.SkeletonBoneCount),
                AnimationNames = template.AnimationNames ?? basePreview.AnimationNames ?? new string[0],
                AnimationPaths = template.AnimationPaths ?? basePreview.AnimationPaths ?? new string[0],
                AnimationClips = template.AnimationClips ?? basePreview.AnimationClips ?? new PreviewAnimationClip[0],
                SkinVertices = template.SkinVertices ?? basePreview.SkinVertices ?? new PreviewSkinVertexData[0],
                SkinBoneSkeletonIndices = template.SkinBoneSkeletonIndices ?? basePreview.SkinBoneSkeletonIndices ?? new int[0],
                SkeletonBones = template.SkeletonBones ?? basePreview.SkeletonBones ?? new PreviewSkeletonBone[0]
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

            List<string> unresolvedCandidates = new List<string>(skiPathCandidates.Length);
            List<string> packageFallbacks = new List<string>(8);
            HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePackage(packageFallbacks, seenPackages, smdPackage);
            AddUniquePackage(packageFallbacks, seenPackages, "grasses");
            AddUniquePackage(packageFallbacks, seenPackages, "models");
            AddUniquePackage(packageFallbacks, seenPackages, "litmodels");
            AddUniquePackage(packageFallbacks, seenPackages, "moxing");
            AddUniquePackage(packageFallbacks, seenPackages, "shaders");
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

                unresolvedCandidates.Add(baseRelative);

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

            string nearestError = string.Empty;
            if (TryResolveNearestSkiByFileName(smdPackage, relativeSmd, unresolvedCandidates, out string nearestRelative)
                && TryReadModelFile(assetManager, smdPackage, nearestRelative, out skiBytes, out nearestError))
            {
                resolvedPackage = smdPackage;
                resolvedRelative = nearestRelative;
                return true;
            }
            if (string.IsNullOrWhiteSpace(lastProbeError) && !string.IsNullOrWhiteSpace(nearestError))
            {
                lastProbeError = nearestError;
            }

            error = string.IsNullOrWhiteSpace(lastProbeError)
                ? "Failed to resolve .ski path for preview."
                : lastProbeError;
            return false;
        }

        private void TryPreferLocalVariantSkinModel(
            string ecmPackage,
            string relativeEcm,
            string referencedPackage,
            string referencedRelative,
            out string resolvedPackage,
            out string resolvedRelative)
        {
            resolvedPackage = referencedPackage;
            resolvedRelative = referencedRelative;

            if (!string.Equals(ecmPackage, referencedPackage, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string ecmDirectory = NormalizeRelativePath(Path.GetDirectoryName(NormalizeRelativePath(relativeEcm)) ?? string.Empty);
            string referencedDirectory = NormalizeRelativePath(Path.GetDirectoryName(NormalizeRelativePath(referencedRelative)) ?? string.Empty);
            string variantName = Path.GetFileName(ecmDirectory) ?? string.Empty;
            string referencedName = Path.GetFileName(referencedDirectory) ?? string.Empty;
            string variantSuffix = ExtractTrailingModelNumberSuffix(variantName);
            if (string.IsNullOrWhiteSpace(ecmDirectory)
                || string.IsNullOrWhiteSpace(variantSuffix)
                || string.IsNullOrWhiteSpace(referencedName)
                || string.Equals(ecmDirectory, referencedDirectory, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    variantName.Substring(0, variantName.Length - variantSuffix.Length),
                    referencedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string[] extensions = new string[] { ".smd", ".ski" };
            for (int i = 0; i < extensions.Length; i++)
            {
                string candidate = NormalizeRelativePath(ecmDirectory + "\\" + variantName + extensions[i]);
                if (pckEntryReaderService.TryReadFileFast(ecmPackage, candidate, out byte[] _, out string localRelative, out string _))
                {
                    resolvedPackage = ecmPackage;
                    resolvedRelative = localRelative;
                    return;
                }
            }
        }

        private string PreferLocalVariantAdditionalSki(string ecmPackage, string relativeEcm, string additionalReference)
        {
            string normalizedReference = NormalizeRelativePath(additionalReference);
            string ecmDirectory = NormalizeRelativePath(Path.GetDirectoryName(NormalizeRelativePath(relativeEcm)) ?? string.Empty);
            string variantName = Path.GetFileName(ecmDirectory) ?? string.Empty;
            string variantSuffix = ExtractTrailingModelNumberSuffix(variantName);
            string fileName = Path.GetFileName(normalizedReference) ?? string.Empty;
            string stem = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            string extension = Path.GetExtension(fileName) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(ecmPackage)
                || string.IsNullOrWhiteSpace(ecmDirectory)
                || string.IsNullOrWhiteSpace(variantSuffix)
                || string.IsNullOrWhiteSpace(stem)
                || !string.Equals(extension, ".ski", StringComparison.OrdinalIgnoreCase))
            {
                return additionalReference;
            }

            List<string> candidates = new List<string>(3);
            candidates.Add(NormalizeRelativePath(ecmDirectory + "\\" + fileName));

            Match terminalLetters = Regex.Match(stem, @"^(.*?)([A-Za-z]+)$");
            if (terminalLetters.Success)
            {
                candidates.Add(NormalizeRelativePath(
                    ecmDirectory + "\\" + terminalLetters.Groups[1].Value + variantSuffix + terminalLetters.Groups[2].Value + extension));
            }

            string ecmStem = Path.GetFileNameWithoutExtension(relativeEcm) ?? string.Empty;
            if (terminalLetters.Success && !string.IsNullOrWhiteSpace(ecmStem))
            {
                candidates.Add(NormalizeRelativePath(
                    ecmDirectory + "\\" + ecmStem + terminalLetters.Groups[2].Value + extension));
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (pckEntryReaderService.TryReadFileFast(ecmPackage, candidates[i], out byte[] _, out string resolvedRelative, out string _))
                {
                    return resolvedRelative;
                }
            }

            return additionalReference;
        }

        private bool TryResolveNearestSkiByFileName(
            string package,
            string anchorRelativePath,
            IEnumerable<string> relativeCandidates,
            out string resolvedRelativePath)
        {
            resolvedRelativePath = string.Empty;

            string normalizedPackage = (package ?? string.Empty).Trim();
            string normalizedAnchor = NormalizeRelativePath(anchorRelativePath);
            if (string.IsNullOrWhiteSpace(normalizedPackage)
                || string.IsNullOrWhiteSpace(normalizedAnchor)
                || relativeCandidates == null)
            {
                return false;
            }

            List<string> fileNames = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidate in relativeCandidates)
            {
                string normalized = NormalizeRelativePath(candidate);
                if (string.IsNullOrWhiteSpace(normalized)
                    || !string.Equals(Path.GetExtension(normalized), ".ski", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string fileName = Path.GetFileName(normalized) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(fileName) || !seen.Add(fileName))
                {
                    continue;
                }

                fileNames.Add(fileName);
            }

            if (fileNames.Count == 0)
            {
                return false;
            }

            return pckEntryReaderService.TryResolveNearestEntryByFileNames(
                normalizedPackage,
                normalizedAnchor,
                fileNames,
                out resolvedRelativePath,
                out string _);
        }

        private bool TryReadDirectSkiWithFallback(
            AssetManager assetManager,
            string package,
            string relativeSki,
            out string resolvedPackage,
            out string resolvedRelativeSki,
            out byte[] skiBytes,
            out string error)
        {
            resolvedPackage = string.Empty;
            resolvedRelativeSki = string.Empty;
            skiBytes = null;
            error = string.Empty;

            if (TryReadModelFile(assetManager, package, relativeSki, out resolvedPackage, out resolvedRelativeSki, out skiBytes, out error))
            {
                return true;
            }

            string firstError = error;
            string[] aliases = BuildDirectSkiPathAliases(relativeSki);
            for (int i = 0; i < aliases.Length; i++)
            {
                if (TryReadModelFile(assetManager, package, aliases[i], out resolvedPackage, out resolvedRelativeSki, out skiBytes, out error))
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(firstError) && !string.IsNullOrWhiteSpace(error))
                {
                    firstError = error;
                }
            }

            if (TryResolveNearestSkiInSameDirectory(package, relativeSki, out string nearestRelative)
                && TryReadModelFile(assetManager, package, nearestRelative, out resolvedPackage, out resolvedRelativeSki, out skiBytes, out error))
            {
                return true;
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Entry not found in " + package + ".pck: " + relativeSki
                : firstError;
            return false;
        }

        private static string[] BuildDirectSkiPathAliases(string relativeSki)
        {
            List<string> aliases = new List<string>(4);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string normalized = NormalizeRelativePath(relativeSki);
            AddUniquePathCandidate(aliases, seen, normalized);

            if (!string.IsNullOrWhiteSpace(normalized)
                && normalized.IndexOf("\\护肩\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AddUniquePathCandidate(aliases, seen, normalized.Replace("盾", "护肩"));
            }

            return aliases.ToArray();
        }

        private bool TryResolveNearestSkiInSameDirectory(
            string package,
            string missingRelativeSki,
            out string resolvedRelativeSki)
        {
            resolvedRelativeSki = string.Empty;

            string normalizedPackage = (package ?? string.Empty).Trim();
            string normalizedMissing = NormalizeRelativePath(missingRelativeSki);
            if (string.IsNullOrWhiteSpace(normalizedPackage) || string.IsNullOrWhiteSpace(normalizedMissing))
            {
                return false;
            }

            string missingDirectory = NormalizeRelativePath(Path.GetDirectoryName(normalizedMissing) ?? string.Empty);
            string missingStem = Path.GetFileNameWithoutExtension(normalizedMissing) ?? string.Empty;
            string missingSuffix = ExtractTrailingModelNumberSuffix(missingStem);
            if (string.IsNullOrWhiteSpace(missingDirectory) || string.IsNullOrWhiteSpace(missingSuffix))
            {
                return false;
            }

            if (!pckEntryReaderService.TryEnumerateEntries(normalizedPackage, out List<string> entries, out string _)
                || entries == null)
            {
                return false;
            }

            string best = string.Empty;
            int bestScore = int.MaxValue;
            for (int i = 0; i < entries.Count; i++)
            {
                string candidate = NormalizeRelativePath(entries[i]);
                if (string.IsNullOrWhiteSpace(candidate)
                    || !string.Equals(Path.GetExtension(candidate), ".ski", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string candidateDirectory = NormalizeRelativePath(Path.GetDirectoryName(candidate) ?? string.Empty);
                if (!string.Equals(candidateDirectory, missingDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string candidateStem = Path.GetFileNameWithoutExtension(candidate) ?? string.Empty;
                if (!string.Equals(ExtractTrailingModelNumberSuffix(candidateStem), missingSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int score = ComputeDirectSkiFallbackScore(missingStem, candidateStem);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            resolvedRelativeSki = best;
            return !string.IsNullOrWhiteSpace(resolvedRelativeSki);
        }

        private static int ComputeDirectSkiFallbackScore(string missingStem, string candidateStem)
        {
            string normalizedMissing = NormalizeEquipmentShoulderStem(missingStem);
            string normalizedCandidate = NormalizeEquipmentShoulderStem(candidateStem);
            if (string.Equals(normalizedMissing, normalizedCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            int common = 0;
            int length = Math.Min(normalizedMissing.Length, normalizedCandidate.Length);
            while (common < length && normalizedMissing[common] == normalizedCandidate[common])
            {
                common++;
            }

            return Math.Abs(normalizedMissing.Length - normalizedCandidate.Length) + (length - common);
        }

        private static string NormalizeEquipmentShoulderStem(string value)
        {
            return (value ?? string.Empty)
                .Replace("盾", "护肩")
                .Replace("男", string.Empty)
                .Replace("女", string.Empty)
                .Trim();
        }

        private static string ExtractTrailingModelNumberSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            Match match = Regex.Match(value, @"(\d+(?:_\d+)*)$");
            return match.Success ? match.Groups[1].Value : string.Empty;
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
            List<PreviewSkinVertexData> mergedSkinVertices = new List<PreviewSkinVertexData>(4096);
            List<PreviewSkeletonBone> mergedSkeletonBones = new List<PreviewSkeletonBone>(64)
            {
                CreateStaticPreviewBone()
            };
            List<MergedAnimationClipBuilder> mergedAnimationClips = new List<MergedAnimationClipBuilder>();
            bool hasMergedSkinning = false;
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
                    hasMergedSkinning = AppendGfxSkinningSegment(
                        resolvedData,
                        mergedSkinVertices,
                        mergedSkeletonBones,
                        mergedAnimationClips) || hasMergedSkinning;
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
                PreviewAnimationClip[] animationClips = BuildMergedAnimationClips(mergedAnimationClips);
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
                    OuterNum = firstResolved.OuterNum,
                    HasSkinningData = hasMergedSkinning,
                    SkinBoneCount = hasMergedSkinning ? mergedSkeletonBones.Count : 0,
                    SkeletonBoneCount = hasMergedSkinning ? mergedSkeletonBones.Count : 0,
                    AnimationNames = animationClips.Select(clip => clip.Name ?? string.Empty).ToArray(),
                    AnimationPaths = animationClips.Select(clip => clip.Path ?? string.Empty).ToArray(),
                    AnimationClips = animationClips,
                    SkinVertices = hasMergedSkinning ? mergedSkinVertices.ToArray() : new PreviewSkinVertexData[0],
                    SkinBoneSkeletonIndices = hasMergedSkinning
                        ? BuildIdentityBoneIndices(mergedSkeletonBones.Count)
                        : new int[0],
                    SkeletonBones = hasMergedSkinning
                        ? mergedSkeletonBones.ToArray()
                        : new PreviewSkeletonBone[0]
                };
                error = string.Empty;
                return true;
            }

            error = string.IsNullOrWhiteSpace(firstError)
                ? "Failed to resolve a previewable model from .gfx file."
                : firstError;
            return false;
        }

        private sealed class MergedAnimationClipBuilder
        {
            public string Name = string.Empty;
            public string Path = string.Empty;
            public float DurationSeconds;
            public float FramesPerSecond;
            public readonly List<PreviewAnimationTrack> Tracks = new List<PreviewAnimationTrack>();
        }

        private static PreviewSkeletonBone CreateStaticPreviewBone()
        {
            return new PreviewSkeletonBone
            {
                Name = "GFX Static",
                ParentIndex = -1,
                RelativeMatrix = (float[])IdentitySkinMatrix.Clone(),
                BindMatrix = (float[])IdentitySkinMatrix.Clone()
            };
        }

        private static bool AppendGfxSkinningSegment(
            ModelPreviewMeshData segment,
            List<PreviewSkinVertexData> mergedSkinVertices,
            List<PreviewSkeletonBone> mergedSkeletonBones,
            List<MergedAnimationClipBuilder> mergedAnimationClips)
        {
            int vertexCount = segment == null || segment.Vertices == null ? 0 : segment.Vertices.Length;
            PreviewSkinVertexData staticSkin = new PreviewSkinVertexData(1f, 0f, 0f, 0u);
            if (vertexCount == 0)
            {
                return false;
            }

            PreviewSkinVertexData[] localSkin = segment.SkinVertices ?? new PreviewSkinVertexData[0];
            PreviewSkeletonBone[] localBones = segment.SkeletonBones ?? new PreviewSkeletonBone[0];
            if (!segment.HasSkinningData
                || localSkin.Length != vertexCount
                || localBones.Length == 0)
            {
                for (int i = 0; i < vertexCount; i++)
                {
                    mergedSkinVertices.Add(staticSkin);
                }
                return false;
            }

            int boneOffset = FindCompatibleSkeletonOffset(mergedSkeletonBones, localBones);
            bool reusedSkeleton = boneOffset >= 0;
            if (!reusedSkeleton)
            {
                if (mergedSkeletonBones.Count + localBones.Length > 256)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        mergedSkinVertices.Add(staticSkin);
                    }
                    return false;
                }

                boneOffset = mergedSkeletonBones.Count;
                for (int i = 0; i < localBones.Length; i++)
                {
                    PreviewSkeletonBone bone = localBones[i];
                    mergedSkeletonBones.Add(new PreviewSkeletonBone
                    {
                        Name = bone == null ? string.Empty : bone.Name ?? string.Empty,
                        ParentIndex = bone != null && bone.ParentIndex >= 0 ? bone.ParentIndex + boneOffset : -1,
                        IsFlipped = bone != null && bone.IsFlipped,
                        RelativeMatrix = bone == null || bone.RelativeMatrix == null
                            ? (float[])IdentitySkinMatrix.Clone()
                            : (float[])bone.RelativeMatrix.Clone(),
                        BindMatrix = bone == null || bone.BindMatrix == null
                            ? (float[])IdentitySkinMatrix.Clone()
                            : (float[])bone.BindMatrix.Clone()
                    });
                }
            }

            int[] localBoneMap = segment.SkinBoneSkeletonIndices ?? new int[0];
            for (int i = 0; i < localSkin.Length; i++)
            {
                PreviewSkinVertexData skin = localSkin[i];
                uint packed = 0u;
                for (int slot = 0; slot < 4; slot++)
                {
                    int localSkinBoneIndex = (int)((skin.PackedBoneIndices >> (slot * 8)) & 0xFFu);
                    int localSkeletonBoneIndex = localSkinBoneIndex < localBoneMap.Length
                        ? localBoneMap[localSkinBoneIndex]
                        : localSkinBoneIndex;
                    int mergedBoneIndex = localSkeletonBoneIndex >= 0 && localSkeletonBoneIndex < localBones.Length
                        ? boneOffset + localSkeletonBoneIndex
                        : 0;
                    packed |= ((uint)mergedBoneIndex & 0xFFu) << (slot * 8);
                }
                mergedSkinVertices.Add(new PreviewSkinVertexData(
                    skin.Weight0,
                    skin.Weight1,
                    skin.Weight2,
                    packed));
            }

            PreviewAnimationClip[] localClips = reusedSkeleton
                ? new PreviewAnimationClip[0]
                : segment.AnimationClips ?? new PreviewAnimationClip[0];
            for (int clipIndex = 0; clipIndex < localClips.Length; clipIndex++)
            {
                PreviewAnimationClip clip = localClips[clipIndex];
                if (clip == null || !clip.IsValid)
                {
                    continue;
                }

                while (mergedAnimationClips.Count <= clipIndex)
                {
                    mergedAnimationClips.Add(new MergedAnimationClipBuilder());
                }
                MergedAnimationClipBuilder builder = mergedAnimationClips[clipIndex];
                if (string.IsNullOrWhiteSpace(builder.Name)) builder.Name = clip.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(builder.Path)) builder.Path = clip.Path ?? string.Empty;
                builder.DurationSeconds = Math.Max(builder.DurationSeconds, clip.DurationSeconds);
                builder.FramesPerSecond = Math.Max(builder.FramesPerSecond, clip.FramesPerSecond);

                PreviewAnimationTrack[] tracks = clip.Tracks ?? new PreviewAnimationTrack[0];
                for (int trackIndex = 0; trackIndex < tracks.Length; trackIndex++)
                {
                    PreviewAnimationTrack track = tracks[trackIndex];
                    if (track == null || track.BoneIndex < 0 || track.BoneIndex >= localBones.Length)
                    {
                        continue;
                    }
                    builder.Tracks.Add(new PreviewAnimationTrack
                    {
                        BoneIndex = boneOffset + track.BoneIndex,
                        Positions = track.Positions ?? new Vector3f[0],
                        Rotations = track.Rotations ?? new PreviewQuaternion[0],
                        Scales = track.Scales ?? new Vector3f[0]
                    });
                }
            }
            return true;
        }

        private static int FindCompatibleSkeletonOffset(
            List<PreviewSkeletonBone> mergedBones,
            PreviewSkeletonBone[] localBones)
        {
            if (mergedBones == null || localBones == null || localBones.Length == 0 || mergedBones.Count < localBones.Length)
            {
                return -1;
            }

            int lastOffset = mergedBones.Count - localBones.Length;
            for (int offset = 0; offset <= lastOffset; offset++)
            {
                bool compatible = true;
                for (int i = 0; i < localBones.Length; i++)
                {
                    PreviewSkeletonBone local = localBones[i];
                    PreviewSkeletonBone merged = mergedBones[offset + i];
                    int expectedParent = local != null && local.ParentIndex >= 0
                        ? offset + local.ParentIndex
                        : -1;
                    if (local == null
                        || merged == null
                        || !string.Equals(local.Name ?? string.Empty, merged.Name ?? string.Empty, StringComparison.Ordinal)
                        || local.IsFlipped != merged.IsFlipped
                        || merged.ParentIndex != expectedParent
                        || !ArePreviewMatricesEquivalent(local.RelativeMatrix, merged.RelativeMatrix)
                        || !ArePreviewMatricesEquivalent(local.BindMatrix, merged.BindMatrix))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (compatible)
                {
                    return offset;
                }
            }
            return -1;
        }

        private static bool ArePreviewMatricesEquivalent(float[] left, float[] right)
        {
            if (left == null || right == null || left.Length < 16 || right.Length < 16)
            {
                return left == right;
            }
            for (int i = 0; i < 16; i++)
            {
                if (Math.Abs(left[i] - right[i]) > 0.00001f)
                {
                    return false;
                }
            }
            return true;
        }

        private static PreviewAnimationClip[] BuildMergedAnimationClips(List<MergedAnimationClipBuilder> builders)
        {
            if (builders == null || builders.Count == 0)
            {
                return new PreviewAnimationClip[0];
            }

            List<PreviewAnimationClip> clips = new List<PreviewAnimationClip>(builders.Count);
            for (int i = 0; i < builders.Count; i++)
            {
                MergedAnimationClipBuilder builder = builders[i];
                if (builder == null || builder.Tracks.Count == 0 || builder.DurationSeconds <= 0.0001f)
                {
                    continue;
                }
                clips.Add(new PreviewAnimationClip
                {
                    Name = string.IsNullOrWhiteSpace(builder.Name) ? "Animation " + (i + 1) : builder.Name,
                    Path = builder.Path ?? string.Empty,
                    DurationSeconds = builder.DurationSeconds,
                    FramesPerSecond = Math.Max(1f, builder.FramesPerSecond),
                    Tracks = builder.Tracks.ToArray()
                });
            }
            return clips.ToArray();
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

                        if (!TryReadModelFile(assetManager, probePackage, probeRelative, out string resolvedNestedPackage, out string resolvedNestedRelative, out byte[] gfxBytes, out string readError))
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
                        List<PreviewSkinVertexData> nestedMergedSkinVertices = new List<PreviewSkinVertexData>(4096);
                        List<PreviewSkeletonBone> nestedMergedSkeletonBones = new List<PreviewSkeletonBone>(64)
                        {
                            CreateStaticPreviewBone()
                        };
                        List<MergedAnimationClipBuilder> nestedMergedAnimationClips = new List<MergedAnimationClipBuilder>();
                        bool nestedHasMergedSkinning = false;
                        ModelPreviewMeshData firstNestedResolved = null;
                        for (int n = 0; n < nestedReferences.Count; n++)
                        {
                            if (TryBuildPreviewFromReferencedPath(
                                assetManager,
                                sourceMappedPath,
                                resolvedNestedPackage,
                                resolvedNestedRelative,
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
                                nestedHasMergedSkinning = AppendGfxSkinningSegment(
                                    nestedResolvedData,
                                    nestedMergedSkinVertices,
                                    nestedMergedSkeletonBones,
                                    nestedMergedAnimationClips) || nestedHasMergedSkinning;
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
                            PreviewAnimationClip[] nestedAnimationClips = BuildMergedAnimationClips(nestedMergedAnimationClips);
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
                                OuterNum = firstNestedResolved.OuterNum,
                                HasSkinningData = nestedHasMergedSkinning,
                                SkinBoneCount = nestedHasMergedSkinning ? nestedMergedSkeletonBones.Count : 0,
                                SkeletonBoneCount = nestedHasMergedSkinning ? nestedMergedSkeletonBones.Count : 0,
                                AnimationNames = nestedAnimationClips.Select(clip => clip.Name ?? string.Empty).ToArray(),
                                AnimationPaths = nestedAnimationClips.Select(clip => clip.Path ?? string.Empty).ToArray(),
                                AnimationClips = nestedAnimationClips,
                                SkinVertices = nestedHasMergedSkinning
                                    ? nestedMergedSkinVertices.ToArray()
                                    : new PreviewSkinVertexData[0],
                                SkinBoneSkeletonIndices = nestedHasMergedSkinning
                                    ? BuildIdentityBoneIndices(nestedMergedSkeletonBones.Count)
                                    : new int[0],
                                SkeletonBones = nestedHasMergedSkinning
                                    ? nestedMergedSkeletonBones.ToArray()
                                    : new PreviewSkeletonBone[0]
                            };
                            error = string.Empty;
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

                    if (TryReadModelFile(assetManager, probePackage, probeRelative, out string resolvedTextPackage, out string resolvedTextRelative, out byte[] nestedBytes, out string nestedReadError)
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
                        List<PreviewSkinVertexData> nestedMergedSkinVertices = new List<PreviewSkinVertexData>(4096);
                        List<PreviewSkeletonBone> nestedMergedSkeletonBones = new List<PreviewSkeletonBone>(64)
                        {
                            CreateStaticPreviewBone()
                        };
                        List<MergedAnimationClipBuilder> nestedMergedAnimationClips = new List<MergedAnimationClipBuilder>();
                        bool nestedHasMergedSkinning = false;
                        ModelPreviewMeshData firstNestedResolved = null;
                        for (int n = 0; n < nestedReferences.Count; n++)
                        {
                            if (TryBuildPreviewFromReferencedPath(
                                assetManager,
                                sourceMappedPath,
                                resolvedTextPackage,
                                resolvedTextRelative,
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
                                nestedHasMergedSkinning = AppendGfxSkinningSegment(
                                    nestedResolvedData,
                                    nestedMergedSkinVertices,
                                    nestedMergedSkeletonBones,
                                    nestedMergedAnimationClips) || nestedHasMergedSkinning;
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
                            PreviewAnimationClip[] nestedAnimationClips = BuildMergedAnimationClips(nestedMergedAnimationClips);
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
                                OuterNum = firstNestedResolved.OuterNum,
                                HasSkinningData = nestedHasMergedSkinning,
                                SkinBoneCount = nestedHasMergedSkinning ? nestedMergedSkeletonBones.Count : 0,
                                SkeletonBoneCount = nestedHasMergedSkinning ? nestedMergedSkeletonBones.Count : 0,
                                AnimationNames = nestedAnimationClips.Select(clip => clip.Name ?? string.Empty).ToArray(),
                                AnimationPaths = nestedAnimationClips.Select(clip => clip.Path ?? string.Empty).ToArray(),
                                AnimationClips = nestedAnimationClips,
                                SkinVertices = nestedHasMergedSkinning
                                    ? nestedMergedSkinVertices.ToArray()
                                    : new PreviewSkinVertexData[0],
                                SkinBoneSkeletonIndices = nestedHasMergedSkinning
                                    ? BuildIdentityBoneIndices(nestedMergedSkeletonBones.Count)
                                    : new int[0],
                                SkeletonBones = nestedHasMergedSkinning
                                    ? nestedMergedSkeletonBones.ToArray()
                                    : new PreviewSkeletonBone[0]
                            };
                            error = string.Empty;
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

            AddSpecialReferenceTargets(candidates, seen, package, normalizedRelative);
            if (!string.Equals(withoutConfigsPrefix, normalizedRelative, StringComparison.OrdinalIgnoreCase))
            {
                AddSpecialReferenceTargets(candidates, seen, package, withoutConfigsPrefix);
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

        private static void AddSpecialReferenceTargets(
            List<KeyValuePair<string, string>> candidates,
            HashSet<string> seen,
            string package,
            string relativePath)
        {
        }

        private static void AddSpecialRelativeAliases(
            List<string> candidates,
            HashSet<string> seen,
            string relativePath)
        {
        }

        private static string DecorateMissingDetachedReferenceError(string relativePath, string error)
        {
            string normalizedRelative = NormalizeRelativePath(relativePath);
            if (string.IsNullOrWhiteSpace(normalizedRelative) || string.IsNullOrWhiteSpace(error))
            {
                return error ?? string.Empty;
            }

            bool looksLikeDetachedReference = normalizedRelative.StartsWith("hide\\", StringComparison.OrdinalIgnoreCase);
            bool looksLikeNotFound = error.IndexOf("Entry not found", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!looksLikeDetachedReference || !looksLikeNotFound)
            {
                return error;
            }

            return "Missing hide reference: " + normalizedRelative + " (not found in any loaded resource package).";
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

        private static bool HasEffectDrivenVisualReferences(string ecmText)
        {
            if (string.IsNullOrWhiteSpace(ecmText))
            {
                return false;
            }

            if (ExtractRepeatedFieldValues(ecmText, "FxFilePath").Length > 0)
            {
                return true;
            }

            if (ExtractRepeatedFieldValues(ecmText, "GfxPath").Length > 0)
            {
                return true;
            }

            if (ExtractRepeatedFieldValues(ecmText, "GfxFile").Length > 0)
            {
                return true;
            }

            return false;
        }

        private static bool IsLikelyBlackPlaceholderPreview(ModelPreviewMeshData previewData)
        {
            if (previewData == null || previewData.Textures == null || previewData.Textures.Length == 0)
            {
                return false;
            }

            int validTextures = 0;
            int blackTextures = 0;
            for (int i = 0; i < previewData.Textures.Length; i++)
            {
                PreviewTextureData texture = previewData.Textures[i];
                if (texture == null || !texture.IsValid)
                {
                    continue;
                }

                validTextures++;
                if (IsLikelyBlackPlaceholderTexture(texture))
                {
                    blackTextures++;
                }
            }

            return validTextures > 0 && validTextures == blackTextures;
        }

        private static bool IsLikelyBlackPlaceholderTexture(PreviewTextureData texture)
        {
            if (texture == null || !texture.IsValid || texture.Pixels == null || texture.Pixels.Length == 0)
            {
                return false;
            }

            int[] pixels = texture.Pixels;
            int step = Math.Max(1, pixels.Length / 4096);
            int visibleSamples = 0;
            int darkSamples = 0;
            long totalLuma = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                int argb = pixels[i];
                int a = (argb >> 24) & 0xFF;
                if (a < 10)
                {
                    continue;
                }

                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;
                int luma = (r + g + b) / 3;
                visibleSamples++;
                totalLuma += luma;
                if (luma <= 6)
                {
                    darkSamples++;
                }
            }

            if (visibleSamples <= 0)
            {
                return false;
            }

            float darkRatio = darkSamples / (float)Math.Max(1, visibleSamples);
            float averageLuma = totalLuma / (float)Math.Max(1, visibleSamples);
            return darkRatio >= 0.985f && averageLuma <= 6.5f;
        }

        private static void PromoteBlackPlaceholderTexturesForPreview(ModelPreviewMeshData previewData)
        {
            if (previewData == null || previewData.Textures == null || previewData.Textures.Length == 0)
            {
                return;
            }

            int fallbackArgb = ResolveBlackPlaceholderFallbackColor(previewData.OrgColorArgb);
            for (int i = 0; i < previewData.Textures.Length; i++)
            {
                PreviewTextureData texture = previewData.Textures[i];
                if (texture == null || !IsLikelyBlackPlaceholderTexture(texture))
                {
                    continue;
                }

                int[] pixels = texture.Pixels;
                for (int p = 0; p < pixels.Length; p++)
                {
                    int a = (pixels[p] >> 24) & 0xFF;
                    if (a <= 0)
                    {
                        continue;
                    }

                    pixels[p] = (a << 24) | (fallbackArgb & 0x00FFFFFF);
                }

                texture.AverageColorArgb = ComputeAverageColorArgb(pixels);
            }
        }

        private static int ResolveBlackPlaceholderFallbackColor(int orgColorArgb)
        {
            int rgb = orgColorArgb & 0x00FFFFFF;
            if (rgb == 0 || rgb == 0x00FFFFFF)
            {
                return unchecked((int)0xFFC8CCD4);
            }

            return unchecked((int)0xFF000000) | rgb;
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

            string[] extensions = new string[] { ".ecm", ".smd", ".ski", ".gfx", ".att", ".sgc", ".stck", ".sdr" };
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

        private string[] BuildAnimationPathsFromSmd(AssetManager assetManager, string smdPackage, string relativeSmd)
        {
            if (assetManager == null || string.IsNullOrWhiteSpace(smdPackage) || string.IsNullOrWhiteSpace(relativeSmd))
            {
                return new string[0];
            }

            if (!TryReadModelFile(assetManager, smdPackage, relativeSmd, out byte[] smdBytes, out string _)
                || smdBytes == null
                || smdBytes.Length == 0)
            {
                return new string[0];
            }

            string[] refs = ExtractReferencedFilesFromSmd(smdBytes, ".stck");
            if (refs == null || refs.Length == 0)
            {
                return new string[0];
            }

            List<string> paths = new List<string>(refs.Length);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < refs.Length; i++)
            {
                string reference = refs[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(reference))
                {
                    continue;
                }

                reference = NormalizeRelativePath(reference);
                if (reference.Length == 0 || !seen.Add(reference))
                {
                    continue;
                }

                paths.Add(reference);
            }

            return paths.ToArray();
        }

        private static string[] BuildAnimationNames(string[] animationPaths)
        {
            if (animationPaths == null || animationPaths.Length == 0)
            {
                return new string[0];
            }

            string[] names = new string[animationPaths.Length];
            for (int i = 0; i < animationPaths.Length; i++)
            {
                string reference = animationPaths[i] ?? string.Empty;
                string label = GetLegacyAnimationLabel(reference);
                if (string.IsNullOrWhiteSpace(label))
                {
                    label = "Animation " + (i + 1).ToString(CultureInfo.InvariantCulture);
                }

                names[i] = label;
            }

            return names;
        }

        private static string GetLegacyAnimationLabel(string reference)
        {
            if (string.IsNullOrEmpty(reference))
            {
                return string.Empty;
            }

            int separator = Math.Max(reference.LastIndexOf('\\'), reference.LastIndexOf('/'));
            int start = separator >= 0 ? separator + 1 : 0;
            int end = reference.Length;
            int extension = reference.LastIndexOf('.');
            if (extension > start)
            {
                end = extension;
            }

            StringBuilder label = new StringBuilder(Math.Max(0, end - start));
            for (int i = start; i < end; i++)
            {
                char value = reference[i];
                if (!char.IsControl(value) && value != '\0')
                {
                    label.Append(value);
                }
            }

            return label.ToString().Trim();
        }

        private PreviewAnimationClip[] BuildAnimationClips(
            AssetManager assetManager,
            string smdPackage,
            string relativeSmd,
            string[] animationPaths,
            string[] animationNames)
        {
            if (assetManager == null || animationPaths == null || animationPaths.Length == 0)
            {
                return new PreviewAnimationClip[0];
            }

            List<PreviewAnimationClip> clips = new List<PreviewAnimationClip>(animationPaths.Length);
            for (int i = 0; i < animationPaths.Length; i++)
            {
                string reference = NormalizeRelativePath(animationPaths[i] ?? string.Empty);
                if (reference.Length == 0)
                {
                    continue;
                }
                if (!TryReadAnimationClipFile(
                    assetManager,
                    smdPackage,
                    relativeSmd,
                    reference,
                    out byte[] clipBytes))
                {
                    continue;
                }

                string name = animationNames != null && i < animationNames.Length ? animationNames[i] : string.Empty;
                if (TryParseStckAnimationClip(clipBytes, name, reference, out PreviewAnimationClip clip))
                {
                    clips.Add(clip);
                }
            }

            return clips.ToArray();
        }

        private bool TryReadAnimationClipFile(
            AssetManager assetManager,
            string smdPackage,
            string relativeSmd,
            string reference,
            out byte[] clipBytes)
        {
            clipBytes = null;
            ResolveReferencedPath(smdPackage, relativeSmd, reference, out string clipPackage, out string clipRelative);

            string smdDirectory;
            string smdName;
            try
            {
                string normalizedSmd = NormalizeRelativePath(relativeSmd);
                smdDirectory = Path.GetDirectoryName(normalizedSmd) ?? string.Empty;
                smdName = Path.GetFileNameWithoutExtension(normalizedSmd) ?? string.Empty;
            }
            catch
            {
                return false;
            }

            string normalizedReference = NormalizeRelativePath(reference);
            string fileName = Path.GetFileName(normalizedReference);
            string[] candidates =
            {
                NormalizeRelativePath(smdDirectory + "\\" + normalizedReference),
                NormalizeRelativePath(smdDirectory + "\\tcks_" + smdName + "\\" + fileName)
            };

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (string.IsNullOrWhiteSpace(candidate)
                    || !seen.Add(candidate)
                    || string.Equals(candidate, clipRelative, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryReadModelFileInPackage(assetManager, smdPackage, candidate, out clipBytes))
                {
                    return true;
                }
            }

            if (pckEntryReaderService.TryResolveNearestEntryByFileNames(
                    smdPackage,
                    relativeSmd,
                    new[] { fileName },
                    out string nearestClipRelative,
                    out string _)
                && !string.IsNullOrWhiteSpace(nearestClipRelative)
                && TryReadModelFileInPackage(assetManager, smdPackage, nearestClipRelative, out clipBytes))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(clipRelative)
                && TryReadModelFileInPackage(assetManager, clipPackage, clipRelative, out clipBytes))
            {
                return true;
            }

            clipBytes = null;
            return false;
        }

        private static bool TryParseStckAnimationClip(byte[] bytes, string name, string path, out PreviewAnimationClip clip)
        {
            clip = null;
            if (bytes == null || bytes.Length < 32)
            {
                return false;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    int magicOffset = FindAsciiBytes(bytes, "MOXBKCTS");
                    if (magicOffset < 0)
                    {
                        return false;
                    }
                    if (magicOffset > 0)
                    {
                        stream.Position = magicOffset;
                    }

                    string magic = Encoding.ASCII.GetString(br.ReadBytes(8));
                    if (!string.Equals(magic, "MOXBKCTS", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    int version = br.ReadInt32();
                    int trackCount = br.ReadInt32();
                    int startFrame = br.ReadInt32();
                    int endFrame = br.ReadInt32();
                    int fps = br.ReadInt32();
                    int frameCount = Math.Max(1, endFrame - startFrame + 1);
                    if (version <= 0 || version > 8 || trackCount <= 0 || trackCount > 4096 || frameCount > 300000)
                    {
                        return false;
                    }

                    List<PreviewAnimationTrack> tracks = new List<PreviewAnimationTrack>(trackCount);
                    for (int i = 0; i < trackCount && TryEnsureRemaining(br, 4); i++)
                    {
                        int boneIndex = br.ReadInt32();
                        if (TryParseStckTrack(br, version, frameCount, startFrame, fps, boneIndex, out PreviewAnimationTrack track))
                        {
                            tracks.Add(track);
                        }
                    }

                    if (tracks.Count == 0)
                    {
                        return false;
                    }

                    float safeFps = fps > 0 ? fps : 30f;
                    clip = new PreviewAnimationClip
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(path) : name,
                        Path = path ?? string.Empty,
                        DurationSeconds = Math.Max(0.033f, frameCount / safeFps),
                        FramesPerSecond = safeFps,
                        Tracks = tracks.ToArray()
                    };
                    return true;
                }
            }
            catch
            {
                clip = null;
                return false;
            }
        }

        private static int FindAsciiBytes(byte[] bytes, string value)
        {
            if (bytes == null || string.IsNullOrEmpty(value) || bytes.Length < value.Length)
            {
                return -1;
            }

            byte[] needle = Encoding.ASCII.GetBytes(value);
            for (int i = 0; i <= bytes.Length - needle.Length; i++)
            {
                bool matched = true;
                for (int n = 0; n < needle.Length; n++)
                {
                    if (bytes[i + n] != needle[n])
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryParseStckTrack(
            BinaryReader br,
            int version,
            int frameCount,
            int startFrame,
            int fps,
            int boneIndex,
            out PreviewAnimationTrack track)
        {
            track = null;
            if (!TryEnsureRemaining(br, 12))
            {
                return false;
            }

            int positionCount = br.ReadInt32();
            int positionSegmentCount = br.ReadInt32();
            br.ReadInt32();
            if (positionCount < 0 || positionCount > 300000 || positionSegmentCount < 0 || positionSegmentCount > 300000)
            {
                return false;
            }

            Vector3f[] positions = ReadVector3Array(br, positionCount);
            if (positions == null)
            {
                return false;
            }

            StckSegment[] positionSegments;
            if (version < 2)
            {
                positionSegments = ReadStckSegments(br, positionSegmentCount);
                if (positionSegments == null)
                {
                    return false;
                }
            }
            else
            {
                if (!TryEnsureRemaining(br, 8))
                {
                    return false;
                }

                int segmentEndTime = br.ReadInt32();
                int compressed = br.ReadInt32();
                if (compressed != 0)
                {
                    short[] keys = ReadShortArray(br, positionCount);
                    if (keys == null)
                    {
                        return false;
                    }
                    positions = ExpandVector3Frames(positions, keys, frameCount);
                }
                positionSegments = BuildSyntheticStckSegments(positions.Length, segmentEndTime);
            }

            positions = ExpandVector3Timeline(positions, positionSegments, frameCount, startFrame, fps);

            if (!TryEnsureRemaining(br, 12))
            {
                return false;
            }

            int rotationCount = br.ReadInt32();
            int rotationSegmentCount = br.ReadInt32();
            br.ReadInt32();
            if (rotationCount < 0 || rotationCount > 300000 || rotationSegmentCount < 0 || rotationSegmentCount > 300000)
            {
                return false;
            }

            PreviewQuaternion[] rotations;
            StckSegment[] rotationSegments;
            if (version < 2)
            {
                rotations = ReadQuaternionArray(br, rotationCount, false);
                rotationSegments = rotations == null ? null : ReadStckSegments(br, rotationSegmentCount);
                if (rotations == null || rotationSegments == null)
                {
                    return false;
                }
            }
            else
            {
                if (!TryEnsureRemaining(br, 4))
                {
                    return false;
                }

                bool compressedRotation = br.ReadInt32() != 0;
                rotations = ReadQuaternionArray(br, rotationCount, compressedRotation);
                if (rotations == null || !TryEnsureRemaining(br, 8))
                {
                    return false;
                }

                int segmentEndTime = br.ReadInt32();
                int compressed = br.ReadInt32();
                if (compressed != 0)
                {
                    short[] keys = ReadShortArray(br, rotationCount);
                    if (keys == null)
                    {
                        return false;
                    }
                    rotations = ExpandQuaternionFrames(rotations, keys, frameCount);
                }
                rotationSegments = BuildSyntheticStckSegments(rotations.Length, segmentEndTime);
            }

            rotations = ExpandQuaternionTimeline(rotations, rotationSegments, frameCount, startFrame, fps);

            track = new PreviewAnimationTrack
            {
                BoneIndex = boneIndex,
                Positions = positions ?? new Vector3f[0],
                Rotations = rotations ?? new PreviewQuaternion[0]
            };
            return true;
        }

        private struct StckSegment
        {
            public int StartTime;
            public int EndTime;
            public int StartIndex;
            public int EndIndex;
        }

        private static StckSegment[] ReadStckSegments(BinaryReader br, int count)
        {
            if (count <= 0)
            {
                return new StckSegment[0];
            }
            if (!TryEnsureRemaining(br, count * 16L))
            {
                return null;
            }

            StckSegment[] segments = new StckSegment[count];
            for (int i = 0; i < count; i++)
            {
                segments[i] = new StckSegment
                {
                    StartTime = br.ReadInt32(),
                    EndTime = br.ReadInt32(),
                    StartIndex = br.ReadInt32(),
                    EndIndex = br.ReadInt32()
                };
            }
            return segments;
        }

        private static StckSegment[] BuildSyntheticStckSegments(int valueCount, int endTime)
        {
            if (valueCount <= 0)
            {
                return new StckSegment[0];
            }
            return new[]
            {
                new StckSegment
                {
                    StartTime = 0,
                    EndTime = Math.Max(0, endTime),
                    StartIndex = 0,
                    EndIndex = valueCount - 1
                }
            };
        }

        private static Vector3f[] ExpandVector3Timeline(
            Vector3f[] values,
            StckSegment[] segments,
            int frameCount,
            int startFrame,
            int fps)
        {
            Vector3f[] source = values ?? new Vector3f[0];
            if (source.Length <= 1 || frameCount <= 1)
            {
                return source;
            }

            Vector3f[] frames = new Vector3f[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                int index = ResolveStckValueIndex(segments, source.Length, frame, startFrame, fps);
                frames[frame] = source[index];
            }
            return frames;
        }

        private static PreviewQuaternion[] ExpandQuaternionTimeline(
            PreviewQuaternion[] values,
            StckSegment[] segments,
            int frameCount,
            int startFrame,
            int fps)
        {
            PreviewQuaternion[] source = values ?? new PreviewQuaternion[0];
            if (source.Length <= 1 || frameCount <= 1)
            {
                return source;
            }

            PreviewQuaternion[] frames = new PreviewQuaternion[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                int index = ResolveStckValueIndex(segments, source.Length, frame, startFrame, fps);
                frames[frame] = source[index];
            }
            return frames;
        }

        private static int ResolveStckValueIndex(
            StckSegment[] segments,
            int valueCount,
            int frame,
            int startFrame,
            int fps)
        {
            if (valueCount <= 1)
            {
                return 0;
            }

            StckSegment[] safeSegments = segments ?? new StckSegment[0];
            if (safeSegments.Length == 0)
            {
                return Math.Min(frame, valueCount - 1);
            }

            float safeFps = fps > 0 ? fps : 30f;
            float time = ((startFrame + frame) / safeFps) * 1000f;
            if (time <= safeSegments[0].StartTime)
            {
                return 0;
            }

            StckSegment last = safeSegments[safeSegments.Length - 1];
            if (time >= last.EndTime)
            {
                return valueCount - 1;
            }

            for (int i = 0; i < safeSegments.Length; i++)
            {
                StckSegment segment = safeSegments[i];
                if (time < segment.StartTime || time > segment.EndTime)
                {
                    continue;
                }

                int index = segment.StartIndex;
                if (segment.StartIndex != segment.EndIndex)
                {
                    index += (int)(((time - segment.StartTime) * safeFps) / 1000f);
                }
                return Math.Max(0, Math.Min(valueCount - 1, index));
            }

            return Math.Min(frame, valueCount - 1);
        }

        private static Vector3f[] ReadVector3Array(BinaryReader br, int count)
        {
            if (count <= 0)
            {
                return new Vector3f[0];
            }
            if (!TryEnsureRemaining(br, count * 12L))
            {
                return null;
            }

            Vector3f[] values = new Vector3f[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = new Vector3f(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            return values;
        }

        private static PreviewQuaternion[] ReadQuaternionArray(BinaryReader br, int count, bool compressed)
        {
            if (count <= 0)
            {
                return new PreviewQuaternion[0];
            }

            long bytes = count * (compressed ? 12L : 16L);
            if (!TryEnsureRemaining(br, bytes))
            {
                return null;
            }

            PreviewQuaternion[] values = new PreviewQuaternion[count];
            for (int i = 0; i < count; i++)
            {
                float x = br.ReadSingle();
                float y = br.ReadSingle();
                float z = br.ReadSingle();
                float w = compressed ? ComputeQuaternionW(x, y, z) : br.ReadSingle();
                values[i] = new PreviewQuaternion(x, y, z, w);
            }
            return values;
        }

        private static float ComputeQuaternionW(float x, float y, float z)
        {
            float value = 1f - (x * x) - (y * y) - (z * z);
            return value > 0f ? (float)Math.Sqrt(value) : 0f;
        }

        private static short[] ReadShortArray(BinaryReader br, int count)
        {
            if (count <= 0)
            {
                return new short[0];
            }
            if (!TryEnsureRemaining(br, count * 2L))
            {
                return null;
            }

            short[] values = new short[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = br.ReadInt16();
            }
            return values;
        }

        private static Vector3f[] ExpandVector3Frames(Vector3f[] keys, short[] thresholds, int frameCount)
        {
            Vector3f[] frames = new Vector3f[Math.Max(1, frameCount)];
            int keyIndex = 0;
            int thresholdIndex = 0;
            for (int frame = 0; frame < frames.Length; frame++)
            {
                frames[frame] = keys.Length == 0 ? Vector3f.Zero : keys[Math.Min(keyIndex, keys.Length - 1)];
                if (thresholds.Length > 0
                    && thresholdIndex < thresholds.Length
                    && thresholds[thresholdIndex] == frame
                    && keyIndex < keys.Length - 1)
                {
                    keyIndex++;
                    thresholdIndex++;
                }
            }
            return frames;
        }

        private static PreviewQuaternion[] ExpandQuaternionFrames(PreviewQuaternion[] keys, short[] thresholds, int frameCount)
        {
            PreviewQuaternion[] frames = new PreviewQuaternion[Math.Max(1, frameCount)];
            int keyIndex = 0;
            int thresholdIndex = 0;
            for (int frame = 0; frame < frames.Length; frame++)
            {
                frames[frame] = keys.Length == 0 ? PreviewQuaternion.Identity : keys[Math.Min(keyIndex, keys.Length - 1)];
                if (thresholds.Length > 0
                    && thresholdIndex < thresholds.Length
                    && thresholds[thresholdIndex] == frame
                    && keyIndex < keys.Length - 1)
                {
                    keyIndex++;
                    thresholdIndex++;
                }
            }
            return frames;
        }

        private static string[] ExtractReferencedFilesFromSmd(byte[] smdBytes, string extension)
        {
            if (smdBytes == null || smdBytes.Length < 8 || string.IsNullOrWhiteSpace(extension))
            {
                return new string[0];
            }

            List<string> values = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Encoding gbk = Encoding.GetEncoding("GBK");
            string extLower = extension.ToLowerInvariant();

            for (int pos = 0; pos <= smdBytes.Length - 4; pos++)
            {
                int len = BitConverter.ToInt32(smdBytes, pos);
                if (len <= 0 || len > 512 || pos + 4 + len > smdBytes.Length)
                {
                    continue;
                }

                try
                {
                    string candidate = gbk.GetString(smdBytes, pos + 4, len).Trim('\0', ' ', '\t', '\r', '\n');
                    AddReferencedPathCandidate(values, seen, candidate, extLower);
                }
                catch
                {
                }
            }

            try
            {
                AddReferencedPathsFromText(values, seen, gbk.GetString(smdBytes), extension, extLower);
            }
            catch
            {
            }

            try
            {
                AddReferencedPathsFromText(values, seen, Encoding.Unicode.GetString(smdBytes), extension, extLower);
            }
            catch
            {
            }

            ExtractReferencedPathsByRawByteScan(values, seen, smdBytes, extension, extLower);
            return values.ToArray();
        }

        private static bool SkipBytes(BinaryReader br, long count)
        {
            if (count <= 0)
            {
                return true;
            }
            if (!TryEnsureRemaining(br, count))
            {
                return false;
            }

            br.BaseStream.Seek(count, SeekOrigin.Current);
            return true;
        }

        private static void AddReferencedPathsFromText(
            List<string> values,
            HashSet<string> seen,
            string text,
            string extension,
            string extensionLower)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            MatchCollection matches = Regex.Matches(text, @"[^\0\r\n\t]+\" + Regex.Escape(extension), RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                AddReferencedPathCandidate(values, seen, matches[i].Value, extensionLower);
            }
        }

        private static void ExtractReferencedPathsByRawByteScan(
            List<string> values,
            HashSet<string> seen,
            byte[] bytes,
            string extension,
            string extensionLower)
        {
            byte[] markerBytes = Encoding.ASCII.GetBytes(extensionLower);
            if (markerBytes.Length == 0)
            {
                return;
            }

            for (int i = 0; i <= bytes.Length - markerBytes.Length; i++)
            {
                bool match = true;
                for (int m = 0; m < markerBytes.Length; m++)
                {
                    byte lower = (byte)char.ToLowerInvariant((char)bytes[i + m]);
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
                    AddReferencedPathCandidate(values, seen, candidate, extensionLower);
                }
                catch
                {
                }
            }
        }

        private static void AddReferencedPathCandidate(
            List<string> values,
            HashSet<string> seen,
            string candidate,
            string extensionLower)
        {
            if (values == null || seen == null || string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            string normalized = NormalizeRelativePath(candidate);
            if (string.IsNullOrWhiteSpace(normalized)
                || !normalized.EndsWith(extensionLower, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (seen.Add(normalized))
            {
                values.Add(normalized);
            }
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

            StringBuilder cleaned = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (!char.IsControl(current) && current != '\0')
                {
                    cleaned.Append(current);
                }
            }

            string normalized = cleaned.ToString()
                .Replace('/', '\\')
                .Trim()
                .Trim('"')
                .Trim()
                .TrimStart('\\');

            if (normalized.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return string.Empty;
            }

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
            out PreviewSkinVertexData[] skinVertices,
            out int[] skinBoneSkeletonIndices,
            out PreviewTextureData[] textures,
            out bool hasSkinningData,
            out int skinBoneCount,
            out int skeletonBoneCount,
            out string error)
        {
            vertices = new Vector3f[0];
            uvs = new Vector2f[0];
            indices = new int[0];
            triangleColors = new int[0];
            triangleTextureIndices = new int[0];
            skinVertices = new PreviewSkinVertexData[0];
            skinBoneSkeletonIndices = new int[0];
            textures = new PreviewTextureData[0];
            hasSkinningData = false;
            skinBoneCount = 0;
            skeletonBoneCount = 0;
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

                    skinBoneCount = header.NumSkinBone;
                    skeletonBoneCount = header.NumSkeletonBone;

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
                    skinBoneSkeletonIndices = BuildSkinBoneSkeletonIndices(skiBoneNames, bindMatricesByBoneName, bindMatricesBySkeletonBoneIndex);

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
                    List<PreviewSkinVertexData> allSkinVertices = new List<PreviewSkinVertexData>(4096);
                    string parseError = string.Empty;
                    bool parsedHasSkinningData = false;

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

                        parsedHasSkinningData = parsedHasSkinningData
                            || header.NumSkinBone > 0
                            || header.NumSkeletonBone > 0;

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
                            float weight0 = br.ReadSingle();
                            float weight1 = br.ReadSingle();
                            float weight2 = br.ReadSingle();
                            uint packedBoneIndices = br.ReadUInt32();
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
                            allSkinVertices.Add(new PreviewSkinVertexData(weight0, weight1, weight2, packedBoneIndices));
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

                            allVertices.Add(new Vector3f(-sourcePoint.X, sourcePoint.Y, sourcePoint.Z));
                            allUvs.Add(new Vector2f(tu, tv));
                            allSkinVertices.Add(new PreviewSkinVertexData(1f, 0f, 0f, (uint)Math.Max(0, boneIndex)));
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
                    skinVertices = allSkinVertices.Count == allVertices.Count
                        ? allSkinVertices.ToArray()
                        : new PreviewSkinVertexData[0];
                    textures = BuildTextureArray(textureNames, textureDataByIndex);
                    hasSkinningData = parsedHasSkinningData;
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

            if (lower.Contains("lms\\mounts\\") || lower.Contains("\\mounts\\"))
            {
                // Legacy ground-mount atlases commonly use alpha as auxiliary/specular data.
                return TextureAlphaPolicy.PreferMountOpaque;
            }

            if (IsMountRelativeSkiPath(lower))
            {
                // Flying mounts mix real opacity masks with auxiliary alpha channels.
                // Classify each texture so cutout cards stay cut out while data-only alpha remains opaque.
                return TextureAlphaPolicy.PreferCutout;
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
            for (int pass = 0; pass < 2; pass++)
            {
                bool allowNearbyFallback = pass > 0;
                for (int i = 0; i < candidates.Length; i++)
                {
                    string texPackage;
                    string texRelative;
                    ResolveReferencedPath(skiPackage, relativeSkiPath, candidates[i], out texPackage, out texRelative);
                    if (string.IsNullOrWhiteSpace(texRelative))
                    {
                        continue;
                    }

                    if (TryReadTextureBytesWithPackageFallback(assetManager, texPackage, relativeSkiPath, texRelative, allowNearbyFallback, out byte[] bytes, out string resolvedTexturePath)
                        && TryDecodeTextureData(texRelative, bytes, alphaPolicy, out PreviewTextureData decoded))
                    {
                        decoded.Name = resolvedTexturePath;
                        textureData = decoded;
                        return true;
                    }
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
            string anchorRelativePath,
            string textureRelativePath,
            bool allowNearbyFallback,
            out byte[] bytes,
            out string resolvedPath)
        {
            bytes = null;
            resolvedPath = string.Empty;

            List<string> packageCandidates = new List<string>(8);
            HashSet<string> seenPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddUniquePackage(packageCandidates, seenPackages, preferredPackage);
            AddUniquePackage(packageCandidates, seenPackages, "models");
            AddUniquePackage(packageCandidates, seenPackages, "models2");
            AddUniquePackage(packageCandidates, seenPackages, "litmodels");
            AddUniquePackage(packageCandidates, seenPackages, "moxing");
            AddUniquePackage(packageCandidates, seenPackages, "shaders");
            AddUniquePackage(packageCandidates, seenPackages, "surfaces");
            AddUniquePackage(packageCandidates, seenPackages, "configs");
            AddUniquePackage(packageCandidates, seenPackages, string.Empty);

            string normalizedPreferredPackage = (preferredPackage ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(normalizedPreferredPackage)
                && pckEntryReaderService.TryReadFileFast(normalizedPreferredPackage, textureRelativePath, out bytes, out string preferredResolvedRelative, out string _))
            {
                resolvedPath = BuildMappedPath(normalizedPreferredPackage, preferredResolvedRelative);
                return true;
            }

            string[] nearbyTextureFileNames = allowNearbyFallback ? BuildTextureFileCandidates(textureRelativePath) : new string[0];
            if (allowNearbyFallback
                && !string.IsNullOrWhiteSpace(normalizedPreferredPackage)
                && !string.IsNullOrWhiteSpace(anchorRelativePath)
                && nearbyTextureFileNames.Length > 0
                && pckEntryReaderService.TryResolveNearestEntryByFileNames(normalizedPreferredPackage, anchorRelativePath, nearbyTextureFileNames, out string nearbyRelative, out string _)
                && pckEntryReaderService.TryReadFileFast(normalizedPreferredPackage, nearbyRelative, out bytes, out string resolvedNearbyRelative, out string _))
            {
                resolvedPath = BuildMappedPath(normalizedPreferredPackage, resolvedNearbyRelative);
                return true;
            }

            for (int i = 0; i < packageCandidates.Count; i++)
            {
                string pkg = packageCandidates[i];
                if (string.IsNullOrWhiteSpace(pkg)
                    || string.Equals(pkg, normalizedPreferredPackage, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (pckEntryReaderService.TryReadFileFast(pkg, textureRelativePath, out bytes, out string packageResolvedRelative, out string _))
                {
                    resolvedPath = BuildMappedPath(pkg, packageResolvedRelative);
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

                AddUniquePathCandidate(candidates, seen, candidate);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    AddUniquePathCandidate(candidates, seen, "textures\\" + fileName);
                    AddUniquePathCandidate(candidates, seen, "texture\\" + fileName);
                    if (!string.IsNullOrWhiteSpace(skiNameNoExt))
                    {
                        AddUniquePathCandidate(candidates, seen, "tex_" + skiNameNoExt + "\\" + fileName);
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
                string withoutExt = normalized.Substring(0, normalized.Length - ext.Length);
                List<string> candidates = new List<string>(6);
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                AddUniquePathCandidate(candidates, seen, normalized);

                string[] alternateExtensions = new string[]
                {
                    ".dds",
                    ".tga",
                    ".bmp",
                    ".png",
                    ".jpg",
                    ".jpeg"
                };

                for (int i = 0; i < alternateExtensions.Length; i++)
                {
                    AddUniquePathCandidate(candidates, seen, withoutExt + alternateExtensions[i]);
                }

                return candidates.ToArray();
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
                            // PWModelViewer renders SKI/ECM mount textures with noAlpha=0 and blending
                            // disabled. Preserve the DDS RGB exactly, including RGB below zero alpha.
                            ForceOpaqueAlphaInPlace(pixels);
                            alphaUsage = TextureAlphaUsage.Opaque;
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
                return CopyTextureBitmapPreservingArgb(source);
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

        private static Bitmap CopyTextureBitmapPreservingArgb(Bitmap source)
        {
            int width = Math.Max(1, source.Width);
            int height = Math.Max(1, source.Height);
            Rectangle rectangle = new Rectangle(0, 0, width, height);
            Bitmap copy = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData sourceData = null;
            BitmapData destinationData = null;
            try
            {
                sourceData = source.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                destinationData = copy.LockBits(rectangle, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                int rowBytes = width * 4;
                byte[] row = new byte[rowBytes];
                for (int y = 0; y < height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(sourceData.Scan0, y * sourceData.Stride);
                    IntPtr destinationRow = IntPtr.Add(destinationData.Scan0, y * destinationData.Stride);
                    Marshal.Copy(sourceRow, row, 0, rowBytes);
                    Marshal.Copy(row, 0, destinationRow, rowBytes);
                }

                return copy;
            }
            catch
            {
                if (destinationData != null)
                {
                    try { copy.UnlockBits(destinationData); } catch { }
                    destinationData = null;
                }
                if (sourceData != null)
                {
                    try { source.UnlockBits(sourceData); } catch { }
                    sourceData = null;
                }
                copy.Dispose();
                Bitmap fallback = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics graphics = Graphics.FromImage(fallback))
                {
                    graphics.DrawImageUnscaled(source, 0, 0);
                }
                return fallback;
            }
            finally
            {
                if (destinationData != null)
                {
                    copy.UnlockBits(destinationData);
                }
                if (sourceData != null)
                {
                    source.UnlockBits(sourceData);
                }
            }
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
                return TextureAlphaUsage.Opaque;
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
            out float[][] bindMatricesBySkeletonBoneIndex,
            out PreviewSkeletonBone[] skeletonBones)
        {
            bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
            bindMatricesBySkeletonBoneIndex = new float[0][];
            skeletonBones = new PreviewSkeletonBone[0];
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
                    // magic[8], dwVersion, iNumBone, iNumJoint, iNumHook, iAnimStart, iAnimEnd, iAnimFPS, reserved[64]
                    if (!TryEnsureRemaining(br, 8 + (7 * 4) + 64))
                    {
                        return false;
                    }

                    string magic = Encoding.ASCII.GetString(br.ReadBytes(8));
                    uint version = br.ReadUInt32();
                    int numBones = br.ReadInt32();
                    br.ReadInt32(); // num joints
                    br.ReadInt32(); // num hooks
                    br.ReadInt32(); // anim start
                    br.ReadInt32(); // anim end
                    br.ReadInt32(); // anim fps
                    br.ReadBytes(64); // reserved

                    if (!string.Equals(magic, "MOXBEKSA", StringComparison.Ordinal)
                        || version == 0u
                        || version > 6u
                        || numBones <= 0
                        || numBones > 4096)
                    {
                        return false;
                    }

                    List<float[]> orderedMatrices = new List<float[]>(numBones);
                    List<PreviewSkeletonBone> orderedBones = new List<PreviewSkeletonBone>(numBones);
                    List<int[]> orderedChildIndices = new List<int[]>(numBones);
                    for (int i = 0; i < numBones; i++)
                    {
                        if (!TryReadLengthPrefixedString(br, gbk, 8192, out string boneName))
                        {
                            return false;
                        }

                        // A3DBone::BONEDATA (pack(1))
                        if (!TryEnsureRemaining(br, (4 * 4) + (16 * 4) + (16 * 4)))
                        {
                            return false;
                        }

                        int flags = br.ReadInt32();
                        int parentIndex = br.ReadInt32(); // iParent
                        br.ReadInt32();     // iFirstJoint
                        int childCount = br.ReadInt32(); // iNumChild
                        if (childCount < 0 || childCount > 2048)
                        {
                            return false;
                        }

                        float[] relativeMatrix = ReadBonMatrix(br); // matRelative
                        float[] bindMatrix = ReadBonMatrix(br); // matBoneInit

                        if (!string.IsNullOrWhiteSpace(boneName) && !bindMatricesByBoneName.ContainsKey(boneName))
                        {
                            bindMatricesByBoneName[boneName] = bindMatrix;
                        }

                        orderedMatrices.Add(bindMatrix);
                        orderedBones.Add(new PreviewSkeletonBone
                        {
                            Name = boneName ?? string.Empty,
                            ParentIndex = parentIndex,
                            IsFlipped = (flags & 2) != 0,
                            RelativeMatrix = relativeMatrix,
                            BindMatrix = bindMatrix
                        });

                        if (!TryEnsureRemaining(br, (long)childCount * 4L))
                        {
                            return false;
                        }

                        int[] childIndices = new int[childCount];
                        for (int child = 0; child < childCount; child++)
                        {
                            childIndices[child] = br.ReadInt32();
                        }
                        orderedChildIndices.Add(childIndices);
                    }

                    // The legacy viewer builds the runtime hierarchy from each bone's
                    // explicit child list. Keep iParent as a fallback for malformed BONs.
                    for (int parent = 0; parent < orderedChildIndices.Count; parent++)
                    {
                        int[] childIndices = orderedChildIndices[parent];
                        for (int child = 0; child < childIndices.Length; child++)
                        {
                            int childIndex = childIndices[child];
                            if (childIndex >= 0 && childIndex < orderedBones.Count && childIndex != parent)
                            {
                                orderedBones[childIndex].ParentIndex = parent;
                            }
                        }
                    }

                    bindMatricesBySkeletonBoneIndex = orderedMatrices.ToArray();
                    skeletonBones = orderedBones.ToArray();
                    return bindMatricesByBoneName.Count > 0 || bindMatricesBySkeletonBoneIndex.Length > 0;
                }
            }
            catch
            {
                bindMatricesByBoneName = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
                bindMatricesBySkeletonBoneIndex = new float[0][];
                skeletonBones = new PreviewSkeletonBone[0];
                return false;
            }
        }

        private static float[] ReadBonMatrix(BinaryReader br)
        {
            float[] raw = new float[16];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = br.ReadSingle();
            }

            float[] matrix = new float[16];
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    matrix[(column * 4) + row] = raw[(row * 4) + column];
                }
            }
            return matrix;
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

        private static int[] BuildSkinBoneSkeletonIndices(
            string[] skiBoneNames,
            Dictionary<string, float[]> bindMatricesByBoneName,
            float[][] bindMatricesBySkeletonBoneIndex)
        {
            if (skiBoneNames == null || skiBoneNames.Length == 0)
            {
                return new int[0];
            }

            int[] indices = new int[skiBoneNames.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
                string boneName = skiBoneNames[i] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(boneName)
                    || bindMatricesByBoneName == null
                    || bindMatricesBySkeletonBoneIndex == null
                    || !bindMatricesByBoneName.TryGetValue(boneName, out float[] matrix)
                    || matrix == null)
                {
                    continue;
                }

                for (int m = 0; m < bindMatricesBySkeletonBoneIndex.Length; m++)
                {
                    if (object.ReferenceEquals(matrix, bindMatricesBySkeletonBoneIndex[m]))
                    {
                        indices[i] = m;
                        break;
                    }
                }
            }

            return indices;
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
                (x * matrix[0]) + (y * matrix[1]) + (z * matrix[2]) + matrix[3],
                (x * matrix[4]) + (y * matrix[5]) + (z * matrix[6]) + matrix[7],
                (x * matrix[8]) + (y * matrix[9]) + (z * matrix[10]) + matrix[11]);
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
