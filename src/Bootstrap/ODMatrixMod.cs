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
            get { return "ODMatrix v" + ModVersion.SemanticVersion; }
        }
    }
}
