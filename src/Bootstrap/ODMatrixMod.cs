using ICities;

namespace ODMatrix.Bootstrap
{
    public sealed class ODMatrixMod : IUserMod
    {
        public string Name
        {
            get { return "ODMatrix"; }
        }

        public string Description
        {
            get { return "居民出行数据采集与反射探测 Mod v" + ModVersion.SemanticVersion; }
        }
    }
}
