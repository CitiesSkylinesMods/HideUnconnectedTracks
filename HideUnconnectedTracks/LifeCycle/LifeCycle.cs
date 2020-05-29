namespace HideUnconnectedTracks.LifeCycle
{
    using HideUnconnectedTracks.Utils;

    public static class LifeCycle
    {
        public static void Load()
        {
            Log.Info("LifeCycle.Load() called");
            TMPEUTILS.Init();
            MeshTables.GenerateVanillaTrainTracks();
        }

        public static void Release()
        {
            Log.Info("LifeCycle.Release() called");
            TMPEUTILS.Init();
        }
    }
}
