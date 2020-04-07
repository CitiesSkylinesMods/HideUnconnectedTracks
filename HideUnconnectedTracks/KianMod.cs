using HarmonyLib;
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
        Harmony harmony = null;
        const string HarmonyId = "CS.kian.HideUnconnectedTracks";
        void InstallHarmony() {
            if (harmony == null) {
                Extensions.Log("HideUnconnectedTracks Patching...", true);
//#if DEBUG
//                HarmonyInstance.DEBUG = true;
//#endif
                harmony = new Harmony(HarmonyId);
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
