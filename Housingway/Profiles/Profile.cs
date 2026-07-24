using System;
using System.Collections.Generic;
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

    public Profile(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Config = new Configuration();
    }
    
    public Profile()
    {
        Id = Guid.NewGuid();
        Name = "";
        Config = null!;
    }

    public void Save() => Task.Run(async () => await SaveAsync());
    
    public async Task SaveAsync()
    {
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
}
