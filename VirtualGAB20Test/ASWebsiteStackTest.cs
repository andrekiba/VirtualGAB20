using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Pulumi;
using Pulumi.Azure.Storage;
using Pulumi.Testing;
using VirtualGAB20Demo1;

namespace VirtualGAB20Test
{
    [TestClass]
    public class ASWebsiteStackTest
    {
        [TestMethod]
        public async Task TestStorageAccount()
        {
            var mocks = Substitute.For<IMocks>();
            var resources = await Deployment.TestAsync<ASWebsiteStack>(mocks);
            var storage = resources.OfType<Account>().SingleOrDefault();
            storage.Should().NotBeNull("Storage not created!");
            storage?.AccountKind.Should().Be("StorageV2");
        }
    }
}