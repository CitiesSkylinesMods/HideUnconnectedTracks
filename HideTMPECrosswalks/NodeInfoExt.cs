using System;
using UnityEngine;

// TODO handle multiple junction nodes.
namespace HideTMPECrosswalks {
    using Utils;

    // [Serializable]
    public class NodeInfoExt : NetInfo.Node {
        public bool bHideCrossings;
        public NetInfo netInfo;
        public NodeInfoExt(NetInfo.Node template, NetInfo netInfo) {
            Extensions.CopyProperties<NetInfo.Node>(this, template);
            this.netInfo = netInfo;

            Extensions.Assert(template.m_material != null, $"template m_material is null netInfo=<{netInfo?.name}>");
            m_nodeMaterial = new Material(template.m_nodeMaterial);
            m_lodMaterial = new Material(template.m_lodMaterial);
            m_material = new Material(template.m_material);
        }

        public void HideCrossings() {
            var seg = netInfo.m_segments[0];
            MaterialUtils.HideCrossings(m_nodeMaterial, seg.m_segmentMaterial, netInfo, lod: false);
            MaterialUtils.HideCrossings(m_lodMaterial, seg.m_lodMaterial, netInfo, lod: true);
            //MaterialUtils.HideCrossings(m_material, seg.m_material, netInfo, lod: false);

            bHideCrossings = true;
        }

        public static bool HasNoZebraTexture(NetInfo netInfo) {
            foreach (NetInfo.Node node in netInfo.m_nodes)
                if (node is NodeInfoExt)
                    return true;
            return false;
        }

        public static void RemoveNoZebraTexture(NetInfo info) {
            //Extensions.Log($"Before len={info.m_nodes.Length}\n" + Environment.StackTrace);
            for (int i = 0; i < info.m_nodes.Length; ++i) {
                if (info.m_nodes[i] is NodeInfoExt) {
                    RemoveNode(info, i);
                    --i;
                }
            }
            //Extensions.Log($"After len={info.m_nodes.Length}\n" + Environment.StackTrace);
        }

        public static void CreateNoZebraTexture(NetInfo info) {
            //Extensions.Log($"Before len={info.m_nodes.Length}\n" + Environment.StackTrace);
            NetInfo.Node template = null;
            foreach (var node in info.m_nodes) {
                if (node.CheckFlags(NetNode.Flags.Junction) && node.m_connectGroup == 0) {
                    if (template != null)
                        throw new NotImplementedException("more than 1 junction node is not handled");
                    template = node;
                }
            }
            Extensions.Assert(template != null, "template!=null");
            NodeInfoExt newNode = new NodeInfoExt(template, info);
            newNode.HideCrossings();
            AddNode(info, newNode);
            //Extensions.Log($"After   len={info.m_nodes.Length}\n" + Environment.StackTrace);
            // PostConditions: 	call Singleton<NetManager>.instance.RebuildLods();
        }

        public static void RemoveNode(NetInfo info, int idx) {
            int n = info.m_nodes.Length;
            //Extensions.Log($"RemoveNode len={info.m_nodes.Length} idx={idx}"
            //    + Environment.StackTrace);
            Extensions.Assert(idx < n, $"idx<n {idx}<{n}");
            NetInfo.Node[] nodes2 = new NetInfo.Node[n - 1];
            for (int i = 0, i2 = 0; i < n; ++i) {
                if (i != idx) {
                    nodes2[i2] = info.m_nodes[i];
                    i2++;
                }
            }
            info.m_nodes = nodes2;
            //Extensions.Log($"DONE: RemoveNode len={info.m_nodes.Length}"
            //    + Environment.StackTrace);
        }

        public static void AddNode(NetInfo info, NetInfo.Node newNode) {
            //Extensions.Log($"AddNode len={info.m_nodes.Length}"
            //    + Environment.StackTrace);
            int n = info.m_nodes.Length;
            NetInfo.Node[] nodes2 = new NetInfo.Node[n + 1];
            for (int i = 0; i < n; ++i) {
                nodes2[i] = info.m_nodes[i];
            }
            nodes2[n] = newNode;
            info.m_nodes = nodes2;
            //Extensions.Log($"DONE! AddNode len={info.m_nodes.Length}"
            //    + Environment.StackTrace);

        }

        public static bool CheckFlags2(NetInfo.Node node, bool hideCrossings) {
            bool b = node is NodeInfoExt && (node as NodeInfoExt).bHideCrossings;
            bool ret = b == hideCrossings;
            //Extensions.Log($"ShouldHideCrossings={hideCrossings}  node is NodeInfoExt={node is NodeInfoExt} bHideCrossings={b} ret={ret}\nstack:"
            //    + Environment.StackTrace);
            return ret;
        }

        public static bool CheckFlags2(NetInfo.Node node, NetNode.Flags flags, bool hideCrossings) {
            return node.CheckFlags(flags) && CheckFlags2(node, hideCrossings);
        }
    }
}
