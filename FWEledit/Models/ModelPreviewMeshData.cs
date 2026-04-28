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
    }
}
