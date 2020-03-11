using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace HideUnconnectedTracks.Utils {
    public static class TMPEUTILS {
        public static bool exists { get; set; } = true;
        public static void Init() => exists = true;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool _HasConnections(uint sourceLaneId, bool startNode) {
            return TrafficManager.Manager.Impl.LaneConnectionManager.Instance.
                HasConnections(sourceLaneId, startNode);
        }
        public static bool HasConnections(uint sourceLaneId, bool startNode) {
            if (exists) {
                try {
                    return _HasConnections(sourceLaneId, startNode);
                }
                catch (FileNotFoundException _) {
                    Debug.Log("ERROR ****** TMPE not found! *****");
                }
                catch (Exception e) {
                    Debug.Log(e + "\n" + e.StackTrace);
                }
                exists = false;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool _AreLanesConnected(uint sourceLaneId, uint targetLaneId, bool sourceStartNode) { 
            return TrafficManager.Manager.Impl.LaneConnectionManager.Instance.
                AreLanesConnected(sourceLaneId, targetLaneId, sourceStartNode);
        }

        public static bool AreLanesConnected(uint sourceLaneId, uint targetLaneId, bool sourceStartNode) {
            if (exists) {
                try {
                    return _AreLanesConnected(sourceLaneId, targetLaneId, sourceStartNode);
                }
                catch (FileNotFoundException _) {
                    Debug.Log("ERROR ****** TMPE not found! *****");
                }
                catch (Exception e) {
                    Debug.Log(e + "\n" + e.StackTrace);
                }
                exists = false;
            }
            return true;
        }

        internal static bool GetLaneEndPoint(ushort segmentId,
                                      bool startNode,
                                      byte laneIndex,
                                      uint laneId,
                                      NetInfo.Lane laneInfo,
                                      out bool outgoing,
                                      out bool incoming,
                                      out Vector3? pos) {
            NetManager netManager = NetManager.instance;

            pos = null;
            outgoing = false;
            incoming = false;

            if ((netManager.m_segments.m_buffer[segmentId].m_flags &
                 (NetSegment.Flags.Created | NetSegment.Flags.Deleted)) != NetSegment.Flags.Created) {
                return false;
            }

            if ((netManager.m_lanes.m_buffer[(uint)laneId].m_flags &
                 ((ushort)NetLane.Flags.Created | (ushort)NetLane.Flags.Deleted)) !=
                (ushort)NetLane.Flags.Created) {
                return false;
            }

            NetInfo.Direction laneDir =
                ((NetManager.instance.m_segments.m_buffer[segmentId].m_flags &
                  NetSegment.Flags.Invert) == NetSegment.Flags.None)
                    ? laneInfo.m_finalDirection
                    : NetInfo.InvertDirection(laneInfo.m_finalDirection);

            if (startNode) {
                if ((laneDir & NetInfo.Direction.Backward) != NetInfo.Direction.None) {
                    outgoing = true;
                }

                if ((laneDir & NetInfo.Direction.Forward) != NetInfo.Direction.None) {
                    incoming = true;
                }

                pos = NetManager.instance.m_lanes.m_buffer[(uint)laneId].m_bezier.a;
            } else {
                if ((laneDir & NetInfo.Direction.Forward) != NetInfo.Direction.None) {
                    outgoing = true;
                }

                if ((laneDir & NetInfo.Direction.Backward) != NetInfo.Direction.None) {
                    incoming = true;
                }

                pos = NetManager.instance.m_lanes.m_buffer[(uint)laneId].m_bezier.d;
            }

            return true;
        }
    }
}
