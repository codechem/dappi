using Dappi.HeadlessCms.Controllers;
using Dappi.HeadlessCms.Core;
using Dappi.HeadlessCms.Tests.Core;
using Dappi.IntegrationTests;

namespace Dappi.HeadlessCms.Tests.Controllers
{
    public class ModelsControllerTests : BaseIntegrationTest
    {
        private readonly ModelsController _controller;
        public ModelsControllerTests(IntegrationWebAppFactory factory) : base(factory)
        {
            
        }

        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }
    }
}