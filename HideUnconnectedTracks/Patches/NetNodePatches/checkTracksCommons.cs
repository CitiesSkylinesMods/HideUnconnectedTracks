using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HideUnconnectedTracks.Patches {
    using System;
    using Utils;
    using static TranspilerUtils;
    public static class CheckTracksCommons {
        public static bool ShouldConnectTracks(
        ushort nodeId,
        ref RenderManager.Instance data,
        ref NetInfo.Node nodeInfo) {
            ushort sourceSegmentID = nodeId.ToNode().GetSegment(data.m_dataInt0 & 7);
            int targetSegmentIDX = data.m_dataInt0 >> 4;
            ushort targetSegmentID = nodeId.ToNode().GetSegment(targetSegmentIDX);
            if (TMPEUTILS.exists) {
                try {
                    return DirectConnectUtil.DetermineDirectConnect(
                        sourceSegmentID,
                        targetSegmentID,
                        nodeId,
                        ref nodeInfo);
                }
                catch (Exception e) {
                    Log.Error(e.Message);
                    TMPEUTILS.exists = false;
                    throw;
                }
            }
            return true;
        }

        static MethodInfo mShouldConnectTracks => typeof(CheckTracksCommons).GetMethod("ShouldConnectTracks") ?? throw new Exception("mShouldConnectTracks is null");
        static MethodInfo mCheckRenderDistance => typeof(RenderManager.CameraInfo).GetMethod("CheckRenderDistance") ?? throw new Exception("mCheckRenderDistance is null");
        static FieldInfo f_m_nodes => typeof(NetInfo).GetField("m_nodes");

        public static void ApplyCheckTracks(List<CodeInstruction> codes, MethodInfo method, int occurance) {
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

            CodeInstruction LDArg_NodeID = GetLDArg(method, "nodeID");
            CodeInstruction LDArg_data = GetLDArg(method, "data");

            int index = 0;
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckRenderDistance), index, counter: occurance);
            Extensions.Assert(index != 0, "index!=0");
            CodeInstruction LDLoc_NodeInfoIDX = Search_LDLoc_NodeInfoIDX(codes, index, counter: 1, dir: -1);
            CodeInstruction LDLocA_NodeInfo = Build_LDLocA_NodeInfo(codes, index, counter: 1, dir: -1);

            //seek to <ldarg.s cameraInfo> instruction:
            index = SearchInstruction(codes, GetLDArg(method, "cameraInfo"), index, counter: occurance, dir: -1);

            Label ContinueIndex = GetContinueLabel(codes, index, dir: -1); // IL_029d: br IL_0570
            {
                var newInstructions = new[]{
                    LDArg_NodeID,
                    LDArg_data,
                    LDLocA_NodeInfo,
                    new CodeInstruction(OpCodes.Call, mShouldConnectTracks),
                    new CodeInstruction(OpCodes.Brfalse, ContinueIndex), // if returned value is false then continue to the next iteration of for loop;
                };

                InsertInstructions(codes, newInstructions, index, true);
            } // end block
        } // end method

        public static CodeInstruction Search_LDLoc_NodeInfoIDX(List<CodeInstruction> codes, int index, int counter, int dir) {
            Extensions.Assert(f_m_nodes != null, "f_m_nodes!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, f_m_nodes), index, counter: counter, dir: dir);

            var code = codes[index + 1];
            Extensions.Assert(IsLdLoc(code), $"IsLdLoc(code) | code={code}");
            return code;

        }

        public static CodeInstruction Build_LDLocA_NodeInfo(List<CodeInstruction> codes, int index, int counter, int dir) {
            Extensions.Assert(f_m_nodes != null, "f_m_nodes!=null failed");
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, f_m_nodes), index, counter: counter, dir: dir);
            var code = codes[index + 3];

            Extensions.Assert(code.IsStloc(), $"IsStLoc(code) | code={code}");
            return new CodeInstruction(OpCodes.Ldloca_S, code.operand);
        }

    }
}
