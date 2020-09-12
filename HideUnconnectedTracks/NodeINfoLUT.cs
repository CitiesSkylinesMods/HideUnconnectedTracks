using System;
using static HideUnconnectedTracks.Utils.DirectConnectUtil;
using System.Linq;
using ObjUnity3D;
using System.IO;

namespace HideUnconnectedTracks {
    using ColossalFramework;
    using System.Collections;
    using System.Collections.Generic;
    using Utils; using KianCommons;
    using KianCommons;
    using static KianCommons.Assertion;
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
            if (TwoWayDouble == null)
                return; //wires
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
            Assert(nodeInfo != null, "nodeInfo==null");
            NetInfo.Node ret = new NetInfo.Node();
            AssemblyTypeExtensions.CopyProperties<NetInfo.Node>(ret, nodeInfo);
            Assert(nodeInfo.m_material != null, $"nodeInfo m_material is null");
            return ret;
        }

    }

    public static class NodeInfoLUT {
        public static Hashtable LUT = new Hashtable();
        public static NodeInfoFamily VanillaTrainTracks;
        public static NodeInfoFamily VanillaTrainWires;


        public static void CreateFamily(IEnumerable<NetInfo> infos, out NodeInfoFamily tracks, out NodeInfoFamily wires)
            => CreateFamily(infos, ConnectType.All, out tracks, out wires);

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
                //Log.Debug("CreateFamilies:info=" + info, true);
                Assert((info.m_connectGroup & connectGroups) != 0, "(info.m_connectGroup & connectGroups) != 0");
                for (int i = 0; i < info.m_nodes.Length; ++i) {
                    var nodeInfo = info.m_nodes[i];
                    if (!nodeInfo.m_connectGroup.IsFlagSet(connectGroups))
                        continue;
                    //Log.Debug$"CreateFamilies:info.m_nodes[{i}]=" + nodeInfo, true);
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
                            family.OneWayEnd = nodeInfo;
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
            var infos2 = infos.Select(info => info.name).ToArray();
            var infos3 = "{" + string.Join(", ", infos2) + "}";
            Log.Debug($"CreateFamilies(infos={infos3})->\n\ttracks:{tracks}\n\twires:{wires}", true);

        }

        public static void GenerateLUTs() {
            GenerateStationTrackLUT();
            GenerateDoubleTrackLUT(); // must be called last to avoid duplicates
        }

        public static void GenerateStationTrackLUT() {
            {
                NetInfo DoubleTrack = GetInfo("Train Track");
                NetInfo OnewayTrack = GetInfo("Train Oneway Track");
                NetInfo StationTrack = GetInfo("Train Station Track");

                var infos = new[] { DoubleTrack, OnewayTrack, StationTrack };
                CreateFamily(infos, ConnectType.Train, out var tracks, out var wires);
                tracks.GenerateExtraMeshes();
                wires.GenerateExtraMeshes();
                LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
                LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = LUT[wires.StationSingle] = wires;

                VanillaTrainTracks = tracks;
                VanillaTrainWires = wires;
                GenerateVanillaCargoTracks();

                Log.Debug("[POINT A0] train tracks:" + tracks);
                tracks.TwoWayDouble.m_nodeMesh.DumpMesh("KIAN-train-twoway-double-tracks.obj");
            }
            // TODO
            // BP elevated train station (normal)

            {
                Log.Debug("Creating Monorail ground family");
                // "Medium Road Monorail Station" "Monorail Station Track" "Monorail Track" "Monorail Oneway Track"
                NetInfo StationTrack = GetInfo("Monorail Station Track");
                NetInfo DoubleTrack = GetInfo("Monorail Track");
                NetInfo OnewayTrack = GetInfo("Monorail Oneway Track");

                var infos = new[] { DoubleTrack, OnewayTrack, StationTrack };
                CreateFamily(infos, ConnectType.Monorail, out var tracks, out var wires);
                tracks.GenerateExtraMeshes();
                //wires.GenerateExtraMeshes();
                LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
                //LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = LUT[wires.StationSingle] = wires;

                // TODO reuse materials to see if it covers "Medium Road Monorail Station"
            }
            {
                Log.Debug("Creating Metro ground family");
                // "Metro Station Track Ground 01" "Metro Track Ground 01"
                NetInfo StationTrack = GetInfo("Metro Station Track Ground 01");
                NetInfo DoubleTrack = GetInfo("Metro Track Ground 01");

                var infos = new[] { DoubleTrack, StationTrack };
                CreateFamily(infos, ConnectType.Metro, out var tracks, out var wires);
                tracks.GenerateExtraMeshes();
                //wires.GenerateExtraMeshes();
                LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = tracks;
                //LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = wires;

                Log.Debug("[POINT A] Created ground metro tracks:" + tracks);
                tracks.TwoWayDouble.m_nodeMesh.DumpMesh("KIAN-metro-twoway-double-tracks.obj");

            }
            {
                Log.Debug("Creating Metro Elevated family");
                // "Metro Station Track Elevated 01" "Metro Track Elevated 01"
                NetInfo StationTrack = GetInfo("Metro Station Track Elevated 01");
                NetInfo DoubleTrack = GetInfo("Metro Track Elevated 01");

                var infos = new[] { DoubleTrack, StationTrack };
                CreateFamily(infos, ConnectType.Metro, out var tracks, out var wires);
                tracks.GenerateExtraMeshes();
                //wires.GenerateExtraMeshes();
                LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = tracks;
                //LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = wires;
            }
        }

        public static void GenerateFamilyLUT(IEnumerable<string> infoNames, ConnectType connectType = ConnectType.All) {
            var infos = infoNames.Select(name => GetInfo(name));
            CreateFamily(infos, connectType, out var tracks, out var wires);

            tracks.GenerateExtraMeshes();
            if (tracks.StationDouble != null && tracks.TwoWayDouble != null) {
                LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = tracks;
                if (tracks.StationSingle != null && tracks.OneWayStart != null)
                    LUT[tracks.StationSingle] = tracks;
            }

            wires.GenerateExtraMeshes();
            if (wires.StationDouble != null && wires.TwoWayDouble != null) {
                LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = wires;
                if (wires.StationSingle != null && wires.OneWayStart != null)
                    LUT[wires.StationSingle] = wires;
            }
        }


        public static void GenerateVanillaCargoTracks() {
            NetInfo CargoStationTrack = GetInfo("Train Cargo Track", false)
                ?? GetInfo("Cargo Train Station Track");

            var infos = new[] { CargoStationTrack };
            CreateFamily(infos, ConnectType.Train, out var cargotracks, out var cargowires);
            cargotracks.FillInTheBlanks(VanillaTrainTracks);
            cargowires.FillInTheBlanks(VanillaTrainWires);

            LUT[cargotracks.StationDouble] = cargotracks;
            LUT[cargowires.StationDouble] = cargowires;
        }

        public static void GenerateDoubleTrackLUT() {
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

                    GenerateDoubleTrackLUT(nodeInfo, info);
                }
            }
        }

        public static void GenerateDoubleTrackLUT(NetInfo.Node nodeInfo, NetInfo info) {
            NodeInfoFamily family = new NodeInfoFamily { TwoWayDouble = nodeInfo };
            family.GenerateExtraMeshes();
            //Utils.Log.Debug($"GenerateCustomDoubleTrackLUT(nodeInfo={nodeInfo} info={info}): family=" + family);
            Assert(family.TwoWayDouble != null, " family=" + family);
            LUT[family.TwoWayDouble] = family;
        }

        public static NetInfo GetInfo(string name, bool throwOnError=true) {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.name == name)
                    return info;
                //Helpers.Log.Debuginfo.name);
            }
            if (throwOnError)
                throw new Exception($"NetInfo {name} not found!");
            else
                Log.Debug($"Warning: NetInfo {name} not found!");
            return null;
        }
    }


}
