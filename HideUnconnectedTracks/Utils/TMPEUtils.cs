using ColossalFramework.Math;
using KianCommons;
using KianCommons.Plugins;
using System.Linq;
using TrafficManager.API.Manager;
using UnityEngine;

namespace HideUnconnectedTracks.Utils {
    public static class TMPEUTILS {
        public static bool exists { get; set; } = true;
        public static void Init() => exists = PluginUtil.GetTrafficManager().IsActive();

        static IRoutingManager RMan => TrafficManager.Constants.ManagerFactory.RoutingManager;

        public static bool IsLaneConnectedTo(uint sourceLaneID, uint targetLaneID, ushort nodeID) {
            return GetForwardRoutings(sourceLaneID, nodeID).Any(item => item.laneId == targetLaneID);
        }
        public static bool AreLanesConnected(uint laneID1, uint laneID2, ushort nodeId) {
            ref var lane = ref laneID1.ToLane();
            ref var lane2 = ref laneID2.ToLane();
            return
                IsLaneConnectedTo(laneID1, laneID2, nodeId) &&
                IsLaneConnectedTo(laneID1, laneID2, nodeId);
        }

        public static LaneTransitionData[] GetForwardRoutings(uint laneID, ushort nodeID) {
            bool startNode = laneID.ToLane().m_segment.ToSegment().IsStartNode(nodeID);
            uint routingIndex = RMan.GetLaneEndRoutingIndex(laneID, startNode);
            return RMan.LaneEndForwardRoutings[routingIndex].transitions ?? new LaneTransitionData[0];
        }
    }
}
