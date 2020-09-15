namespace HideUnconnectedTracks {
    using System;
    using System.Collections.Generic;
    using static HideUnconnectedTracks.Utils.DirectConnectUtil;
    using ColossalFramework;

    public enum TrackType {
        None,
        Train,
        Metro,
        Monorail,
        Tram,
        Trolly,
        All,
    }

    public static class TrackTypeExtensions {
        public static NetInfo.ConnectGroup GetConnectGroups(this TrackType connectType) {
            switch (connectType) {
                case TrackType.Train: return TRAIN;
                case TrackType.Metro: return METRO;
                case TrackType.Monorail: return MONORAIL;
                case TrackType.Trolly: return TROLLY;
                case TrackType.Tram: return TRAM;
                case TrackType.None: return NetInfo.ConnectGroup.None;
                case TrackType.All: return TRAIN | METRO | MONORAIL | TROLLY | TRAM;
                default: throw new Exception("Unreachable code");
            }
        }

        public static TrackType GetTrackType(this NetInfo.ConnectGroup connectGroup) {
            if (connectGroup.IsFlagSet(TRAIN)) return TrackType.Train;
            if (connectGroup.IsFlagSet(METRO)) return TrackType.Metro;
            if (connectGroup.IsFlagSet(MONORAIL)) return TrackType.Monorail;
            if (connectGroup.IsFlagSet(TROLLY)) return TrackType.Trolly;
            if (connectGroup.IsFlagSet(TRAM)) return TrackType.Tram;
            return TrackType.None;
        }

        public static IEnumerable<TrackType> GetTrackTypes(this NetInfo.ConnectGroup connectGroup) {
            List<TrackType> ret = new List<TrackType>();
            if (connectGroup.IsFlagSet(TRAIN)) ret.Add(TrackType.Train);
            if (connectGroup.IsFlagSet(METRO)) ret.Add(TrackType.Metro);
            if (connectGroup.IsFlagSet(MONORAIL)) ret.Add(TrackType.Monorail);
            if (connectGroup.IsFlagSet(TROLLY)) ret.Add(TrackType.Trolly);
            if (connectGroup.IsFlagSet(TRAM)) ret.Add(TrackType.Tram);
            return ret;
        }
    }
}
