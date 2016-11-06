using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FastNats.Client;

namespace FastNats.ClientTests
{
    [TestClass]
    public class ConnectionTests
    {
        UnitTestUtilities utils = new UnitTestUtilities();

        [TestMethod]
        public void ConnectionStatusTest()
        {
            using (new NATSServer())
            {
                IConnection c = utils.DefaultTestConnection;
                Assert.AreEqual(ConnState.CONNECTED, c.State);
                c.Close();
                Assert.AreEqual(ConnState.CLOSED, c.State);
            }
        }
    }
}
