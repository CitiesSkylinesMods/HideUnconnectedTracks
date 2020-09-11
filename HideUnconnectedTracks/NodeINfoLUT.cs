using System;
using static HideUnconnectedTracks.Utils.DirectConnectUtil;

namespace HideUnconnectedTracks {
    using ColossalFramework;
    using System.Collections;
    using System.Collections.Generic;
    using Utils;
    using static Utils.Extensions;
    public class NodeInfoFamily {
        public NetInfo.Node TwoWayDouble;
        public NetInfo.Node TwoWayRight;
        public NetInfo.Node TwoWayLeft;

        public NetInfo.Node OneWay;
        public NetInfo.Node OneWayEnd; //right
        public NetInfo.Node OneWayStart; //left

        public NetInfo.Node StationDouble;
        public NetInfo.Node StationSingle;

        public override string ToString() =>
            $"TwoWayDouble:{TwoWayDouble} TwoWayRight:{TwoWayRight} TwoWayLeft:{TwoWayLeft} | " +
            $"OneWay:{OneWay} OneWayEnd:{OneWayEnd} OneWayStart:{OneWayStart} | " +
            $"StationDouble:{StationDouble} StationSingle:{StationSingle}";


        public void GenerateExtraMeshes() {
            if (TwoWayRight == null) {
                TwoWayRight = CopyNodeInfo_shallow(TwoWayDouble);
                TwoWayRight.m_nodeMesh = TwoWayRight.m_nodeMesh.CutMesh(keepLeftSide: false);
            }
            if (TwoWayLeft == null) {
                TwoWayLeft = CopyNodeInfo_shallow(TwoWayDouble);
                TwoWayLeft.m_nodeMesh = TwoWayLeft.m_nodeMesh.CutMesh(keepLeftSide: true);
            }
        }

        public void FillInTheBlanks(NodeInfoFamily source) {
            TwoWayDouble = TwoWayDouble ?? source.TwoWayDouble;
            TwoWayRight = TwoWayRight ?? source.TwoWayRight;
            TwoWayLeft = TwoWayLeft ?? source.TwoWayLeft;

            OneWay = OneWay ?? source.OneWay;
            OneWayEnd = OneWayEnd ?? source.OneWayEnd;
            OneWayStart = OneWayStart ?? source.OneWayStart;

            StationDouble = StationDouble ?? source.StationDouble;
            StationSingle = StationSingle ?? source.StationSingle;
        }

        public bool IsComplete() =>
            TwoWayDouble != null && TwoWayRight != null && TwoWayLeft != null &&
            OneWay != null && OneWayEnd != null && OneWayStart != null &&
            StationDouble != null && StationSingle != null;

        public static NetInfo.Node CopyNodeInfo_shallow(NetInfo.Node nodeInfo) {
            NetInfo.Node ret = new NetInfo.Node();
            Extensions.CopyProperties<NetInfo.Node>(ret, nodeInfo);
            Assert(nodeInfo.m_material != null, $"nodeInfo m_material is null");
            return ret;
        }

    }

    public static class NodeInfoLUT {
        public static Hashtable LUT = new Hashtable();
        public static NodeInfoFamily VanillaTrainTracks;
        public static NodeInfoFamily VanillaTrainWires;

