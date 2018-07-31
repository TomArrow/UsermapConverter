using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UsermapConverter
{
    class Usermap
    {
        public class SandboxContentHeader
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Author { get; set; }
            public Int64 Size { get; set; }
            public Int64 Timestamp { get; set; }
            public int MapId { get; set; }
        }

        public class SandboxMap
        {
            public Int16 ScnrObjectCount { get; set; }
            public Int16 TotalObjectCount { get; set; }
            public Int16 BudgetEntryCount { get; set; }
            public int MapId { get; set; }

            public List<SandboxPlacement> Placements { get; set; }
            public List<BudgetEntry> Budget { get; set; }
        }

        public class SandboxPlacement
        {
            public static SandboxPlacement Null = new SandboxPlacement()
            {
                PlacementFlags = 0,
                Unknown_1 = 0,
                ObjectDatumHandle = 0xFFFFFFFF,
                GizmoDatumHandle = 0xFFFFFFFF,
                BudgetIndex = -1,
                Position = new Vector3(),
                RightVector = new Vector3(),
                UpVector = new Vector3(),
                Unknown_2 = 0,
                Unknown_3 = 0,
                EngineFlags = 0,
                Flags = 0,
                Team = 0,
                Extra = 0,
                RespawnTime = 0,
                ObjectType = 0,
                ZoneShape = 0,
                ZoneRadiusWidth = 0,
                ZoneDepth = 0,
                ZoneTop = 0,
                ZoneBottom = 0,
            };

            public ushort PlacementFlags;
            public ushort Unknown_1;
            public UInt32 ObjectDatumHandle;
            public UInt32 GizmoDatumHandle;
            public int BudgetIndex;
            public Vector3 Position;
            public Vector3 RightVector;
            public Vector3 UpVector;
            public UInt32 Unknown_2;
            public UInt32 Unknown_3;
            public ushort EngineFlags;
            public byte Flags;
            public byte Team;
            public byte Extra;
            public byte RespawnTime;
            public byte ObjectType;
            public byte ZoneShape;
            public float ZoneRadiusWidth;
            public float ZoneDepth;
            public float ZoneTop;
            public float ZoneBottom;

            public SandboxPlacement Clone()
            {
                return (SandboxPlacement)this.MemberwiseClone();
            }
        }

        public struct Vector3
        {
            public float X, Y, Z;

            public Vector3(float x = 0, float y = 0, float z = 0)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
            }
        }

        public class BudgetEntry
        {
            public UInt32 TagIndex;
            public byte RuntimeMin;
            public byte RuntimeMax;
            public byte CountOnMap;
            public byte DesignTimeMax;
            public float Cost;

            public BudgetEntry Clone()
            {
                return (BudgetEntry)this.MemberwiseClone();
            }
        }

        public static SandboxContentHeader DeserializeContentHeader(EndianStream stream)
        {
            var content = new SandboxContentHeader();
            stream.SeekTo(0x138);
            if(!stream.ReadAscii(4).Equals("mapv"))
            {
                throw new InvalidDataException("expected mapv magic.");
            }

            stream.SeekTo(0x48);
            content.Name = stream.ReadUTF16(16);
            content.Description = stream.ReadAscii(128);
            content.Author = stream.ReadAscii(16);
            stream.SeekTo(0x108);
            content.Size = stream.ReadInt64();
            content.Timestamp = stream.ReadInt64();
            stream.SeekTo(0x120);
            content.MapId = stream.ReadInt32();
            return content;
        }

        public static SandboxMap DeserializeSandboxMap(EndianStream stream)
        {
            var map = new SandboxMap();

            stream.SeekTo(0x228);
            map.MapId = stream.ReadInt32();

            stream.SeekTo(0x242);
            map.ScnrObjectCount = stream.ReadInt16();
            map.TotalObjectCount = stream.ReadInt16();
            map.BudgetEntryCount = stream.ReadInt16();

            map.Placements = new List<SandboxPlacement>();

            stream.SeekTo(0x278);
            for (var i = 0; i < 640; i++)
                map.Placements.Add(DeserializePlacement(stream));

            map.Budget = new List<BudgetEntry>();

            stream.SeekTo(0xD498);
            for (var i = 0; i < 256; i++)
                map.Budget.Add(DeserializeBudgetEntry(stream));

            return map;
        }

        public static Vector3 DeserializeVector3(EndianStream stream)
        {
            return new Vector3 { X = stream.ReadFloat(), Y = stream.ReadFloat(), Z = stream.ReadFloat() };
        }

        public static void SerializeBudgetEntry(EndianStream stream, BudgetEntry entry)
        {
            stream.WriteUInt32(entry.TagIndex);
            stream.WriteUInt8(entry.RuntimeMin);
            stream.WriteUInt8(entry.RuntimeMax);
            stream.WriteUInt8(entry.CountOnMap);
            stream.WriteUInt8(entry.DesignTimeMax);
            stream.WriteFloat(entry.Cost);
        }

        public static BudgetEntry DeserializeBudgetEntry(EndianStream stream)
        {
            var entry = new BudgetEntry();
            entry.TagIndex = stream.ReadUInt32();
            entry.RuntimeMin = stream.ReadUInt8();
            entry.RuntimeMax = stream.ReadUInt8();
            entry.CountOnMap = stream.ReadUInt8();
            entry.DesignTimeMax = stream.ReadUInt8();
            entry.Cost = stream.ReadFloat();
            return entry;
        }

        public static void SerializeVector3(EndianStream stream, Vector3 vector)
        {
            stream.WriteFloat(vector.X);
            stream.WriteFloat(vector.Y);
            stream.WriteFloat(vector.Z);
        }

        public static void SerializePlacement(EndianStream stream, SandboxPlacement placement)
        {
            stream.WriteUInt16(placement.PlacementFlags);
            stream.WriteUInt16(placement.Unknown_1);
            stream.WriteUInt32(placement.ObjectDatumHandle);
            stream.WriteUInt32(placement.GizmoDatumHandle);
            stream.WriteInt32(placement.BudgetIndex);
            SerializeVector3(stream, placement.Position);
            SerializeVector3(stream, placement.RightVector);
            SerializeVector3(stream, placement.UpVector);
            stream.WriteUInt32(placement.Unknown_2);
            stream.WriteUInt32(placement.Unknown_3);
            stream.WriteUInt16(placement.EngineFlags);
            stream.WriteUInt8(placement.Flags);
            stream.WriteUInt8(placement.Team);
            stream.WriteUInt8(placement.Extra);
            stream.WriteUInt8(placement.RespawnTime);
            stream.WriteUInt8(placement.ObjectType);
            stream.WriteUInt8(placement.ZoneShape);
            stream.WriteFloat(placement.ZoneRadiusWidth);
            stream.WriteFloat(placement.ZoneDepth);
            stream.WriteFloat(placement.ZoneTop);
            stream.WriteFloat(placement.ZoneBottom);
        }

        public static SandboxPlacement DeserializePlacement(EndianStream stream)
        {
            var placement = new SandboxPlacement();
            placement.PlacementFlags = stream.ReadUInt16();
            placement.Unknown_1 = stream.ReadUInt16();
            placement.ObjectDatumHandle = stream.ReadUInt32();
            placement.GizmoDatumHandle = stream.ReadUInt32();
            placement.BudgetIndex = stream.ReadInt32();
            placement.Position = DeserializeVector3(stream);
            placement.RightVector = DeserializeVector3(stream);
            placement.UpVector = DeserializeVector3(stream);
            placement.Unknown_2 = stream.ReadUInt32();
            placement.Unknown_3 = stream.ReadUInt32();
            placement.EngineFlags = stream.ReadUInt16();
            placement.Flags = stream.ReadUInt8();
            placement.Team = stream.ReadUInt8();
            placement.Extra = stream.ReadUInt8();
            placement.RespawnTime = stream.ReadUInt8();
            placement.ObjectType = stream.ReadUInt8();
            placement.ZoneShape = stream.ReadUInt8();
            placement.ZoneRadiusWidth = stream.ReadFloat();
            placement.ZoneDepth = stream.ReadFloat();
            placement.ZoneTop = stream.ReadFloat();
            placement.ZoneBottom = stream.ReadFloat();
            return placement;
        }

    }

}
