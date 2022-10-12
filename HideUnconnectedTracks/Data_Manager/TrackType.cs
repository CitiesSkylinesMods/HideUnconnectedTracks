namespace HideUnconnectedTracks {
    using System;
    using System.Collections.Generic;
    using ColossalFramework;
    using static KianCommons.DCUtil;

    public enum TrackType {
        None,
        Train,
        Metro,
        Monorail,
        Tram,
        Trolley,
        All,
    }

    public static class TrackTypeExtensions {
        public static NetInfo.ConnectGroup GetConnectGroups(this TrackType connectType) {
            switch (connectType) {
                case TrackType.Train: return TRAIN;
                case TrackType.Metro: return METRO;
                case TrackType.Monorail: return MONORAIL;
                case TrackType.Trolley: return TROLLEY;
                case TrackType.Tram: return TRAM;
                case TrackType.None: return NetInfo.ConnectGroup.None;
                case TrackType.All: return TRAIN | METRO | MONORAIL | TROLLEY | TRAM;
                default: throw new Exception("Unreachable code");
            }
        }

        public static TrackType GetTrackType(this NetInfo.ConnectGroup connectGroup) {
            if (connectGroup.IsFlagSet(TRAIN)) return TrackType.Train;
            if (connectGroup.IsFlagSet(METRO)) return TrackType.Metro;
            if (connectGroup.IsFlagSet(MONORAIL)) return TrackType.Monorail;
            if (connectGroup.IsFlagSet(TROLLEY)) return TrackType.Trolley;
            if (connectGroup.IsFlagSet(TRAM)) return TrackType.Tram;
            return TrackType.None;
        }

        public static IEnumerable<TrackType> GetTrackTypes(this NetInfo.ConnectGroup connectGroup) {
            List<TrackType> ret = new List<TrackType>();
            if (connectGroup.IsFlagSet(TRAIN)) ret.Add(TrackType.Train);
            if (connectGroup.IsFlagSet(METRO)) ret.Add(TrackType.Metro);
            if (connectGroup.IsFlagSet(MONORAIL)) ret.Add(TrackType.Monorail);
            if (connectGroup.IsFlagSet(TROLLEY)) ret.Add(TrackType.Trolley);
            if (connectGroup.IsFlagSet(TRAM)) ret.Add(TrackType.Tram);
            return ret;
        }
    }
}
