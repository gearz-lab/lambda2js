using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lambda2Js.Tests
{
    [TestClass]
    public class ScriptVersionTests
    {
        [TestMethod]
        public void NonStd()
        {
            var scv = ScriptVersion.Es50.NonStandard();
            Assert.AreEqual(100005000, (int)scv);
        }

        [TestMethod]
        public void MsJ()
        {
            var scv = ScriptVersion.Es50.MicrosoftJScript(90);
            Assert.AreEqual(309005000, (int)scv);
        }

        [TestMethod]
        public void Js()
        {
            var scv = ScriptVersion.Es50.Javascript(181);
            Assert.AreEqual(218105000, (int)scv);
        }

        [TestMethod]
        public void Proposals()
        {
            var scv = ScriptVersion.Es50.Proposals();
            Assert.AreEqual(5001, (int)scv);
        }

        [TestMethod]
        public void Deprecated()
        {
            var scv = ScriptVersion.Es50.Deprecated();
            Assert.AreEqual(5002, (int)scv);
        }

        [TestMethod]
        public void EsNext()
        {
            var scv = ScriptVersion.EsLatestStable.Proposals();
            Assert.AreEqual((int)ScriptVersion.EsNext, (int)scv);
        }
    }
}
