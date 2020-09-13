using HarmonyLib;
using ICities;
using JetBrains.Annotations;
using KianCommons;
using CitiesHarmony.API;
using System.Runtime.CompilerServices;

namespace HideUnconnectedTracks.LifeCycle {
    public class KianModInfo : IUserMod {
        public string Name => "RM Unconnected Tracks ";
        public string Description => "Automatically hide unconnected track textures";
        const string HarmonyId = "CS.kian.HideUnconnectedTracks";

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.NoInlining)]

        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            HarmonyHelper.DoOnHarmonyReady(() => HarmonyUtil.InstallHarmony(HarmonyId));
            if (HelpersExtensions.InGame)
                LifeCycle.Load();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            LifeCycle.Release();
            HarmonyUtil.UninstallHarmony(HarmonyId);
        }

    }
}
