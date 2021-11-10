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

        public void Write_LE_Short(short slice) { GetNumber(BitConverter.GetBytes(slice), true ); }
        public void Write_LE_Int  (int   slice) { GetNumber(BitConverter.GetBytes(slice), true ); }
        public void Write_BE_Short(short slice) { GetNumber(BitConverter.GetBytes(slice), false); }
        public void Write_BE_Int  (int   slice) { GetNumber(BitConverter.GetBytes(slice), false); }

        private void GetNumber(byte[] slice, bool littleEndian)
        {
            if (( BitConverter.IsLittleEndian && !littleEndian)
             || (!BitConverter.IsLittleEndian &&  littleEndian))
            {
                Array.Reverse(slice);
            }
            _slices.Add(slice);
        }

        public void Write_U8String(string slice)
        {
            if (!string.IsNullOrEmpty(slice))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(slice);
                _slices.Add(bytes);
            }
        }

        public void Write_Bytes(byte[] slice)
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

        public short Read_LE_Short() { return BitConverter.ToInt16(GetNumber(2, true ), 0); }
        public int   Read_LE_Int  () { return BitConverter.ToInt32(GetNumber(4, true ), 0); }
        public short Read_BE_Short() { return BitConverter.ToInt16(GetNumber(2, false), 0); }
        public int   Read_BE_Int  () { return BitConverter.ToInt32(GetNumber(4, false), 0); }

        private byte[] GetNumber(int length, bool littleEndian)
        {
            byte[] slice = Read_Bytes(length);
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

        public string Read_U8String(int length)
        {
            byte[] slice = Read_Bytes(length);
            if (slice != null)
            {
                return Encoding.UTF8.GetString(slice);
            }
            else
            {
                return null;
            }
        }

        public byte[] Read_Bytes(int length)
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
