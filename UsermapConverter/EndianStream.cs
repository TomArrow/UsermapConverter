using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UsermapConverter
{
    class EndianStream : IDisposable
    {
        private readonly Stack<EndianType> _endianessStack = new Stack<EndianType>();

        public enum EndianType { LittleEndian, BigEndian }

        public Stream Stream { get; }
        public EndianType Endianness { get; set; }

        public EndianStream(Stream stream, EndianType endianness)
        {
            Stream = stream;
            Endianness = endianness;
        }

        public bool CanSeek => Stream.CanSeek;
        public long Position => Stream.Position;
        public long SeekTo(long offset) => Stream.Seek(offset, SeekOrigin.Begin);
        public void Skip(int bytes) => Stream.Seek(bytes, SeekOrigin.Current);

        public Int32 ReadInt64() => BitConverter.ToInt32(ReadValue(8), 0);
        public UInt32 ReadUInt64() => BitConverter.ToUInt32(ReadValue(8), 0);
        public Int32 ReadInt32() => BitConverter.ToInt32(ReadValue(4), 0);
        public UInt32 ReadUInt32() => BitConverter.ToUInt32(ReadValue(4), 0);
        public Int16 ReadInt16() => BitConverter.ToInt16(ReadValue(2), 0);
        public UInt16 ReadUInt16() => BitConverter.ToUInt16(ReadValue(2), 0);
        public sbyte ReadInt8() => (sbyte)Stream.ReadByte();
        public byte ReadUInt8() => (byte)Stream.ReadByte();
        public float ReadFloat() => BitConverter.ToSingle(ReadValue(4), 0);
        public double ReadDouble() => BitConverter.ToDouble(ReadValue(8), 0);

        public void WriteInt64(Int64 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteUInt64(UInt64 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteInt32(Int32 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteUInt32(UInt32 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteInt16(Int16 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteUInt16(UInt16 value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteInt8(sbyte value) => Stream.WriteByte((byte)value);
        public void WriteUInt8(byte value) => Stream.WriteByte(value);
        public void WriteFloat(float value) => WriteValue(BitConverter.GetBytes(value));
        public void WriteDouble(double value) => WriteValue(BitConverter.GetBytes(value));

        public void PushEndian() => _endianessStack.Push(Endianness);
        public void PopEndian() => Endianness = _endianessStack.Pop();

        public string ReadAscii(int length)
        {
            var buff = new byte[length];
            Stream.Read(buff, 0, length);
            return Encoding.ASCII.GetString(buff).TrimEnd('\0');
        }

        public string ReadUTF16(int length)
        {
            var buff = new byte[length * 2];
            Stream.Read(buff, 0, length * 2);

            if (Endianness == EndianType.LittleEndian)
                return Encoding.Unicode.GetString(buff).TrimEnd('\0');
            else
                return Encoding.BigEndianUnicode.GetString(buff).TrimEnd('\0');
        }

        private byte[] ReadValue(int length)
        {
            var buff = new byte[length];
            Stream.Read(buff, 0, length);
            if (Endianness == EndianType.BigEndian)
                Array.Reverse(buff);
            return buff;
        }

        private void WriteValue(byte[] bytes)
        {
            if (Endianness == EndianType.BigEndian)
                Array.Reverse(bytes);
            Stream.Write(bytes, 0, bytes.Length);
        }

        public byte[] ReadBytes(int count)
        {
            var buff = new byte[count];
            Stream.Read(buff, 0, count);
            return buff;
        }

        public void WriteBytes(byte[] bytes)
        {
            Stream.Write(bytes, 0, bytes.Length);
        }

        public void Dispose()
        {
            Stream.Dispose();
        }

        internal void WriteUTF16(string value, int length)
        {
            WriteBytes(Encoding.BigEndianUnicode.GetBytes(value.PadRight(length, '\0')));
        }

        internal void WriteAscii(string value, int length)
        {
            WriteBytes(Encoding.ASCII.GetBytes(value.PadRight(length, '\0')));
        }
    }
}
