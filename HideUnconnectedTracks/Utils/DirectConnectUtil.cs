 namespace HideUnconnectedTracks.Utils {
    using ColossalFramework;
    using System;
    using TrafficManager.Manager.Impl;
    using UnityEngine.Networking.Types;
    using static HideUnconnectedTracks.Utils.Extensions;

    public static class DirectConnectUtil {
        //public const VehicleInfo.VehicleType TRACK_VEHICLE_TYPES =
        //    VehicleInfo.VehicleType.Tram |
        //    VehicleInfo.VehicleType.Metro |
        //    VehicleInfo.VehicleType.Train |
        //    VehicleInfo.VehicleType.Monorail;

        public static bool HasLane(ushort segmentID, VehicleInfo.VehicleType vehicleType) =>
             (segmentID.ToSegment().Info.m_vehicleTypes & vehicleType) != 0;

        internal static VehicleInfo.VehicleType GetVehicleType(NetInfo.ConnectGroup flags) {
            VehicleInfo.VehicleType ret = 0;
            const NetInfo.ConnectGroup TRAM =
                NetInfo.ConnectGroup.CenterTram |
                NetInfo.ConnectGroup.NarrowTram |
                NetInfo.ConnectGroup.SingleTram |
                NetInfo.ConnectGroup.WideTram;
            const NetInfo.ConnectGroup TRAIN =
                NetInfo.ConnectGroup.DoubleTrain |
                NetInfo.ConnectGroup.SingleTrain |
                NetInfo.ConnectGroup.TrainStation;
            const NetInfo.ConnectGroup MONO_RAIL =
                NetInfo.ConnectGroup.DoubleMonorail |
                NetInfo.ConnectGroup.SingleMonorail |
                NetInfo.ConnectGroup.MonorailStation;
            const NetInfo.ConnectGroup METRO =
                NetInfo.ConnectGroup.DoubleMetro |
                NetInfo.ConnectGroup.SingleMetro |
                NetInfo.ConnectGroup.MetroStation;

            const NetInfo.ConnectGroup TROLLY =
                NetInfo.ConnectGroup.CenterTrolleybus |
                NetInfo.ConnectGroup.NarrowTrolleybus |
                NetInfo.ConnectGroup.SingleTrolleybus |
                NetInfo.ConnectGroup.WideTrolleybus;

            if ((flags & TRAM) != 0) {
                ret |= VehicleInfo.VehicleType.Tram;
                ret |= VehicleInfo.VehicleType.Metro; // MOM
            }
            if ((flags & METRO) != 0) {
                ret |= VehicleInfo.VehicleType.Metro;
            }
            if ((flags & TRAIN) != 0) {
                ret |= VehicleInfo.VehicleType.Train;
            }
            if ((flags & MONO_RAIL) != 0) {
                ret |= VehicleInfo.VehicleType.Monorail;
            }
            if ((flags & TROLLY) != 0) {
                ret |= VehicleInfo.VehicleType.Trolleybus;
            }
            return ret;
        }

        /// <summary>
        /// Checks if any lanes from source segment can go to target segment.
        /// Precondition: assuming that the segments can have connected lanes.
        /// </summary>
        /// <param name="sourceSegmentId"></param>
        /// <param name="targetSegmentId"></param>
        /// <param name="nodeId"></param>
        /// <param name="laneType"></param>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        internal static bool HasDirectConnect(
            ushort segmentId1,
            ushort segmentId2,
            ushort nodeId,
            int nodeInfoIDX) {
            NetInfo.Node nodeInfo = segmentId1.ToSegment().Info.m_nodes[nodeInfoIDX];
            VehicleInfo.VehicleType vehicleType = GetVehicleType(nodeInfo.m_connectGroup);
            if (!HasLane(segmentId1, vehicleType)) // vehicleType == 0 is also checked here
                return true; 
            return HasDirectConnect(
                segmentId1,
                segmentId2,
                nodeId,
                NetInfo.LaneType.All,
                vehicleType);
        }

        /// <summary>
        /// Checks if any lanes from source segment can go to target segment.
        /// Precondition: assuming that the segments can have connected lanes.
        /// </summary>
        /// <param name="sourceSegmentId"></param>
        /// <param name="targetSegmentId"></param>
        /// <param name="nodeId"></param>
        /// <param name="laneType"></param>
        /// <param name="vehicleType"></param>
        /// <returns></returns>
        internal static bool HasDirectConnect(
            ushort sourceSegmentId,
            ushort targetSegmentId,
            ushort nodeId,
            NetInfo.LaneType laneType,
            VehicleInfo.VehicleType vehicleType) {
            bool sourceStartNode = (bool)netService.IsStartNode(sourceSegmentId, nodeId);
            var sourceLaneInfos = sourceSegmentId.ToSegment().Info.m_lanes;
            int nSource = sourceLaneInfos.Length;

            var targetLaneInfos = targetSegmentId.ToSegment().Info.m_lanes;
            int nTarget = targetLaneInfos.Length;

            uint sourceLaneId, targetLaneId;
            int sourceLaneIndex, targetLaneIndex;
            for (sourceLaneIndex = 0, sourceLaneId = sourceSegmentId.ToSegment().m_lanes;
                sourceLaneIndex < nSource;
                ++sourceLaneIndex, sourceLaneId = sourceLaneId.ToLane().m_nextLane) {
                //Extensions.Log($"sourceLaneId={sourceLaneId} {sourceLaneInfos[sourceLaneIndex].m_laneType} & {laneType} = {sourceLaneInfos[sourceLaneIndex].m_laneType & laneType}\n" +
                //    $"{sourceLaneInfos[sourceLaneIndex].m_vehicleType} & {vehicleType} = {sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType}");

                if ((sourceLaneInfos[sourceLaneIndex].m_laneType & laneType) == 0 ||
                    (sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType) == 0) {
                    continue;
                }
                //Extensions.Log($"POINT A> ");
                for (targetLaneIndex = 0, targetLaneId = targetSegmentId.ToSegment().m_lanes;
                    targetLaneIndex < nTarget;
                    ++targetLaneIndex, targetLaneId = targetLaneId.ToLane().m_nextLane) {
                    //Extensions.Log($"targetLaneId={targetLaneId} {targetLaneInfos[targetLaneIndex].m_laneType} & {laneType} = {targetLaneInfos[targetLaneIndex].m_laneType & laneType}\n" +
                    //    $"{targetLaneInfos[targetLaneIndex].m_vehicleType} & {vehicleType} = {targetLaneInfos[targetLaneIndex].m_vehicleType & vehicleType}");
                    if ((targetLaneInfos[targetLaneIndex].m_laneType & laneType) == 0 ||
                        (targetLaneInfos[targetLaneIndex].m_vehicleType & vehicleType) == 0) {
                        continue;
                    }

                    bool connected = AreLanesConnected(
                        sourceSegmentId, sourceLaneId, (byte)sourceLaneIndex,
                        targetSegmentId, targetLaneId, (byte)targetLaneIndex,
                        nodeId);

                    //Log($"sourceLaneId={sourceLaneId} targetLaneId={targetLaneId} sourceStartNode={sourceStartNode} connected={connected}");
                    if (connected) {
                        return true;
                    }

                }
            }
            return false;
        }

        static bool AreLanesConnected(
            ushort segmentId1, uint laneId1, byte laneIndex1,
            ushort segmentId2, uint laneId2, byte laneIndex2,
            ushort nodeId) {
            bool startNode1 = (bool)netService.IsStartNode(segmentId1, nodeId);
            bool startNode2 = (bool)netService.IsStartNode(segmentId2, nodeId);
            TMPEUTILS.GetLaneEndPoint(
                segmentId1,
                startNode1,
                laneIndex1,
                laneId1,
                segmentId1.ToSegment().Info.m_lanes[laneIndex1],
                out bool isSource1,
                out bool isTarget1,
                out _);
            TMPEUTILS.GetLaneEndPoint(
                segmentId2,
                startNode2,
                laneIndex2,
                laneId2,
                segmentId2.ToSegment().Info.m_lanes[laneIndex2],
                out bool isSource2,
                out bool isTarget2,
                out _);

            if ((isSource1 && isTarget2)) {

                bool b1 = TMPEUTILS.HasConnections(laneId1, startNode1);
                bool b2 = TMPEUTILS.AreLanesConnected(laneId1, laneId2, startNode1);
                return !b1 || b2;
            } else if (isTarget1 && isSource2) {
                bool b1 = TMPEUTILS.HasConnections(laneId2, startNode2);
                bool b2 = TMPEUTILS.AreLanesConnected(laneId2, laneId1, startNode2);
                return !b1 || b2;
            } else {
                return false;
            }
        }

        [Flags]
        enum ConnectionT {
            None=0,
            Right = 1,
            Left = 2,
            Both=Right|Left,
        }

        internal static bool DetermineDirectConnect(
            ushort segmentId1,
            ushort segmentId2,
            ushort nodeId,
            ref NetInfo.Node nodeInfo) {
            VehicleInfo.VehicleType vehicleType = GetVehicleType(nodeInfo.m_connectGroup);
            if (!HasLane(segmentId1, vehicleType)) // vehicleType == 0 is also checked here
                return true;
            var nodeInfo2 = DetermineDirectConnect(
                nodeInfo,
                segmentId1,
                segmentId2,
                nodeId,
                NetInfo.LaneType.All,
                vehicleType);
            if (nodeInfo2 == null)
                return false;
            nodeInfo = nodeInfo2;
            return true;
        }


        /// <summary>
        /// Precondition: assuming that the segments can have connected lanes.
        /// </summary>
        internal static NetInfo.Node DetermineDirectConnect(
            NetInfo.Node nodeInfo,
            ushort sourceSegmentId,
            ushort targetSegmentId,
            ushort nodeId,
            NetInfo.LaneType laneType,
            VehicleInfo.VehicleType vehicleType) {
            NetInfo sourceInfo = sourceSegmentId.ToSegment().Info;
            NetInfo targetInfo = targetSegmentId.ToSegment().Info;
            Log._Debug("DetermineDirectConnect() called");


            if (!LaneConnectionManager.Instance.HasNodeConnections(nodeId))
                return nodeInfo;
            if (targetInfo != sourceInfo)
                return nodeInfo;
            if (!MeshTables.LUT.ContainsKey(nodeInfo))
                return nodeInfo;

            ConnectionT connections = default;

            bool sourceStartNode = (bool)netService.IsStartNode(sourceSegmentId, nodeId);
            bool targetStartNode = (bool)netService.IsStartNode(sourceSegmentId, nodeId);
            var sourceLaneInfos = sourceSegmentId.ToSegment().Info.m_lanes;
            int nSource = sourceLaneInfos.Length;

            var targetLaneInfos = targetSegmentId.ToSegment().Info.m_lanes;
            int nTarget = targetLaneInfos.Length;

            uint sourceLaneId, targetLaneId;
            int sourceLaneIndex, targetLaneIndex;
            for (sourceLaneIndex = 0, sourceLaneId = sourceSegmentId.ToSegment().m_lanes;
                sourceLaneIndex < nSource;
                ++sourceLaneIndex, sourceLaneId = sourceLaneId.ToLane().m_nextLane) {
                //Extensions.Log($"sourceLaneId={sourceLaneId} {sourceLaneInfos[sourceLaneIndex].m_laneType} & {laneType} = {sourceLaneInfos[sourceLaneIndex].m_laneType & laneType}\n" +
                //    $"{sourceLaneInfos[sourceLaneIndex].m_vehicleType} & {vehicleType} = {sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType}");

                if ((sourceLaneInfos[sourceLaneIndex].m_laneType & laneType) == 0 ||
                    (sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType) == 0) {
                    continue;
                }
                //Extensions.Log($"POINT A> ");
                for (targetLaneIndex = 0, targetLaneId = targetSegmentId.ToSegment().m_lanes;
                    targetLaneIndex < nTarget;
                    ++targetLaneIndex, targetLaneId = targetLaneId.ToLane().m_nextLane) {
                    //Extensions.Log($"targetLaneId={targetLaneId} {targetLaneInfos[targetLaneIndex].m_laneType} & {laneType} = {targetLaneInfos[targetLaneIndex].m_laneType & laneType}\n" +
                    //    $"{targetLaneInfos[targetLaneIndex].m_vehicleType} & {vehicleType} = {targetLaneInfos[targetLaneIndex].m_vehicleType & vehicleType}");
                    if ((targetLaneInfos[targetLaneIndex].m_laneType & laneType) == 0 ||
                        (targetLaneInfos[targetLaneIndex].m_vehicleType & vehicleType) == 0) {
                        continue;
                    }

                    bool connected = AreLanesConnected(
                        sourceSegmentId, sourceLaneId, (byte)sourceLaneIndex,
                        targetSegmentId, targetLaneId, (byte)targetLaneIndex,
                        nodeId);

                    //Log($"sourceLaneId={sourceLaneId} targetLaneId={targetLaneId} sourceStartNode={sourceStartNode} connected={connected}");
                    if (connected) {
                        var dir1 = sourceLaneInfos[sourceLaneIndex].m_direction;
                        if(dir1 == NetInfo.Direction.Forward)
                            connections |= ConnectionT.Right;
                        else
                            connections |= ConnectionT.Left;
                    }
                }
            }


            var table = (MeshTable)MeshTables.LUT[nodeInfo];

            Log._Debug($"DetermineDirectConnect: nodeID={nodeId} sourceSegmentID={sourceSegmentId} targetSegmentID={targetSegmentId}" +
                $"nodeInfo.m_connectGroup={nodeInfo.m_connectGroup}\n =>" +
                $"connections={connections}");

            switch (connections) {
                case ConnectionT.Left:
                    return table.OneSideStart;
                case ConnectionT.Right:
                    return table.OneSideEnd;
                case ConnectionT.None:
                    return null;
                case ConnectionT.Both:
                    return nodeInfo;
            }
            throw new Exception("unreachable code");
        }

    }
}
