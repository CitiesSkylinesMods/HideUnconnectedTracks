using Harmony;
using ColossalFramework;
using TrafficManager.Manager.Impl;

namespace HideTMPECrosswalks.Patches {
    using Utils;
    //public bool SetPedestrianCrossingAllowed(ushort segmentId, bool startNode, bool value);
    [HarmonyPatch(typeof(JunctionRestrictionsManager), "SetPedestrianCrossingAllowed")]
    static class SetPedestrianCrossingAllowed {
        public static void Postfix(ushort segmentId, bool startNode) {
            ushort nodeID = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
            Singleton<NetManager>.instance.UpdateNode(nodeID);
        }

    }
}
