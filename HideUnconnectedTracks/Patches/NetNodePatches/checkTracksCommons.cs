using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace HideUnconnectedTracks.Patches {
    using System;
    using UnityEngine;
    using Utils;
    using static TranspilerUtils;
    public static class CheckTracksCommons {
        public static bool ShouldConnectTracks(
        ushort nodeId,
        ref RenderManager.Instance data,
        ref NetInfo.Node nodeInfo,
        ref Vector4 dataVector0) {
            ushort sourceSegmentID = nodeId.ToNode().GetSegment(data.m_dataInt0 & 7);
            int targetSegmentIDX = data.m_dataInt0 >> 4;
            ushort targetSegmentID = nodeId.ToNode().GetSegment(targetSegmentIDX);
            if (TMPEUTILS.exists) {
                try {
                    bool ret = DirectConnectUtil.DetermineDirectConnect(
                        sourceSegmentID,
                        targetSegmentID,
                        nodeId,
                        ref nodeInfo,
                        out bool fipMesh);
                    if (fipMesh) {
                        dataVector0.x = -dataVector0.x;
                        dataVector0.y = -dataVector0.y;
                    }
                    return ret;
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
        static FieldInfo fNodes => typeof(NetInfo).GetField("m_nodes") ?? throw new Exception("fNodes is null");
        static FieldInfo fDataVector0 => typeof(RenderManager.Instance).GetField(nameof(RenderManager.Instance.m_dataVector0)) ?? throw new Exception("fDataVector0 is null");

        public static void ApplyCheckTracks(List<CodeInstruction> codes, MethodInfo method, int occurance) {
            /* TODO update this comment
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
            int index = 0;
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Callvirt, mCheckRenderDistance), index, counter: occurance);
            Extensions.Assert(index != 0, "index!=0");
            CodeInstruction LDLocA_NodeInfo = Build_LDLocA_NodeInfo(codes, index, counter: 1, dir: -1);
            CodeInstruction LDLocA_DataVector0 = Build_LDLocA_DataVector0(codes, index, counter: 1, dir: -1);

            //seek to <ldarg.s cameraInfo> instruction:
            index = SearchInstruction(codes, GetLDArg(method, "cameraInfo"), index, counter: occurance, dir: -1);

            Label ContinueIndex = GetContinueLabel(codes, index, dir: -1); // IL_029d: br IL_0570
            {
                var newInstructions = new[]{
                    GetLDArg(method, "nodeID"),
                    GetLDArg(method, "data"),
                    LDLocA_NodeInfo,
                    LDLocA_DataVector0,
                    new CodeInstruction(OpCodes.Call, mShouldConnectTracks),
                    new CodeInstruction(OpCodes.Brfalse, ContinueIndex), // if returned value is false then continue to the next iteration of for loop;
                };

                InsertInstructions(codes, newInstructions, index, true);
            } // end block
        } // end method

        public static CodeInstruction Build_LDLocA_NodeInfo(List<CodeInstruction> codes, int index, int counter, int dir) {
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, fNodes), index, counter: counter, dir: dir);
            var code = codes[index + 3];

            Extensions.Assert(code.IsStloc(), $"IsStLoc(code) | code={code}");
            return new CodeInstruction(OpCodes.Ldloca_S, code.operand);
        }

        public static CodeInstruction Build_LDLocA_DataVector0(List<CodeInstruction> codes, int index, int counter, int dir) {
            index = SearchInstruction(codes, new CodeInstruction(OpCodes.Ldfld, fDataVector0), index, counter: counter, dir: dir);
            var code = codes[index + 1];

            Extensions.Assert(code.IsStloc(), $"IsStLoc(code) | code={code}");
            return new CodeInstruction(OpCodes.Ldloca_S, code.operand);
        }


    }
}
