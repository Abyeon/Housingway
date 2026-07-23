using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;

namespace Housingway.Utils;

/// <summary>
/// Helper tools for handling saving/loading data.
/// </summary>
public static class Serializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
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

    /// <summary>
    /// Load a file from the given path
    /// </summary>
    /// <param name="path">The path to get the file from</param>
    /// <typeparam name="T">Type to interpret file as</typeparam>
    /// <returns>Instance of T</returns>
    public static async Task<T> LoadFile<T>(string path)
    {
        var file = new FileInfo(path);
        var defaultData = Activator.CreateInstance<T>();
        
        if (file.Exists)
        {
            try
            {
                var text = await Service.ReliableFileStorage.ReadAllTextAsync(file.FullName);
                var data = JsonSerializer.Deserialize<T>(text, Options);

                if (data is null)
                {
                    data = defaultData;
                    await SaveFile(file.FullName, data);
                }
                
                return data;
            } catch (Exception e)
            {
                Service.Log.Error(e, $"Error while loading file {file.FullName}.");
            }
        }
        
        await SaveFile(path, defaultData);
        return defaultData;
    }

    /// <summary>
    /// Save a file at the given path
    /// </summary>
    /// <param name="path">Path to save the file at</param>
    /// <param name="data">Data to save</param>
    /// <typeparam name="T">Type of Data</typeparam>
    /// <exception cref="NullReferenceException">Throws if data is null</exception>
    public static async Task SaveFile<T>(string path, T data)
    {
        try
        {
            if (data is null)
            {
                throw new NullReferenceException("Data is null");
            }

            var text = JsonSerializer.Serialize(data, data.GetType(), Options);
            await Service.ReliableFileStorage.WriteAllTextAsync(path, text);
        }
        catch (Exception e)
        {
            Service.Log.Error(e, $"Error while trying to save file {path}");
        }
    }
    
    /// <summary>
    /// Get the files at the given path
    /// </summary>
    /// <param name="path">Path to fetch files in</param>
    /// <returns>Array of files</returns>
    public static FileInfo[] GetDirectoryFiles(params string[] path)
    {
        var dir = GetDirectory(path);
        return dir.GetFiles();
    }
    
    /// <summary>
    /// Get the info of the file at the provided path
    /// </summary>
    /// <param name="path">Path to fetch the file from</param>
    /// <returns>FileInfo</returns>
    public static FileInfo GetFileInfo(params string[] path)
    {
        var dir = GetDirectory(path[..^1]);
        return new FileInfo(Path.Combine(dir.FullName, path[^1]));
    }

    /// <summary>
    /// Get the directory object from a given path, creates one if it does not exist
    /// </summary>
    /// <param name="path">Path to form the directory from</param>
    /// <returns>DirectoryInfo</returns>
    public static DirectoryInfo GetDirectory(params string[] path)
    {
        var directory = Service.PluginInterface.ConfigDirectory;
        foreach (var dir in path)
        {
            directory = new DirectoryInfo(Path.Combine(directory.FullName, dir));
            if (!directory.Exists)
            {
                directory.Create();
            }
        }

        return directory;
    }
}
