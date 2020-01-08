using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HideTMPECrosswalks.Utils {
    using ColossalFramework;
    using Patches;
    using Settings;
    using static TextureUtils;

    public static class PrefabUtils {
        internal static bool IsNormalGroundRoad(this NetInfo info) {
            try {
                if (info != null && info.m_netAI is RoadAI) {
                    var ai = info.m_netAI as RoadAI;
                    return ai.m_elevatedInfo != null && ai.m_slopeInfo != null;
                }
                return false;
            }
            catch (Exception e) {
                Extensions.Log(e.Message);
                Extensions.Log("IsNormalGroundRoad catched exception");
                Extensions.Log($"exception: info = {info}");
                Extensions.Log($"exception: info is {info.GetType()}");
                Extensions.Log($"Exception: name = {info?.name} ");
                return false;
            }
        }

        private static string GetRoadTitle(NetInfo info) {
            Extensions.Assert(info.IsNormalGroundRoad(), $"IsNormalGroundRoad(info={info.name})");

            //TODO: use Regular expressions instead of to lower.
            string name = info.GetUncheckedLocalizedTitle().ToLower();

            List<string> postfixes = new List<string>(new[] { "with", "decorative", "grass", "trees", });
            //postfixes.Add("one-way");
            if (info.m_isCustomContent) {
                postfixes = new List<string>(new[] { "_data" });
            }

            foreach (var postfix in postfixes) {
                name = name.Replace(postfix, "");
            }

            name = name.Trim();
            return name;
        }

        public static string[] GetRoadNames() {
            List<string> ret = new List<string>();
            for (uint i = 0; i < PrefabCollection<NetInfo>.LoadedCount(); ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (IsNormalGroundRoad(info)) {
                    string name = GetRoadTitle(info);
                    if (name != null && !ret.Contains(name))
                        ret.Add(name);
                }
            }
            var ret2 = ret.ToArray();
            Array.Sort(ret2);
            return ret2;
        }

        // TODO make array.
        private static Hashtable Always_Table;
        private static Hashtable Never_Table;
        //private static bool[] Always_array;
        //private static bool[] Never_array;

        public static void CacheAlways(IEnumerable<string> roads) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            //Always_array = new bool[count];
            Always_Table = new Hashtable(count * 10);
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (IsNormalGroundRoad(info)) {
                    string name = GetRoadTitle(info);
                    bool b = (bool)roads.Contains(name);
                    RoadAI ai = info.m_netAI as RoadAI;
                    Always_Table[i] = b;
                    //Extensions.Log($"naem converted to {name} | from {info.name}");
                    Always_Table[ai.m_elevatedInfo.m_prefabDataIndex] = b;
                    Always_Table[ai.m_slopeInfo.m_prefabDataIndex] = b;
                    Always_Table[ai.m_bridgeInfo.m_prefabDataIndex] = b;
                    Always_Table[ai.m_tunnelInfo.m_prefabDataIndex] = b;
                }
            }
        }

        public static void CacheNever(IEnumerable<string> roads) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            Never_Table = new Hashtable(count * 10);
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (IsNormalGroundRoad(info)) {
                    string name = GetRoadTitle(info);
                    bool b = (bool)roads.Contains(name);
                    RoadAI ai = info.m_netAI as RoadAI;
                    Never_Table[i] = b;
                    Never_Table[ai.m_slopeInfo.m_prefabDataIndex] = b;
                    Never_Table[ai.m_elevatedInfo.m_prefabDataIndex] = b;
                    Never_Table[ai.m_bridgeInfo.m_prefabDataIndex] = b;
                    Never_Table[ai.m_tunnelInfo.m_prefabDataIndex] = b;
                }
            }
        }

        public static bool AlwaysZebra(NetInfo info) {
            try { return (bool)Always_Table[info.m_prefabDataIndex]; }
            catch {
                Extensions.Log($"AlwaysZebra:Always_array[{info.m_prefabDataIndex}] index out of range. info:{info.name}");
                return false;
            }
        }

        public static bool NeverZebra(NetInfo info) {
            try { return (bool)Never_Table[info.m_prefabDataIndex]; }
            catch {
                Extensions.Log($"NeverZebra:Never_array[{info.m_prefabDataIndex}] index out of range. info:{info.name}");
                return false;
            }
        }



        //public static void CachePrefabs() {
        //    if (MaterialCache == null) {
        //        MaterialCache = new Hashtable(100);
        //    }
        //    if (TextureCache == null) {
        //        TextureCache = new Hashtable(100);
        //    }

        //    for (ushort segmentID = 0; segmentID < NetManager.MAX_SEGMENT_COUNT; ++segmentID) {
        //        foreach (bool bStartNode in new bool[] { false, true }) {
        //            if (TMPEUTILS.HasCrossingBan(segmentID, bStartNode)) {
        //                NetSegment segment = segmentID.ToSegment();
        //                ushort nodeID = bStartNode ? segment.m_startNode : segment.m_endNode;
        //                foreach (var node in segment.Info.m_nodes) {
        //                    //cache:
        //                    Extensions.Log("Caching " + segment.Info.name);
        //                    NetNode_RenderInstance.CalculateMaterial(node.m_nodeMaterial, nodeID, segmentID);
        //                }
        //            }
        //        }
        //    }
        //    Extensions.Log("all prefabs cached");
        //}

        //public static void ClearALLCache() {
        //    //MaterialCache = null;
        //    //TextureCache = null;
        //    Always_Table = Never_Table = null;
        //    Extensions.Log("cache cleared");
        //}

        //public static NetInfo GetGroundInfo(this NetInfo info) {
        //    if (info.m_netAI is RoadAI)
        //        return info;
        //    int n = PrefabCollection<NetInfo>.LoadedCount();
        //    for (uint i = 0; i < n; ++i) {
        //        NetInfo info2 = PrefabCollection<NetInfo>.GetLoaded(i);
        //        if (IsNormalGroundRoad(info2)) {
        //            RoadAI ai = info2.m_netAI as RoadAI;
        //            bool b;
        //            b = ai.m_elevatedInfo == info;
        //            b |= ai.m_bridgeInfo == info;
        //            b |= ai.m_tunnelInfo == info;
        //            b |= ai.m_slopeInfo == info;
        //            if (b)
        //                return info2;
        //        }
        //    }
        //    return null;//in case of failure.
        //}

        public static bool isAsym(this NetInfo info) => info.m_forwardVehicleLaneCount != info.m_backwardVehicleLaneCount;
        public static bool isOneWay(this NetInfo info) => info.m_forwardVehicleLaneCount == 0 ||  info.m_backwardVehicleLaneCount == 0;

        public static bool HasMedian(this NetInfo info) {
            foreach (var lane in info.m_lanes) {
                if (lane.m_laneType == NetInfo.LaneType.None) {
                    return true;
                }
            }
            return false;
        }

        public static bool HasDecoration(this NetInfo info) {
            string title = info.GetUncheckedLocalizedTitle().ToLower();
            return title.Contains("tree") || title.Contains("grass") || title.Contains("arterial");
        }

        public static float ScaleRatio(this NetInfo info) {
            float ret = 1f;
            if (info.m_netAI is RoadAI) {
                bool b = info.HasDecoration() || info.HasMedian() || info.m_isCustomContent;
                b |= info.isAsym() && !info.isOneWay() && info.name != "AsymAvenueL2R3";
                if(!b)
                    ret = 0.91f;
                Extensions.Log(info.name + " : Scale: " + ret);
            }
            return ret;
        }

        public static bool IsNExt(NetInfo info) {
            string c = info.m_class.name.ToLower();
            bool ret = c.StartsWith("next");
            Extensions.Log($"IsNExt returns {ret} : {info.GetUncheckedLocalizedTitle()} : " + c);
            return ret;
        }

        public static bool HasSameNodeAndSegmentTextures(NetInfo info, int texID) {
            foreach (var node in info.m_nodes) {
                foreach (var seg in info.m_segments) {
                    if (node.m_material == seg.m_material) return true;
                    //Texture t1 = node.m_material.GetTexture(texID);
                    //Texture t2 = seg.m_material.GetTexture(texID);
                    //if (t1 == t2)
                    //    return true;
                }
            }
            return false;
        }

        public static void CreateNoZebraTextures() {
            TextureUtils.Init();
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (IsNormalGroundRoad(info) && CanHideCrossing(info)) {
                    if (info.GetUncheckedLocalizedTitle() == "Four-Lane Road") {
                        NodeInfoExt.RemoveNoZebraTexture(info);
                        NodeInfoExt.CreateNoZebraTexture(info);
                    }
                }
            }
            Singleton<NetManager>.instance.RebuildLods();
        }

        public static void RemoveNoZebraTextures() {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (IsNormalGroundRoad(info) && CanHideCrossing(info)) {
                    //if (info.GetUncheckedLocalizedTitle() == "Four-Lane Road")
                        NodeInfoExt.RemoveNoZebraTexture(info);
                }
            }
        }
        public static bool CanHideCrossing(NetInfo info) {
            bool ret = info.m_netAI is RoadBaseAI;
            ret &= info.m_hasPedestrianLanes;
            //ret &= info.GetUncheckedLocalizedTitle() == "Four-Lane Road";
            return ret;

            //TODO optimize
            //bool never = PrefabUtils.NeverZebra(info);
            //bool always = PrefabUtils.AlwaysZebra(info);
            //ret |= always;
            //ret &= !never;
        }


    } // end class
} // end namespace
