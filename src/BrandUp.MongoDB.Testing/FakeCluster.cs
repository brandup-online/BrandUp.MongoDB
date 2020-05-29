using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using MongoDB.Libmongocrypt;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BrandUp.MongoDB.Testing
{
    public class FakeCluster : ICluster
    {
        public ClusterId ClusterId => throw new NotImplementedException();
        public ClusterDescription Description => throw new NotImplementedException();
        public ClusterSettings Settings => throw new NotImplementedException();
        public CryptClient CryptClient => throw new NotImplementedException();

        public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        public ICoreServerSession AcquireServerSession()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public void Initialize()
        {
        }

        public IServer SelectServer(IServerSelector selector, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IServer> SelectServerAsync(IServerSelector selector, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}
