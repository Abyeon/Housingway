using System;
using System.Runtime.CompilerServices;

namespace Housingway.Utils.Extensions;

public static class EnumExtensions
{
    extension<T>(T value) where T : struct, Enum
    {
        public T SetFlag(T flag, bool enabled)
        {
            ulong tempVal = Unsafe.As<T, ulong>(ref value);
            ulong tempFlag = Unsafe.As<T, ulong>(ref flag);

            if (enabled)
            {
                tempVal |= tempFlag;
            }
            else
            {
                tempVal &= ~tempFlag;
            }
            
            return Unsafe.As<ulong, T>(ref tempVal);
        }
    }
}