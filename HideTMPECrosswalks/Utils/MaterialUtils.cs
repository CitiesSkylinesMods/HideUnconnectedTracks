using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace HideTMPECrosswalks.Utils {
    using static Extensions;
    public static class MaterialUtils {
        public static void HideCrossing(Material material, NetInfo info) {
            bool asym = info.isAsym();
            var ticks = System.Diagnostics.Stopwatch.StartNew();

            Texture tex = material.GetTexture(ID_Defuse);
            if (tex != null) {
                    Extensions.Log("Texture cache hit: " + tex.name);
                    tex = TextureUtils.Process(tex, TextureUtils.Crop);
                    if (asym) tex = TextureUtils.Process(tex, TextureUtils.Mirror);
                    (tex as Texture2D).Compress(false);
                material.SetTexture(ID_Defuse, tex);
            }

            if (info.GetClassLevel() > ItemClass.Level.Level1 || info.m_isCustomContent) {
                tex = material.GetTexture(ID_APRMap);
                if (tex != null) {
                    tex = TextureUtils.Process(tex, TextureUtils.Crop);
                    Material material2 = info.m_segments[0]?.m_material;
                    Texture tex2 = material2?.GetTexture(ID_APRMap);
                    if (tex2 != null) {
                        Extensions.Log($"melding {info.name} - node material = {material.name} -> ret | segment material = {material2.name}");
                        tex = TextureUtils.Process(tex, tex2, TextureUtils.MeldDiff);
                    }

                    if (asym) tex = TextureUtils.Process(tex, TextureUtils.Mirror);
                    (tex as Texture2D).Compress(false);
                    material.SetTexture(ID_APRMap, tex);
                }
            }
            Extensions.Log($"processed new material for {info.name} ticks=" + ticks.ElapsedTicks.ToString("E2"));
        }
    }
}

