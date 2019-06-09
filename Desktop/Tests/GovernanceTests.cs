using FhiModel.Governance;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class GovernanceTests
    {

        [TestMethod]
        public void ParseFileTest()
        {
            var stuff = Import.Read(@"..\..\..\TestData\Governance\3S results.csv", "A", "K", "Please provide");
        }
    }
}