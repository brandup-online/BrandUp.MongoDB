using MongoDB.Driver.Core.Clusters;
using Xunit;

namespace BrandUp.MongoDB.Testing.Tests
{
    public class FakeClusterTest
    {
        readonly FakeCluster cluster = new();

        [Fact]
        public void ClusterId_NotNull()
        {
            Assert.NotNull(cluster.ClusterId);
        }

        [Fact]
        public void Settings_NotNull()
        {
            Assert.NotNull(cluster.Settings);
        }

        [Fact]
        public void Description_IsStandaloneWithNoServers()
        {
            Assert.NotNull(cluster.Description);
            Assert.Equal(ClusterType.Standalone, cluster.Description.Type);
            Assert.Empty(cluster.Description.Servers);
        }

        [Fact]
        public void AcquireServerSession_NotNull()
        {
            Assert.NotNull(cluster.AcquireServerSession());
        }

        [Fact]
        public void StartSession_NotNull()
        {
            using var session = cluster.StartSession();

            Assert.NotNull(session);
        }
    }
}
