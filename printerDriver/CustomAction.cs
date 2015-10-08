using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;

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


        public static void EnusreGS(String configFileName)
        {
            if (!File.Exists(configFileName))
                throw new Exception("Config file [" + configFileName + "]not found");


            var regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GPL Ghostscript", false);
            if (null == regKey)
                throw new Exception("GPL Ghostscript not installed");

            var subkeys = regKey.GetSubKeyNames();

            var latestVersion = subkeys.Select(n =>
                {

                    Version ver;
                    if (!Version.TryParse(n, out ver))
                        ver = null;

                    return new
                    {
                        Version = ver,
                        subKey = n
                    };
                        
                }).Where(v=>null != v.Version).OrderByDescending(v=>v.Version).FirstOrDefault();

            if(null == latestVersion)
                throw new Exception("GS version key not found");


            var verKey = regKey.OpenSubKey(latestVersion.subKey);
            var gsDLL = verKey.GetValue("GS_DLL") as String;

            if (String.IsNullOrWhiteSpace(gsDLL) || !File.Exists(gsDLL))
                throw new Exception("GhostScript Engine dll not found");

            var gsPath = Path.GetDirectoryName(gsDLL);

            var ps2pdfPath = Path.Combine(
                Directory.GetParent(gsPath).FullName,"lib","ps2pdf14.bat");

            if (!File.Exists(ps2pdfPath))
                throw new Exception("ps2pdfPath [" + ps2pdfPath+"] not found");


            var configFile = new XmlDocument();
            configFile.Load(configFileName);
            XmlNode root = configFile.DocumentElement;
            XmlNode myNode = configFile.SelectSingleNode(@"configuration/applicationSettings/EvoPrinterUI.Properties.Settings/setting[@name='GSBinDirectory']");
            myNode.FirstChild.InnerText = gsPath;

            myNode = configFile.SelectSingleNode(@"configuration/applicationSettings/EvoPrinterUI.Properties.Settings/setting[@name='ps2pdfPath']");
            myNode.FirstChild.InnerText = ps2pdfPath;

            
            configFile.Save(configFileName);

        }

        [CustomAction]
        public static ActionResult setupPrinterDriver(Session session)
        {
            
            /*
            var g1 = 5 * 60; //5 min
            while (g1-- > 0)
            {
                System.Threading.Thread.Sleep(1000); //1 sec
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

                //EnusreGS(execParam + ".config");

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
