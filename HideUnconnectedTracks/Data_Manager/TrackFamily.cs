namespace HideUnconnectedTracks {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ColossalFramework;
    using KianCommons;
    using HideUnconnectedTracks.Utils;
    using static KianCommons.Assertion;
    using static MeshTable;
    using static NodeInfoLUT;
    using static HideUnconnectedTracks.Utils.DirectConnectUtil;

    public class TrackFamily {
        public Dictionary<NodeInfoClass, NodeInfoFamily> SubFamilies;
        public TrackFamily() => SubFamilies = new Dictionary<NodeInfoClass, NodeInfoFamily>();

        public IEnumerable<NodeInfoClass> GetTypes() => SubFamilies?.Keys;
        public bool Empty => SubFamilies.Count == 0;

        public NodeInfoFamily GetOrCreateSubFamily(NodeInfoClass nodeClass) {
            if (SubFamilies.TryGetValue(nodeClass, out var subFamily)) {
                return subFamily;
            }
            return SubFamilies[nodeClass] = new NodeInfoFamily { Type = nodeClass };
        }


        /// <summary>
        /// Fills in tracks/wires NodeInfoFamily. does not create any new meshes.
        /// Post condition: 
        /// call wires.GenerateExtraMeshes() and  tracks.GenerateExtraMeshes() if neccessary.
        /// TODO: support tracks that have multipel wire/track node infos.
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="tracks"></param>
        /// <param name="wires"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "HAA0202:Value type to reference type conversion allocation for string concatenation", Justification = "<not performance critical>")]
        public static TrackFamily CreateFamily(IEnumerable<NetInfo> infos, TrackType trackType = TrackType.All) { 
            NetInfo.ConnectGroup connectGroups = trackType.GetConnectGroups();
            var trackFamily = new TrackFamily();

            foreach (var info in infos) {
                //Log.Debug("CreateFamilies:info=" + info, true);
                Assert((info.m_connectGroup & connectGroups) != 0, "(info.m_connectGroup & connectGroups) != 0");
                for (int i = 0; i < info.m_nodes.Length; ++i) {
                    var nodeInfo = info.m_nodes[i];
                    if (!nodeInfo.m_connectGroup.IsFlagSet(connectGroups))
                        continue;
                    //Log.Debug$"CreateFamilies:info.m_nodes[{i}]=" + nodeInfo, true);
                    Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE | SINGLE | STATION), "unexpected nodeInfo.m_connectGroup=" + info.m_nodeConnectGroups);

                    NodeInfoClass nodeClass = new NodeInfoClass(nodeInfo);
                    NodeInfoFamily subFamily = trackFamily.GetOrCreateSubFamily(nodeClass);
                    var oneway = nodeInfo.m_connectGroup & NetInfo.ConnectGroup.Oneway;
                    if (info.m_connectGroup.IsFlagSet(DOUBLE)) {
                        if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE)) {
                            Assert(oneway == 0, "(DOUBLE to DOUBLE)expected oneway=0 got " + info.m_connectGroup);
                            subFamily.TwoWayDouble = nodeInfo;
                        } else {
                            throw new Exception("(DOUBLE) unexpected nodeInfo.m_connectGroup=" + nodeInfo.m_connectGroup);
                        }
                    } else if (info.m_connectGroup.IsFlagSet(SINGLE)) {
                        if (oneway == NetInfo.ConnectGroup.OnewayStart) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE), "(OnewayStart) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            subFamily.OneWayStart = nodeInfo;
                        } else if (oneway == NetInfo.ConnectGroup.OnewayEnd) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE), "(OnewayEnd) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            subFamily.OneWayEnd = nodeInfo;
                        } else if (oneway == NetInfo.ConnectGroup.Oneway) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(SINGLE), "(Oneway) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            subFamily.OneWay = nodeInfo;
                        } else {
                            throw new Exception("unexpected oneway=None");
                        }
                    } else if (info.m_connectGroup.IsFlagSet(STATION)) {
                        Assert(oneway == 0, "(STATION) expected oneway=0 got " + info.m_connectGroup);
                        // usually station and double are the same but for metro station for some reasong this is not the case
                        if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE))
                            subFamily.StationDouble = nodeInfo;
                        else if (nodeInfo.m_connectGroup.IsFlagSet(STATION))
                            subFamily.Station = nodeInfo;
                        else //single
                            subFamily.StationSingle = nodeInfo;
                    } else {
                        throw new Exception("unexpected info.m_connectGroup=" + info.m_connectGroup);
                    }
                }// for each nodeInfo
            } //foreach info
#if DEBUG
            var infos2 = infos.Select(info => info.name).ToArray();
            var infos3 = "{" + string.Join(", ", infos2) + "}";

            var subfamily2 = trackFamily.SubFamilies.Values
                .Select(_subFamily => _subFamily.ToString());
            var subfamily3 = string.Join("\n\t", subfamily2.ToArray());
            Log.Debug($"CreateFamilies(infos={infos3})->\nsub families={subfamily3}", true);
#endif
            return trackFamily;

        }
    }
}