        /// <summary>
        /// Fills in tracks/wires NodeInfoFamily. does not create any new meshes.
        /// Post condition: 
        /// call wires.GenerateExtraMeshes() and  tracks.GenerateExtraMeshes() if neccessary.
        /// TODO: support tracks that have multipel wire/track node infos.
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="connectType"></param>
        /// <param name="tracks"></param>
        /// <param name="wires"></param>
        public static void CreateFamily(IEnumerable<NetInfo> infos, ConnectType connectType,
            out NodeInfoFamily tracks, out NodeInfoFamily wires) {
            NetInfo.ConnectGroup connectGroups = connectType.GetConnectGroups();
            tracks = new NodeInfoFamily();
            wires = new NodeInfoFamily();
            foreach (var info in infos) {
                Log("CreateFamilies:info=" + info, true);
                Assert((info.m_connectGroup & connectGroups) != 0, "(info.m_connectGroup & connectGroups) != 0");
                for (int i = 0; i < info.m_nodes.Length; ++i) {
                    var nodeInfo = info.m_nodes[i];
                    if (!nodeInfo.m_connectGroup.IsFlagSet(connectGroups))
                        continue;
                    Log($"CreateFamilies:info.m_nodes[{i}]=" + nodeInfo, true);
                    Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE | SINGLE | STATION), "unexpected nodeInfo.m_connectGroup=" + info.m_nodeConnectGroups);

                    NodeInfoFamily family = nodeInfo.m_requireWindSpeed ? wires : tracks;
                    var oneway = nodeInfo.m_connectGroup & NetInfo.ConnectGroup.Oneway;
                    if (info.m_connectGroup.IsFlagSet(DOUBLE)) {
                        if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE)) {
                            Assert(oneway == 0, "(DOUBLE to DOUBLE)expected oneway=0 got " + info.m_connectGroup);
                            family.TwoWayDouble = nodeInfo;
                        } else {
                            throw new Exception("(DOUBLE) unexpected nodeInfo.m_connectGroup=" + nodeInfo.m_connectGroup);
                        }
                    } else if (info.m_connectGroup.IsFlagSet(SINGLE)) {
                        if (oneway == NetInfo.ConnectGroup.OnewayStart) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE), "(OnewayStart) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            family.OneWayStart = nodeInfo;
                        } else if (oneway == NetInfo.ConnectGroup.OnewayEnd) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(DOUBLE), "(OnewayEnd) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            family.OneWayStart = nodeInfo;
                        } else if (oneway == NetInfo.ConnectGroup.Oneway) {
                            Assert(nodeInfo.m_connectGroup.IsFlagSet(SINGLE), "(Oneway) unexpected nodeInfo.m_connectGroup=" + info.m_connectGroup);
                            family.OneWay = nodeInfo;
                        } else {
                            throw new Exception("unexpected oneway=None");
                        }
                    } else if (info.m_connectGroup.IsFlagSet(STATION)) {
                        Assert(oneway == 0, "(STATION) expected oneway=0 got " + info.m_connectGroup);
                        if (nodeInfo.m_connectGroup.IsFlagSet(STATION | DOUBLE))
                            family.StationDouble = nodeInfo;
                        else //single
                            family.StationSingle = nodeInfo;
                    } else {
                        throw new Exception("unexpected info.m_connectGroup=" + info.m_connectGroup);
                    }
                }// for each nodeInfo
            } //foreach info
        }

        public static void GenerateLUTs() {
            GenerateVanillaTrainLUT();
            GenerateCustomDoubleTrackLUT(); // must be called last to avoid duplicates
        }

        public static void GenerateVanillaTrainLUT() {
            NetInfo DoubleTrack = GetInfo("Train Track");
            NetInfo OnewayTrack = GetInfo("Train Oneway Track");
            NetInfo StationTrack = GetInfo("Train Station Track");
            NetInfo CargoStationTrack = GetInfo("Train Cargo Track",false)
                ?? GetInfo("Cargo Train Station Track"); 

            var infos = new[] { DoubleTrack, OnewayTrack, StationTrack };
            CreateFamily(infos, ConnectType.Train, out var tracks, out var wires);
            tracks.GenerateExtraMeshes();
            wires.GenerateExtraMeshes();
            LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
            LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = LUT[wires.StationSingle] = wires;

            infos = new[] { CargoStationTrack };
            CreateFamily(infos, ConnectType.Train, out var cargotracks, out var cargowires);
            cargotracks.FillInTheBlanks(tracks);
            cargowires.FillInTheBlanks(wires);

            LUT[cargotracks.StationDouble] = cargotracks;
            LUT[cargowires.StationDouble] = cargowires;

            VanillaTrainTracks = tracks;
            VanillaTrainWires = wires;
        }

        public static void GenerateCustomDoubleTrackLUT() {
            int n = PrefabCollection<NetInfo>.LoadedCount();
            for(uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null) continue;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect)
                        continue;
                    if (LUT.ContainsKey(nodeInfo))
                        continue; // skip vanilla/duplicate
                    if (!nodeInfo.m_connectGroup.IsFlagSet(DOUBLE))
                        continue; 
                    if (!IsTrack(nodeInfo, info))
                        continue; // skip median

                    GenerateCustomDoubleTrackLUT(nodeInfo, info);
                }
            }
        }

        public static void GenerateCustomDoubleTrackLUT(NetInfo.Node nodeInfo, NetInfo info) {
            NodeInfoFamily family = new NodeInfoFamily { TwoWayDouble = nodeInfo };
            family.GenerateExtraMeshes();
            //Utils.Log._Debug($"GenerateCustomDoubleTrackLUT(nodeInfo={nodeInfo} info={info}): family=" + family);
            Assert(family.TwoWayDouble != null, " family=" + family);
            LUT[family.TwoWayDouble] = family;
        }

        public static NetInfo GetInfo(string name, bool throwOnError=true) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log(info.name);
            }
            if (throwOnError)
                throw new Exception($"NetInfo {name} not found!");
            else
                Log($"Warning: NetInfo {name} not found!");
            return null;
        }
    }


}
