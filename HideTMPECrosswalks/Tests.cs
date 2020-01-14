#if DEBUG
using ICities;
using UnityEngine;
using ColossalFramework;
using System;

namespace HideTMPECrosswalks {
    using Utils;
    using static DebugTests;
    using static Utils.PrefabUtils;

    public class TestOnLoad : LoadingExtensionBase {
        public override void OnCreated(ILoading loading) { base.OnCreated(loading); Test(); }
        public override void OnLevelLoaded(LoadMode mode) => Test();

        public static void Test() {
            if (!Extensions.InGame && !Extensions.InAssetEditor)
                return;

            //Extensions.Log("Testing ...");
            ////DebugTests.NameTest();
            ////DebugTests.Dumps();
            ////DebugTests.WierdNodeTest();
            ////DebugTests.UVTest();
            //_Test();
            //Extensions.Log("Testing Done!");
        }

        public static void _Test() {
            foreach (var info in Roads()) {
                Extensions.Log(info.name);
                try {
                    ChangeTextures(info);
                    //TouchTextures(info);
                }
                catch (Exception e) {
                    Extensions.Log(e.ToString());
                }
            }
            Extensions.Log("Rebuilding lods ... ");
            Singleton<NetManager>.instance.RebuildLods();
        }
    }

    public static class DebugTests {
        public static string R6L => "Six-Lane Road";
        public static string R4L => "Four-Lane Road";

        public class NodeExt : NetInfo.Node { public int extra_field = 2; }

        public static NetInfo GetInfo() {
            foreach (var info in Roads())
                if (info.GetUncheckedLocalizedTitle() == "Four-Lane Road")
                    return info;
            return null;
        }

        public static void ChangeTextures(NetInfo info) {
            Extensions.Log("MARK L0J");

            var node = info.m_nodes[0];

            {
                var node2 = new NodeExt();
                Extensions.CopyProperties<NetInfo.Node>(node2, node);
                node = node2;
            }

            info.m_nodes = new NetInfo.Node[] { node };
            //info.m_nodes[0] = node;

            node.m_nodeMaterial = new Material(node.m_nodeMaterial);
            node.m_material = new Material(node.m_material);
            node.m_lodMaterial = new Material(node.m_lodMaterial);

            var seg = info.m_segments[0];

            //MaterialUtils.HideCrossings(node.m_nodeMaterial, seg.m_segmentMaterial, info, false);
            //MaterialUtils.HideCrossings(node.m_material, seg.m_material, info, false);
            MaterialUtils.HideCrossings(node.m_lodMaterial, seg.m_lodMaterial, info, true);
        }

        public static void TouchTextures(NetInfo info) {
            Extensions.Log("MARK L7K");

            var node = info.m_nodes[0];

            {
                var node2 = new NodeExt();
                Extensions.CopyProperties<NetInfo.Node>(node2, node);
                node = node2;
            }

            info.m_nodes = new NetInfo.Node[] { node };
            //info.m_nodes[0] = node;

            node.m_nodeMaterial = new Material(node.m_nodeMaterial);
            node.m_material = new Material(node.m_material);
            node.m_lodMaterial = new Material(node.m_lodMaterial);

            //MaterialUtils.NOPMaterial(node.m_nodeMaterial, false);
            //MaterialUtils.NOPMaterial(node.m_material, false);
            MaterialUtils.NOPMaterial(node.m_lodMaterial, true);
        }

        public static void MakeSameNodeSegMat(NetInfo info) {
            var node = info.m_nodes[0];
            var seg = info.m_segments[0];
            //foreach (string texType in TextureNames.Names) {
            //    try {
            //        Texture tseg = seg.m_material.GetTexture(texType);
            //        node.m_material.SetTexture(texType, tseg);
            //        string m = $"{info.name} - {texType}> Made node tex == seg tex ";
            //        Extensions.Log(Extensions.BIG(m));
            //    }
            //    catch { }
            //}
            node.m_material = node.m_nodeMaterial = seg.m_material;
            string m = $"{info.name}> Made node material == seg material ";
            Extensions.Log(Extensions.BIG(m));
        }

        public static void WierdNodeTest() {
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (PrefabUtils.IsNormalGroundRoad(info)) {
                    if (info.GetUncheckedLocalizedTitle() == R6L) {
                        info = (info.m_netAI as RoadAI).m_elevatedInfo;
                        MakeSameNodeSegMat(info);
                    }
                }
            }
        }
        public static void NameTest() {
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info?.m_netAI is RoadAI) {
                    string name = info.GetUncheckedLocalizedTitle();
                    bool b;
                    //b = name.ToLower().Contains("asym");
                    b = true;
                    if (b) {
                        string m = name;
                        RoadAI ai = info.m_netAI as RoadAI;
                        //m += "|" + ai?.m_elevatedInfo?.name;
                        //m += "|" + ai?.m_bridgeInfo?.name;
                        //m += "|" + ai?.m_slopeInfo?.name;
                        //m += "|" + ai?.m_tunnelInfo?.name;
                        m += "|| level:" + info.GetClassLevel();
                        m += " class:" + info.m_class;
                        m += " category:" + info.category;
                        Extensions.Log(m);
                    }
                }
            }
            Extensions.Log(Extensions.BIG("DONE PRITING NAMES!"));
        }
        public static bool RoadNameEqual(string n1, string n2) => n1.Trim().ToLower() == n2.Trim().ToLower();

        public static void UVTest() {
            NetInfo info1 = null, info2 = null;
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.IsNormalGroundRoad()) {
                    string name = info.GetUncheckedLocalizedTitle().Trim();
                    bool b1 = name == "Six-Lane Road";
                    bool b2 = name.Contains("Six-Lane Road") && name.ToLower().Contains("grass");
                    if (b1) info1 = info;
                    if (b2) info2 = info;
                    if (info1 != null && info2 != null) {
                        DumpUtils.LogUVs(info1, info2);
                        break;
                    }
                }
            }

        }
    }
}

#endif
