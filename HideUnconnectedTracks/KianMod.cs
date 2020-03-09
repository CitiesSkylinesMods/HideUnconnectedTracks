using Harmony;
using ICities;
using JetBrains.Annotations;
using HideUnconnectedTracks.Utils;
using HideUnconnectedTracks.Patches;
using System;
using UnityEngine.SceneManagement;

namespace HideUnconnectedTracks {
    public class KianModInfo : IUserMod {
        public string Name => "RM Tracks ";
        public string Description => "Automatically hide unconnected track textures";

        [UsedImplicitly]
        public void OnEnabled() {
            System.IO.File.WriteAllText("mod.debug.log", ""); // restart log.
            InstallHarmony();
            LoadingWrapperPatch.OnPostLevelLoaded += SegmentEnd.InitSegmentEndArray;
            if(SceneManager.GetActiveScene().name.Equals("Game")) 
                SegmentEnd.InitSegmentEndArray();
        }

        [UsedImplicitly]
        public void OnDisabled() {
            UninstallHarmony();
            LoadingWrapperPatch.OnPostLevelLoaded -= SegmentEnd.InitSegmentEndArray;


#if DEBUG
            LoadingWrapperPatch.OnPostLevelLoaded -= TestOnLoad.Test;
#endif
        }

        #region Harmony
        HarmonyInstance harmony = null;
        const string HarmonyId = "CS.kian.HideTMPECrosswalks";
        void InstallHarmony() {
            if (harmony == null) {
                Extensions.Log("HideTMPECrosswalks Patching...",true);
#if DEBUG
                HarmonyInstance.DEBUG = true;
#endif
                HarmonyInstance.SELF_PATCHING = false;
                harmony = HarmonyInstance.Create(HarmonyId);
                harmony.PatchAll(GetType().Assembly);
                Extensions.Log("HideTMPECrosswalks Patching Completed!", true);
            }
        }

        void UninstallHarmony() {
            if (harmony != null) {
                harmony.UnpatchAll(HarmonyId);
                harmony = null;
                Extensions.Log("HideTMPECrosswalks patches Reverted.",true);
            }
        }
        #endregion
    }
}
