using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;

namespace BrandUp.MongoDB.Testing
{
    public class FakeCluster : ICluster
    {
        readonly ClusterId clusterId = new();
        readonly ClusterSettings settings = new();
        readonly ClusterDescription description;

        public FakeCluster()
        {
            description = new ClusterDescription(
                clusterId,
                directConnection: false,
                dnsMonitorException: null,
                ClusterType.Standalone,
                Enumerable.Empty<ServerDescription>());
        }

        public ClusterId ClusterId => clusterId;
        public ClusterDescription Description => description;
        public ClusterSettings Settings => settings;

        public event EventHandler<ClusterDescriptionChangedEventArgs>? DescriptionChanged;

        protected void OnDescriptionChanged(ClusterDescriptionChangedEventArgs e)
        {
            DescriptionChanged?.Invoke(this, e);
        }

        public ICoreServerSession AcquireServerSession()
        {
            return NoCoreSession.Instance.ServerSession;
        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions? options = null)
        {
            return NoCoreSession.NewHandle();
        }
    }
}
