using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace printerDriver.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void testGSINstall()
        {
            printerDriver.CustomActions.EnusreGS("..//sasd.txt");
        }


        [TestMethod]
        public void testMonitorCreation()
        {
            var s= new printerDriver.SpoolerHelper(null);

            var monName = "evoTestMonitor";

            s.AddPrinterMonitor(monName);
            
            var k =s.GetMonitors();

            var myMon = k.SingleOrDefault(m => m.pName == monName);

            Assert.IsNotNull(myMon.pName);

            s.RemovePrinterMonitor(monName);
            
            myMon = s.GetMonitors().SingleOrDefault(m => m.pName == monName);
            Assert.IsNull(myMon.pName);
        }
    }
}
