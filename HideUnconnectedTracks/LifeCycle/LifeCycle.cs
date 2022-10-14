namespace HideUnconnectedTracks.LifeCycle
{
    using HideUnconnectedTracks.Utils;
    using KianCommons;

    public static class LifeCycle
    {
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            TMPEUtil.Init();
            NodeInfoLUT.GenerateLUTs();
            Log.Info("LifeCycle.Load() successful");
        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            NodeInfoLUT.LUT.Clear();
            TMPEUtil.Init();
        }
    }
}
