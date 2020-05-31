using System;

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
        public const NetInfo.ConnectGroup DOUBLE =
            NetInfo.ConnectGroup.DoubleMetro | NetInfo.ConnectGroup.DoubleMonorail | NetInfo.ConnectGroup.DoubleTrain;

        public const NetInfo.ConnectGroup SINGLE =
            NetInfo.ConnectGroup.SingleMetro | NetInfo.ConnectGroup.SingleMonorail | NetInfo.ConnectGroup.SingleTrain;

        public const NetInfo.ConnectGroup STATION =
            NetInfo.ConnectGroup.MetroStation | NetInfo.ConnectGroup.MonorailStation | NetInfo.ConnectGroup.TrainStation;

        public static Hashtable LUT = new Hashtable();
        public static NodeInfoFamily VanillaTrainTracks;
        public static NodeInfoFamily VanillaTrainWires;

        public static void CreateFamilies(List<NetInfo> infos, ConnectType connectType,
            out NodeInfoFamily tracks, out NodeInfoFamily wires) {
            var connectGroups = connectType.GetConnectGroups();
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
            wires.GenerateExtraMeshes();
            tracks.GenerateExtraMeshes();
        }

        public static void GenerateVanillaTrainLUT() {
            NetInfo DoubleTrack = GetInfo("Train Track");
            NetInfo OnewayTrack = GetInfo("Train Oneway Track");
            NetInfo StationTrack = GetInfo("Train Station Track");
            NetInfo CargoStationTrack = GetInfo("Carg Train Station Track"); // TODO support this.
            var infos = new List<NetInfo>(new[] { DoubleTrack, OnewayTrack, StationTrack });
            CreateFamilies(infos, ConnectType.Train, out var tracks, out var wires);
            LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
            LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = LUT[wires.StationSingle] = wires;
        }


        public static NetInfo GetInfo(string name) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log(info.name);
            }
            throw new Exception("NetInfo not found!");
        }
    }


}
