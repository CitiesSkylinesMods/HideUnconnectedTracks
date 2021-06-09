namespace HideUnconnectedTracks.Utils {
    using ColossalFramework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TrafficManager.Manager.Impl;
    using KianCommons;
    using static KianCommons.DCUtil;
    using static KianCommons.NetUtil;

    public static class DirectConnectUtil {
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
            try {
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
            } catch (Exception ex) {
                ex.Log();
                return false;
            }
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
            try {
                bool sourceStartNode = IsStartNode(sourceSegmentId, nodeId);
                var sourceLaneInfos = sourceSegmentId.ToSegment().Info.m_lanes;
                int nSource = sourceLaneInfos.Length;

                var targetLaneInfos = targetSegmentId.ToSegment().Info.m_lanes;
                int nTarget = targetLaneInfos.Length;

                uint sourceLaneId, targetLaneId;
                int sourceLaneIndex, targetLaneIndex;
                for (sourceLaneIndex = 0, sourceLaneId = sourceSegmentId.ToSegment().m_lanes;
                    sourceLaneIndex < nSource;
                    ++sourceLaneIndex, sourceLaneId = sourceLaneId.ToLane().m_nextLane) {
                    //Log.Debug($"sourceLaneId={sourceLaneId} {sourceLaneInfos[sourceLaneIndex].m_laneType} & {laneType} = {sourceLaneInfos[sourceLaneIndex].m_laneType & laneType}\n" +
                    //    $"{sourceLaneInfos[sourceLaneIndex].m_vehicleType} & {vehicleType} = {sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType}");

                    if ((sourceLaneInfos[sourceLaneIndex].m_laneType & laneType) == 0 ||
                        (sourceLaneInfos[sourceLaneIndex].m_vehicleType & vehicleType) == 0) {
                        continue;
                    }
                    //Log.Debug($"POINT A> ");
                    for (targetLaneIndex = 0, targetLaneId = targetSegmentId.ToSegment().m_lanes;
                        targetLaneIndex < nTarget;
                        ++targetLaneIndex, targetLaneId = targetLaneId.ToLane().m_nextLane) {
                        //Log.Debug($"targetLaneId={targetLaneId} {targetLaneInfos[targetLaneIndex].m_laneType} & {laneType} = {targetLaneInfos[targetLaneIndex].m_laneType & laneType}\n" +
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
            } catch(Exception ex) { ex.Log(); }
            return false;
        }

        static bool AreLanesConnected(
            ushort segmentId1, uint laneId1, byte laneIndex1,
            ushort segmentId2, uint laneId2, byte laneIndex2,
            ushort nodeId) {
            bool startNode1 = (bool)NetUtil.IsStartNode(segmentId1, nodeId);
            bool startNode2 = (bool)NetUtil.IsStartNode(segmentId2, nodeId);
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
            None = 0,
            Right = 1,
            Left = 2,
            Both = Right | Left,

            RL = 4,
            LR = 8,
            CrissCross = RL | LR,
        }

        internal static bool DetermineDirectConnect(
            ushort segmentId1,
            ushort segmentId2,
            ushort nodeId,
            ref NetInfo.Node nodeInfo,
            out bool flipMesh) {
            try {
                flipMesh = false;
                VehicleInfo.VehicleType vehicleType = GetVehicleType(nodeInfo.m_connectGroup, nodeId.ToNode().Info);
                if (!HasLane(segmentId1, vehicleType)) // vehicleType == 0 is also checked here
                    return true; // not a track ... but a median.
                var nodeInfo2 = DetermineDirectConnect(
                    nodeInfo,
                    segmentId1,
                    segmentId2,
                    nodeId,
                    NetInfo.LaneType.All,
                    vehicleType,
                    out flipMesh);
                if (nodeInfo2 == null)
                    return false;
                nodeInfo = nodeInfo2;
                return true;
            } catch (Exception ex) {
                ex.Log();
                flipMesh = false;
                return false;
            }
            
        }

        static List<LaneData> sourceLanes_ = new List<LaneData>(2);
        static List<LaneData> targetLanes_ = new List<LaneData>(2);

        static void Repopulate(this List<LaneData> list, LaneDataIterator it) {
            list.Clear();
            while (it.MoveNext())
                list.Add(it.Current);
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
            VehicleInfo.VehicleType vehicleType,
            out bool flipMesh) {
            flipMesh = false;
            try {
                ref NetSegment sourceSegment = ref sourceSegmentId.ToSegment();
                ref NetSegment targetSegment = ref targetSegmentId.ToSegment();
                NetInfo sourceInfo = sourceSegment.Info;
                NetInfo targetInfo = targetSegment.Info;
                //Log.Debug($"DetermineDirectConnect(source:{sourceSegmentId} target:{targetSegmentId} node:{nodeId}) called");

                if (!LaneConnectionManager.Instance.HasNodeConnections(nodeId))
                    return nodeInfo;

                if (!NodeInfoLUT.LUT.ContainsKey(nodeInfo)) {
                    bool res = HasDirectConnect(
                        sourceSegmentId,
                        targetSegmentId,
                        nodeId,
                        NetInfo.LaneType.All,
                        vehicleType);
                    return res ? nodeInfo : null;
                }

                ConnectionT connections = default;

                bool sourceStartNode = sourceSegment.IsStartNode(nodeId);
                bool sourceInvert = sourceSegment.IsInvert();
                bool targetStartNode = targetSegment.IsStartNode(nodeId);
                bool targetInvert = targetSegment.IsInvert();

                var sourceLanesIterator = new LaneDataIterator(
                    sourceSegmentId,
                    laneType: laneType,
                    vehicleType: vehicleType);

                var targetLanesIterator = new LaneDataIterator(
                    targetSegmentId,
                    laneType: laneType,
                    vehicleType: vehicleType);

                int nSourceLanes = sourceLanesIterator.Count;
                int nTargetLanes = targetLanesIterator.Count;

                if (nSourceLanes <= 1 && nTargetLanes <= 1) {
                    return nodeInfo;
                } else if (nSourceLanes > 2 || nTargetLanes > 2) {
                    bool res = HasDirectConnect(
                        sourceSegmentId,
                        targetSegmentId,
                        nodeId,
                        NetInfo.LaneType.All,
                        vehicleType);
                    return res ? nodeInfo : null;
                }
                
                bool isSourceSignle = nSourceLanes == 1;
                bool isTargetSingle = nTargetLanes == 1;

                sourceLanes_.Repopulate(sourceLanesIterator);
                targetLanes_.Repopulate(targetLanesIterator);

                for (int i = 0; i < nSourceLanes; ++i) {
                    for (int j = 0; j < nTargetLanes; ++j) {
                        var sourceLane = sourceLanes_[i];
                        var targetLane = targetLanes_[j];
                        LaneData otherSourceLane, otherTargetLane;
                        if (isSourceSignle)
                            otherSourceLane = sourceLane;
                        else
                            otherSourceLane = sourceLanes_[i == 0 ? 1 : 0];
                        if (isTargetSingle)
                            otherTargetLane = targetLane;
                        else
                            otherTargetLane = targetLanes_[j == 0 ? 1 : 0]; 

                        bool connected = AreLanesConnected(
                            sourceSegmentId, sourceLane.LaneID, (byte)sourceLane.LaneIndex,
                            targetSegmentId, targetLane.LaneID, (byte)targetLane.LaneIndex,
                            nodeId);
                        
                        //Log($"sourceLaneId={sourceLaneId} targetLaneId={targetLaneId} sourceStartNode={sourceStartNode} connected={connected}");
                        if (connected) {
                            bool sourceSmallerPos = sourceLane.LaneInfo.m_position < otherSourceLane.LaneInfo.m_position;
                            bool sourceRight = sourceStartNode == !sourceInvert == sourceSmallerPos;
                            bool targetSmallerPos = targetLane.LaneInfo.m_position < otherTargetLane.LaneInfo.m_position;
                            bool targetRight = targetStartNode == !targetInvert == !targetSmallerPos;
                            //if (sourceRight != targetRight)
                            //    Log.Debug($"DetermineDirectConnect: nodeID={nodeId} sourceSegmentID={sourceSegmentId} targetSegmentID={targetSegmentId} " +
                            //        $"nodeInfo.m_connectGroup={nodeInfo.m_connectGroup} =>\n" +
                            //        $" source : {sourceRight} = {sourceStartNode} == !{sourceInvert} == {sourceSmallerPos}\n" +
                            //        $" target : {targetRight} = {targetStartNode} == !{targetInvert} == !{targetSmallerPos}");


                            if (isTargetSingle) {
                                if (sourceRight)
                                    connections |= ConnectionT.Right;
                                else
                                    connections |= ConnectionT.Left;
                            } else if (isSourceSignle) {
                                if (targetRight)
                                    connections |= ConnectionT.Right;
                                else
                                    connections |= ConnectionT.Left;
                            } else {
                                if (sourceRight & targetRight)
                                    connections |= ConnectionT.Right;
                                else if (!sourceRight & !targetRight)
                                    connections |= ConnectionT.Left;
                                else if (sourceRight & !targetRight)
                                    connections |= ConnectionT.RL;
                                else// if (!sourceFlag & targetFlag)
                                    connections |= ConnectionT.LR;
                            }
                        }
                    }
                }

                var table = (NodeInfoFamily)NodeInfoLUT.LUT[nodeInfo];
                //Log.Debug($"[P0] DetermineDirectConnect: nodeID={nodeId} sourceSegmentID={sourceSegmentId} targetSegmentID={targetSegmentId} " +
                //    $"nodeInfo.m_connectGroup={nodeInfo.m_connectGroup}\n =>" +
                //    $"connections={connections} isTargetSingle={isTargetSingle}");

                if (isTargetSingle) {
                    switch (connections) {
                        case ConnectionT.None:
                            return null;
                        case ConnectionT.Left:
                            return table.OneWayEnd;
                        case ConnectionT.Right:
                            flipMesh = true;
                            return table.OneWayStart;
                        case ConnectionT.Both:
                            return nodeInfo;
                        default:
                            throw new Exception("Unreachable code");
                    }
                }
                switch (connections) {
                    case ConnectionT.None:
                        return null;
                    case ConnectionT.Left:
                        return table.TwoWayLeft;
                    case ConnectionT.Right:
                        return table.TwoWayRight;
                    case ConnectionT.Both:
                        //Log.Debug("return table.TwoWayDouble;\n");
                        return table.TwoWayDouble;

                    default: // criss cross
                             //Log.Debug($"DetermineDirectConnect[P1]: nodeID={nodeId} sourceSegmentID={sourceSegmentId} targetSegmentID={targetSegmentId} " +
                             //    $"nodeInfo.m_connectGroup={nodeInfo.m_connectGroup}\n =>" +
                             //    $"connections={connections}");
                        return nodeInfo;
                }
            } catch (Exception ex) {
                ex.Log();
                return nodeInfo;
            }
        }
    }
}


