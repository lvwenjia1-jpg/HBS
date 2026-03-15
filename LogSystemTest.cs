using System.IO;
using NUnit.Framework;

namespace TestProject1
{
    [TestFixture]
    public class LogSystemTest
    {
        IRumBridgeInstance client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            RumBridge.SetNativePath(Path.Combine(baseDir, "runtimes"));
        }

        [SetUp]
        public void Setup()
        {
            client = RumBridge.Instance;
        }

        [Test]
        public void Test1()
        {
            client.Initialize(
                appId: "cvzenqnq6s@238c56c7452a554",
                appName: "??",
                env: RumEnv.Daily,
                configAddress: "",
                appVersion: "1.0.0",
                cachePath: "",
                handlerPath: "",
                logLevel: RumLogLevel.Info,
                autoCurl: false,
                autoCrash: false,
                autoCef: false);

            client.StartSession();
            client.ReportCustomLog("type", "name", RumLogLevel.Info, "??????Log");
        }
    }
}
