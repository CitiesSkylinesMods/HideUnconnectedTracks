using Harmony;
using ICities;
using JetBrains.Annotations;
using HideUnconnectedTracks.Utils;
using HideUnconnectedTracks.Patches;
using System;
using UnityEngine.SceneManagement;

namespace HideUnconnectedTracks {
    public class KianModInfo : IUserMod {
        public string Name => "RM Unconnected Tracks ";
        public string Description => "Automatically hide unconnected track textures";

        [UsedImplicitly]
        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            InstallHarmony();
        }

        [UsedImplicitly]
        public void OnDisabled() {
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
