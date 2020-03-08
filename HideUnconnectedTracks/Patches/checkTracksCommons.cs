using Harmony;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
/* Notes
 * experiment 1: 
 */

namespace HideUnconnectedTracks.Patches {
    using Utils;
    using static TranspilerUtils;
    public static class CheckTracksCommons {
        public static bool ShouldConnectTracks(
            ushort segmentId1,
            ushort segmentId2,
            ushort nodeId,
            int nodeInfoIDX) {
            NetInfo.Node nodeInfo = segmentId1.ToSegment().Info.m_nodes[nodeInfoIDX];
            VehicleInfo.VehicleType vehicleType = ConnectionUtils.GetVehicleType(nodeInfo, nodeInfo.m_connectGroup);
            if (vehicleType == 0)
                return true;
            return ConnectionUtils.ShouldConnectTracks(
                segmentId1,
                segmentId2,
                nodeId,
                NetInfo.LaneType.All,
                vehicleType);
        }

        static MethodInfo mShouldConnectTracks => typeof(CheckTracksCommons).GetMethod("ShouldConnectTracks");
        static MethodInfo mCheckRenderDistance => typeof(RenderManager.CameraInfo).GetMethod("CheckRenderDistance");
        static MethodInfo mGetSegment => typeof(NetNode).GetMethod("GetSegment");
        static FieldInfo  f_m_nodes => typeof(NetInfo).GetField("m_nodes");

        public static void ApplyCheckTracks(List<CodeInstruction> codes, MethodInfo method, int occurance) {
            Extensions.Assert(mCheckRenderDistance != null, "mCheckRenderDistance!=null failed");
            Extensions.Assert(mShouldConnectTracks != null, "mShouldConnectTracks!=null failed");
            //Extensions.Assert(method != null, "did not manage to find original function to patch");
            //var parameters = method.GetParameters();
            //Extensions.Assert(parameters[nodeID_arg - 1].ParameterType == typeof(ushort),
            //    $"wrong nodeID_arg. parameters[{nodeID_arg - 1}] is {parameters[nodeID_arg - 1].ParameterType} , expected ushort");
            //var variables = method.GetMethodBody().LocalVariables;
            //Extensions.Assert(variables[segmentID_loc].LocalType == typeof(ushort),
            //    $"wrong segmentID_loc. LocalVariables[{segmentID_loc}] is {variables[segmentID_loc].LocalType} , expected ushort");


            int index = 0;
            /*
            --->insert here
            [164 17 - 164 95]
            IL_02c0: ldarg.1      // cameraInfo
            IL_02c1: ldarg.s      data
            IL_02c3: ldfld        valuetype [UnityEngine]UnityEngine.Vector3 RenderManager/Instance::m_position
            IL_02c8: ldloc.s      node // <--= LDLoc_NodeInfo
            IL_02ca: ldfld        float32 NetInfo/Node::m_lodRenderDistance
            IL_02cf: callvirt     instance bool RenderManager/CameraInfo::CheckRenderDistance(valuetype [UnityEngine]UnityEngine.Vector3, float32)
            IL_02d4: brfalse      IL_0405
             */
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckRenderDistance), index, counter: occurance);
            Extensions.Assert(index != 0, "index!=0");
            CodeInstruction LDLoc_NodeInfoIDX = Search_LDLoc_NodeInfoIDX(codes, index, counter:1, dir:-1);

            //seek to <ldarg.s cameraInfo> instruction:
            index = SearchInstruction(codes, GetLDArg(method, "cameraInfo"), index, counter: occurance,dir:-1); 
            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID");
            CodeInstruction LDLoc_segmentID2 = BuildSegmentLDLocFromSearchedSTLoc(codes, index, counter: 1, dir: -1);
            CodeInstruction LDLoc_segmentID1 = BuildSegmentLDLocFromSearchedSTLoc(codes, index, counter: 2, dir: -1);
            Label ContinueIndex = GetContinueLabel(codes, index, dir: -1); // IL_029d: br IL_0570
            {
                var newInstructions = new[]{
                    LDLoc_segmentID1,// push segmentID1 into the stack
                    LDLoc_segmentID2, // push segmentID2 into the stack
                    LDArg_NodeID, // push nodeID into the stack
                    LDLoc_NodeInfoIDX, // push nodeInfoIDX into stack
                    new CodeInstruction(OpCodes.Call, mShouldConnectTracks),
                    new CodeInstruction(OpCodes.Brfalse, ContinueIndex), // if returned value is false then continue to the next iteration of for loop;
                };

                InsertInstructions(codes, newInstructions, index, true);
            } // end block
        } // end method

        public static CodeInstruction BuildSegmentLDLocFromSearchedSTLoc(List<CodeInstruction> codes, int index, int counter = 1, int dir=-1) {
            Extensions.Assert(mGetSegment != null, "mGetSegment!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, mGetSegment), index, counter: counter, dir: dir);

            var code = codes[index + 1];
            Extensions.Assert(IsStLoc(code), $"IsStLoc(code) | code={code}");

            return BuildLdLocFromStLoc(code);
        }

        public static CodeInstruction Search_LDLoc_segmentIDX(List<CodeInstruction> codes, int index, int counter = 1, int dir = -1) {
            Extensions.Assert(mGetSegment != null, "mGetSegment!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Call, mGetSegment), index, counter: counter, dir: dir);

            var code = codes[index + 1];
            Extensions.Assert(IsLdLoc(code), $"IsLdLoc(code) | code={code}");
            return code;
        }


        public static CodeInstruction Search_LDLoc_NodeInfoIDX(List<CodeInstruction> codes, int index, int counter , int dir) {
            Extensions.Assert(f_m_nodes != null, "f_m_nodes!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, f_m_nodes), index, counter: counter, dir: dir);

            var code = codes[index + 1];
            Extensions.Assert(IsLdLoc(code), $"IsLdLoc(code) | code={code}");
            return code;
            
        }



    }
}
