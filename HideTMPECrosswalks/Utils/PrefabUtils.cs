using System;
using System.Collections.Generic;

namespace HideTMPECrosswalks.Utils {
    using ColossalFramework;
    using ICities;
    using System.Diagnostics;

    public static class PrefabUtils {
        internal static bool IsNormalRoad(this NetInfo info) {
            try {
                bool ret = info?.m_netAI is RoadBaseAI;
                string name = info.name;
                ret &= name != null;
                ret &=name.Trim() != "";
                ret &= !name.ToLower().Contains("toll");
                return ret;
            }
            catch (Exception e) {
                Extensions.Log(e.Message);
                Extensions.Log("IsNormalRoad catched exception");
                Extensions.Log($"exception: info = {info}");
                Extensions.Log($"exception: info is {info.GetType()}");
                Extensions.Log($"Exception: name = {info?.name} ");
                return false;
            }
        }

        internal static bool IsNormalGroundRoad(this NetInfo info) {
            bool ret = info.IsNormalRoad();
            if(ret && info?.m_netAI is RoadAI) {
                var ai = info.m_netAI as RoadAI;
                return ai.m_elevatedInfo != null && ai.m_slopeInfo != null;
            }
            return false;
        }

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

        public static bool IsNExt(this NetInfo info) {
            string c = info.m_class.name.ToLower();
            bool ret = c.StartsWith("next");
            //Extensions.Log($"IsNExt returns {ret} : {info.GetUncheckedLocalizedTitle()} : " + c);
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

        public static IEnumerable<NetInfo> Roads() {
#if !DEBUG // exclude in asset editor
            if (Extensions.currentMode == AppMode.AssetEditor)
                yield return null;
#endif
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.CanHideMarkings() ) {
                    yield return info;
                }
            }
        }

        public static void CreateNoZebraTextures() {
            TextureUtils.Init();
            foreach(var info in Roads()) {
                Extensions.Log("CreateNoZebraTextures: " + info.GetLocalizedTitle());
                try {
                    NodeInfoExt.RemoveNoZebraTexture(info);
                    NodeInfoExt.CreateNoZebraTexture(info);
                } catch (Exception e) {
                    Extensions.Log($"{info?.name ?? "null"} Failed!\n" + e);
                }
            }
            UpdateAll();
            Extensions.Log("CreateNoZebraTextures DONE!");
        }

        public static void RemoveNoZebraTextures() {
            foreach (var info in Roads()) {
                try {
                    NodeInfoExt.RemoveNoZebraTexture(info);
                }
                catch (Exception e) {
                    Extensions.Log($"{info?.name ?? "null"} Failed!\n" + e);
                }
            }
            TextureUtils.Clear();
            Extensions.Log("RemoveNoZebraTextures DONE!");
        }

        public static void UpdateAll() {
            var ticks = Stopwatch.StartNew();
            Singleton<NetManager>.instance.RebuildLods(); ticks.LogLap("RebuildLods ");
            for (ushort id = 1; id < NetManager.MAX_SEGMENT_COUNT; ++id) {
                if ((id.ToSegment().m_flags & NetSegment.Flags.Created) != 0) {
                    Singleton<NetManager>.instance.UpdateSegment(id);
                }
            }
            ticks.LogLap("Update all segments ");
        }

        public static int CountJunctionNodes(this NetInfo netInfo) {
            int ret = 0;
            foreach (var node in netInfo.m_nodes)
                if (!(node is NodeInfoExt) && node.CheckFlags(NetNode.Flags.Junction) && node.m_connectGroup == 0)
                    ret++;
            return ret;
        }

        public static bool CanHideMarkings(this NetInfo info) {
            bool ret = info.IsNormalRoad();
            ret &= info.CountJunctionNodes() == 1; // TODO handle more than 1 junction nodes.
            ret &= info.m_connectGroup == NetInfo.ConnectGroup.None;
            ret &= info.m_connectionClass == null;
            ret &= info.m_nodeConnectGroups == NetInfo.ConnectGroup.None;
            ret &= info.IsNormalGroundRoad(); // TODO support E/B/T/S
            ret &= info.category == "RoadsMedium" || info.category == "RoadsLarge"; // info.category == "RoadsHighway";
            //ret &= info.category == "RoadsMedium";
            //ret &= info.isAsym() && !info.isOneWay();
            return ret;
        }

        public static bool CanHideCrossings(this NetInfo info) {
            bool ret = info.CanHideMarkings();
            ret &= info.m_hasPedestrianLanes;
            ret &= info.m_hasForwardVehicleLanes;
            return ret;
        }
    } // end class
} // end namespace
