using System;
using System.Linq;
using UnityEngine;
using static HideUnconnectedTracks.Utils.DirectConnectUtil;

namespace HideUnconnectedTracks {
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using static KianCommons.Assertion;

    public class MeshTable {
        public static MeshTable MeshLUT = new MeshTable();

        Hashtable _meshTable = new Hashtable(1000);
        Dictionary<string, NodeInfoFamily> _md5Table = new Dictionary<string, NodeInfoFamily>(1000);

        /// <summary>
        /// takes longer but ignores mesh name
        /// assuming there is a 1:1 correspondence between mesh name and mesh shape,
        /// this can be set to false to speed up loading times.
        /// </summary>
        const bool vertexBasedMD5_ = false;

        public static byte[] ToBytes(Vector3[] v) {
            const int vector3Size = sizeof(float) * 3;
            byte[] buffer = new byte[v.Length * vector3Size];
            for (int i = 0; i < v.Length; ++i) {
                var b1 = BitConverter.GetBytes(v[i].x);
                var b2 = BitConverter.GetBytes(v[i].y);
                var b3 = BitConverter.GetBytes(v[i].z);
                Buffer.BlockCopy(b1, 0, buffer, i * vector3Size, sizeof(float) * 0);
                Buffer.BlockCopy(b1, 0, buffer, i * vector3Size + sizeof(float) * 1, sizeof(float));
                Buffer.BlockCopy(b1, 0, buffer, i * vector3Size + sizeof(float) * 2, sizeof(float));
            }
            return buffer;
        }

        public static string ToMD5(Vector3[] vertices) {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                // TODO optimise:

                var bytes = ToBytes(vertices);
                byte[] hashBytes = md5.ComputeHash(bytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++) {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public NodeInfoFamily this[Mesh key] {
            get {
                if (key == null) return null;
                var ret = _meshTable[key] as NodeInfoFamily;
                if (vertexBasedMD5_) {
                    if (ret != null)
                        return ret;
                    string md5 = ToMD5(key.vertices);
                    foreach (var pair in _md5Table) {
                        if (pair.Key == md5)
                            return pair.Value;
                    }
                }
                return ret;
            }
            set {
                AssertNotNull(value);
                AssertNotNull(key);
                _meshTable[key] = value;
                if (vertexBasedMD5_) {
                    string md5 = ToMD5(key.vertices);
                    _md5Table[md5] = value;
                }
            }
        }
    }
}
