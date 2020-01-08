using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

//TODO check out material.MainTextureScale
//regarding weird nodes, what if we return a copy of the material?

namespace HideTMPECrosswalks.Utils {
    using static PrefabUtils;
    using static TextureUtils;

    public static class MaterialUtils {

        public static string[] APRMapExceptions = new[] { "" }; // TODO complete list.


        public static Hashtable MaterialCache = null;
        public static void Init() => MaterialCache = new Hashtable(500);
        public static void Clear() => MaterialCache = null;

        public static Material HideCrossingCached(Material material, NetInfo info) {
            try {
                if (MaterialCache == null) {
                    return material; // exiting game.
                }
                if (MaterialCache.Contains(material)) {
                    return (Material)MaterialCache[material];
                }
                if (HasSameNodeAndSegmentTextures(info, ID_Defuse)) {
                    // TODO why this works but the WierdNodeTest() fails.
                    string m = $"{info.name} is {info.category} is without proper node texture.";
                    Extensions.Log(m);
                    MaterialCache[material] = material;
                    return material;
                }

                var ticks = System.Diagnostics.Stopwatch.StartNew();
                Material ret = new Material(material);
                HideCrossings(ret, info);
                MaterialCache[material] = ret;
                Extensions.Log($"Cached new texture for {info.name} ticks=" + ticks.ElapsedTicks.ToString("E2"));
                return ret;
            }
            catch (Exception e) {
                Extensions.Log(e.ToString());
                MaterialCache[material] = material; // do not repeat the same mistake!
                return material;
            }
        }

        static void P(string s) => Extensions.Log("Point " + s);

        public static void HideCrossings(Material material, NetInfo info) {
            if (material == null) throw new ArgumentNullException("material");
            if (info == null) throw new ArgumentNullException("info");
            bool dump = false;
#if DEBUG
            //dump = true;
#endif

            if (dump) {
                DumpUtils.Dump(info);
                //DumpUtils.Dump(material, "node-original", info);
            }


            Texture tex = material.GetTexture(ID_Defuse);
            if (tex != null) {
                if (TextureCache.Contains(tex)) {
                    tex = TextureCache[tex] as Texture;
                    Extensions.Log("Texture cache hit: " + tex.name);
                } else {
                    Extensions.Log("POINT A tex = " + tex.name);
                    tex = Process(tex, Crop);
                    (tex as Texture2D).Compress(true);
                    TextureCache[tex] = tex;
                }

                material.SetTexture(ID_Defuse, tex);
                if (dump) DumpUtils.Dump(tex, DumpUtils.GetFilePath(ID_Defuse, "node-processed", info));
            }

            string[] exempt_categories = {
                    //"RoadsTiny",
                    "RoadsSmall",
                    //"RoadsSmallHV",
                };
            if (!exempt_categories.Contains(info.category)) {
                tex = material.GetTexture(ID_APRMap);
                Extensions.Log("POINT B: tex.width=" + tex.width);
                if (tex != null) {
                    if (TextureCache.Contains(tex)) {
                        tex = TextureCache[tex] as Texture;
                        Extensions.Log("Texture cache hit: " + tex.name);
                    } else {
                        tex = Process(tex, Crop);
                        Material material2 = info.m_segments[0]?.m_material;
                        Texture tex2 = material2?.GetTexture(ID_APRMap);
                        Texture tex2_orig = tex2;
                        if (tex2 != null) {
                            if (info.m_netAI is RoadAI) {
                                if (info.isAsym()) tex2 = Process(tex2, Mirror);
                                float ratio = info.ScaleRatio();
                                if (ratio != 1f) {
                                    Texture2D ScaleRatio(Texture2D t) => Scale(t, ratio);
                                    tex2 = Process(tex2, ScaleRatio);
                                }
                                if(tex2 != tex2_orig && dump)
                                    DumpUtils.Dump(tex2, DumpUtils.GetFilePath(ID_APRMap, "segment-processed", info));
                            }
                            tex = Process(tex, tex2, MeldDiff);
                            if (dump) DumpUtils.Dump(tex, DumpUtils.GetFilePath(ID_APRMap, "node-processed", info));
                        }
                        (tex as Texture2D).Compress(true); //TODO make un-readable?
                        TextureCache[tex] = tex;
                    }
                    material.SetTexture(ID_APRMap, tex);
                } // end if cache
            } // end if tex
        } // end if !exempt
    }
}

