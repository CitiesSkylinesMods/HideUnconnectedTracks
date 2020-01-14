using ICities;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;

/* Notes
 * experiment 1: I created a road cirlcle and connected the middle to avoid any bend/end/direct_connect nodes.
 *   B1 I put break point on all checkflags that I will not patch.
 *   J1 I addded and deleted junctiuons (without adding bend/end nodes)
 *   Z1 I zoommed in and out
 *   R1 No breakpoint was hit.
 *
 *   Z2 I zoomed out
 *   B2 I added break points on the checkflags calls that I will patch.
 *   R2 No break points was hit
 *
 *   Z3 I zoomed in
 *   R3 renderinstance breakpoint was hit continuesly
 *
 *   B4 I removed the breakpoint that was hit in R3 (render instance call to checkflags that I will patch).
 *   J4 I added a junction
 *   Z4 I did this while zoomed in or zoomed out.
 *   R4 the break points I put in B2 in *groupdata() where hit once everytime I made a change.
 *
 *  experiment 2: ShouldHideCrossings always returns true.
 *
 *
 */


namespace HideTMPECrosswalks.Patches {
    using Utils;
    using static TranspilerUtils;
    public static class CheckFlagsCommon {
        public static bool ShouldHideCrossing(ushort nodeID, ushort segmentID) {
            NetInfo info = segmentID.ToSegment().Info;
            bool ret1 = PrefabUtils.CanHideCrossing(info);
#if DEBUG

            if (Extensions.currentMode == AppMode.AssetEditor) {
                //Extensions.Log($"Should hide crossings: {ret} | stack:\n" + System.Environment.StackTrace);
                return ret1; // always hide crossings in asset editor for quick testing.
            }
#endif
            ret1 &= TMPEUTILS.HasCrossingBan(segmentID, nodeID);
            bool ret2 = info.m_netAI is RoadBaseAI;
            ret2 &= NS2Utils.HideJunction(segmentID);
            return ret1 || ret2;
        }

        public static bool CheckFlags(NetInfo.Node node, NetNode.Flags flags, ushort nodeID, ushort segmentID) {
            // Extensions.Log("CheckFlagsCommon.CheckFlags() stack=\n" + System.Environment.StackTrace);
            bool hideCrossings = ShouldHideCrossing(nodeID, segmentID);
            bool ret = NodeInfoExt.CheckFlags2(node, flags, hideCrossings);
            //Extensions.Log($"flags={flags} | ShouldHideCrossings={hideCrossings}  node is NodeInfoExt={node is NodeInfoExt} ret={ret}\n"
            //    + Environment.StackTrace);
            return ret;
        }

        static MethodInfo mCheckFlags2 => typeof(CheckFlagsCommon).GetMethod("CheckFlags");
        static MethodInfo mCheckFlags => typeof(NetInfo.Node).GetMethod("CheckFlags");
        static MethodInfo mGetSegment => typeof(NetNode).GetMethod("GetSegment");


        // returns the position of First DrawMesh after index.
        public static void PatchCheckFlags(List<CodeInstruction> codes, int occurance, byte nodeID_arg,byte segmentID_loc, MethodInfo method) {
            Extensions.Assert(mCheckFlags != null, "mCheckFlags!=null failed");
            Extensions.Assert(mCheckFlags2 != null, "mCheckFlags0!=null failed");
            //Extensions.Assert(method != null, "did not manage to find original function to patch");
            //var parameters = method.GetParameters();
            //Extensions.Assert(parameters[nodeID_arg - 1].ParameterType == typeof(ushort),
            //    $"wrong nodeID_arg. parameters[{nodeID_arg - 1}] is {parameters[nodeID_arg - 1].ParameterType} , expected ushort");
            //var variables = method.GetMethodBody().LocalVariables;
            //Extensions.Assert(variables[segmentID_loc].LocalType == typeof(ushort),
            //    $"wrong segmentID_loc. LocalVariables[{segmentID_loc}] is {variables[segmentID_loc].LocalType} , expected ushort");


            int index = 0;
            // callvirt instance bool NetInfo/Node::CheckFlags(Flags)
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckFlags), index, counter: occurance);
            Extensions.Assert(index != 0, "index!=0");

            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID");
            int counter_GetSegment = 1;
            if (method.Name == "PopulateGroupData")
                counter_GetSegment = 2;
            CodeInstruction LDLoc_segmentID = BuildSegnentLDLocFromPrevSTLoc(codes, index, counter: counter_GetSegment);

            { // replace node.CheckFlags(flags) with CheckFlags2(node,flags,nodeID, segmentID)
                var newInstructions = new[]{
                    LDArg_NodeID, // ldarg nodeID | push nodeID into the stack
                    LDLoc_segmentID, // ldloc.s segmentID | push segmentID into the stack
                    new CodeInstruction(OpCodes.Call, mCheckFlags2), // call Material mCheckFlags2(node,flags, nodeID, segmentID).
                    };
                ReplaceInstructions(codes, newInstructions, index);
            } // end block
        } // end method

        public static CodeInstruction BuildSegnentLDLocFromPrevSTLoc(List<CodeInstruction> codes, int index, int counter=1) {
            Extensions.Assert(mGetSegment != null, "mGetSegment!=null");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, mGetSegment), index, counter: counter, dir: -1);

            var code = codes[index + 1];
            Extensions.Assert(IsStLoc(code), $"IsStLoc(code) | code={code}");

            return BuildLdLocFromStLoc(code);
        }



    }
}
