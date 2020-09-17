namespace HideUnconnectedTracks {
    using ColossalFramework;
    using KianCommons;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using static HideUnconnectedTracks.Utils.DirectConnectUtil;
    using static KianCommons.Assertion;

    public class TrackFamily {
        public Dictionary<NodeInfoClass, NodeInfoFamily> SubFamilyDict;
        public TrackFamily() => SubFamilyDict = new Dictionary<NodeInfoClass, NodeInfoFamily>();

        public IEnumerable<NodeInfoClass> GetTypes() => SubFamilyDict?.Keys;
        public bool Empty => SubFamilyDict.Count == 0;

        NodeInfoFamily GetOrCreateSubFamily(NodeInfoClass nodeClass) {
            if (SubFamilyDict.TryGetValue(nodeClass, out var subFamily)) {
                Log.Debug("GetOrCreateSubFamily found a match with subFamily=" + subFamily);
                return subFamily;
            }
            Log.Debug("GetOrCreateSubFamily creates new family");
            return SubFamilyDict[nodeClass] = new NodeInfoFamily { Class = nodeClass };
        }

        public bool IsHopefull() {
            bool ret = SubFamilyDict.Values.Any(item => item.TwoWayDouble != null);
            ret &= SubFamilyDict.Values.Any(item => item.OneWay != null);
            ret &= SubFamilyDict.Values.Any(item => item.OneWayEnd != null);
            ret &= SubFamilyDict.Values.Any(item => item.OneWayStart != null);
            ret &= SubFamilyDict.Values.Any(item => item.StationDouble != null);
            ret &= SubFamilyDict.Values.Any(item => item.StationSingle != null);
            return ret;
        }

        public bool IsConsistent => !SubFamilyDict.Values
            .Any(_subfamily => !_subfamily.CanComplete);

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
        public static TrackFamily CreateFamily(IEnumerable<NetInfo> infos,
            TrackType trackType = TrackType.All, int inconsistencyLevel = 0) {
            NetInfo.ConnectGroup connectGroups = trackType.GetConnectGroups();
            var trackFamily = new TrackFamily();

            foreach (var info in infos) {
                Log.Debug("CreateFamilies:info=" + info, true);
                Assert((info.m_connectGroup & connectGroups) != 0, "(info.m_connectGroup & connectGroups) != 0");

                // list of used node classes in this net info.
                HashSet<NodeInfoClassMetaData> usedList = new HashSet<NodeInfoClassMetaData>();
                for (int i = 0; i < info.m_nodes.Length; ++i) {
                    var nodeInfo = info.m_nodes[i];
                    if (!nodeInfo.m_connectGroup.IsFlagSet(connectGroups))
                        continue;
                    Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE | SINGLE | STATION), "unexpected nodeInfo.m_connectGroup=" + info.m_nodeConnectGroups);

                    int usedCount = NodeInfoClassMetaData.Count(usedList, nodeInfo);
                    NodeInfoClass nodeClass = new NodeInfoClass(nodeInfo, usedCount, inconsistencyLevel);
                    Log.Debug($"node class calculated: info.m_nodes[{i}].mesh={nodeInfo.m_nodeMesh.name} -> {nodeClass}");
                    usedList.Add(new NodeInfoClassMetaData {
                        NodeInfoClass = nodeClass,
                        ConnectGroup = nodeInfo.m_connectGroup
                    });

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
                        } else if (nodeInfo.m_connectGroup.IsFlagSet(SINGLE)) {
                            // TODO: Not sure about this. is this for connecting to bidrectional tracks
                            // as well as oneway?
                            subFamily.OneWay = nodeInfo;
                        } else {
                            //throw new Exception("unexpected oneway : " + oneway);

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

            bool inconsistent = !trackFamily.IsConsistent;
            bool hopefull = trackFamily.IsHopefull();

            var infos2 = infos.Select(info => info.name).ToArray();
            var infos3 = "{" + string.Join(", ", infos2) + "}";
            var subfamilies = trackFamily.SubFamilyDict.Values;
            var subfamily2 = subfamilies.Select(_subFamily => _subFamily.ToString());
            var subfamily3 = string.Join("\n\t", subfamily2.ToArray());
            string m = $"prefab names={infos3} sub-families=\n\t" + $"{subfamily3}";

            if (inconsistent && hopefull && inconsistencyLevel < 2) {
                int newInconsistencyLevel = inconsistencyLevel + 1;
                Log.Info($"Warning: the following family has incosnsitent nodes (ie NetInfo.Node).\n" + m +
                $"\nincreasing inconsistency level from {inconsistencyLevel} to {newInconsistencyLevel} and trying again ...");
                trackFamily = CreateFamily(infos, trackType, newInconsistencyLevel);
            } else if(inconsistent) {
                Log.Info($"WARNING: the following family is incomplete\n" + m);
            } else {
                Log.Info($"Sucessfully created tracks for:\n" + m);
            }

            return trackFamily;
        }
    }
}
