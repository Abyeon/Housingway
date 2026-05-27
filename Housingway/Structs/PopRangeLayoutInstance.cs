using System;
using System.Numerics;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using InteropGenerator.Runtime.Attributes;

namespace Housingway.Structs;

[Inherits<ILayoutInstance>]
[StructLayout(LayoutKind.Explicit, Size = 0xA0)]
public unsafe struct PopRangeLayoutInstance
{
    [FieldOffset(0x80)] private IntPtr StartAddress;
    [FieldOffset(0x88)] private IntPtr EndAddress;
    
    public Span<Vector3> RelativePositions
    {
        get
        {
            if (StartAddress == IntPtr.Zero || EndAddress == IntPtr.Zero)
                return Span<Vector3>.Empty;
            
            long byteLength = EndAddress.ToInt64() - StartAddress.ToInt64();
            int count = (int)(byteLength / sizeof(Vector3));
            
            return new Span<Vector3>((void*)StartAddress, count);
        }
    }
}
