using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using UnityEngine;
using ColossalFramework;
using System.Reflection;

namespace HideTMPECrosswalks {
    using Utils;
    //[Serializable]
    public class NodeInfoExt : NetInfo.Node {
        public bool bHideCrossings;
        public NetInfo netInfo;
        public NodeInfoExt(NetInfo.Node template, NetInfo netInfo) {
            PrefabUtils.CopyProperties(template, this);
            this.netInfo = netInfo;
            m_mesh = AssetEditorRoadUtils.CopyMesh(m_mesh);
            m_lodMesh = AssetEditorRoadUtils.CopyMesh(m_lodMesh);
            m_material = AssetEditorRoadUtils.CopyMaterial(m_material);
            m_lodMaterial = AssetEditorRoadUtils.CopyMaterial(m_lodMaterial);
        }
        public void HideCrossings() {
            MaterialUtils.HideCrossing(m_material, netInfo);
            MaterialUtils.HideCrossing(m_lodMaterial, netInfo);
            bHideCrossings = true;
        }

        public static void CreateNoZebra(NetInfo info) {
            NetInfo.Node template = null;
            foreach (var node in info.m_nodes) {
                if (node.CheckFlags(NetNode.Flags.Junction)) {
                    if (template != null)
                        throw new NotImplementedException("Why are there two junction nodes");
                    template = node;
                    break;
                }
            }
            NodeInfoExt newNode = new NodeInfoExt(template, info);
            newNode.HideCrossings();
            AddNode(info, newNode);
            // PostConditions: 	call Singleton<NetManager>.instance.RebuildLods();
        }

        public static void AddNode(NetInfo info, NetInfo.Node newNode) {
            int n = info.m_nodes.Length;
            NetInfo.Node[] nodes2 = new NetInfo.Node[n + 1];
            for (int i = 0; i < n; ++i) {
                nodes2[i] = info.m_nodes[i];
            }
            nodes2[n] = newNode;
            info.m_nodes = nodes2;
        }

        public static bool CheckFlags2(NetInfo.Node node, bool hideCrossings) {
            bool b = node is NodeInfoExt && (node as NodeInfoExt).bHideCrossings;
            return  b == hideCrossings;
        }

        public static bool CheckFlags2(NetInfo.Node node, NetNode.Flags flags, bool hideCrossings) {
            return node.CheckFlags(flags) && CheckFlags2(node, hideCrossings);
        }


        // TODO check duplicate junction node
        // TODO check if No Crossing Node already exist.
        // TODO check serialization
    }

}
