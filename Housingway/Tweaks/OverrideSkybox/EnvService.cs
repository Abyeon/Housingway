using System;
using System.Linq;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Housingway.Structs.Env;
using Housingway.Utils;

namespace Housingway.Tweaks.OverrideSkybox;

[Flags]
public enum EnvOverride {
	None = 0x000,
	TimeWeather = 0x001,
	SkyId = 0x002,
	Lighting = 0x04,
	Stars = 0x008,
	Fog = 0x010,
	Clouds = 0x020,
	Rain = 0x040,
	Dust = 0x080,
	Wind = 0x100,
	Housing = 0x200,
}

// https://github.com/ktisis-tools/Ktisis/blob/a3d000a983ec75a07f96736f93d15ef53fcbec33/Ktisis/Scene/Modules/EnvModule.cs
public class EnvService : IDisposable {

    public EnvService()
    {
        Plugin.GameInteropProvider.InitializeFromAttributes(this);
        
        envStateCopyHook.Enable();
        envUpdateHook.Enable();
    }

    public void Dispose()
    {
        envStateCopyHook.Dispose();
        envUpdateHook.Dispose();
    }

    // EnvState

	public EnvOverride Override { get; set; } = EnvOverride.None;

	public float Time { get; set; }
	public int Day { get; set; }

	public byte Weather { get; set; }
	
	private unsafe void ApplyState(EnvState* dest, EnvState state) {
		var flags = Enum.GetValues<EnvOverride>()
			.Where(flag => flag > EnvOverride.TimeWeather && this.Override.HasFlag(flag));

		foreach (var flag in flags) {
			switch (flag) {
				case EnvOverride.SkyId:
					dest->SkyId = state.SkyId;
					break;
				case EnvOverride.Lighting:
					dest->Lighting = state.Lighting;
					break;
				case EnvOverride.Stars:
					dest->Stars = state.Stars;
					break;
				case EnvOverride.Fog:
					dest->Fog = state.Fog;
					break;
				case EnvOverride.Clouds:
					dest->Clouds = state.Clouds;
					break;
				case EnvOverride.Rain:
					dest->Rain = state.Rain;
					break;
				case EnvOverride.Dust:
					dest->Dust = state.Dust;
					break;
				case EnvOverride.Wind:
					dest->Wind = state.Wind;
					break;
			}
		}
	}
	
	// Hooks
	
	private unsafe delegate nint EnvStateCopyDelegate(EnvState* dest, EnvState* src);
	private unsafe delegate nint EnvManagerUpdateDelegate(EnvManagerEx* env, float a2, float a3);

	[Signature("E8 ?? ?? ?? ?? 49 3B F5 75 0D")]
	private EnvStateCopyDelegate envStateCopy = null!;

	[Signature("E8 ?? ?? ?? ?? 49 3B F5 75 0D", DetourName = nameof(EnvStateCopyDetour))]
	private Hook<EnvStateCopyDelegate> envStateCopyHook = null!;
	private unsafe nint EnvStateCopyDetour(EnvState* dest, EnvState* src) {
		EnvState? original = null;
		if (HousingService.IsInside && Override != 0)
			original = *dest;
		var exec = envStateCopyHook.Original(dest, src);
		if (original != null)
			ApplyState(dest, original.Value);
		return exec;
	}

	[Signature("40 53 48 83 EC 30 48 8B 05 ?? ?? ?? ?? 48 8B D9 0F 29 74 24 ??", DetourName = nameof(EnvUpdateDetour))]
	private Hook<EnvManagerUpdateDelegate> envUpdateHook = null!;
	private unsafe nint EnvUpdateDetour(EnvManagerEx* env, float a2, float a3) {
		if (HousingService.IsInside && Override.HasFlag(EnvOverride.TimeWeather)) {
			env->_base.DayTimeSeconds = Time;
			env->_base.ActiveWeather = Weather;
		}
		return envUpdateHook.Original(env, a2, a3);
	}
}
