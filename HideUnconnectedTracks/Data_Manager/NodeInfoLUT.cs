namespace HideUnconnectedTracks {
    using System;
    using System.Linq;
    using ColossalFramework;
    using KianCommons;
    using System.Collections;
    using System.Collections.Generic;
    using Utils;
    using static KianCommons.Assertion;
    using static MeshTable;
    using static HideUnconnectedTracks.Utils.DirectConnectUtil;
    using System.IO;

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
                        // usually station and double are the same but for metro station for some reasong this is not the case
                        if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE))
                            family.StationDouble = nodeInfo;
                        else if (nodeInfo.m_connectGroup.IsFlagSet(STATION))
                            family.Station = nodeInfo;
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
            LUT = new Hashtable(10000);
            MeshLUT = new MeshTable();

            var families = new List<string>(new [] {
                "Train Track,Train Oneway Track,Train Station Track",
                "Monorail Station Track,Monorail Track,Monorail Oneway Track",
                "Metro Station Track Ground 01,Metro Track Ground 01",
            });
            using (Stream fs = new FileStream("track-famlilies.txt", FileMode.OpenOrCreate)) {
                using (StreamReader reader = new StreamReader(fs)) {
                    while (reader.ReadLine() is string line)
                        families.Add(line);
                }
            }
            Log.Info("families =\n" + string.Join("\n", families.ToArray()));


            foreach (string family in families)
                GenerateFamilyLUT(family);

            GenerateDoubleTrackLUT(); // call after GenerateFamilyLUT to avoid duplicates.
            RecycleStationTrackMesh();
        }

        //public static void GenerateKnownFamiliesLUT(IEnumerable<string> families) {
            //{
            //    NetInfo DoubleTrack = GetInfo("Train Track");
            //    NetInfo OnewayTrack = GetInfo("Train Oneway Track");
            //    NetInfo StationTrack = GetInfo("Train Station Track");

            //    var infos = new[] { DoubleTrack, OnewayTrack, StationTrack };
            //    CreateFamily(infos, ConnectType.Train, out var tracks, out var wires);
            //    tracks.GenerateExtraMeshes();
            //    wires.GenerateExtraMeshes();
            //    LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
            //    LUT[wires.StationDouble] = LUT[wires.TwoWayDouble] = LUT[wires.StationSingle] = wires;

            //    VanillaTrainTracks = tracks;
            //    VanillaTrainWires = wires;
            //    //GenerateVanillaCargoTracks();
            //}
            //{
            //    Log.Debug("Creating Monorail ground family");
            //    // "Medium Road Monorail Station" "Monorail Station Track" "Monorail Track" "Monorail Oneway Track"
            //    NetInfo StationTrack = GetInfo("Monorail Station Track");
            //    NetInfo DoubleTrack = GetInfo("Monorail Track");
            //    NetInfo OnewayTrack = GetInfo("Monorail Oneway Track");

            //    var infos = new[] { DoubleTrack, OnewayTrack, StationTrack };
            //    CreateFamily(infos, ConnectType.Monorail, out var tracks, out var wires);
            //    tracks.GenerateExtraMeshes();
            //    LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = LUT[tracks.StationSingle] = tracks;
            //    // TODO reuse materials to see if it covers "Medium Road Monorail Station"
            //}
            //{
            //    Log.Debug("Creating Metro ground family");
            //    // "Metro Station Track Ground 01" "Metro Track Ground 01"
            //    NetInfo StationTrack = GetInfo("Metro Station Track Ground 01");
            //    NetInfo DoubleTrack = GetInfo("Metro Track Ground 01");

            //    var infos = new[] { DoubleTrack, StationTrack };
            //    CreateFamily(infos, ConnectType.Metro, out var tracks, out _);
            //    tracks.GenerateExtraMeshes();
            //    LUT[tracks.Station] = LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = tracks;
            //}
            //// elevated does not work because the direct connect texture also has background texture.
            ////{
            ////    Log.Debug("Creating Metro Elevated family");
            ////    // "Metro Station Track Elevated 01" "Metro Track Elevated 01"
            ////    NetInfo StationTrack = GetInfo("Metro Station Track Elevated 01");
            ////    NetInfo DoubleTrack = GetInfo("Metro Track Elevated 01");

            ////    var infos = new[] { DoubleTrack, StationTrack };
            ////    CreateFamily(infos, ConnectType.Metro, out var tracks, out _);
            ////    tracks.GenerateExtraMeshes();
            ////    LUT[tracks.Station] = LUT[tracks.StationDouble] = LUT[tracks.TwoWayDouble] = tracks;
            ////}
        //}

        public static void GenerateFamilyLUT(string family) {
            Log.Info("Generating LUT for family:" + family);
            var infoNames = family.Split(',').Select(name => name.Trim());
            GenerateFamilyLUT(infoNames);
        }

        public static void GenerateFamilyLUT(IEnumerable<string> infoNames, ConnectType connectType = ConnectType.All) {
            var infos = infoNames.Select(name => GetInfo(name));
            CreateFamily(infos, connectType, out var tracks, out var wires);

            tracks.GenerateExtraMeshes();
            tracks.AddStationsToLUT();

            wires.GenerateExtraMeshes();
            wires.AddStationsToLUT();
        }

        //public static void GenerateVanillaCargoTracks() {
        //    NetInfo CargoStationTrack = GetInfo("Train Cargo Track", false)
        //        ?? GetInfo("Cargo Train Station Track");

        //    var infos = new[] { CargoStationTrack };
        //    CreateFamily(infos, ConnectType.Train, out var cargotracks, out var cargowires);
        //    cargotracks.FillInTheBlanks(VanillaTrainTracks);
        //    cargowires.FillInTheBlanks(VanillaTrainWires);

        //    LUT[cargotracks.StationDouble] = cargotracks;
        //    LUT[cargowires.StationDouble] = cargowires;
        //}

        public static void GenerateDoubleTrackLUT() {
            int n = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null) continue;
                if (!info.m_connectGroup.IsFlagSet(DOUBLE))
                    continue;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect)
                        continue;
                    if (LUT.ContainsKey(nodeInfo))
                        continue; // skip vanilla/duplicate
                    if (!nodeInfo.m_connectGroup.IsFlagSet(DOUBLE))
                        continue;
                    if (!IsTrack(nodeInfo, info))
                        continue; // skip median

                    GenerateDoubleTrackLUT(nodeInfo);
                }
            }
        }

        public static void GenerateDoubleTrackLUT(NetInfo.Node nodeInfo) {
            NodeInfoFamily family = new NodeInfoFamily { TwoWayDouble = nodeInfo };
            family.GenerateExtraMeshes();
            //Utils.Log.Debug($"GenerateCustomDoubleTrackLUT(nodeInfo={nodeInfo} info={info}): family=" + family);
            LUT[family.TwoWayDouble] = family;
        }

        public static void RecycleStationTrackMesh() {
            int n = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null) continue;
                if (!info.m_connectGroup.IsFlagSet(STATION))
                    continue;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect)
                        continue;
                    if (LUT.ContainsKey(nodeInfo))
                        continue; // skip vanilla/duplicate
                   if (!IsTrack(nodeInfo, info))
                        continue; // skip median

                    RecycleStationTrackMesh(nodeInfo);
                }
            }
        }

        public static void RecycleStationTrackMesh(NetInfo.Node nodeInfo) {
            NodeInfoFamily cache = MeshLUT[nodeInfo.m_nodeMesh];
            if (cache == null) return;
            NodeInfoFamily family = new NodeInfoFamily();
            family.FillInTheBlanks(cache);

            if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE)) {
                family.StationDouble = nodeInfo;
            } else if (nodeInfo.m_connectGroup.IsFlagSet(SINGLE)) {
                family.StationSingle = nodeInfo;
            } else if (nodeInfo.m_connectGroup.IsFlagSet(STATION)) {
                family.Station = nodeInfo;
            } else return;

            family.GenerateExtraMeshes();
            family.AddStationsToLUT();
        }

        public static NetInfo GetInfo(string name, bool throwOnError = true) {
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
