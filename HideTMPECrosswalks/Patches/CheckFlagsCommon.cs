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
