using HarmonyLib;
using ICities;
using JetBrains.Annotations;
using HideUnconnectedTracks.Utils;
using CitiesHarmony.API;
using System.Runtime.CompilerServices;

namespace HideUnconnectedTracks.LifeCycle {
    public class KianModInfo : IUserMod {
        public string Name => "RM Unconnected Tracks ";
        public string Description => "Automatically hide unconnected track textures";

        [UsedImplicitly]
        [MethodImpl(MethodImplOptions.NoInlining)]

        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            HarmonyHelper.DoOnHarmonyReady(InstallHarmony);
            if (Extensions.InGame)
                LifeCycle.Load();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            LifeCycle.Release();
            UninstallHarmony();
        }

        #region Harmony
        bool installed = false;
        const string HarmonyId = "CS.kian.HideUnconnectedTracks";

        [MethodImpl(MethodImplOptions.NoInlining)]
        void InstallHarmony() {
            if (!installed) {
                Extensions.Log("HideUnconnectedTracks Patching...", true);
#if DEBUG
                //HarmonyInstance.DEBUG = true;
#endif
                Harmony harmony = new Harmony(HarmonyId);
                harmony.PatchAll(GetType().Assembly);
                Extensions.Log("HideUnconnectedTracks Patching Completed!", true);
                installed = true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void UninstallHarmony() {
            if (installed) {
                Harmony harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                Extensions.Log("HideUnconnectedTracks patches Reverted.", true);
                installed = false;
            }
        }
        #endregion
    }
}
