namespace DecompiledSources {
    using ColossalFramework;
    using HideUnconnectedTracks.Utils; using KianCommons;
    using System;
    using UnityEngine;
    using static HideUnconnectedTracks.Utils.Extensions;

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
        private void RenderInstance(RenderManager.CameraInfo cameraInfo, ushort nodeID, NetInfo info1, int iter, NetNode.Flags flags, ref uint instanceIndex, ref RenderManager.Instance data) {
            ref NetNode This = ref nodeID.ToNode();
            if (data.m_dirty) {
                data.m_dirty = false;
                if (iter == 0) {
                    if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None) {
                        this.RefreshJunctionData(nodeID, info1, instanceIndex);
                    } else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None) {
                        this.RefreshBendData(nodeID, info1, instanceIndex, ref data);
                    } else if ((flags & NetNode.Flags.End) != NetNode.Flags.None) {
                        this.RefreshEndData(nodeID, info1, instanceIndex, ref data);
                    }
                }
            }
            if (data.m_initialized) {
                if ((flags & NetNode.Flags.Junction) != NetNode.Flags.None) {
                    if ((data.m_dataInt0 & 8) != 0) {
                        ushort segmentID1 = This.GetSegment(data.m_dataInt0 & 7);
                        ushort segmentID2 = This.GetSegment(data.m_dataInt0 >> 4);
                        if (segmentID1 != 0 && segmentID2 != 0) {
                            NetManager netMan = Singleton<NetManager>.instance;
                            info1 = netMan.m_segments.m_buffer[(int)segmentID1].Info;
                            NetInfo info2 = netMan.m_segments.m_buffer[(int)segmentID2].Info;
                            NetNode.Flags flags2 = flags;
                            if (((netMan.m_segments.m_buffer[(int)segmentID1].m_flags | netMan.m_segments.m_buffer[(int)segmentID2].m_flags) & NetSegment.Flags.Collapsed) != NetSegment.Flags.None) {
                                flags2 |= NetNode.Flags.Collapsed;
                            }
                            for (int i = 0; i < info1.m_nodes.Length; i++) {
                                NetInfo.Node node = info1.m_nodes[i];
                                if (node.CheckFlags(flags2) && node.m_directConnect &&
                                    (node.m_connectGroup == NetInfo.ConnectGroup.None ||
                                    (node.m_connectGroup & info2.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None)) {
                                    Vector4 m_dataVector3 = data.m_dataVector3;
                                    Vector4 m_dataVector0 = data.m_dataVector0;
                                    if (node.m_requireWindSpeed) {
                                        m_dataVector3.w = data.m_dataFloat0;
                                    }
                                    if ((node.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None) {
                                        bool startNode1 = (bool)IsStartNode(segmentID1, nodeID);
                                        bool invert1 = segmentID1.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
                                        bool onewayEnd1 = !startNode1 == invert1; //tail in RHT
                                        bool startNode2 = (bool)IsStartNode(segmentID2, nodeID);
                                        bool invert2 = segmentID2.ToSegment().m_flags.IsFlagSet(NetSegment.Flags.Invert);
                                        bool flag2 = startNode2 == !invert2; //tail in RHT
                                        bool oneway2 = info2.m_hasBackwardVehicleLanes != info2.m_hasForwardVehicleLanes;
                                        bool directional = node.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.Directional);

                                        if (oneway2 || directional) {
                                            if (onewayEnd1 == flag2) goto IL_570; // if <- -> then no connection.
                                        }
                                        if (onewayEnd1) {
                                            if (node.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.OnewayStart)) goto IL_570;
                                        } else {
                                            if (node.m_connectGroup.IsFlagSet(NetInfo.ConnectGroup.OnewayEnd)) goto IL_570;
                                            m_dataVector0.x = -m_dataVector0.x;
                                            m_dataVector0.y = -m_dataVector0.y;
                                        }
                                    }
                                    if (cameraInfo.CheckRenderDistance(data.m_position, node.m_lodRenderDistance)) {
                                        netMan.m_materialBlock.Clear();
                                        netMan.m_materialBlock.SetMatrix((int)netMan.ID_LeftMatrix, data.m_dataMatrix0);
                                        netMan.m_materialBlock.SetMatrix((int)netMan.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                        netMan.m_materialBlock.SetVector((int)netMan.ID_MeshScale, m_dataVector0);
                                        netMan.m_materialBlock.SetVector((int)netMan.ID_ObjectIndex, m_dataVector3);
                                        netMan.m_materialBlock.SetColor((int)netMan.ID_Color, data.m_dataColor0);
                                        if (node.m_requireSurfaceMaps && data.m_dataTexture1 != null) {
                                            netMan.m_materialBlock.SetTexture((int)netMan.ID_SurfaceTexA, data.m_dataTexture0);
                                            netMan.m_materialBlock.SetTexture((int)netMan.ID_SurfaceTexB, data.m_dataTexture1);
                                            netMan.m_materialBlock.SetVector((int)netMan.ID_SurfaceMapping, data.m_dataVector1);
                                        }
                                        netMan.m_drawCallData.m_defaultCalls = netMan.m_drawCallData.m_defaultCalls + 1;
                                        Graphics.DrawMesh(node.m_nodeMesh, data.m_position, data.m_rotation, node.m_nodeMaterial, node.m_layer, null, 0, netMan.m_materialBlock);
                                    } else {
                                        NetInfo.LodValue combinedLod = node.m_combinedLod;
                                        if (combinedLod != null) {
                                            if (node.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod.m_surfaceTexA) {
                                                if (combinedLod.m_lodCount != 0) {
                                                    NetSegment.RenderLod(cameraInfo, combinedLod);
                                                }
                                                combinedLod.m_surfaceTexA = data.m_dataTexture0;
                                                combinedLod.m_surfaceTexB = data.m_dataTexture1;
                                                combinedLod.m_surfaceMapping = data.m_dataVector1;
                                            }
                                            combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.m_dataMatrix0;
                                            combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                            combinedLod.m_meshScales[combinedLod.m_lodCount] = m_dataVector0;
                                            combinedLod.m_objectIndices[combinedLod.m_lodCount] = m_dataVector3;
                                            combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.m_position;
                                            combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.m_position);
                                            combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.m_position);
                                            if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length) {
                                                NetSegment.RenderLod(cameraInfo, combinedLod);
                                            }
                                        }
                                    }
                                }
                            IL_570:;
                            }
                        }
                    } else {
                        ushort segment3 = This.GetSegment(data.m_dataInt0 & 7);
                        if (segment3 != 0) {
                            NetManager instance2 = Singleton<NetManager>.instance;
                            info1 = instance2.m_segments.m_buffer[(int)segment3].Info;
                            for (int j = 0; j < info1.m_nodes.Length; j++) {
                                NetInfo.Node node2 = info1.m_nodes[j];
                                if (node2.CheckFlags(flags) && !node2.m_directConnect) {
                                    Vector4 dataVector3 = data.m_extraData.m_dataVector4;
                                    if (node2.m_requireWindSpeed) {
                                        dataVector3.w = data.m_dataFloat0;
                                    }
                                    if (cameraInfo.CheckRenderDistance(data.m_position, node2.m_lodRenderDistance)) {
                                        instance2.m_materialBlock.Clear();
                                        instance2.m_materialBlock.SetMatrix(instance2.ID_LeftMatrix, data.m_dataMatrix0);
                                        instance2.m_materialBlock.SetMatrix(instance2.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                        instance2.m_materialBlock.SetMatrix(instance2.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                                        instance2.m_materialBlock.SetMatrix(instance2.ID_RightMatrixB, data.m_dataMatrix1);
                                        instance2.m_materialBlock.SetVector(instance2.ID_MeshScale, data.m_dataVector0);
                                        instance2.m_materialBlock.SetVector(instance2.ID_CenterPos, data.m_dataVector1);
                                        instance2.m_materialBlock.SetVector(instance2.ID_SideScale, data.m_dataVector2);
                                        instance2.m_materialBlock.SetVector(instance2.ID_ObjectIndex, dataVector3);
                                        instance2.m_materialBlock.SetColor(instance2.ID_Color, data.m_dataColor0);
                                        if (node2.m_requireSurfaceMaps && data.m_dataTexture1 != null) {
                                            instance2.m_materialBlock.SetTexture(instance2.ID_SurfaceTexA, data.m_dataTexture0);
                                            instance2.m_materialBlock.SetTexture(instance2.ID_SurfaceTexB, data.m_dataTexture1);
                                            instance2.m_materialBlock.SetVector(instance2.ID_SurfaceMapping, data.m_dataVector3);
                                        }
                                        NetManager netManager2 = instance2;
                                        netManager2.m_drawCallData.m_defaultCalls = netManager2.m_drawCallData.m_defaultCalls + 1;
                                        Graphics.DrawMesh(node2.m_nodeMesh, data.m_position, data.m_rotation, node2.m_nodeMaterial, node2.m_layer, null, 0, instance2.m_materialBlock);
                                    } else {
                                        NetInfo.LodValue combinedLod2 = node2.m_combinedLod;
                                        if (combinedLod2 != null) {
                                            if (node2.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod2.m_surfaceTexA) {
                                                if (combinedLod2.m_lodCount != 0) {
                                                    NetNode.RenderLod(cameraInfo, combinedLod2);
                                                }
                                                combinedLod2.m_surfaceTexA = data.m_dataTexture0;
                                                combinedLod2.m_surfaceTexB = data.m_dataTexture1;
                                                combinedLod2.m_surfaceMapping = data.m_dataVector3;
                                            }
                                            combinedLod2.m_leftMatrices[combinedLod2.m_lodCount] = data.m_dataMatrix0;
                                            combinedLod2.m_leftMatricesB[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix3;
                                            combinedLod2.m_rightMatrices[combinedLod2.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                            combinedLod2.m_rightMatricesB[combinedLod2.m_lodCount] = data.m_dataMatrix1;
                                            combinedLod2.m_meshScales[combinedLod2.m_lodCount] = data.m_dataVector0;
                                            combinedLod2.m_centerPositions[combinedLod2.m_lodCount] = data.m_dataVector1;
                                            combinedLod2.m_sideScales[combinedLod2.m_lodCount] = data.m_dataVector2;
                                            combinedLod2.m_objectIndices[combinedLod2.m_lodCount] = dataVector3;
                                            combinedLod2.m_meshLocations[combinedLod2.m_lodCount] = data.m_position;
                                            combinedLod2.m_lodMin = Vector3.Min(combinedLod2.m_lodMin, data.m_position);
                                            combinedLod2.m_lodMax = Vector3.Max(combinedLod2.m_lodMax, data.m_position);
                                            if (++combinedLod2.m_lodCount == combinedLod2.m_leftMatrices.Length) {
                                                NetNode.RenderLod(cameraInfo, combinedLod2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                } else if ((flags & NetNode.Flags.End) != NetNode.Flags.None) {
                    NetManager instance3 = Singleton<NetManager>.instance;
                    for (int k = 0; k < info1.m_nodes.Length; k++) {
                        NetInfo.Node node3 = info1.m_nodes[k];
                        if (node3.CheckFlags(flags) && !node3.m_directConnect) {
                            Vector4 dataVector4 = data.m_extraData.m_dataVector4;
                            if (node3.m_requireWindSpeed) {
                                dataVector4.w = data.m_dataFloat0;
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, node3.m_lodRenderDistance)) {
                                instance3.m_materialBlock.Clear();
                                instance3.m_materialBlock.SetMatrix(instance3.ID_LeftMatrix, data.m_dataMatrix0);
                                instance3.m_materialBlock.SetMatrix(instance3.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance3.m_materialBlock.SetMatrix(instance3.ID_LeftMatrixB, data.m_extraData.m_dataMatrix3);
                                instance3.m_materialBlock.SetMatrix(instance3.ID_RightMatrixB, data.m_dataMatrix1);
                                instance3.m_materialBlock.SetVector(instance3.ID_MeshScale, data.m_dataVector0);
                                instance3.m_materialBlock.SetVector(instance3.ID_CenterPos, data.m_dataVector1);
                                instance3.m_materialBlock.SetVector(instance3.ID_SideScale, data.m_dataVector2);
                                instance3.m_materialBlock.SetVector(instance3.ID_ObjectIndex, dataVector4);
                                instance3.m_materialBlock.SetColor(instance3.ID_Color, data.m_dataColor0);
                                if (node3.m_requireSurfaceMaps && data.m_dataTexture1 != null) {
                                    instance3.m_materialBlock.SetTexture(instance3.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance3.m_materialBlock.SetTexture(instance3.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance3.m_materialBlock.SetVector(instance3.ID_SurfaceMapping, data.m_dataVector3);
                                }
                                NetManager netManager3 = instance3;
                                netManager3.m_drawCallData.m_defaultCalls = netManager3.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(node3.m_nodeMesh, data.m_position, data.m_rotation, node3.m_nodeMaterial, node3.m_layer, null, 0, instance3.m_materialBlock);
                            } else {
                                NetInfo.LodValue combinedLod3 = node3.m_combinedLod;
                                if (combinedLod3 != null) {
                                    if (node3.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod3.m_surfaceTexA) {
                                        if (combinedLod3.m_lodCount != 0) {
                                            NetNode.RenderLod(cameraInfo, combinedLod3);
                                        }
                                        combinedLod3.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod3.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod3.m_surfaceMapping = data.m_dataVector3;
                                    }
                                    combinedLod3.m_leftMatrices[combinedLod3.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod3.m_leftMatricesB[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix3;
                                    combinedLod3.m_rightMatrices[combinedLod3.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod3.m_rightMatricesB[combinedLod3.m_lodCount] = data.m_dataMatrix1;
                                    combinedLod3.m_meshScales[combinedLod3.m_lodCount] = data.m_dataVector0;
                                    combinedLod3.m_centerPositions[combinedLod3.m_lodCount] = data.m_dataVector1;
                                    combinedLod3.m_sideScales[combinedLod3.m_lodCount] = data.m_dataVector2;
                                    combinedLod3.m_objectIndices[combinedLod3.m_lodCount] = dataVector4;
                                    combinedLod3.m_meshLocations[combinedLod3.m_lodCount] = data.m_position;
                                    combinedLod3.m_lodMin = Vector3.Min(combinedLod3.m_lodMin, data.m_position);
                                    combinedLod3.m_lodMax = Vector3.Max(combinedLod3.m_lodMax, data.m_position);
                                    if (++combinedLod3.m_lodCount == combinedLod3.m_leftMatrices.Length) {
                                        NetNode.RenderLod(cameraInfo, combinedLod3);
                                    }
                                }
                            }
                        }
                    }
                } else if ((flags & NetNode.Flags.Bend) != NetNode.Flags.None) {
                    NetManager instance4 = Singleton<NetManager>.instance;
                    for (int l = 0; l < info1.m_segments.Length; l++) {
                        NetInfo.Segment segment4 = info1.m_segments[l];
                        bool flag3;
                        if (segment4.CheckFlags(info1.m_netAI.GetBendFlags(nodeID, ref This), out flag3) && !segment4.m_disableBendNodes) {
                            Vector4 dataVector5 = data.m_dataVector3;
                            Vector4 dataVector6 = data.m_dataVector0;
                            if (segment4.m_requireWindSpeed) {
                                dataVector5.w = data.m_dataFloat0;
                            }
                            if (flag3) {
                                dataVector6.x = -dataVector6.x;
                                dataVector6.y = -dataVector6.y;
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, segment4.m_lodRenderDistance)) {
                                instance4.m_materialBlock.Clear();
                                instance4.m_materialBlock.SetMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                                instance4.m_materialBlock.SetMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance4.m_materialBlock.SetVector(instance4.ID_MeshScale, dataVector6);
                                instance4.m_materialBlock.SetVector(instance4.ID_ObjectIndex, dataVector5);
                                instance4.m_materialBlock.SetColor(instance4.ID_Color, data.m_dataColor0);
                                if (segment4.m_requireSurfaceMaps && data.m_dataTexture1 != null) {
                                    instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance4.m_materialBlock.SetVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                                }
                                NetManager netManager4 = instance4;
                                netManager4.m_drawCallData.m_defaultCalls = netManager4.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(segment4.m_segmentMesh, data.m_position, data.m_rotation, segment4.m_segmentMaterial, segment4.m_layer, null, 0, instance4.m_materialBlock);
                            } else {
                                NetInfo.LodValue combinedLod4 = segment4.m_combinedLod;
                                if (combinedLod4 != null) {
                                    if (segment4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod4.m_surfaceTexA) {
                                        if (combinedLod4.m_lodCount != 0) {
                                            NetSegment.RenderLod(cameraInfo, combinedLod4);
                                        }
                                        combinedLod4.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod4.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod4.m_surfaceMapping = data.m_dataVector1;
                                    }
                                    combinedLod4.m_leftMatrices[combinedLod4.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod4.m_rightMatrices[combinedLod4.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod4.m_meshScales[combinedLod4.m_lodCount] = dataVector6;
                                    combinedLod4.m_objectIndices[combinedLod4.m_lodCount] = dataVector5;
                                    combinedLod4.m_meshLocations[combinedLod4.m_lodCount] = data.m_position;
                                    combinedLod4.m_lodMin = Vector3.Min(combinedLod4.m_lodMin, data.m_position);
                                    combinedLod4.m_lodMax = Vector3.Max(combinedLod4.m_lodMax, data.m_position);
                                    if (++combinedLod4.m_lodCount == combinedLod4.m_leftMatrices.Length) {
                                        NetSegment.RenderLod(cameraInfo, combinedLod4);
                                    }
                                }
                            }
                        }
                    }
                    for (int m = 0; m < info1.m_nodes.Length; m++) {
                        ushort segment5 = this.GetSegment(data.m_dataInt0 & 7);
                        ushort segment6 = this.GetSegment(data.m_dataInt0 >> 4);
                        if (((instance4.m_segments.m_buffer[(int)segment5].m_flags | instance4.m_segments.m_buffer[(int)segment6].m_flags) & NetSegment.Flags.Collapsed) != NetSegment.Flags.None) {
                            NetNode.Flags flags3 = flags | NetNode.Flags.Collapsed;
                        }
                        NetInfo.Node node4 = info1.m_nodes[m];
                        if (node4.CheckFlags(flags) && node4.m_directConnect && (node4.m_connectGroup == NetInfo.ConnectGroup.None || (node4.m_connectGroup & info1.m_connectGroup & NetInfo.ConnectGroup.AllGroups) != NetInfo.ConnectGroup.None)) {
                            Vector4 dataVector7 = data.m_dataVector3;
                            Vector4 dataVector8 = data.m_dataVector0;
                            if (node4.m_requireWindSpeed) {
                                dataVector7.w = data.m_dataFloat0;
                            }
                            if ((node4.m_connectGroup & NetInfo.ConnectGroup.Oneway) != NetInfo.ConnectGroup.None) {
                                bool flag4 = instance4.m_segments.m_buffer[(int)segment5].m_startNode == nodeID == ((instance4.m_segments.m_buffer[(int)segment5].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                bool flag5 = instance4.m_segments.m_buffer[(int)segment6].m_startNode == nodeID == ((instance4.m_segments.m_buffer[(int)segment6].m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.None);
                                if (flag4 == flag5) {
                                    goto IL_1637;
                                }
                                if (flag4) {
                                    if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayStart) == NetInfo.ConnectGroup.None) {
                                        goto IL_1637;
                                    }
                                } else {
                                    if ((node4.m_connectGroup & NetInfo.ConnectGroup.OnewayEnd) == NetInfo.ConnectGroup.None) {
                                        goto IL_1637;
                                    }
                                    dataVector8.x = -dataVector8.x;
                                    dataVector8.y = -dataVector8.y;
                                }
                            }
                            if (cameraInfo.CheckRenderDistance(data.m_position, node4.m_lodRenderDistance)) {
                                instance4.m_materialBlock.Clear();
                                instance4.m_materialBlock.SetMatrix(instance4.ID_LeftMatrix, data.m_dataMatrix0);
                                instance4.m_materialBlock.SetMatrix(instance4.ID_RightMatrix, data.m_extraData.m_dataMatrix2);
                                instance4.m_materialBlock.SetVector(instance4.ID_MeshScale, dataVector8);
                                instance4.m_materialBlock.SetVector(instance4.ID_ObjectIndex, dataVector7);
                                instance4.m_materialBlock.SetColor(instance4.ID_Color, data.m_dataColor0);
                                if (node4.m_requireSurfaceMaps && data.m_dataTexture1 != null) {
                                    instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexA, data.m_dataTexture0);
                                    instance4.m_materialBlock.SetTexture(instance4.ID_SurfaceTexB, data.m_dataTexture1);
                                    instance4.m_materialBlock.SetVector(instance4.ID_SurfaceMapping, data.m_dataVector1);
                                }
                                NetManager netManager5 = instance4;
                                netManager5.m_drawCallData.m_defaultCalls = netManager5.m_drawCallData.m_defaultCalls + 1;
                                Graphics.DrawMesh(node4.m_nodeMesh, data.m_position, data.m_rotation, node4.m_nodeMaterial, node4.m_layer, null, 0, instance4.m_materialBlock);
                            } else {
                                NetInfo.LodValue combinedLod5 = node4.m_combinedLod;
                                if (combinedLod5 != null) {
                                    if (node4.m_requireSurfaceMaps && data.m_dataTexture0 != combinedLod5.m_surfaceTexA) {
                                        if (combinedLod5.m_lodCount != 0) {
                                            NetSegment.RenderLod(cameraInfo, combinedLod5);
                                        }
                                        combinedLod5.m_surfaceTexA = data.m_dataTexture0;
                                        combinedLod5.m_surfaceTexB = data.m_dataTexture1;
                                        combinedLod5.m_surfaceMapping = data.m_dataVector1;
                                    }
                                    combinedLod5.m_leftMatrices[combinedLod5.m_lodCount] = data.m_dataMatrix0;
                                    combinedLod5.m_rightMatrices[combinedLod5.m_lodCount] = data.m_extraData.m_dataMatrix2;
                                    combinedLod5.m_meshScales[combinedLod5.m_lodCount] = dataVector8;
                                    combinedLod5.m_objectIndices[combinedLod5.m_lodCount] = dataVector7;
                                    combinedLod5.m_meshLocations[combinedLod5.m_lodCount] = data.m_position;
                                    combinedLod5.m_lodMin = Vector3.Min(combinedLod5.m_lodMin, data.m_position);
                                    combinedLod5.m_lodMax = Vector3.Max(combinedLod5.m_lodMax, data.m_position);
                                    if (++combinedLod5.m_lodCount == combinedLod5.m_leftMatrices.Length) {
                                        NetSegment.RenderLod(cameraInfo, combinedLod5);
                                    }
                                }
                            }
                        }
                    IL_1637:;
                    }
                }
            }
            instanceIndex = (uint)data.m_nextInstance;
        }

    }
}
