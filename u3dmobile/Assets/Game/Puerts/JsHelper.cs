//use the menu item "U3DMOBILE/Install Puerts" to install puerts,
//and add "U3DMOBILE_USE_PUERTS" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_PUERTS

using Puerts;
using System.Text;

namespace U3DMobile
{
    public static class JsHelper
    {
        public static ArrayBuffer UTF8Buffer(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                return new ArrayBuffer(bytes);
            }
            else
            {
                return null;
            }
        }

        public static string UTF8String(ArrayBuffer buffer)
        {
            if (buffer != null && buffer.Bytes != null)
            {
                return Encoding.UTF8.GetString(buffer.Bytes);
            }
            else
            {
                return null;
            }
        }

        public static ArrayBuffer GetBuffer(byte[] bytes)
        {
            if (bytes != null && bytes.Length > 0)
            {
                return new ArrayBuffer(bytes);
            }
            else
            {
                return null;
            }
        }

        public static byte[] GetBytes(ArrayBuffer buffer)
        {
            if (buffer != null)
            {
                return buffer.Bytes;
            }
            else
            {
                return null;
            }
        }
    }
}

#endif
