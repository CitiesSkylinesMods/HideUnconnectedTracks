using Harmony;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace HideTMPECrosswalks.Patches.NetNodePatches {
    using Utils;

    //[HarmonyPatch()]
    public static class CalculateGroupData {
        static void Log(string m) => Extensions.Log("CalculateGroupData Transpiler: " + m);
        //public bool CalculateGroupData(ushort nodeID, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        static MethodInfo Target => typeof(global::NetNode).GetMethod("CalculateGroupData", BindingFlags.Public | BindingFlags.Instance);
        static MethodBase TargetMethod() {
            var ret = Target;
            Extensions.Assert(ret != null, "did not manage to find original function to patch");
            Log("aquired method " + ret);
            return ret;
        }

        //static bool Prefix(ushort nodeID){}
        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                var codes = TranspilerUtils.ToCodeList(instructions);
                CheckFlagsCommon.PatchCheckFlags(codes, occurance: 3, nodeID_arg: 1, segmentID_loc: 30, Target);
                Log("successfully patched NetNode.CalculateGroupData");
                return codes;
            }
            catch (Exception e) {
                Log(e + "\n" + Environment.StackTrace);
                throw e;
            }
        }
    } // end class
} // end name space