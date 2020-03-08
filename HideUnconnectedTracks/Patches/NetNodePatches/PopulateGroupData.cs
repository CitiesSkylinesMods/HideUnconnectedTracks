using Harmony;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace HideTMPECrosswalks.Patches.NetNodePatches {
    using Utils;

    //[HarmonyPatch()]
    public static class PopulateGroupData {
        static void Log(string m) => Extensions.Log("PopulateGroupData Transpiler: " + m);

        // YES: public void PopulateGroupData(ushort nodeID, int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        // NO: public static void PopulateGroupData(NetInfo info, NetInfo.Node nodeInfo, Matrix4x4 leftMatrix, Matrix4x4 rightMatrix, Vector4 meshScale, Vector4 objectIndex, ref int vertexIndex, ref int triangleIndex, RenderGroup.MeshData data, ref bool requireSurfaceMaps)
        // NO: public static void PopulateGroupData(NetInfo info, NetInfo.Node nodeInfo, Matrix4x4 leftMatrix, Matrix4x4 rightMatrix, Matrix4x4 leftMatrixB, Matrix4x4 rightMatrixB, Vector4 meshScale, Vector4 centerPos, Vector4 sideScale, Vector4 objectIndex, ref int vertexIndex, ref int triangleIndex, RenderGroup.MeshData data, ref bool requireSurfaceMaps)
        static MethodInfo Target => typeof(global::NetNode).GetMethod("PopulateGroupData", BindingFlags.Public | BindingFlags.Instance);
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
                CheckFlagsCommon.PatchCheckFlags(codes, occurance: 3, nodeID_arg: 1, segmentID_loc: 79, Target);
                Log("successfully patched NetNode.PopulateGroupData");
                return codes;
            }
            catch (Exception e) {
                Log(e + "\n" + Environment.StackTrace);
                throw e;
            }
        }
    } // end class
} // end name space