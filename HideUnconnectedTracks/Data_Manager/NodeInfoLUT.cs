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

        public static void GenerateFamilyLUT(string family) {
            Log.Info("Generating LUT for family:" + family);
            var infoNames = family.Split(',').Select(name => name.Trim());
            GenerateFamilyLUT(infoNames);
        }

        public static void GenerateFamilyLUT(IEnumerable<string> infoNames, TrackType trackType = TrackType.All) {
            var infos = infoNames.Select(name => GetInfo(name));
            var trackFamily = TrackFamily.CreateFamily(infos, trackType);

            foreach(var subFamily in trackFamily.SubFamilies.Values) {
                subFamily.GenerateExtraMeshes();
                subFamily.AddStationsToLUT();
            }
        }

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
