using System;
using System.Collections.Generic;
using Housingway.Config;

namespace Housingway.Profiles;

[Serializable]
public class Profile
{
    public string Name { get; set; }
    public Configuration Config { get; set; }
    public List<Address> Addresses { get; set; } = [];

    public Profile(string name)
    {
        Name = name;
        Config = new Configuration();

        if (Address.TryGetAddress(out var currAddress))
        {
            Addresses.Add(currAddress);
        }
    }
}
