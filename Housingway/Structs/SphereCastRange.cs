using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Housingway.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x70)]
public struct SphereCastRange
{
    [FieldOffset(0x50)] public Vector4 Cast;
}
