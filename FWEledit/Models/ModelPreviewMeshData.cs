namespace FWEledit
{
    public struct Vector2f
    {
        public float X;
        public float Y;

        public Vector2f(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public sealed class PreviewTextureData
    {
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public int[] Pixels { get; set; } = new int[0];
        public int AverageColorArgb { get; set; } = unchecked((int)0xFFFFFFFF);
        public bool UsesAlphaAsOpacity { get; set; }
        public bool HasTransparency { get; set; }

        public bool IsValid
        {
            get
            {
                return Width > 0
                    && Height > 0
                    && Pixels != null
                    && Pixels.Length == (Width * Height);
            }
        }
    }

    public sealed class ModelPreviewMeshData
    {
        public string SourceMappedPath { get; set; } = string.Empty;
        public string EcmPath { get; set; } = string.Empty;
        public string EcmAbsolutePath { get; set; } = string.Empty;
        public string SkinModelPath { get; set; } = string.Empty;
        public string SkiPath { get; set; } = string.Empty;
        public string SkiAbsolutePath { get; set; } = string.Empty;
        public Vector3f[] Vertices { get; set; } = new Vector3f[0];
        public Vector2f[] UVs { get; set; } = new Vector2f[0];
        public int[] Indices { get; set; } = new int[0];
        public int[] TriangleColors { get; set; } = new int[0];
        public int[] TriangleTextureIndices { get; set; } = new int[0];
        public PreviewTextureData[] Textures { get; set; } = new PreviewTextureData[0];
        public int SrcBlend { get; set; } = 5;
        public int DestBlend { get; set; } = 6;
        public int EmissiveColorArgb { get; set; } = 0;
        public int OrgColorArgb { get; set; } = unchecked((int)0xFFFFFFFF);
        public float[] ShaderFloats { get; set; } = new float[0];
        public int OuterNum { get; set; }
        public bool HasSkinningData { get; set; }
        public int SkinBoneCount { get; set; }
        public int SkeletonBoneCount { get; set; }
        public string[] AnimationNames { get; set; } = new string[0];
        public string[] AnimationPaths { get; set; } = new string[0];
        public PreviewAnimationClip[] AnimationClips { get; set; } = new PreviewAnimationClip[0];
        public PreviewSkinVertexData[] SkinVertices { get; set; } = new PreviewSkinVertexData[0];
        public int[] SkinBoneSkeletonIndices { get; set; } = new int[0];
        public PreviewSkeletonBone[] SkeletonBones { get; set; } = new PreviewSkeletonBone[0];
        public int VertexCount
        {
            get { return Vertices == null ? 0 : Vertices.Length; }
        }

        public int TriangleCount
        {
            get { return Indices == null ? 0 : (Indices.Length / 3); }
        }

        public bool HasTriangleColors
        {
            get { return TriangleColors != null && TriangleColors.Length == TriangleCount; }
        }

        public bool HasUvData
        {
            get { return UVs != null && UVs.Length == VertexCount; }
        }

        public bool HasTriangleTextureIndices
        {
            get { return TriangleTextureIndices != null && TriangleTextureIndices.Length == TriangleCount; }
        }

        public bool HasAnimations
        {
            get { return AnimationNames != null && AnimationNames.Length > 0; }
        }
    }

    public struct PreviewSkinVertexData
    {
        public float Weight0;
        public float Weight1;
        public float Weight2;
        public uint PackedBoneIndices;

        public PreviewSkinVertexData(float weight0, float weight1, float weight2, uint packedBoneIndices)
        {
            Weight0 = weight0;
            Weight1 = weight1;
            Weight2 = weight2;
            PackedBoneIndices = packedBoneIndices;
        }
    }

    public sealed class PreviewSkeletonBone
    {
        public string Name { get; set; } = string.Empty;
        public int ParentIndex { get; set; } = -1;
        public bool IsFlipped { get; set; }
        public float[] RelativeMatrix { get; set; } = new float[0];
        public float[] BindMatrix { get; set; } = new float[0];
    }

    public sealed class PreviewAnimationClip
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public float DurationSeconds { get; set; }
        public float FramesPerSecond { get; set; }
        public PreviewAnimationTrack[] Tracks { get; set; } = new PreviewAnimationTrack[0];

        public bool IsValid
        {
            get { return Tracks != null && Tracks.Length > 0 && DurationSeconds > 0.0001f; }
        }
    }

    public sealed class PreviewAnimationTrack
    {
        public int BoneIndex { get; set; }
        public Vector3f[] Positions { get; set; } = new Vector3f[0];
        public PreviewQuaternion[] Rotations { get; set; } = new PreviewQuaternion[0];
        public Vector3f[] Scales { get; set; } = new Vector3f[0];
    }

    public struct PreviewQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public PreviewQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static PreviewQuaternion Identity
        {
            get { return new PreviewQuaternion(0f, 0f, 0f, 1f); }
        }
    }
}
