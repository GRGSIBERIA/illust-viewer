using Microsoft.VisualStudio.TestTools.UnitTesting;
using IllustViewer.Library;

namespace ViewerTest
{
    [TestClass]
    public class ConfigTest
    {
        ConfigFile config;

        public ConfigTest()
        {
            config = new ConfigFile("config.xml");
        }

        [TestMethod]
        public void TestSave()
        {
            config.Save();
        }
    }
}
