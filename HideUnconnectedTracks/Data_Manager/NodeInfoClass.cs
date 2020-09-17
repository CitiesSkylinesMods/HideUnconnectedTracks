namespace HideUnconnectedTracks {
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// like NodeInfoClass but with metadata that can be used to calculated used count.
    /// </summary>
    public struct NodeInfoClassMetaData {
        public NodeInfoClass NodeInfoClass;
        public NetInfo.ConnectGroup ConnectGroup;

        /// <summary>
        /// find a match with NetInfo.Node
        /// this happens if NodeInfoClass matches ignoring used index and elements excluded by inconsitency level
        /// but including NodeInfoClassMetaData.ConnectGroup.
        /// </summary>
        public bool Matches(NetInfo.Node nodeInfo) {
            NodeInfoClass nodeInfoClass2 = new NodeInfoClass(
                nodeInfo,
                NodeInfoClass.UsedIndex, // ignore used index
                NodeInfoClass.InconsistencyLevel // ignore excluded
                );
            return ConnectGroup == nodeInfo.m_connectGroup &&
                NodeInfoClass.Equals(nodeInfoClass2); 
        }

        public static int Count(IEnumerable<NodeInfoClassMetaData> nodeClasses, NetInfo.Node nodeInfo) {
            return nodeClasses.Count(nodeClasses => nodeClasses.Matches(nodeInfo));
        }
    }


    public struct NodeInfoClass {
        public int InconsistencyLevel;

        /// <summary>
        /// increments once per node info in the same NetInfo.
        /// </summary>
        public int UsedIndex;

        ///<summary> if there are shared connect groups for multiple tracks, then create multiple
        /// bool IsTrackTrain, IsTrackMetro ... fields.
        /// </summary>
        public TrackType Track;

        public bool RequireWindSpeed;

        public NetNode.Flags RequiredFlags;
        public NetNode.Flags ForbiddenFlags;

        public int Layer;
        public bool EmptyTransparent;
        public bool RequireSurfaceMaps;

        /// <param name="consistencyLevel">0=consistent 1=inconsistent</param>
        public NodeInfoClass(NetInfo.Node nodeInfo, int usedIndex, int inconsistencyLevel) {
            InconsistencyLevel = inconsistencyLevel;

            UsedIndex = usedIndex;
            Track = nodeInfo.m_connectGroup.GetTrackType();
            RequireWindSpeed = nodeInfo.m_requireWindSpeed;

            RequiredFlags = default;
            ForbiddenFlags = default;
            Layer = -1;
            EmptyTransparent = default;
            RequireSurfaceMaps = default;

            if (inconsistencyLevel <= 1)
                RequiredFlags = nodeInfo.m_flagsRequired;

            if (inconsistencyLevel == 0) {
                ForbiddenFlags = nodeInfo.m_flagsForbidden;

                Layer = nodeInfo.m_layer;
                EmptyTransparent = nodeInfo.m_emptyTransparent;
                RequireSurfaceMaps = nodeInfo.m_requireSurfaceMaps;
            }
        }

        public override string ToString() {
            var ret = $"NodeInfoClass(UsedIndex={UsedIndex}|Track={Track}|Wire={RequireWindSpeed}";
            if (Layer != -1)
                ret += "|layer=" + Layer;
            if (RequiredFlags != default)
                ret += "|RequiredFlags=" + RequiredFlags;
            if (ForbiddenFlags != default)
                ret += "|ForbiddenFlags=" + ForbiddenFlags;

            if (RequireSurfaceMaps)
                ret += "|SurfaceMaps";
            if (EmptyTransparent)
                ret += "|EmptyTransparent";

            return ret + ")";
        }

    }
}