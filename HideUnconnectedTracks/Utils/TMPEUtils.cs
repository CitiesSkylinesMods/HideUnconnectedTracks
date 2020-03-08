using System;
using System.IO;
using UnityEngine;

namespace HideUnconnectedTracks.Utils {
    public static class TMPEUTILS {
        static bool exists = true;

        public static bool HasCrossingBan(ushort segmentID, ushort nodeID) {
            bool bStartNode = nodeID == segmentID.ToSegment().m_startNode;
            return HasCrossingBan(segmentID, bStartNode);
        }

        public static bool HasCrossingBan(ushort segmentID, bool bStartNode) {
            if (!exists)
                return false;
            try {
                return _HasCrossingBan(segmentID, bStartNode);
            }
            catch (FileNotFoundException _) {
                Debug.Log("ERROR ****** TMPE not found! *****");
                exists = false;
            }
            catch (Exception e) {
                Debug.Log(e + "\n" + e.StackTrace);
                exists = false;
            }
            return false;
        }

        private static bool _HasCrossingBan(ushort segmentID, bool bStartNode) {
            CSUtil.Commons.TernaryBool b = TrafficManager.Manager.Impl.JunctionRestrictionsManager.Instance.GetPedestrianCrossingAllowed(segmentID, bStartNode);
            return b == CSUtil.Commons.TernaryBool.False;
        }
    }
}
