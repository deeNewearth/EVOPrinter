using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace printerDriver
{
    public class CustomActions
    {
        readonly static string PRINTERNAME = @"Print to Evo";

        [CustomAction]
        public static ActionResult removePrinterDriver(Session session)
        {
            
            /*
            var g1 = true;
            while (g1)
            {
                System.Threading.Thread.Sleep(1000);
            }
            */

            try
            {
                session.Log("Begin removePrinterDriver Action");

                var spooler = new SpoolerHelper(new LogHelper(session));
                spooler.RemoveVPrinter(PRINTERNAME);


                session.Log("End removePrinterDriver Action");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("ERROR in custom action removePrinterDriver :  {0}",
                            ex.ToString());
                return ActionResult.Success;
            }

        }

        [CustomAction]
        public static ActionResult setupPrinterDriver(Session session)
        {
            /*
            var g1 = true;
            while (g1)
            {
                System.Threading.Thread.Sleep(1000);
            }
             */
             

            try
            {
                session.Log("Begin setupPrinterDriver Action");

                string execPath = session.CustomActionData["execFile"];
                if (String.IsNullOrWhiteSpace(execPath))
                    throw new Exception("execpath is null");

                string execParam = session.CustomActionData["execParam"];
                if (String.IsNullOrWhiteSpace(execParam))
                    throw new Exception("execParam is null");

                execPath = String.Format("\"{0}\" \"{1}\"  \"%p\" \"%f\" ", execPath, execParam);
                //var j1 = session["EXECFILE"];

                var spooler = new SpoolerHelper(new LogHelper(session));

                spooler.AddVPrinter(PRINTERNAME, execPath);

                session.Log("End setupPrinterDriver Action");
                return ActionResult.Success;
            }
            catch (Exception ex)
            {
                session.Log("ERROR in custom action setupPrinterDriver :  {0}",
                            ex.ToString());
                return ActionResult.Failure;
            }
            
        }
    }
}
