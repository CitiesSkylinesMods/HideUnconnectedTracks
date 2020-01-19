using System;
using UnityEngine;

//TODO check out material.MainTextureScale
//regarding weird nodes, what if we return a copy of the material?

namespace HideTMPECrosswalks.Utils {
    using static TextureUtils;

    public static class MaterialUtils {
        public static Texture2D TryGetTexture2D(this Material material, int textureID) {
            try {
                Texture texture = material.GetTexture(textureID);
                if (texture is Texture2D)
                    return texture as Texture2D;
            }
            catch { }
            Extensions.Log("Warning: failed to get texture from material :" + material.name);
            return null;
        }


        public static void HideCrossings(Material material, Material segMaterial, NetInfo info, bool lod = false) {
            if (material == null) throw new ArgumentNullException("material");
            if (segMaterial == null) throw new ArgumentNullException("segMaterial");
            if (info == null) throw new ArgumentNullException("info");

            Texture2D tex, tex2;
            bool dump = false;
#if DEBUG
            //dump = lod;
#endif
            //if (dump) DumpUtils.Dump(info);

            tex = material.TryGetTexture2D(ID_Defuse);
            if (tex != null) {
                if (dump) DumpUtils.Dump(tex, info);
                if (TextureCache.Contains(tex)) {
                    tex = TextureCache[tex] as Texture2D;
                    Extensions.Log("Texture cache hit: " + tex.name);
                } else {
                    Extensions.Log("POINT A tex = " + tex.name);
                    tex = tex.GetReadableCopy();
                    tex.CropAndStrech(); if (dump) DumpUtils.Dump(tex, info);
                    tex.Finalize(lod);
                    TextureCache[tex] = tex;
                }
                if (dump) DumpUtils.Dump(tex, info);
                material.SetTexture(ID_Defuse, tex);
                if (dump) DumpUtils.Dump(tex, DumpUtils.GetFilePath(ID_Defuse, "node-processed", info));
            }



            if (info.category != "RoadsSmall" || !info.m_isCustomContent || info.isAsym()) {
                tex = material.TryGetTexture2D(ID_APRMap);
                tex2 = segMaterial.TryGetTexture2D(ID_APRMap); Extensions.Log("POINT B: tex.width=" + tex.width);
                if (tex != null && tex2 != null) {
                    if (dump) DumpUtils.Dump(tex, info);
                    if (dump) DumpUtils.Dump(tex2, info);
                    if (TextureCache.Contains(tex)) {
                        tex = TextureCache[tex] as Texture2D;
                        Extensions.Log("Texture cache hit: " + tex.name);
                    } else {
                        Extensions.Log("POINT B tex = " + tex.name);
                        bool linear = lod && !info.IsNExt();
                        tex = tex.GetReadableCopy(linear: linear);
                        tex2 = tex2.GetReadableCopy(linear: linear);

                        tex.CropAndStrech(); if (dump) DumpUtils.Dump(tex, info);
                        if (info.m_netAI is RoadAI) {
                            if (info.isAsym() && !info.isOneWay()) {
                                tex2.Mirror();
                                if (dump) DumpUtils.Dump(tex2, info);
                            }
                            tex2.Scale(info.ScaleRatio());
                            if (info.ScaleRatio() != 1f && dump) DumpUtils.Dump(tex2, info);
                        }
                        tex.MeldDiff(tex2); if (dump) DumpUtils.Dump(tex, info);

                        tex.Finalize(lod);
                        TextureCache[tex] = tex;
                    }
                    material.SetTexture(ID_APRMap, tex);
                    if (dump) DumpUtils.Dump(tex, DumpUtils.GetFilePath(ID_APRMap, "node-processed", info));
                } // end if cache
            } // end if tex
        } // end if category

        public static void NOPMaterial(Material material, bool lod = false) {
            if (material == null) throw new ArgumentNullException("material");

            Texture2D tex = material.TryGetTexture2D(ID_Defuse);
            if (tex != null) {
                tex = tex.GetReadableCopy();
                tex.NOP();
                material.SetTexture(ID_Defuse, tex);
            } // end if

            tex = material.TryGetTexture2D(ID_APRMap);
            if (tex != null) {
                tex = tex.GetReadableCopy(linear: lod);
                tex.NOP();
                material.SetTexture(ID_APRMap, tex);
            } // end if
        } // end method
    } // end class
} // end namesapce

