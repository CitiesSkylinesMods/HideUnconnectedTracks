namespace HideUnconnectedTracks {
    using ColossalFramework;
    using KianCommons;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using static KianCommons.DCUtil;
    using static MeshTable;
    using HideUnconnectedTracks.Utils;

    public static class NodeInfoLUT {
        public static Hashtable LUT = new Hashtable();

        public static readonly string[] BuiltInFamilies = new[] {
                "Train Track,Train Oneway Track,Train Station Track",
                "Monorail Station Track,Monorail Track,Monorail Oneway Track",
                "Metro Station Track Ground 01,Metro Track Ground 01",
                "1574857232.R69Railway GrCo 2x2 S_Data, 1574857232.R69Railway GrCo 1x1_Data, 1574857232.R69Railway GrCo 2x2_Data",
            };

        public static string FamiliesPath = "track-famlilies.txt";

        public static void GenerateLUTs() {
            LUT = new Hashtable(10000);
            MeshLUT = new MeshTable();

            PrefabFixesAndWorkArounds();

            try {
                using (Stream fs0 = new FileStream(FamiliesPath, FileMode.CreateNew, FileAccess.Write)) {
                    using (StreamWriter writer = new StreamWriter(fs0)) {
                        foreach (var line in BuiltInFamilies)
                            writer.WriteLine(line);
                    }
                }
            }
            catch (IOException) { } // file already exists

            List<string> families = new List<string>();
            using (Stream fs1 = new FileStream("track-famlilies.txt", FileMode.OpenOrCreate, FileAccess.Read)) {
                using (StreamReader reader = new StreamReader(fs1)) {
                    Log.Debug("filling in track-famlilies.txt");
                    while (reader.ReadLine() is string line)
                        families.Add(line);
                }
            }
            Log.Info("families =\n" + string.Join("\n", families.ToArray()));

            foreach (string family in families)
                GenerateFamilyLUT(family);

            GenerateDoubleTrackLUT(); // call after GenerateFamilyLUT to avoid duplicates.
            RecycleStationTrackMesh();
            // TODO: recycle single-2way and double-oneway tracks as well.
        }

        static void PrefabFixesAndWorkArounds() {
            // if info.m_ConnectGroup == DOUBLE and nodeInfo.DC and  nodeInfo.m_connectGroup == None :
            //    set nodeInfo.m_connectGroup = DOUBLE
            Log.Info("PrefabFixesWorkArounds() called", false);

            int n = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info.m_netAI is RoadBaseAI) continue; // might be a median
                if (info == null) continue;
                if (!info.m_requireDirectRenderers) continue;
                if (info.m_connectGroup.IsFlagSet(STATION | SINGLE)) continue;
                if (!info.m_connectGroup.IsFlagSet(DOUBLE)) continue;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect) continue;
                    if (nodeInfo.m_connectGroup == NetInfo.ConnectGroup.None)
                        nodeInfo.m_connectGroup = info.m_connectGroup;
                }
            }
        }


        static void GenerateFamilyLUT(string family) {
            Log.Info("Generating LUT for family:" + family);
            var infoNames = family.Split(',').Select(name => name.Trim());
            GenerateFamilyLUT(infoNames);

            static void GenerateFamilyLUT(IEnumerable<string> infoNames, TrackType trackType = TrackType.All) {
                var infos = infoNames.Select(name => NetInfoUtil.GetInfo(name, throwOnError: false));
                infos = infos.Where(info => info != null);

                var trackFamily = TrackFamily.CreateFamily(infos, trackType);

                foreach (var subFamily in trackFamily.SubFamilyDict.Values) {
                    subFamily.GenerateExtraMeshes();
                    subFamily.AddStationsToLUT();
                }
            }
        }

        static void GenerateDoubleTrackLUT() {
            Log.Info("GenerateDoubleTrackLUT() called");
            int n = PrefabCollection<NetInfo>.LoadedCount();
            int generatedCount = 0;
            int recycledCount = 0;
            int processedPrefabCount = 0;
            for (uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null) continue;
                if (!info.m_connectGroup.IsFlagSet(DOUBLE))
                    continue;
                bool processed = false;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect)
                        continue;
                    if (LUT.ContainsKey(nodeInfo))
                        continue; // skip vanilla/duplicate
                    if (!nodeInfo.m_connectGroup.IsFlagSet(DOUBLE))
                        continue;
                    if (!IsTrack(nodeInfo, info))
                        continue; // skip median

                    bool generated = GenerateDoubleTrackLUT(nodeInfo);
                    if (generated) generatedCount++;
                    else recycledCount++;
                    processed = true;
                }
                if (processed)
                    processedPrefabCount++;
            }
            Log.Info($"GenerateDoubleTrackLUT() successful.\n" +
                $"generated:{generatedCount} recycled:{recycledCount} pairs of half tracks for {processedPrefabCount} track prefabs");


            /// <returns>true if new meshes where generated</returns>
            static bool GenerateDoubleTrackLUT(NetInfo.Node nodeInfo) {
                NodeInfoFamily family = new NodeInfoFamily { TwoWayDouble = nodeInfo };
                bool ret = family.GenerateExtraMeshes();
                //Utils.Log.Debug($"GenerateCustomDoubleTrackLUT(nodeInfo={nodeInfo} info={info}): family=" + family);
                LUT[family.TwoWayDouble] = family;
                return ret;
            }
        }

        public static void RecycleStationTrackMesh() {
            Log.Info("RecycleStationTrackMesh() called");

            int n = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < n; ++i) {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null) continue;
                if (!info.m_connectGroup.IsFlagSet(STATION))
                    continue;
                Log.Debug("[p1] " + info.name );
                bool recycled = false;
                foreach (var nodeInfo in info.m_nodes) {
                    if (!nodeInfo.m_directConnect)
                        continue;
                    Log.Debug("[p2] " + info.name);
                    if (LUT.ContainsKey(nodeInfo))
                        continue; // skip vanilla/duplicate
                    if (!IsTrack(nodeInfo, info))
                        continue; // skip median

                    Log.Debug("[p3] " + info.name);
                    recycled |= Recycle(info, nodeInfo);
                }
                if (recycled)
                    Log.Info("Recycled half track meshes for station track: " + info.name, false);
            }

            /// <returns>true if recycling was succesful.</returns>
            static bool Recycle(NetInfo info, NetInfo.Node nodeInfo) {
                NodeInfoFamily cache = MeshLUT[nodeInfo.m_nodeMesh];
                if (cache == null) return false;
                NodeInfoFamily family = new NodeInfoFamily();
                family.FillInTheBlanks(cache);

                if (nodeInfo.m_connectGroup.IsFlagSet(DOUBLE)) {
                    family.StationDouble = nodeInfo;
                } else if (nodeInfo.m_connectGroup.IsFlagSet(SINGLE)) {
                    Log.Debug("[p4] ");
                    family.StationSingle = nodeInfo;
                } else if (nodeInfo.m_connectGroup.IsFlagSet(STATION)) {
                    family.Station = nodeInfo;
                } else return false;

                family.GenerateExtraMeshes(); // only recycles here.
                family.AddStationsToLUT();
                return true;
            }
        }
    }
}
