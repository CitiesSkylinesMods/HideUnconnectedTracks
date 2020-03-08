#if DEBUG
using ICities;
using UnityEngine;
using ColossalFramework;
using System;

namespace HideUnconnectedTracks {
    using Utils;
    using static DebugTests;

    public class TestOnLoad : LoadingExtensionBase {
        public override void OnCreated(ILoading loading) { base.OnCreated(loading); Test(); }
        public override void OnLevelLoaded(LoadMode mode) => Test();

        public static void Test() {
            if (!Extensions.InGame && !Extensions.InAssetEditor)
                return;
            //Test1();

        }

    }

    public static class DebugTests {
        public static void Test1() {

        }
    }
}

#endif
