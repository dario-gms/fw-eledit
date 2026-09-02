using System;
using System.IO;
using System.Text;

namespace FWEledit
{
    public sealed class NpcGenFileService
    {
        private const int ExportInfoByteLength = 132;
        private readonly Encoding textEncoding = Encoding.GetEncoding(936);

        public NpcGenData Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("npcgen.data was not found.", filePath);
            }

            using (FileStream stream = File.OpenRead(filePath))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                NpcGenData data = new NpcGenData();
                data.FilePath = filePath;
                data.Version = reader.ReadInt32();
                int areaCount = reader.ReadInt32();
                data.ResourceAreaCount = reader.ReadInt32();
                if (data.Version >= 6)
                {
                    data.DynamicObjectCount = reader.ReadInt32();
                }
                if (data.Version >= 7)
                {
                    data.ControllerCount = reader.ReadInt32();
                }
                if (data.Version >= 16)
                {
                    data.FlyAreaCount = reader.ReadInt32();
                }
                if (data.Version == 16)
                {
                    data.MineralCubeCount = reader.ReadInt32();
                    data.MineralSphereCount = reader.ReadInt32();
                }
                if (data.Version == 22)
                {
                    data.ExportInfoBytes = reader.ReadBytes(ExportInfoByteLength);
                }
                else if (data.Version >= 23)
                {
                    data.ExportInfoBytes = reader.ReadBytes(ExportInfoByteLength * 2);
                }
                if (data.Version >= 20)
                {
                    data.CopyCount = reader.ReadUInt32();
                    data.FlyCopyCount = reader.ReadUInt32();
                }

                for (int i = 0; i < areaCount; i++)
                {
                    data.Areas.Add(ReadArea(reader, data.Version));
                }
                for (int i = 0; i < data.ResourceAreaCount; i++)
                {
                    data.ResourceAreas.Add(ReadResourceArea(reader, data.Version));
                }
                for (int i = 0; i < data.DynamicObjectCount; i++)
                {
                    data.DynamicObjects.Add(ReadDynamicObject(reader, data.Version));
                }
                for (int i = 0; i < data.ControllerCount; i++)
                {
                    data.Controllers.Add(ReadController(reader, data.Version));
                }
                for (int i = 0; i < data.FlyAreaCount; i++)
                {
                    data.FlyAreas.Add(ReadFlyArea(reader));
                }

