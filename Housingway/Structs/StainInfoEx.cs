using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Housingway.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xE0)]
public struct StainInfoEx
{
    [FieldOffset(0xC8)] public ulong PropertyFlags;
    [FieldOffset(0xCC)] public PropertyArray Properties;
}

[InlineArray(8)]
public struct PropertyArray
{
    private ushort Property;
}
