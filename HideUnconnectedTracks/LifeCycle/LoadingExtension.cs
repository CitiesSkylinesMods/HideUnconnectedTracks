namespace HideUnconnectedTracks.LifeCycle
{
    using ICities;
    using HideUnconnectedTracks.Utils;

    public class LoadingExtention : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            Log._Debug("LoadingExtention.OnLevelLoaded");
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario)
                LifeCycle.Load();
        }

        public override void OnLevelUnloading()
        {
            Log._Debug("LoadingExtention.OnLevelUnloading");
            LifeCycle.Release();
        }
    }
}
