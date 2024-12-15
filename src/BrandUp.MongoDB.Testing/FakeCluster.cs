using System;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;

namespace BrandUp.MongoDB.Testing
{
    public class FakeCluster : ICluster
    {
        public ClusterId ClusterId => throw new NotImplementedException();
        public ClusterDescription Description => throw new NotImplementedException();
        public ClusterSettings Settings => throw new NotImplementedException();

        public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        protected void OnDescriptionChanged(ClusterDescriptionChangedEventArgs e)
        {
            if (DescriptionChanged != null)
                DescriptionChanged(this, e);
        }

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

        public ICoreSessionHandle StartSession(CoreSessionOptions options = null)
        {
            throw new NotImplementedException();
        }
    }
}