                long remaining = reader.BaseStream.Length - reader.BaseStream.Position;
                data.TailBytes = remaining > 0 ? reader.ReadBytes((int)remaining) : new byte[0];
                return data;
            }
        }

        public void Save(NpcGenData data, string filePath)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Invalid npcgen.data path.", "filePath");
            }

            if (File.Exists(filePath))
            {
                File.Copy(filePath, filePath + ".bak", true);
            }

            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(data.Version);
                writer.Write(data.Areas.Count);
                writer.Write(data.ResourceAreas.Count);
                if (data.Version >= 6)
                {
                    writer.Write(data.DynamicObjects.Count);
                }
                if (data.Version >= 7)
                {
                    writer.Write(data.Controllers.Count);
                }
                if (data.Version >= 16)
                {
                    writer.Write(data.FlyAreas.Count);
                }
                if (data.Version == 16)
                {
                    writer.Write(data.MineralCubeCount);
                    writer.Write(data.MineralSphereCount);
                }
                if (data.Version == 22 || data.Version >= 23)
                {
                    writer.Write(data.ExportInfoBytes ?? new byte[0]);
                }
                if (data.Version >= 20)
                {
                    writer.Write(data.CopyCount);
                    writer.Write(data.FlyCopyCount);
                }

                foreach (NpcGenArea area in data.Areas)
                {
                    WriteArea(writer, data.Version, area);
                }
                foreach (NpcGenResourceArea area in data.ResourceAreas)
                {
                    WriteResourceArea(writer, data.Version, area);
                }
                foreach (NpcGenDynamicObject dynObj in data.DynamicObjects)
                {
                    WriteDynamicObject(writer, data.Version, dynObj);
                }
                foreach (NpcGenController controller in data.Controllers)
                {
                    WriteController(writer, data.Version, controller);
                }
                foreach (NpcGenFlyArea flyArea in data.FlyAreas)
                {
                    WriteFlyArea(writer, flyArea);
                }
                if (data.TailBytes != null && data.TailBytes.Length > 0)
                {
                    writer.Write(data.TailBytes);
                }
            }

            data.FilePath = filePath;
        }

        private NpcGenArea ReadArea(BinaryReader reader, int version)
        {
            NpcGenArea area = new NpcGenArea();
            area.AreaType = reader.ReadInt32();
            int entryCount = reader.ReadInt32();
            area.Position = ReadPointF3(reader);
            area.Direction = ReadPointF3(reader);
            area.Extents = ReadPointF3(reader);
            area.NpcType = reader.ReadInt32();
            area.GroupType = reader.ReadInt32();
            area.InitGen = reader.ReadByte();
            area.AutoRevive = reader.ReadByte();
            area.ValidOnce = reader.ReadByte();
            area.GenId = reader.ReadInt32();
            if (version >= 7)
            {
                area.ControllerId = reader.ReadInt32();
                area.LifeTime = reader.ReadInt32();
                area.MaxNum = reader.ReadInt32();
            }
            int attachCount = 0;
            if (version >= 12)
            {
                area.ExportId = reader.ReadInt32();
                attachCount = reader.ReadInt32();
                area.RawAttachCount = attachCount;
            }
            if (version >= 13)
            {
                area.BufferRegionId = reader.ReadInt32();
            }
            for (int i = 0; i < entryCount; i++)
            {
                area.Entries.Add(ReadEntry(reader));
            }
            for (int i = 0; i < attachCount; i++)
            {
                area.AttachIds.Add(reader.ReadInt32());
            }
            return area;
        }

        private NpcGenEntry ReadEntry(BinaryReader reader)
        {
            return new NpcGenEntry
            {
                Id = reader.ReadInt32(),
                Num = reader.ReadInt32(),
                Refresh = reader.ReadInt32(),
                DiedTimes = reader.ReadInt32(),
                Aggressive = reader.ReadInt32(),
                OffsetWater = reader.ReadSingle(),
                OffsetTerrain = reader.ReadSingle(),
                Faction = reader.ReadInt32(),
                FactionHelper = reader.ReadInt32(),
                FactionAccept = reader.ReadInt32(),
                NeedHelp = reader.ReadByte(),
                DefaultFaction = reader.ReadByte(),
                DefaultFactionHelper = reader.ReadByte(),
                DefaultFactionAccept = reader.ReadByte(),
                PathId = reader.ReadInt32(),
                LoopType = reader.ReadInt32(),
                SpeedFlag = reader.ReadInt32(),
                DeadTime = reader.ReadInt32()
            };
        }

        private NpcGenResourceArea ReadResourceArea(BinaryReader reader, int version)
        {
            NpcGenResourceArea area = new NpcGenResourceArea();
            area.Position = ReadPointF3(reader);
            area.ExtX = reader.ReadSingle();
            area.ExtZ = reader.ReadSingle();
            int entryCount = reader.ReadInt32();
            area.InitGen = reader.ReadByte();
            area.AutoRevive = reader.ReadByte();
            area.ValidOnce = reader.ReadByte();
            area.GenId = reader.ReadInt32();
            if (version >= 6)
            {
                area.DirectionBytes = reader.ReadBytes(2);
                area.RadiusByte = reader.ReadByte();
            }
            if (version >= 7)
            {
                area.ControllerId = reader.ReadInt32();
                area.MaxNum = reader.ReadInt32();
            }
            int attachCount = 0;
            if (version >= 12)
            {
                area.ExportId = reader.ReadInt32();
                attachCount = reader.ReadInt32();
                area.RawAttachCount = attachCount;
            }
            if (version >= 17)
            {
                area.Type = reader.ReadInt32();
                area.ExtY = reader.ReadSingle();
                area.Radius = reader.ReadSingle();
            }
            for (int i = 0; i < entryCount; i++)
            {
                area.Entries.Add(new NpcGenResourceEntry
                {
                    ResourceType = reader.ReadInt32(),
                    TemplateId = reader.ReadInt32(),
                    RefreshTime = reader.ReadInt32(),
                    Number = reader.ReadInt32(),
                    HeightOffset = reader.ReadSingle()
                });
            }
            for (int i = 0; i < attachCount; i++)
            {
                area.AttachIds.Add(reader.ReadInt32());
            }
            return area;
        }

        private NpcGenDynamicObject ReadDynamicObject(BinaryReader reader, int version)
        {
            NpcGenDynamicObject dynObj = new NpcGenDynamicObject();
            dynObj.DynamicObjectId = reader.ReadInt32();
            dynObj.Position = ReadPointF3(reader);
            dynObj.DirectionBytes = reader.ReadBytes(2);
            dynObj.RadiusByte = reader.ReadByte();
            if (version >= 9)
            {
                dynObj.Scale = reader.ReadByte();
            }
            if (version >= 10)
            {
                dynObj.ControllerId = reader.ReadUInt32();
            }
            if (version >= 14)
            {
                dynObj.Broadcast = reader.ReadByte();
            }
            if (version >= 24)
            {
                dynObj.ExportId = reader.ReadUInt32();
            }
            return dynObj;
        }

        private NpcGenController ReadController(BinaryReader reader, int version)
        {
            NpcGenController controller = new NpcGenController();
            controller.Id = reader.ReadInt32();
            controller.ControllerId = reader.ReadInt32();
            controller.Name = ReadFixedString(reader, 128);
            controller.Active = reader.ReadByte();
            controller.WaitTime = reader.ReadInt32();
            controller.StopTime = reader.ReadInt32();
            controller.ActiveTimeInvalid = reader.ReadByte();
            controller.StopTimeInvalid = reader.ReadByte();
            controller.Time1 = ReadTime(reader);
            controller.Time2 = ReadTime(reader);
            if (version >= 8)
            {
                controller.ActiveTimeRange = reader.ReadInt32();
            }
            if (version >= 11)
            {
                controller.RepeatActive = reader.ReadByte();
            }
            if (version >= 15)
            {
                controller.ZoneMask = reader.ReadInt64();
            }
            return controller;
        }

        private NpcGenFlyArea ReadFlyArea(BinaryReader reader)
        {
            NpcGenFlyArea area = new NpcGenFlyArea();
            int pointCount = reader.ReadInt32();
            for (int i = 0; i < pointCount; i++)
            {
                area.Points.Add(new System.Drawing.PointF(reader.ReadSingle(), reader.ReadSingle()));
            }
            area.Height = reader.ReadSingle();
            area.RequiredLevel = reader.ReadInt32();
            return area;
        }

        private void WriteArea(BinaryWriter writer, int version, NpcGenArea area)
        {
            writer.Write(area.AreaType);
            writer.Write(area.Entries.Count);
            WritePointF3(writer, area.Position);
            WritePointF3(writer, area.Direction);
            WritePointF3(writer, area.Extents);
            writer.Write(area.NpcType);
            writer.Write(area.GroupType);
            writer.Write(area.InitGen);
            writer.Write(area.AutoRevive);
            writer.Write(area.ValidOnce);
            writer.Write(area.GenId);
            if (version >= 7)
            {
                writer.Write(area.ControllerId);
                writer.Write(area.LifeTime);
                writer.Write(area.MaxNum);
            }
            if (version >= 12)
            {
                writer.Write(area.ExportId);
                writer.Write(GetAttachCountForWrite(area.RawAttachCount, area.AttachIds.Count));
            }
            if (version >= 13)
            {
                writer.Write(area.BufferRegionId);
            }
            foreach (NpcGenEntry entry in area.Entries)
            {
                WriteEntry(writer, entry);
            }
            foreach (int attachId in area.AttachIds)
            {
                writer.Write(attachId);
            }
        }

        private void WriteEntry(BinaryWriter writer, NpcGenEntry entry)
        {
            writer.Write(entry.Id);
            writer.Write(entry.Num);
            writer.Write(entry.Refresh);
            writer.Write(entry.DiedTimes);
            writer.Write(entry.Aggressive);
            writer.Write(entry.OffsetWater);
            writer.Write(entry.OffsetTerrain);
            writer.Write(entry.Faction);
            writer.Write(entry.FactionHelper);
            writer.Write(entry.FactionAccept);
            writer.Write(entry.NeedHelp);
            writer.Write(entry.DefaultFaction);
            writer.Write(entry.DefaultFactionHelper);
            writer.Write(entry.DefaultFactionAccept);
            writer.Write(entry.PathId);
            writer.Write(entry.LoopType);
            writer.Write(entry.SpeedFlag);
            writer.Write(entry.DeadTime);
        }

        private void WriteResourceArea(BinaryWriter writer, int version, NpcGenResourceArea area)
        {
            WritePointF3(writer, area.Position);
            writer.Write(area.ExtX);
            writer.Write(area.ExtZ);
            writer.Write(area.Entries.Count);
            writer.Write(area.InitGen);
            writer.Write(area.AutoRevive);
            writer.Write(area.ValidOnce);
            writer.Write(area.GenId);
            if (version >= 6)
            {
                writer.Write(EnsureLength(area.DirectionBytes, 2));
                writer.Write(area.RadiusByte);
            }
            if (version >= 7)
            {
                writer.Write(area.ControllerId);
                writer.Write(area.MaxNum);
            }
            if (version >= 12)
            {
                writer.Write(area.ExportId);
                writer.Write(GetAttachCountForWrite(area.RawAttachCount, area.AttachIds.Count));
            }
            if (version >= 17)
            {
                writer.Write(area.Type);
                writer.Write(area.ExtY);
                writer.Write(area.Radius);
            }
            foreach (NpcGenResourceEntry entry in area.Entries)
            {
                writer.Write(entry.ResourceType);
                writer.Write(entry.TemplateId);
                writer.Write(entry.RefreshTime);
                writer.Write(entry.Number);
                writer.Write(entry.HeightOffset);
            }
            foreach (int attachId in area.AttachIds)
            {
                writer.Write(attachId);
            }
        }

        private static int GetAttachCountForWrite(int rawAttachCount, int actualCount)
        {
            if (actualCount > 0)
            {
                return actualCount;
            }

            return rawAttachCount < 0 ? rawAttachCount : 0;
        }

        private void WriteDynamicObject(BinaryWriter writer, int version, NpcGenDynamicObject dynObj)
        {
            writer.Write(dynObj.DynamicObjectId);
            WritePointF3(writer, dynObj.Position);
            writer.Write(EnsureLength(dynObj.DirectionBytes, 2));
            writer.Write(dynObj.RadiusByte);
            if (version >= 9)
            {
                writer.Write(dynObj.Scale);
            }
            if (version >= 10)
            {
                writer.Write(dynObj.ControllerId);
            }
            if (version >= 14)
            {
                writer.Write(dynObj.Broadcast);
            }
            if (version >= 24)
            {
                writer.Write(dynObj.ExportId);
            }
        }

        private void WriteController(BinaryWriter writer, int version, NpcGenController controller)
        {
            writer.Write(controller.Id);
            writer.Write(controller.ControllerId);
            writer.Write(ToFixedBytes(controller.Name, 128));
            writer.Write(controller.Active);
            writer.Write(controller.WaitTime);
            writer.Write(controller.StopTime);
            writer.Write(controller.ActiveTimeInvalid);
            writer.Write(controller.StopTimeInvalid);
            WriteTime(writer, controller.Time1);
            WriteTime(writer, controller.Time2);
            if (version >= 8)
            {
                writer.Write(controller.ActiveTimeRange);
            }
            if (version >= 11)
            {
                writer.Write(controller.RepeatActive);
            }
            if (version >= 15)
            {
                writer.Write(controller.ZoneMask);
            }
        }

        private void WriteFlyArea(BinaryWriter writer, NpcGenFlyArea area)
        {
            writer.Write(area.Points.Count);
            foreach (System.Drawing.PointF point in area.Points)
            {
                writer.Write(point.X);
                writer.Write(point.Y);
            }
            writer.Write(area.Height);
            writer.Write(area.RequiredLevel);
        }

        private PointF3 ReadPointF3(BinaryReader reader)
        {
            return new PointF3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private void WritePointF3(BinaryWriter writer, PointF3 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
            writer.Write(value.Z);
        }

        private NpcGenTime ReadTime(BinaryReader reader)
        {
            return new NpcGenTime
            {
                Year = reader.ReadInt32(),
                Month = reader.ReadInt32(),
                Week = reader.ReadInt32(),
                Day = reader.ReadInt32(),
                Hour = reader.ReadInt32(),
                Minute = reader.ReadInt32()
            };
        }

        private void WriteTime(BinaryWriter writer, NpcGenTime time)
        {
            NpcGenTime value = time ?? new NpcGenTime();
            writer.Write(value.Year);
            writer.Write(value.Month);
            writer.Write(value.Week);
            writer.Write(value.Day);
            writer.Write(value.Hour);
            writer.Write(value.Minute);
        }

        private string ReadFixedString(BinaryReader reader, int byteLength)
        {
            byte[] bytes = reader.ReadBytes(byteLength);
            int length = Array.IndexOf(bytes, (byte)0);
            if (length < 0)
            {
                length = bytes.Length;
            }
            return textEncoding.GetString(bytes, 0, length);
        }

        private byte[] ToFixedBytes(string text, int byteLength)
        {
            byte[] output = new byte[byteLength];
            if (!string.IsNullOrEmpty(text))
            {
                byte[] source = textEncoding.GetBytes(text);
                Buffer.BlockCopy(source, 0, output, 0, Math.Min(source.Length, byteLength));
            }
            return output;
        }

        private static byte[] EnsureLength(byte[] input, int length)
        {
            byte[] output = new byte[length];
            if (input != null)
            {
                Buffer.BlockCopy(input, 0, output, 0, Math.Min(input.Length, output.Length));
            }
            return output;
        }
    }
}
