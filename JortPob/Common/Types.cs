using System;
using System.Numerics;

namespace JortPob.Common
{
    public static class Vector3Extension
    {
        public static bool IsNaN(this Vector3 vec3)
        {
            return float.IsNaN(vec3.X) | float.IsNaN(vec3.Y) | float.IsNaN(vec3.Z);
        }

        /*public static Vector3 Round(this Vector3 vec3, int decimalPlaces)
        {
            return new Vector3(
                (float)Math.Round(vec3.X, decimalPlaces),
                (float)Math.Round(vec3.Y, decimalPlaces),
                (float)Math.Round(vec3.Z, decimalPlaces)
            );
        }*/

        public static bool TolerantEquals(this Vector3 A, Vector3 B)
        {
            return Vector3.Distance(A, B) <= 0.001f; // imprecision really do be a cunt
        }
    }

    public class Box
    {
        public int x1, y1, x2, y2;
        public Box(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1; this.y1 = y1;
            this.x2 = x2; this.y2 = y2;
        }
    }
    public class Int2
    {
        public readonly int x, y;
        public Int2(int x, int y)
        {
            this.x = x; this.y = y;
        }

        public static bool operator ==(Int2 a, Int2 b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(Int2 a, Int2 b) => !(a == b);

        public bool Equals(Int2 b)
        {
            return x == b.x && y == b.y;
        }
        public override bool Equals(object a) => Equals(a as Int2);

        public static Int2 operator +(Int2 a, Int2 b)
        {
            return a.Add(b);
        }

        public Int2 Add(Int2 b)
        {
            return new Int2(x + b.x, y + b.y);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x.GetHashCode();
                hashCode = hashCode * 397 ^ y.GetHashCode();
                return hashCode;
            }
        }

        public int[] Array()
        {
            int[] r = { x, y };
            return r;
        }
    }

    public class UShort2
    {
        public readonly ushort x, y;
        public UShort2(ushort x, ushort y)
        {
            this.x = x; this.y = y;
        }

        public static bool operator ==(UShort2 a, UShort2 b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(UShort2 a, UShort2 b) => !(a == b);

        public bool Equals(UShort2 b)
        {
            return x == b.x && y == b.y;
        }
        public override bool Equals(object a) => Equals(a as UShort2);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x.GetHashCode();
                hashCode = hashCode * 397 ^ y.GetHashCode();
                return hashCode;
            }
        }

        public ushort[] Array()
        {
            ushort[] r = { x, y };
            return r;
        }
    }

    public class Byte4
    {
        public readonly byte x, y, z, w;
        public Byte4(byte a)
        {
            x = a; y = a; z = a; w = a;
        }

        public Byte4(int x, int y, int z, int w)
        {

            this.x = (byte)Math.Max(0, Math.Min(byte.MaxValue, x)); this.y = (byte)Math.Max(0, Math.Min(byte.MaxValue, y)); this.z = (byte)Math.Max(0, Math.Min(byte.MaxValue, z)); this.w = (byte)Math.Max(0, Math.Min(byte.MaxValue, w));
        }

        public Byte4(byte x, byte y, byte z, byte w)
        {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
    }
}