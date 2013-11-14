using AutoMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace App.Test
{
    [TestClass]
    public class AutoMapperConfigurationtest
    {
        [TestMethod]
        public void TestMappings()
        {
            Mapper.Initialize(Startup.ConfigureMapper);
            Mapper.AssertConfigurationIsValid();
        }
    }
}
