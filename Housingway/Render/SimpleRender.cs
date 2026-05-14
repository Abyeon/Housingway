using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision.Math;
using Pictomancy;

namespace Housingway.Render;

public readonly struct Tri(int v1, int v2, int v3)
{
    public int V1 { get; } = v1;
    public int V2 { get; } = v2;
    public int V3 { get; } = v3;
}

public class SimpleRender(PctDrawList? draw) : IDisposable
{
    private readonly List<Vector3> vertices = [];
    private readonly Queue<Tri> triangles = new();

    private Matrix4x3 world;
    private Matrix4x4 inverseWorld;

    public void AddVertex(Vector3 vertex) => vertices.Add(vertex);
    public void AddTriangle(int v1, int v2, int v3) => triangles.Enqueue(new Tri(v1, v2, v3));
    public void AddInstance(Matrix4x3 worldMat)
    {
        world = worldMat;
        Matrix4x4.Invert(world.FullMatrix(), out inverseWorld);
    }
    
    public void Dispose()
    {
        if (draw == null) return;

        while (triangles.Count != 0)
        {
            var tri = triangles.Dequeue();
            
            var v1 = TransformVert(vertices[tri.V1], world);
            var v2 = TransformVert(vertices[tri.V2], world);
            var v3 = TransformVert(vertices[tri.V3], world);
            
            var edge1 = v2 - v1;
            var edge2 = v3 - v1;
            var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                        
            var incline = Vector3.Dot(normal, Vector3.UnitY);

            var inclineColor = incline switch
            {
                < -0.0001f => ImGuiColors.ParsedBlue,
                <= 0.4f => ImGuiColors.DalamudRed,
                _ => ImGuiColors.ParsedGreen
            };
            
            // A bit expensive to do every frame, need to actually build and save meshes with their colors.
            var diffuseFactor = Math.Max(0.5f, Vector3.Dot(normal, -Vector3.UnitX with { Y = 0.5f })) + 0.3f;
            inclineColor = (inclineColor * Vector4.One * diffuseFactor) with { W = 1f };

            draw.AddTriangleFilled(v1, v3, v2, ImGui.ColorConvertFloat4ToU32(inclineColor));
        }
    }
    
    private static Vector3 TransformVert(Vector3 vert, Matrix4x3 matrix)
    {
        return new Vector3(
            (vert.X * matrix.M11) + (vert.Y * matrix.M21) + (vert.Z * matrix.M31) + matrix.M41,
            (vert.X * matrix.M12) + (vert.Y * matrix.M22) + (vert.Z * matrix.M32) + matrix.M42,
            (vert.X * matrix.M13) + (vert.Y * matrix.M23) + (vert.Z * matrix.M33) + matrix.M43
        );
    }
}
