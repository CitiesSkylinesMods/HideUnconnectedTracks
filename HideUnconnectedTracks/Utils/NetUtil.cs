using ColossalFramework;
using System.Collections.Generic;
using System.Linq;
using System;

namespace HideUnconnectedTracks.Utils {
    public static class NetUtil {
        public static IEnumerable<LaneData> IterateLanes(
            ushort segmentId,
            bool? startNode = null,
            NetInfo.LaneType laneType = NetInfo.LaneType.All,
            VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All) {
            int idx = 0;
            if (segmentId.ToSegment().Info == null) {
                Log.Error("null info: potentially cuased by missing assets");
                yield break;
            }
            int n = segmentId.ToSegment().Info.m_lanes.Length;
            bool inverted = segmentId.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
            for (uint laneID = segmentId.ToSegment().m_lanes;
                laneID != 0 && idx < n;
                laneID = laneID.ToLane().m_nextLane, idx++) {
                var laneInfo = segmentId.ToSegment().Info.m_lanes[idx];
                bool forward = laneInfo.m_finalDirection == NetInfo.Direction.Forward;
                var ret = new LaneData {
                    LaneID = laneID,
                    LaneIndex = idx,
                    LaneInfo = laneInfo,
                    StartNode = forward ^ !inverted,
                };
                if (startNode != null && startNode != ret.StartNode)
                    continue;
                if (!ret.LaneInfo.m_laneType.IsFlagSet(laneType))
                    continue;
                if (!ret.LaneInfo.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                yield return ret;
            }
        }

        /// <summary>
        /// sorted from outer lane to inner lane when heading toward <paramref name="startNode"/>
        /// </summary>
        /// <param name="segmentId"></param>
        /// <param name="startNode"></param>
        /// <param name="laneType"></param>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        public static LaneData[] GetSortedLanes(
            ushort segmentId,
            bool? startNode,
            NetInfo.LaneType laneType = NetInfo.LaneType.All,
            VehicleInfo.VehicleType vehicleType = VehicleInfo.VehicleType.All) {
            var lanes = IterateLanes(
                segmentId: segmentId,
                startNode: startNode,
                laneType: laneType,
                vehicleType: vehicleType).ToArray();

            LaneData[] ret = new LaneData[lanes.Length];
            for (int i = 0; i < lanes.Length; ++i) {
                int j = segmentId.ToSegment().Info.m_sortedLanes[i];
                ret[i] = lanes[j];
            }

            // make sure that the outmost lane is the first lane.
            bool reverse = ret[0].LaneInfo.m_direction == NetInfo.Direction.Backward;

            if (reverse) {
                // reverse order so that the first lane is the outer lane.
                ret = ret.Reverse().ToArray();
            }
            return ret;
        }


    }

    public struct LaneData {
        public uint LaneID;
        public int LaneIndex;
        public NetInfo.Lane LaneInfo;
        public bool StartNode;
        public ushort SegmentID => LaneID.ToLane().m_segment;
        public ref NetSegment Segment => ref SegmentID.ToSegment();
        public ushort NodeID => StartNode ? Segment.m_startNode : Segment.m_endNode;
        public override string ToString() => $"LaneData:[segment:{SegmentID} node:{NodeID} lane ID:{LaneID} {LaneInfo.m_laneType} {LaneInfo.m_vehicleType}]";
    }
}
