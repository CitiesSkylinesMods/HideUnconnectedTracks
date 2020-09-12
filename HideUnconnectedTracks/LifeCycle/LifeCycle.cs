namespace HideUnconnectedTracks.LifeCycle
{
    using HideUnconnectedTracks.Utils; using KianCommons;

    public static class LifeCycle
    {
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            TMPEUTILS.Init();
            NodeInfoLUT.GenerateLUTs();

        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            NodeInfoLUT.LUT = null;
            TMPEUTILS.Init();
        }
    }
}
