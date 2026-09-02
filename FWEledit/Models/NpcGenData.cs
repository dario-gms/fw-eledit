using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcGenData
    {
        public string FilePath { get; set; }
        public int Version { get; set; }
        public int ResourceAreaCount { get; set; }
        public int DynamicObjectCount { get; set; }
        public int ControllerCount { get; set; }
        public int FlyAreaCount { get; set; }
        public int MineralCubeCount { get; set; }
        public int MineralSphereCount { get; set; }
        public byte[] ExportInfoBytes { get; set; }
        public uint CopyCount { get; set; }
        public uint FlyCopyCount { get; set; }
        public List<NpcGenArea> Areas { get; private set; }
        public List<NpcGenResourceArea> ResourceAreas { get; private set; }
        public List<NpcGenDynamicObject> DynamicObjects { get; private set; }
        public List<NpcGenController> Controllers { get; private set; }
        public List<NpcGenFlyArea> FlyAreas { get; private set; }
        public byte[] TailBytes { get; set; }

        public NpcGenData()
        {
            Areas = new List<NpcGenArea>();
            ResourceAreas = new List<NpcGenResourceArea>();
            DynamicObjects = new List<NpcGenDynamicObject>();
            Controllers = new List<NpcGenController>();
            FlyAreas = new List<NpcGenFlyArea>();
            ExportInfoBytes = new byte[0];
            TailBytes = new byte[0];
        }
    }

    public sealed class NpcGenArea
    {
        public int AreaType { get; set; }
        public PointF3 Position { get; set; }
        public PointF3 Direction { get; set; }
        public PointF3 Extents { get; set; }
        public int NpcType { get; set; }
        public int GroupType { get; set; }
        public byte InitGen { get; set; }
        public byte AutoRevive { get; set; }
        public byte ValidOnce { get; set; }
        public int GenId { get; set; }
        public int ControllerId { get; set; }
        public int LifeTime { get; set; }
        public int MaxNum { get; set; }
        public int ExportId { get; set; }
        public int RawAttachCount { get; set; }
        public int BufferRegionId { get; set; }
        public List<NpcGenEntry> Entries { get; private set; }
        public List<int> AttachIds { get; private set; }

        public NpcGenArea()
        {
            Entries = new List<NpcGenEntry>();
            AttachIds = new List<int>();
        }
    }

    public sealed class NpcGenEntry
    {
        public int Id { get; set; }
        public int Num { get; set; }
        public int Refresh { get; set; }
        public int DiedTimes { get; set; }
        public int Aggressive { get; set; }
        public float OffsetWater { get; set; }
        public float OffsetTerrain { get; set; }
        public int Faction { get; set; }
        public int FactionHelper { get; set; }
        public int FactionAccept { get; set; }
        public byte NeedHelp { get; set; }
        public byte DefaultFaction { get; set; }
        public byte DefaultFactionHelper { get; set; }
        public byte DefaultFactionAccept { get; set; }
        public int PathId { get; set; }
        public int LoopType { get; set; }
        public int SpeedFlag { get; set; }
        public int DeadTime { get; set; }
    }

    public sealed class NpcGenController
    {
        public int Id { get; set; }
        public int ControllerId { get; set; }
        public string Name { get; set; }
        public byte Active { get; set; }
        public int WaitTime { get; set; }
        public int StopTime { get; set; }
        public byte ActiveTimeInvalid { get; set; }
        public byte StopTimeInvalid { get; set; }
        public NpcGenTime Time1 { get; set; }
        public NpcGenTime Time2 { get; set; }
        public int ActiveTimeRange { get; set; }
        public byte RepeatActive { get; set; }
        public long ZoneMask { get; set; }
    }

    public sealed class NpcGenTime
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
    }

    public sealed class NpcGenResourceArea
    {
        public PointF3 Position { get; set; }
        public float ExtX { get; set; }
        public float ExtZ { get; set; }
        public byte InitGen { get; set; }
        public byte AutoRevive { get; set; }
        public byte ValidOnce { get; set; }
        public int GenId { get; set; }
        public byte[] DirectionBytes { get; set; }
        public byte RadiusByte { get; set; }
        public int ControllerId { get; set; }
        public int MaxNum { get; set; }
        public int ExportId { get; set; }
        public int RawAttachCount { get; set; }
        public int Type { get; set; }
        public float ExtY { get; set; }
        public float Radius { get; set; }
        public List<NpcGenResourceEntry> Entries { get; private set; }
        public List<int> AttachIds { get; private set; }

        public NpcGenResourceArea()
        {
            DirectionBytes = new byte[0];
            Entries = new List<NpcGenResourceEntry>();
            AttachIds = new List<int>();
        }
    }

    public sealed class NpcGenResourceEntry
    {
        public int ResourceType { get; set; }
        public int TemplateId { get; set; }
        public int RefreshTime { get; set; }
        public int Number { get; set; }
        public float HeightOffset { get; set; }
    }

    public sealed class NpcGenDynamicObject
    {
        public int DynamicObjectId { get; set; }
        public PointF3 Position { get; set; }
        public byte[] DirectionBytes { get; set; }
        public byte RadiusByte { get; set; }
        public byte Scale { get; set; }
        public uint ControllerId { get; set; }
        public byte Broadcast { get; set; }
        public uint ExportId { get; set; }

        public NpcGenDynamicObject()
        {
            DirectionBytes = new byte[0];
        }
    }

    public sealed class NpcGenFlyArea
    {
        public List<PointF> Points { get; private set; }
        public float Height { get; set; }
        public int RequiredLevel { get; set; }

        public NpcGenFlyArea()
        {
            Points = new List<PointF>();
        }
    }

    public struct PointF3
    {
        public float X;
        public float Y;
        public float Z;

        public PointF3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
