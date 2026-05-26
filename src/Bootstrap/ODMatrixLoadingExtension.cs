using ICities;

namespace ODMatrix.Bootstrap
{
    public sealed class ODMatrixLoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            ModLifecycle.OnLevelLoaded(mode);
        }

        public override void OnLevelUnloading()
        {
            ModLifecycle.OnLevelUnloading();
        }
    }
}
