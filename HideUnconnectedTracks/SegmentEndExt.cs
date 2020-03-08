namespace HideUnconnectedTracks {
    using System.Collections.Generic;
    using Utils;
    using GenericGameBridge.Service;
    using TrafficManager.Manager.Impl;
    using TrafficManager.State;
    using ColossalFramework;
    using UnityEngine;


    internal class SegmentEnd {
        internal readonly ushort SegmentID;
        internal readonly ushort NodeID;
        internal NetInfo Info { get; private set; }

        private byte[] _connections;

        internal SegmentEnd(ushort segmentId, ushort nodeId) {
            SegmentID = segmentId;
            NodeID = nodeId;
            Recalculate();
        }

        public bool GetShouldConnect(int segmentIDX, int nodeInfoIDX) =>
            _connections[(int)nodeInfoIDX].GetBit(segmentIDX);

        private void SetShouldConnect(int segmentIDX, int nodeInfoIDX, bool value) =>
            _connections[nodeInfoIDX].SetBit(segmentIDX, value);

        private static bool ShouldConnectTracksRaw(
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

        internal void Recalculate() {
            Info = SegmentID.ToSegment().Info;
            _connections = new byte[Info.m_nodes.Length];
            for (int segmentIDX = 0; segmentIDX < 8; ++segmentIDX) {
                ushort targetSegmentId = NodeID.ToNode().GetSegment(segmentIDX);
                for (int nodeInfoIDX = 0; nodeInfoIDX < Info.m_nodes.Length; ++nodeInfoIDX) {
                    bool connect = ShouldConnectTracksRaw(
                        SegmentID,
                        targetSegmentId,
                        NodeID,
                        nodeInfoIDX);
                    SetShouldConnect(segmentIDX, nodeInfoIDX, connect);
                }
            }
        }

        #region static
        internal static SegmentEnd[] SegmentEndArray;

        static int GetSegmentEndIndex(ushort segmentId, bool startNode) =>
            ExtSegmentEndManager.Instance.GetIndex(segmentId, startNode);

        internal static void InitSegmentEndArray() {
            SegmentEndArray =  new SegmentEnd[NetManager.MAX_SEGMENT_COUNT * 2];
            for (ushort segmentId = 0; segmentId < NetManager.MAX_SEGMENT_COUNT; ++segmentId) {
                OnUpdateSegment(segmentId);
            }
        } // end method

        public static void OnUpdateSegment(ushort segmentId) {
            var flags = segmentId.ToSegment().m_flags;
            foreach (var startNode in new[] { false, true }) {
                int index = GetSegmentEndIndex(segmentId, startNode);
                if (!Extensions.netService.IsSegmentValid(segmentId)) {
                    SegmentEndArray[index] = null;
                } else {
                    ushort nodeId = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
                    SegmentEndArray[index] = new SegmentEnd(segmentId, nodeId);
                }
            }
        }

        public static bool GetShouldConnectTracks(
            ushort sourceSegmentId,
            int targetSegmentIDX,
            ushort nodeId,
            int nodeInfoIDX) {
            bool ?startNode = (bool)Extensions.netService.IsStartNode(sourceSegmentId, nodeId);
            Extensions.Assert(startNode != null, "startNode != null");
            int index = GetSegmentEndIndex(sourceSegmentId, (bool)startNode);
            return SegmentEndArray?[index]?.GetShouldConnect(targetSegmentIDX, nodeInfoIDX) ?? true;
        }

        #endregion
    } // end class
} // end namespace