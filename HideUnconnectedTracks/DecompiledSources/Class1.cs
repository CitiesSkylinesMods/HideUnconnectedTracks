using ColossalFramework;
using HideUnconnectedTracks.Utils;
using System;
using UnityEngine;

public class NetNode_ {
    private void RefreshJunctionData(ushort nodeID, NetInfo info, uint instanceIndex) {
        ref NetNode This = ref Singleton<NetManager>.instance.m_nodes.m_buffer[nodeID];
        NetManager netMan = Singleton<NetManager>.instance;
        Vector3 centerPos = This.m_position;
        for (int i1 = 0; i1 < 8; i1++) {
            ushort segmentID1 = This.GetSegment(i1);
            if (segmentID1 != 0) {
                NetInfo info1 = netMan.m_segments.m_buffer[(int)segmentID1].Info;
                ItemClass connectionClass = info1.GetConnectionClass();
                Vector3 dir1 = segmentID1.ToSegment().GetDirection(nodeID);
                float max_dot = -1f;
                for (int i2 = 0; i2 < 8; i2++) {
                    ushort segmentID2 = This.GetSegment(i2);
                    if (segmentID2 != 0 && segmentID2 != segmentID1) {
                        NetInfo info2 = netMan.m_segments.m_buffer[(int)segmentID2].Info;
                        ItemClass connectionClass2 = info2.GetConnectionClass();
                        if (connectionClass.m_service == connectionClass2.m_service || (info1.m_nodeConnectGroups & info2.m_connectGroup) != NetInfo.ConnectGroup.None || (info2.m_nodeConnectGroups & info1.m_connectGroup) != NetInfo.ConnectGroup.None) {
                            Vector3 dir2 = segmentID2.ToSegment().GetDirection(nodeID);
                            float turnAngleCos = dir1.x * dir2.x + dir1.z * dir2.z;
                            if (connectionClass.m_service == connectionClass2.m_service) {
                                max_dot = Mathf.Max(max_dot, turnAngleCos); // the smaller the angle the bigger the dot product
                            }
                            if (i2 > i1) {
                                // CanDirectConnecti = m_requireDirectRenderers and ( iether Does not have connect group or has matching connect group)
                                bool CanDirectConnect1 = info1.m_requireDirectRenderers && (info1.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info1.m_nodeConnectGroups & info2.m_connectGroup) != NetInfo.ConnectGroup.None);
                                bool CanDirectConnect2 = info2.m_requireDirectRenderers && (info2.m_nodeConnectGroups == NetInfo.ConnectGroup.None || (info2.m_nodeConnectGroups & info1.m_connectGroup) != NetInfo.ConnectGroup.None);
                                if (CanDirectConnect1 || CanDirectConnect2) {
                                    float maxTurnAngleCos = 0.01f - Mathf.Min(info1.m_maxTurnAngleCos, info2.m_maxTurnAngleCos);
                                    if (turnAngleCos < maxTurnAngleCos && instanceIndex != ushort.MaxValue) {
                                        // prioirties are basde on halfwdith
                                        float prioirty1;
                                        if (CanDirectConnect1) {
                                            prioirty1 = info1.m_netAI.GetNodeInfoPriority(segmentID1, ref netMan.m_segments.m_buffer[(int)segmentID1]);
                                        } else {
                                            prioirty1 = -1E+08f;
                                        }
                                        float prioirty2;
                                        if (CanDirectConnect2) {
                                            prioirty2 = info2.m_netAI.GetNodeInfoPriority(segmentID2, ref netMan.m_segments.m_buffer[(int)segmentID2]);
                                        } else {
                                            prioirty2 = -1E+08f;
                                        }
                                        if (prioirty1 >= prioirty2) {
                                            This.RefreshJunctionData(nodeID, i1, i2, info1, info2, segmentID1, segmentID2, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                                        } else {
                                            This.RefreshJunctionData(nodeID, i2, i1, info2, info1, segmentID2, segmentID1, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (info1.m_requireSegmentRenderers) {
                    centerPos += dir1 * Mathf.Max(2f + max_dot * 2f, info1.m_minCornerOffset * 0.4f);
                }
            }
        }
        centerPos.y = This.m_position.y + (float)This.m_heightOffset * 0.015625f;
        if (info.m_requireSegmentRenderers) {
            for (int k = 0; k < 8; k++) {
                ushort segment3 = This.GetSegment(k);
                if (segment3 != 0 && instanceIndex != 65535u) {
                    This.RefreshJunctionData(nodeID, k, segment3, centerPos, ref instanceIndex, ref Singleton<RenderManager>.instance.m_instances[(int)((UIntPtr)instanceIndex)]);
                }
            }
        }
    }
}
