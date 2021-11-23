using System;
using System.Collections.Generic;
using System.Text;

namespace U3DMobile
{
    public class BytesWriter
    {
        private List<byte[]> _slices = new List<byte[]>();
        private byte[] _bytes;

        public byte[] GetBytes()
        {
            //calculate the bytes length.
            int length = 0;
            foreach (byte[] slice in _slices)
            {
                length += slice.Length;
            }

            //join the slices if needed.
            if (_bytes == null || _bytes.Length != length)
            {
                _bytes = new byte[length];

                int index = 0;
                foreach (byte[] slice in _slices)
                {
                    Array.Copy(slice, 0, _bytes, index, slice.Length);
                    index += slice.Length;
                }
            }

            return _bytes;
        }

        public void WriteLeInt16(short slice) { WriteNumber(BitConverter.GetBytes(slice), true ); }
        public void WriteLeInt32(int   slice) { WriteNumber(BitConverter.GetBytes(slice), true ); }
        public void WriteBeInt16(short slice) { WriteNumber(BitConverter.GetBytes(slice), false); }
        public void WriteBeInt32(int   slice) { WriteNumber(BitConverter.GetBytes(slice), false); }

        private void WriteNumber(byte[] slice, bool littleEndian)
        {
            if (( BitConverter.IsLittleEndian && !littleEndian)
             || (!BitConverter.IsLittleEndian &&  littleEndian))
            {
                Array.Reverse(slice);
            }
            _slices.Add(slice);
        }

        public void WriteByteArr(byte[] slice)
        {
            if (slice != null && slice.Length > 0)
            {
                _slices.Add(slice);
            }
        }
    }

    public class BytesReader
    {
        private byte[] _bytes;
        private int    _index;

        public BytesReader(byte[] bytes)
        {
            _bytes = bytes;
            _index = 0;
        }

        public short ReadLeInt16() { return BitConverter.ToInt16(ReadNumber(2, true ), 0); }
        public int   ReadLeInt32() { return BitConverter.ToInt32(ReadNumber(4, true ), 0); }
        public short ReadBeInt16() { return BitConverter.ToInt16(ReadNumber(2, false), 0); }
        public int   ReadBeInt32() { return BitConverter.ToInt32(ReadNumber(4, false), 0); }

        private byte[] ReadNumber(int length, bool littleEndian)
        {
            byte[] slice = ReadByteArr(length);
            if (slice == null)
            {
                return new byte[length];
            }

            if (( BitConverter.IsLittleEndian && !littleEndian)
             || (!BitConverter.IsLittleEndian &&  littleEndian))
            {
                Array.Reverse(slice);
            }
            return slice;
        }

        public string ReadUTF8Str(int length)
        {
            byte[] slice = ReadByteArr(length);
            if (slice != null)
            {
                return Encoding.UTF8.GetString(slice);
            }
            else
            {
                return null;
            }
        }

        public byte[] ReadByteArr(int length)
        {
            if (length <= 0)
            {
                return null;
            }
            if (_bytes == null || _index + length > _bytes.Length)
            {
                return null;
            }

            byte[] slice = new byte[length];
            
            Array.Copy(_bytes, _index, slice, 0, length);
            _index += length;

            return slice;
        }
    }
}
