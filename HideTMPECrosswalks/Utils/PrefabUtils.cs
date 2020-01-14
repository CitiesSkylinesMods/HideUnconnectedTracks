using System;
using System.Collections.Generic;

namespace HideTMPECrosswalks.Utils {
    using ColossalFramework;

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
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.IsNormalGroundRoad() && info.CanHideCrossing()) {
                    if (info.category == "RoadsMedium" || info.category == "RoadsLarge") // TODO delete
                        yield return info;
                }
            }
        }

        public static void CreateNoZebraTextures() {
            TextureUtils.Init();
            foreach(var info in Roads()) {
                NodeInfoExt.RemoveNoZebraTexture(info);
                NodeInfoExt.CreateNoZebraTexture(info);
            }
            Singleton<NetManager>.instance.RebuildLods();
        }

        public static void RemoveNoZebraTextures() {
            foreach (var info in Roads()) {
                NodeInfoExt.RemoveNoZebraTexture(info);
            }
            TextureUtils.Clear();
        }

        public static bool CanHideCrossing(this NetInfo info) {
            bool ret = info.m_netAI is RoadBaseAI;
            ret &= info.m_hasPedestrianLanes;
            ret &= info.m_hasForwardVehicleLanes;
            //ret &= info.GetUncheckedLocalizedTitle() == "Four-Lane Road";
            return ret;
        }
    } // end class
} // end namespace
