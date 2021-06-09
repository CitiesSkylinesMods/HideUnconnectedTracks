namespace HideUnconnectedTracks.Utils {
    using ColossalFramework;
    using KianCommons;
    using System;

    internal static class NetInfoUtil {
        /// <summary>
        /// Note: trims white spaces before comparison (both input name and asset name)
        /// </summary>
        public static NetInfo GetInfo(string name, bool throwOnError = true) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name.Trim() == name.Trim())
                    return info;
                //Helpers.Log.Debuginfo.name);
            }
            if (throwOnError)
                throw new Exception($"NetInfo {name} not found!");
            else
                Log.Debug($"Warning: NetInfo {name} not found!");
            return null;
        }

        public static bool IsSingleBidireactional(this NetInfo info, VehicleInfo.VehicleType vehicleType) {
            int count = 0;
            foreach (var lane in info.m_lanes) {
                if (!lane.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                var dir = lane.m_direction;
                if (dir.CheckFlags(NetInfo.Direction.Both))
                    count++;
                else
                    return false;
            }
            return count == 1;
        }

        public static bool IsDoubleOneWay(this NetInfo info, VehicleInfo.VehicleType vehicleType) {
            int forward = 0, backward = 0;
            foreach (var lane in info.m_lanes) {
                if (!lane.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                var dir = lane.m_direction;
                if (dir == NetInfo.Direction.Forward)
                    forward++;
                else if (dir == NetInfo.Direction.Backward)
                    backward++;
                else
                    return false;
            }
            return forward == 2 && backward == 0 || forward == 0 && backward == 2;
        }

        public static bool IsDoubleBidirectional(this NetInfo info, VehicleInfo.VehicleType vehicleType) {
            int count = 0;
            foreach (var lane in info.m_lanes) {
                if (!lane.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                var dir = lane.m_direction;
                if (dir.CheckFlags(NetInfo.Direction.Both))
                    count++;
                else
                    return false;
            }
            return count == 2;
        }

        public static bool IsSingleNormal(this NetInfo info, VehicleInfo.VehicleType vehicleType) {
            int count = 0;
            foreach (var lane in info.m_lanes) {
                if (!lane.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                var dir = lane.m_direction;
                if (dir == NetInfo.Direction.Forward || dir == NetInfo.Direction.Backward)
                    count++;
                else
                    return false;
            }
            return count == 1;
        }

        public static bool IsDoubleNormal(this NetInfo info, VehicleInfo.VehicleType vehicleType) {
            int forward = 0, backward = 0;
            foreach (var lane in info.m_lanes) {
                if (!lane.m_vehicleType.IsFlagSet(vehicleType))
                    continue;
                var dir = lane.m_direction;
                if (dir == NetInfo.Direction.Forward)
                    forward++;
                else if (dir == NetInfo.Direction.Backward)
                    backward++;
                else
                    return false;
            }
            return forward == 1 && backward == 1;
        }
    }
}
