using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Housingway.Config;
using Housingway.Utils;

namespace Housingway.Profiles;

[Serializable]
public class Profile
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Configuration Config { get; set; }

    [JsonIgnore]
    public bool AddedByIpc { get; set; } = false;

    public Profile(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Config = new Configuration();
    }
    
    public Profile()
    {
        Id = Guid.Empty;
        Name = "";
        Config = null!;
    }

    public void Save() => Task.Run(async () => await SaveAsync());
    
    public async Task SaveAsync()
    {
        if (AddedByIpc) return; // return early if somebody else added this profile
        
        try
        {
            await Serializer.SaveFile(GetPath(), this);
        }
        catch (Exception e)
        {
            Service.Log.Error(e, $"Error while saving {Name}");
        }
    }
    
    public void Delete() => Task.Run(async () => await DeleteAsync());

    public async Task DeleteAsync()
    {
        await Serializer.DeleteFile(GetPath());
    }

    private string GetPath() => Serializer.GetFileInfo("Profiles", Name).FullName + ".json";

    public bool IsValid()
    {
        return Id != Guid.Empty && !string.IsNullOrEmpty(Name);
    }
}
