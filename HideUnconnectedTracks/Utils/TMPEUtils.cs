namespace HideUnconnectedTracks.Utils {
    using TrafficManager.API.Manager;
    using KianCommons.Plugins;

    public static class TMPEUtil {
        public static bool Exists { get; private set; }
        public static void Init() => Exists = PluginUtil.GetTrafficManager().IsActive();

        public static IManagerFactory TMPE => TrafficManager.API.Implementations.ManagerFactory;
        public static IJunctionRestrictionsManager JRMan => TMPE?.JunctionRestrictionsManager;
        public static IRoutingManager RMan => TMPE?.RoutingManager;
        public static ILaneConnectionManager LCM => TMPE?.LaneConnectionManager;
        public static LaneTransitionData[] GetForwardRoutings(uint laneID, bool startNode) {
            uint routingIndex = RMan.GetLaneEndRoutingIndex(laneID, startNode);
            return RMan.LaneEndForwardRoutings[routingIndex].transitions;
        }
    }
}
