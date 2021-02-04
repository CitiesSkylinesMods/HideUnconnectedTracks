using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using static KianCommons.Assertion;
using KianCommons.Patches;

namespace HideUnconnectedTracks.Patches.NetNodePatches {
    using Utils; using KianCommons;

    [HarmonyPatch()]
    public static class RenderInstance {
        static string _logprefix = "NetNode.RenderInstance.Transpiler: ";
        static bool VERBOSE => KianCommons.Log.VERBOSE;

        // RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
        static MethodInfo Target => typeof(global::NetNode).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodBase TargetMethod() {
            var ret = Target;
            Assertion.Assert(ret != null, "did not manage to find original function to patch");
            if(VERBOSE) Log.Debug(_logprefix + "aquired method " + ret);
            return ret;
        }

        //static bool Prefix(ushort nodeID){}
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = instructions.ToCodeList();
                CheckTracksCommons.ApplyCheckTracks(codes, original, occurance:1);

                Log.Debug("successfully patched NetNode.RenderInstance()");
                return codes;
            }catch(Exception e) {
                if (VERBOSE) Log.Error(e.ToString(), false);
                throw e;
            }
        }
    } // end class
} // end name space