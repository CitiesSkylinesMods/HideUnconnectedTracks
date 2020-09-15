namespace HideUnconnectedTracks {
    using KianCommons;
    using static KianCommons.Assertion;
    using static MeshTable;
    using static NodeInfoLUT;

    public struct NodeInfoClass {
        // if there are shared connect groups for multiple tracks, then create multiple
        // bool IsTrackTrain, IsTrackMetro ... fields.
        public TrackType Track;
        public NetNode.Flags RequiredFlags;
        public NetNode.Flags ForbiddenFlags;
        public bool RequireWindSpeed;
        public int Layer;
        public bool EmptyTransparent;
        public bool RequireSurfaceMaps;

        public NodeInfoClass(NetInfo.Node nodeInfo) {
            Track = nodeInfo.m_connectGroup.GetTrackType();
            RequiredFlags = nodeInfo.m_flagsRequired;
            ForbiddenFlags = nodeInfo.m_flagsForbidden;
            RequireWindSpeed = nodeInfo.m_requireWindSpeed;
            Layer = nodeInfo.m_layer;
            EmptyTransparent = nodeInfo.m_emptyTransparent;
            RequireSurfaceMaps = nodeInfo.m_requireSurfaceMaps;
        }
    }

    public class NodeInfoFamily {
        public NodeInfoClass Type;

        public NetInfo.Node TwoWayDouble;
        public NetInfo.Node TwoWayRight;
        public NetInfo.Node TwoWayLeft;

        public NetInfo.Node OneWay;
        public NetInfo.Node OneWayEnd; //right
        public NetInfo.Node OneWayStart; //left

        public NetInfo.Node StationDouble;
        public NetInfo.Node StationSingle;
        public NetInfo.Node Station;

        public override string ToString() =>
            $"TwoWayDouble:{TwoWayDouble} TwoWayRight:{TwoWayRight} TwoWayLeft:{TwoWayLeft} | " +
            $"OneWay:{OneWay} OneWayEnd:{OneWayEnd} OneWayStart:{OneWayStart} | " +
            $"StationDouble:{StationDouble} StationSingle:{StationSingle} Station:{Station}";

        public void GenerateExtraMeshes() {
            if (TwoWayDouble == null)
                return; //wires

            NodeInfoFamily cached = MeshLUT[TwoWayDouble.m_nodeMesh];

            if (TwoWayRight == null) {
                TwoWayRight = CopyNodeInfo_shallow(TwoWayDouble);
                if (cached != null)
                    TwoWayRight.m_nodeMesh = cached.TwoWayRight.m_nodeMesh;
                else
                    TwoWayRight.m_nodeMesh = TwoWayDouble.m_nodeMesh.CutMesh2(keepLeftSide: false);
            }
            if (TwoWayLeft == null) {
                TwoWayLeft = CopyNodeInfo_shallow(TwoWayDouble);
                if (cached != null)
                    TwoWayLeft.m_nodeMesh = cached.TwoWayLeft.m_nodeMesh;
                else
                    TwoWayLeft.m_nodeMesh = TwoWayDouble.m_nodeMesh.CutMesh2(keepLeftSide: true);
            }

            MeshLUT[TwoWayDouble.m_nodeMesh] = this;
            if (StationDouble != null)
                MeshLUT[StationDouble.m_nodeMesh] = this;
            if (StationSingle != null)
                MeshLUT[StationSingle.m_nodeMesh] = this;
            if (Station != null)
                MeshLUT[Station.m_nodeMesh] = this;
        }

        public void FillInTheBlanks(NodeInfoFamily source, bool station = false) {
            TwoWayDouble = TwoWayDouble ?? source.TwoWayDouble;
            TwoWayRight = TwoWayRight ?? source.TwoWayRight;
            TwoWayLeft = TwoWayLeft ?? source.TwoWayLeft;

            OneWay = OneWay ?? source.OneWay;
            OneWayEnd = OneWayEnd ?? source.OneWayEnd;
            OneWayStart = OneWayStart ?? source.OneWayStart;

            if (station) {
                StationDouble = StationDouble ?? source.StationDouble;
                StationSingle = StationSingle ?? source.StationSingle;
                Station = Station ?? source.Station;
            }
        }

        /// <summary>
        /// precondition: all fields are filled in/generated where applicaplbe.
        /// </summary>
        public void AddStationsToLUT() {
            if (StationDouble != null && TwoWayDouble != null) {
                LUT[StationDouble] = LUT[TwoWayDouble] = this;
                if (StationSingle != null && OneWayStart != null)
                    LUT[StationSingle] = this;
                if (Station != null)
                    LUT[Station] = this;
            }
        }

        public bool IsComplete() =>
            TwoWayDouble != null && TwoWayRight != null && TwoWayLeft != null &&
            OneWay != null && OneWayEnd != null && OneWayStart != null &&
            StationDouble != null && StationSingle != null;

        public static NetInfo.Node CopyNodeInfo_shallow(NetInfo.Node nodeInfo) {
            Assert(nodeInfo != null, "nodeInfo==null");
            NetInfo.Node ret = new NetInfo.Node();
            AssemblyTypeExtensions.CopyProperties<NetInfo.Node>(ret, nodeInfo);
            Assert(nodeInfo.m_material != null, $"nodeInfo m_material is null");
            return ret;
        }

    }
}
