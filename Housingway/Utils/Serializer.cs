using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Bindings.ImGui;

namespace Housingway.Utils;

/// <summary>
/// Helper tools for compressing and decompressing data from or to the clipboard.
/// </summary>
public static class Serializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        IncludeFields = true
    };
    
    /// <summary>
    /// Compresses supplied data into a base64 string in the user's clipboard
    /// </summary>
    /// <param name="data">Data to compress</param>
    /// <typeparam name="T">Type to compress</typeparam>
    public static void CompressToClipboard<T>(T data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        var compressed = Compress(json);
        var base64 = Convert.ToBase64String(compressed);
        
        Service.ChatGui.Print($"Copied {compressed.Length} bytes to clipboard.");
        
        ImGui.SetClipboardText(base64);
    }

    /// <summary>
    /// Tries to decompress data from the clipboard, and if unsuccessful returns the default for T
    /// </summary>
    /// <param name="data">Output Data of type T</param>
    /// <typeparam name="T">Type to interpret clipboard data as</typeparam>
    /// <returns></returns>
    public static bool TryDecompressFromClipboard<T>(out T data)
    {
        try
        {
            var temp = DecompressFromClipboard<T>();
            
            if (temp is not null)
            {
                data = temp;
                return true;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        data = default!;
        return false;
    }

    /// <summary>
    /// Decompresses Base64 from the user's clipboard
    /// </summary>
    /// <typeparam name="T">Type to interpret data as</typeparam>
    /// <returns>The decompressed data</returns>
    /// <exception cref="NullReferenceException">Throws if clipboard is empty</exception>
    public static T? DecompressFromClipboard<T>()
    {
        var data = ImGui.GetClipboardText();
        if (string.IsNullOrEmpty(data)) throw new NullReferenceException("Clipboard text is empty");
        
        var bytes = Convert.FromBase64String(data);
        var decompressed = Decompress(bytes);
        
        return JsonSerializer.Deserialize<T>(decompressed, Options);
    }

    /// <summary>
    /// Compresses text using Brotli
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static byte[] Compress(string text)
    {
        var buffer = Encoding.UTF8.GetBytes(text);
        
        using var memory = new MemoryStream();
        using (var brotli = new BrotliStream(memory, CompressionLevel.SmallestSize))
        {
            brotli.Write(buffer, 0, buffer.Length);
        }
        
        return memory.ToArray();
    }

    /// <summary>
    /// Decompresses byte[] into a string
    /// </summary>
    /// <param name="data">The data to decompress</param>
    /// <returns>Decompressed string</returns>
    public static string Decompress(byte[] data)
    {
        using var memory = new MemoryStream(data);
        using var brotli = new BrotliStream(memory, CompressionMode.Decompress);
        using var stream = new StreamReader(brotli, Encoding.UTF8);
        
        return stream.ReadToEnd();
    }
}
