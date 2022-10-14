namespace HideUnconnectedTracks.Utils {
    using System;
    using System.Collections.Generic;
    using KianCommons;
    using static KianCommons.DCUtil;
    using static KianCommons.NetUtil;
    using static HideUnconnectedTracks.Utils.TMPEUtil;
    using TrafficManager.API.Traffic.Enums;
    using TrafficManager.API.Manager;

    public static class DirectConnectUtil {
        public static bool IsSegmentConnectedToSegment(
            ushort sourceSegmentId,
            ushort targetSegmentId,
            ushort nodeId,
            NetInfo.LaneType laneType,
            VehicleInfo.VehicleType vehicleType) {
            ref NetSegment sourceSegment = ref sourceSegmentId.ToSegment();
            bool sourceStartNode = sourceSegment.IsStartNode(nodeId);
            var sourceLaneInfos = sourceSegment.Info.m_lanes;
            int nSource = sourceLaneInfos.Length;

            uint sourceLaneId;
            int sourceLaneIndex;
            for (sourceLaneIndex = 0, sourceLaneId = sourceSegment.m_lanes;
                sourceLaneIndex < nSource && sourceLaneId != 0;
                ++sourceLaneIndex, sourceLaneId = sourceLaneId.ToLane().m_nextLane) {
                NetInfo.Lane sourceLaneInfo = sourceLaneInfos[sourceLaneIndex];
                if (Log.VERBOSE) Log.Debug(
                    $"sourceLaneId={sourceLaneId} {sourceLaneInfo.m_laneType} & {laneType} = {sourceLaneInfo.m_laneType & laneType}\n" +
                    $"{sourceLaneInfo.m_vehicleType} & {vehicleType} = {sourceLaneInfo.m_vehicleType & vehicleType}");

                if (sourceLaneInfo.m_laneType.IsFlagSet(laneType)||
                    sourceLaneInfo.m_vehicleType.IsFlagSet(vehicleType)) {
                    continue;
                }

                bool connected = IsLaneConnectedToSegment(sourceLaneId, sourceLaneInfo, targetSegmentId, sourceStartNode);
                if (connected) {
                    return true;
                }
            }
            return false;
        }


        public static bool IsLaneConnectedToSegment(uint sourceLaneId, NetInfo.Lane sourceLaneInfo, ushort targetSegmentID, bool startNode) {
            var transitions = GetForwardRoutings(sourceLaneId, startNode);
            if (transitions == null) return false;
            bool sourceHasTrolley = sourceLaneInfo.HasTrolley();
            var targetLaneInfos = targetSegmentID.ToSegment().Info.m_lanes;
            foreach (LaneTransitionData transition in transitions) {
                if (transition.type is LaneEndTransitionType.Invalid)
                    continue;

                if (transition.segmentId != targetSegmentID)
                    continue;

                bool trackOrTrolly = transition.HasTrack();
                if (!trackOrTrolly) {
                    trackOrTrolly =
                        sourceHasTrolley &&
                        targetLaneInfos[transition.laneIndex].HasTrolley();
                }

                if (trackOrTrolly) return true;
            }

            return false;
        }

        static bool HasTrolley(this NetInfo.Lane laneInfo) => laneInfo.m_vehicleType.IsFlagSet(VehicleInfo.VehicleType.Trolleybus);
        static bool HasTrack(this LaneTransitionData transition) => (transition.group & LaneEndTransitionGroup.Track) != 0;

        public static bool IsLaneConnectedToLane(
            uint sourceLaneId, NetInfo.Lane sourceLaneInfo, bool sourceStartNode,
            uint targetLaneId, NetInfo.Lane targetLaneInfo) {
            var transitions = GetForwardRoutings(sourceLaneId, sourceStartNode);
            if (transitions == null) return false;
            bool trolly = sourceLaneInfo.HasTrolley() && targetLaneInfo.HasTrolley();
            foreach (var transition in transitions) {
                if (transition.type is LaneEndTransitionType.Invalid)
                    continue;

                if (transition.laneId != targetLaneId)
                    continue;

                bool trackOrTrolly = transition.HasTrack() || trolly;

                if(trackOrTrolly) return true;
            }
            return false;
        }

        static bool AreLanesConnected(
            uint laneId1, NetInfo.Lane laneInfo1, bool startNode1,
            uint laneId2, NetInfo.Lane laneInfo2, bool startNode2) {
            return
                IsLaneConnectedToLane(
                    laneId1, laneInfo1, startNode1,
                    laneId2, laneInfo2) ||
                IsLaneConnectedToLane(
                    laneId2, laneInfo2, startNode2,
                    laneId1, laneInfo1);
        }

        /// <summary>
        /// Checks if any lanes from source segment can go to target segment.
        /// Precondition: assuming that the segments can have connected lanes.
        /// </summary>
        internal static bool HasDirectConnect(
            ushort segmentId1,
            ushort segmentId2,
            ushort nodeId,
            int nodeInfoIDX) {
            try {
                NetInfo.Node nodeInfo = segmentId1.ToSegment().Info.m_nodes[nodeInfoIDX];
                VehicleInfo.VehicleType vehicleType = nodeInfo.GetVehicleType();
                if (!HasLane(segmentId1, vehicleType)) { // vehicleType == 0 is also checked here
                    return true;
                }
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
                return
                    IsSegmentConnectedToSegment(sourceSegmentId, targetSegmentId, nodeId, laneType, vehicleType) ||
                    IsSegmentConnectedToSegment(targetSegmentId, sourceSegmentId, nodeId, laneType, vehicleType);
            } catch(Exception ex) { ex.Log(); }
            return false;
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
            flipMesh = false;
            try {
                VehicleInfo.VehicleType vehicleType = nodeInfo.GetVehicleType();
                if (!HasLane(segmentId1, vehicleType) // vehicleType == 0 is also checked here
                    /*.LogRet($"HasLane({segmentId1.ToSegment().Info.name}, {vehicleType})")*/) 
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
                //Log.Debug($"{nameof(DetermineDirectConnect)}(source:{sourceSegmentId} target:{targetSegmentId} node:{nodeId}) called");

                if (!LCM.HasNodeConnections(nodeId))
                    return nodeInfo;

                if (!NodeInfoLUT.LUT.ContainsKey(nodeInfo)) {
                    Log.Debug("[P1]");
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
                            sourceLane.LaneID, sourceLane.LaneInfo, sourceStartNode,
                            targetLane.LaneID, targetLane.LaneInfo, targetStartNode);
                        
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

                var table = NodeInfoLUT.LUT[nodeInfo];
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


