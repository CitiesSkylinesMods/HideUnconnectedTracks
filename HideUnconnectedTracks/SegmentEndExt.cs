namespace HideUnconnectedTracks {
    using System.Collections.Generic;
    using Utils;
    using GenericGameBridge.Service;
    using TrafficManager.Manager.Impl;
    using TrafficManager.State;
    using ColossalFramework;
    using UnityEngine;


    internal class SegmentEnd {
        private byte[] _connections;

        internal SegmentEnd(ushort segmentId, ushort nodeId) {
            Recalculate(segmentId, nodeId);
        }

        public bool GetShouldConnect(int segmentIDX, int nodeInfoIDX) =>
            _connections[nodeInfoIDX].GetBit(segmentIDX);

        private void SetShouldConnect(int segmentIDX, int nodeInfoIDX, bool value) =>
            _connections[nodeInfoIDX].SetBit(segmentIDX, value);

        internal void Recalculate(ushort segmentId, ushort nodeID) {
            NetInfo info = segmentId.ToSegment().Info;
            _connections = _connections ?? new byte[info.m_nodes.Length];
            for (int segmentIDX = 0; segmentIDX < 8; ++segmentIDX) {
                ushort targetSegmentId = nodeID.ToNode().GetSegment(segmentIDX);
                for (int nodeInfoIDX = 0; nodeInfoIDX < info.m_nodes.Length; ++nodeInfoIDX) {
                    bool connect = ConnectionUtils.ShouldConnectTracks(
                        segmentId,
                        targetSegmentId,
                        nodeID,
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
            foreach (var startNode in new[] { false, true }) {
                int index = GetSegmentEndIndex(segmentId, startNode);
                if (!Extensions.netService.IsSegmentValid(segmentId)) {
                    SegmentEndArray[index] = null;
                } else {
                    Extensions.Assert(SegmentEndArray != null, "SegmentEndArray!=null");
                    ushort nodeId = startNode ? segmentId.ToSegment().m_startNode : segmentId.ToSegment().m_endNode;
                    if (SegmentEndArray[index] == null)
                        SegmentEndArray[index] = new SegmentEnd(segmentId, nodeId);
                    else
                        SegmentEndArray[index].Recalculate(segmentId, nodeId);
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
            OnUpdateSegment(sourceSegmentId);
            Extensions.Assert(SegmentEndArray != null, "SegmentEndArray!=null");
            Extensions.Assert(SegmentEndArray?[index] != null, "SegmentEndArray!=null");
            return SegmentEndArray[index].GetShouldConnect(targetSegmentIDX, nodeInfoIDX);
        }

        #endregion
    } // end class
} // end namespace