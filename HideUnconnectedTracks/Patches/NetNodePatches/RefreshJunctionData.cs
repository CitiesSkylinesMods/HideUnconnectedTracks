//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Reflection.Emit;

//namespace HideUnconnectedTracks.Patches.NetNodePatches {
//    using Utils;

//    //[HarmonyPatch()]
//    public static class RefreshJunctionData {
//        static void Log(string m) => Extensions.Log("NetNode_RefreshJunctionData Transpiler: " + m);

//        //private void RefreshJunctionData(ushort nodeID, int segmentIndex, int segmentIndex2, NetInfo info, NetInfo info2, ushort nodeSegment, ushort nodeSegment2, ref uint instanceIndex, ref RenderManager.Instance data)
//        delegate void TargetDelegate(ushort nodeID, int segmentIndex, int segmentIndex2, NetInfo info, NetInfo info2, ushort nodeSegment, ushort nodeSegment2, ref uint instanceIndex, ref RenderManager.Instance data);

//        static MethodBase TargetMethod() {
//            return AccessTools.DeclaredMethod(
//                typeof(NetNode),
//                "RefreshJunctionData",
//                TranspilerUtils.GetGenericArguments<TargetDelegate>())
//                ?? throw new Exception("RefreshJunctionData failed to find TargetMethod");
//        }

//        public static bool Prefix(ushort nodeID,  ushort nodeSegment, ushort nodeSegment2, ref uint instanceIndex, ref RenderManager.Instance data) {
//            return true; //TODO cache data.
//        }
//    } // end class
//} // end name space