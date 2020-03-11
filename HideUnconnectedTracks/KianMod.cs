using Harmony;
using ICities;
using JetBrains.Annotations;
using HideUnconnectedTracks.Utils;
using ColossalFramework;
using UnityEngine;

namespace HideUnconnectedTracks {
    public class KianModInfo : IUserMod {
        public string Name => "RM Unconnected Tracks ";
        public string Description => "Automatically hide unconnected track textures";

        [UsedImplicitly]
        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            InstallHarmony();
            LoadingManager.instance.m_levelPreLoaded += TMPEUTILS.Init;
            TMPEUTILS.Init();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            LoadingManager.instance.m_levelPreLoaded -= TMPEUTILS.Init;
            UninstallHarmony();
        }

        #region Harmony
        HarmonyInstance harmony = null;
        const string HarmonyId = "CS.kian.HideUnconnectedTracks";
        void InstallHarmony() {
            if (harmony == null) {
                Extensions.Log("HideUnconnectedTracks Patching...", true);
#if DEBUG
                HarmonyInstance.DEBUG = true;
#endif
                HarmonyInstance.SELF_PATCHING = false;
                harmony = HarmonyInstance.Create(HarmonyId);
                harmony.PatchAll(GetType().Assembly);
                Extensions.Log("HideUnconnectedTracks Patching Completed!", true);
            }
        }

        void UninstallHarmony() {
            if (harmony != null) {
                harmony.UnpatchAll(HarmonyId);
                harmony = null;
                Extensions.Log("HideUnconnectedTracks patches Reverted.", true);
            }
        }
        #endregion
    }
}
