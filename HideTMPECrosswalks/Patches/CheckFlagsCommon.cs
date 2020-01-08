using ICities;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;


namespace HideTMPECrosswalks.Patches {
    using Utils;
    using static TranspilerUtils;
    public static class CheckFlagsCommon {
        public static bool ShouldHideCrossing(ushort nodeID, ushort segmentID) {
            NetInfo info = segmentID.ToSegment().Info;
            bool ret = PrefabUtils.CanHideCrossing(info);
#if DEBUG
            if (Extensions.currentMode == AppMode.AssetEditor) {
                //Extensions.Log($"Should hide crossings: {ret} | stack:\n" + System.Environment.StackTrace);
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
            bool ret = NodeInfoExt.CheckFlags2(node, flags, hideCrossings);
            //Extensions.Log($"flags={flags} | ShouldHideCrossings={hideCrossings}  node is NodeInfoExt={node is NodeInfoExt} ret={ret}\n"
            //    + Environment.StackTrace);
            return ret;
        }

        static MethodInfo mCheckFlags0 => typeof(RenderInstance).GetMethod("CheckFlags");
        static MethodInfo mCheckFlags => typeof(NetInfo.Node).GetMethod("CheckFlags");



        // returns the position of First DrawMesh after index.
        public static void PatchCheckFlags(List<CodeInstruction> codes, int counter, byte segmentID_loc) {
            Extensions.Assert(mCheckFlags != null, "mCheckFlags!=null failed");
            Extensions.Assert(mCheckFlags0 != null, "mCheckFlags0!=null failed");

            int index = 0;


            // IL_05da: callvirt instance bool NetInfo/Node::CheckFlags(Flags)
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckFlags), index, counter: counter);
            Extensions.Assert(index != 0, "index!=0");

            { // replace node.CheckFlags(flags) with CheckFlags2(node,flags,nodeID, segmentID)
                var newInstructions = new[]{
                    new CodeInstruction(OpCodes.Ldarg_2), // ldarg.2 | push nodeID into the stack
                    new CodeInstruction(OpCodes.Ldloc_S, segmentID_loc), // ldloc.s segmentID | push segmentID into the stack
                    new CodeInstruction(OpCodes.Call, mCheckFlags0), // call Material mCheckFlags2(node,flags, nodeID, segmentID).
                    };
                ReplaceInstructions(codes, newInstructions, index);
            } // end block
        } // end method

    }
}
