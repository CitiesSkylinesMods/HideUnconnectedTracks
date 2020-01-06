using System;
using ColossalFramework;
using UnityEngine;
using HideTMPECrosswalks.Utils;
using System.Reflection;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;
using ICities;

namespace HideTMPECrosswalks.Patches {
    using Utils;
    using static TranspilerUtils;
    [HarmonyPatch()]
    public static class NetNode_RenderInstance {

        public static bool ShouldHideCrossing(ushort nodeID, ushort segmentID) {
            NetInfo info = segmentID.ToSegment().Info;
            bool ret = PrefabUtils.CanHideCrossing(info);
#if DEBUG
            if (Extensions.currentMode == AppMode.AssetEditor) {
                return ret; // always hide crossings in asset editor for quick testing.
            }
#endif
            ret &= TMPEUTILS.HasCrossingBan(segmentID, nodeID);

            //TODO optimize
            //bool never = PrefabUtils.NeverZebra(info);
            //bool always = PrefabUtils.AlwaysZebra(info);
            //ret |= always;
            //ret &= !never;

            return ret;
        }

        public static bool CheckFlags(NetInfo.Node node, NetNode.Flags flags, ushort nodeID, ushort segmentID) {
            bool hideCrossings = ShouldHideCrossing(nodeID, segmentID);
            return NodeInfoExt.CheckFlags2(node, flags, hideCrossings);
        }

        static void Log(string m) => Extensions.Log("NetNode_RenderInstance Transpiler: " + m);

        //static bool Prefix(ushort nodeID) {
        //    NetNode node = nodeID.ToNode();
        //    return true;
        //}

        static MethodBase TargetMethod() {
            Log("TargetMethod");
            // RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info, int iter, Flags flags, ref uint instanceIndex, ref RenderManager.Instance data)
            var ret = typeof(global::NetNode).GetMethod("RenderInstance", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ret == null) {
                throw new Exception("did not manage to find original function to patch");
            }
            Log("aquired method " + ret);
            return ret;
        }

        #region Transpiler
        static MethodInfo mCheckFlags2 => typeof(NetNode_RenderInstance).GetMethod("CheckFlags");
        static MethodInfo mCheckFlags => typeof(NetInfo.Node).GetMethod("CheckFlags");

        public static IEnumerable<CodeInstruction> Transpiler(ILGenerator il, IEnumerable<CodeInstruction> instructions) {
            try {
                if (mCheckFlags == null || mCheckFlags2 == null) {
                    throw new Exception("method info is null");
                }

                var originalCodes = new List<CodeInstruction>(instructions);
                var codes = new List<CodeInstruction>(originalCodes);

                PatchCheckFlags(codes, 2, 13); // patch second draw mesh.

                Log("successfully patched NetNode.RenderInstance");

                return codes;
            }catch(Exception e) {
                Log(e + "\n" + Environment.StackTrace);
                throw e;
            }
        }

        // returns the position of First DrawMesh after index.
        public static void PatchCheckFlags(List<CodeInstruction> codes, int counter, byte segmentID_loc) {
            int index = 0;

            /*[253 15 - 253 67]
             * IL_05d6: ldloc.s node_V_16
             * IL_05d8: ldarg.s flags
             * IL_05da: callvirt instance bool NetInfo/Node::CheckFlags(Flags) <----------
             * IL_05df: brfalse IL_09d3 */
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckFlags), index, counter:counter );
            Extensions.Assert(index != 0, "index!=0");

            { // replace node.CheckFlags(flags) with CheckFlags2(node,flags,nodeID, segmentID)
                var newInstructions = new[]{
                    new CodeInstruction(OpCodes.Ldarg_2), // ldarg.2 | push nodeID into the stack
                    new CodeInstruction(OpCodes.Ldloc_S, segmentID_loc), // ldloc.s segmentID | push segmentID into the stack
                    new CodeInstruction(OpCodes.Call, mCheckFlags2), // call Material mCheckFlags2(node,flags, nodeID, segmentID).
                    };
                ReplaceInstructions(codes, newInstructions, index);
            } // end block
        } // end method

        #endregion Transpiler
    } // end class
} // end name space