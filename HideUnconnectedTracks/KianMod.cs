using HarmonyLib;
using ICities;
using JetBrains.Annotations;
using HideUnconnectedTracks.Utils;
using CitiesHarmony.API;

namespace HideUnconnectedTracks {
    public class KianModInfo : IUserMod {
        public string Name => "RM Unconnected Tracks ";
        public string Description => "Automatically hide unconnected track textures";

        [UsedImplicitly]
        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            HarmonyHelper.DoOnHarmonyReady(InstallHarmony); 
            LoadingManager.instance.m_levelPreLoaded += TMPEUTILS.Init;
            TMPEUTILS.Init();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            LoadingManager.instance.m_levelPreLoaded -= TMPEUTILS.Init;
            UninstallHarmony();
        }

        #region Harmony
        bool installed = false;
        const string HarmonyId = "CS.kian.HideUnconnectedTracks";
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
