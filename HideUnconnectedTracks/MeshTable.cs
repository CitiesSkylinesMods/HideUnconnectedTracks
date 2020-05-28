using System;
using ObjUnity3D;
using UnityEngine;

namespace HideUnconnectedTracks {
    using System.Reflection;
    using Utils;
    public struct MeshTable {
        public NetInfo.Node TwoWayDouble;
        public NetInfo.Node OneSideEnd;//right
        public NetInfo.Node OneSideStart;//left

        public NetInfo.Node OneWay;
        public NetInfo.Node OneWayEnd; //right
        public NetInfo.Node OneWayStart; //left

        public NetInfo.Node StationDouble;
        public NetInfo.Node StationSingle;
    }

    public static class MeshTables {
        public static MeshTable VanillaTrainTracks;
        public static MeshTable VanillaTrainWires;

        public static void LoadMesh(this Mesh mesh, string fileName) {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            var stream = executingAssembly.GetManifestResourceStream("HideUnconnectedTracks.Resources." + fileName);
            mesh.LoadOBJ(OBJLoader.LoadOBJ(stream));
        }

        public static void GenerateVanillaTrainTracks() {
            VanillaTrainTracks = new MeshTable();
            NetInfo DoubleTrack = GetInfo("Train Track");
            NetInfo OnewayTrack = GetInfo("Train Oneway Track");
            NetInfo StationTrack = GetInfo("Train Station Track");
            VanillaTrainTracks.TwoWayDouble = DoubleTrack.m_nodes[1];
            VanillaTrainTracks.OneSideEnd = CloneNodeInfo(VanillaTrainTracks.TwoWayDouble);
            VanillaTrainTracks.OneSideEnd.m_nodeMesh.LoadMesh("Train Track Node Right Track.obj");
            VanillaTrainTracks.OneSideStart = CloneNodeInfo(VanillaTrainTracks.TwoWayDouble);
            VanillaTrainTracks.OneSideStart.m_nodeMesh.LoadMesh("Train Track Node Left Track.obj");

            VanillaTrainTracks.OneWay = OnewayTrack.m_nodes[1];
            VanillaTrainTracks.OneWayEnd = OnewayTrack.m_nodes[4];
            VanillaTrainTracks.OneWayStart = OnewayTrack.m_nodes[5];

            VanillaTrainTracks.StationDouble = StationTrack.m_nodes[1];
            VanillaTrainTracks.StationSingle = StationTrack.m_nodes[4];
        }

        public static void GenerateVanillaTrainWires() {
            VanillaTrainWires = new MeshTable();
            NetInfo DoubleTrack = GetInfo("Train Track");
            NetInfo OnewayTrack = GetInfo("Train Oneway Track");
            NetInfo StationTrack = GetInfo("Train Station Track");
            VanillaTrainWires.TwoWayDouble = DoubleTrack.m_nodes[3];
            VanillaTrainWires.OneSideEnd = CloneNodeInfo(VanillaTrainWires.TwoWayDouble);
            VanillaTrainWires.OneSideEnd.m_nodeMesh.LoadMesh("Train Track Node Right Track.obj");
            VanillaTrainWires.OneSideStart = CloneNodeInfo(VanillaTrainWires.TwoWayDouble);
            VanillaTrainWires.OneSideStart.m_nodeMesh.LoadMesh("Train Track Node Left Track.obj");

            VanillaTrainWires.OneWay = OnewayTrack.m_nodes[3];
            VanillaTrainWires.OneWayEnd = OnewayTrack.m_nodes[6];
            VanillaTrainWires.OneWayStart = OnewayTrack.m_nodes[7];

            VanillaTrainWires.StationDouble = StationTrack.m_nodes[3];
            VanillaTrainWires.StationSingle = StationTrack.m_nodes[5];
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

        public static NetInfo.Node CloneNodeInfo(NetInfo.Node nodeInfo) {
            NetInfo.Node ret = new NetInfo.Node();
            Extensions.CopyProperties<NetInfo.Node>(ret, nodeInfo);
            Extensions.Assert(nodeInfo.m_material != null, $"nodeInfo m_material is null");
            ret.m_nodeMaterial = new Material(nodeInfo.m_nodeMaterial);
            ret.m_lodMaterial = new Material(nodeInfo.m_lodMaterial);
            ret.m_material = new Material(nodeInfo.m_material);
            return ret;
        }
    }


}
