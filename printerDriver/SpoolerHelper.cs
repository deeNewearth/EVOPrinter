/*
Printer++ Virtual Printer Processor
Copyright (C) 2012 - Printer++

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using Microsoft.Win32;
using System.Linq;

namespace printerDriver
{
    public class SpoolerHelper
    {
        readonly LogHelper _logHelper;



        public SpoolerHelper(LogHelper logHelper=null)
        {
            _logHelper = null == logHelper?new LogHelper(null):logHelper;
        }


        #region PInvoke Codes
        #region Printer Monitor
        //API for Adding Print Monitors

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 DeleteMonitor(String pName, String pEnvironment, String pMonitorName);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumMonitors(string pName, uint level, IntPtr pMonitors, uint cbBuf, ref uint pcbNeeded, ref uint pcReturned);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITOR_INFO_2
        {
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pEnvironment;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDLLName;
        }
        //http://msdn.microsoft.com/en-us/library/windows/desktop/dd183341(v=vs.85).aspx
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 AddMonitor(String pName, UInt32 Level, ref MONITOR_INFO_2 pMonitors);
        /*[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITOR_INFO_2
        {
            public string pName;
            public string pEnvironment;
            public string pDLLName;
        }*/
        #endregion
        #region Printer Port
        private const int MAX_PORTNAME_LEN = 64;
        private const int MAX_NETWORKNAME_LEN = 49;
        private const int MAX_SNMP_COMMUNITY_STR_LEN = 33;
        private const int MAX_QUEUENAME_LEN = 33;
        private const int MAX_IPADDR_STR_LEN = 16;
        private const int RESERVED_BYTE_ARRAY_SIZE = 540;

        private enum PrinterAccess
        {
            ServerAdmin = 0x01,
            ServerEnum = 0x02,
            PrinterAdmin = 0x04,
            PrinterUse = 0x08,
            JobAdmin = 0x10,
            JobRead = 0x20,
            StandardRightsRequired = 0x000f0000,
            PrinterAllAccess = (StandardRightsRequired | PrinterAdmin | PrinterUse)
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PrinterDefaults
        {
            public IntPtr pDataType;
            public IntPtr pDevMode;
            public PrinterAccess DesiredAccess;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PortData
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PORTNAME_LEN)]
            public string sztPortName;
        }

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool OpenPrinter(string printerName, out IntPtr phPrinter, ref PrinterDefaults printerDefaults);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr phPrinter);
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool XcvDataW(IntPtr hXcv, string pszDataName, IntPtr pInputData, UInt32 cbInputData, out IntPtr pOutputData, UInt32 cbOutputData, out UInt32 pcbOutputNeeded, out UInt32 pdwStatus);

        #endregion
        #region Printer Driver
        //API for Adding Printer Driver
        //http://msdn.microsoft.com/en-us/library/windows/desktop/dd183346(v=vs.85).aspx
        //http://pinvoke.net/default.aspx/winspool.DRIVER_INFO_2
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 AddPrinterDriver(String pName, UInt32 Level, ref DRIVER_INFO_3 pDriverInfo);

        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 DeletePrinterDriver(String pName, String pEnvironment, String pDriverName);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DRIVER_INFO_3
        {
            public uint cVersion;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pEnvironment;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDriverPath;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDataFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pConfigFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pHelpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDependentFiles;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pMonitorName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDefaultDataType;
        }
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool GetPrinterDriverDirectory(StringBuilder pName, StringBuilder pEnv, int Level, [Out] StringBuilder outPath, int bufferSize, ref int Bytes);
        #endregion
        #region Printer
        //API for Adding Printer
        
        //http://msdn.microsoft.com/en-us/library/windows/desktop/dd183343(v=vs.85).aspx
        [DllImport("winspool.drv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern IntPtr AddPrinter(string pName, uint Level, [In] ref PRINTER_INFO_2 pPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        static extern bool DeletePrinter(IntPtr hPrinter);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }
        #endregion
        #endregion

        public const int ERROR_INSUFFICIENT_BUFFER = 122;
        public List<MONITOR_INFO_2> GetMonitors()
        {
            List<MONITOR_INFO_2> ports = new List<MONITOR_INFO_2>();
            uint pcbNeeded = 0;
            uint pcReturned = 0;

            if (EnumMonitors(null, 2, IntPtr.Zero, 0, ref pcbNeeded, ref pcReturned))
            {
                //succeeds, but must not, because buffer is zero (too small)!
                throw new Exception("EnumMonitors should fail!");
            }

            int lastWin32Error = Marshal.GetLastWin32Error();
            if (lastWin32Error == ERROR_INSUFFICIENT_BUFFER)
            {

                IntPtr pMonitors = Marshal.AllocHGlobal((int)pcbNeeded);
                if (EnumMonitors(null, 2, pMonitors, pcbNeeded, ref pcbNeeded, ref pcReturned))
                {
                    IntPtr currentMonitor = pMonitors;

                    for (int i = 0; i < pcReturned; i++)
                    {
                        ports.Add((MONITOR_INFO_2)Marshal.PtrToStructure(currentMonitor, typeof(MONITOR_INFO_2)));
                        currentMonitor = (IntPtr)(currentMonitor.ToInt32() + Marshal.SizeOf(typeof(MONITOR_INFO_2)));
                    }
                    Marshal.FreeHGlobal(pMonitors);

                    return ports;
                }
            }
            throw new Win32Exception(Marshal.GetLastWin32Error());

        }


        public void RemovePrinterMonitor(string monitorName)
        {
            var myMon = GetMonitors().SingleOrDefault(m => m.pName == monitorName);
            if (String.IsNullOrWhiteSpace(myMon.pName))
            {
                _logHelper.Log("The monitor is not installed");
                return;
            }

            MONITOR_INFO_2 mi2 = new MONITOR_INFO_2();

            mi2.pName = monitorName;
            mi2.pEnvironment = null;
            mi2.pDLLName = "mfilemon.dll";

            if (DeleteMonitor(null, null, monitorName) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void AddPrinterMonitor(string monitorName)
        {
            var myMon = GetMonitors().SingleOrDefault(m => m.pName == monitorName);
            if (!String.IsNullOrWhiteSpace(myMon.pName))
            {
                _logHelper.Log("The monitor is already installed");
                return;
            }


            MONITOR_INFO_2 mi2 = new MONITOR_INFO_2();

            mi2.pName = monitorName;
            mi2.pEnvironment = null;
            mi2.pDLLName = "mfilemon.dll";

            if (AddMonitor(null, 2, ref mi2) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void AddDeletePrinterPort(string portName, string monitorName, bool deletePort = false)
        {
            IntPtr printerHandle;
            PrinterDefaults defaults = new PrinterDefaults { DesiredAccess = PrinterAccess.ServerAdmin };
            if (!OpenPrinter(",XcvMonitor " + monitorName, out printerHandle, ref defaults))
                throw new Exception("Could not open printer for the monitor port " + monitorName + "!");

            try
            {
                PortData portData = new PortData { sztPortName = portName };
                uint size = (uint)Marshal.SizeOf(portData);
                IntPtr pointer = Marshal.AllocHGlobal((int)size);
                Marshal.StructureToPtr(portData, pointer, true);
                IntPtr outputData;
                UInt32 outputNeeded, status;
                if (!XcvDataW(printerHandle, deletePort ? "DeletePort" : "AddPort", pointer, size, out outputData, 0, out outputNeeded, out status))
                    throw new Exception(status.ToString());
            }
            finally
            {
                ClosePrinter(printerHandle);
            }
        }

        public GenericResult GetPrinterDirectory()
        {
            GenericResult retVal = new GenericResult("GetPrinterDirectory");
            StringBuilder str = new StringBuilder(1024);
            int i = 0;
            GetPrinterDriverDirectory(null, null, 1, str, 1024, ref i);
            try
            {
                GetPrinterDriverDirectory(null, null, 1, str, 1024, ref i);
                retVal.Success = true;
                retVal.Message = str.ToString();
            }
            catch (Exception ex)
            {
                retVal.Exception = ex;
                retVal.Message = retVal.Exception.Message;
            }
            return retVal;
        }

        public void AddPrinterDriver(string driverName, string driverPath, string dataPath, string configPath, string helpPath)
        {
            
            DRIVER_INFO_3 di = new DRIVER_INFO_3();
            di.cVersion = 3;
            di.pName = driverName;
            di.pEnvironment = null;
            di.pDriverPath = driverPath;
            di.pDataFile = dataPath;
            di.pConfigFile = configPath;
            di.pHelpFile = helpPath;
            di.pDependentFiles = "";
            di.pMonitorName = null;
            di.pDefaultDataType = "RAW";
            if (AddPrinterDriver(null, 3, ref di) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

        }

        public void AddPrinter(string printerName, string portName, string driverName)
        {
            PRINTER_INFO_2 pi = new PRINTER_INFO_2();

            pi.pServerName = null;
            pi.pPrinterName = printerName;
            pi.pShareName = "";
            pi.pPortName = portName;
            pi.pDriverName = driverName;    // "Apple Color LW 12/660 PS";
            pi.pComment = "PrintToEVO";
            pi.pLocation = "";
            pi.pDevMode = new IntPtr(0);
            pi.pSepFile = "";
            pi.pPrintProcessor = "WinPrint";
            pi.pDatatype = "RAW";
            pi.pParameters = "";
            pi.pSecurityDescriptor = new IntPtr(0);

            var hPrt = AddPrinter(null, 2, ref pi);
            if (hPrt == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            ClosePrinter(hPrt);
        }

        void removeVirtualPort(string monitorName, string portName)
        {
            try
            {
                string keyName = string.Format(@"SYSTEM\CurrentControlSet\Control\Print\Monitors\{0}\{1}", monitorName, portName);
                Registry.LocalMachine.DeleteSubKey(keyName);
            }
            catch (Exception ex)
            {
                _logHelper.Log("Failed to remove reg key : "+ ex.Message);
            }
        }

        void ConfigureVirtualPort(string monitorName, string portName, string userCommand)
        {
            string appPath = @"C:\EvoPrinterQUEUE";
            string outputPath = string.Format(@"{0}\Temp", appPath);
            string filePattern = "%r_%c_%u_%Y%m%d_%H%n%s_%j.ps";
             
            var execPath = appPath;

            string keyName = string.Format(@"SYSTEM\CurrentControlSet\Control\Print\Monitors\{0}\{1}", monitorName, portName);
            Registry.LocalMachine.CreateSubKey(keyName);
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(keyName, true);
            regKey.SetValue("OutputPath", outputPath, RegistryValueKind.String);
            regKey.SetValue("FilePattern", filePattern, RegistryValueKind.String);
            regKey.SetValue("Overwrite", 0, RegistryValueKind.DWord);
            regKey.SetValue("UserCommand", userCommand, RegistryValueKind.String);
            regKey.SetValue("ExecPath", execPath, RegistryValueKind.String);
            regKey.SetValue("WaitTermination", 0, RegistryValueKind.DWord);
            regKey.SetValue("PipeData", 0, RegistryValueKind.DWord);
            regKey.Close();
        }

        public GenericResult RestartSpoolService()
        {
            GenericResult retVal = new GenericResult("RestartSpoolService");
            try
            {
                ServiceController sc = new ServiceController("Spooler");
                if (sc.Status != ServiceControllerStatus.Stopped || sc.Status != ServiceControllerStatus.StopPending)
                    sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);
                sc.Start();
                retVal.Success = true;
            }
            catch (Exception ex)
            {
                retVal.Exception = ex;
                retVal.Message = retVal.Exception.Message;
            }
            return retVal;
        }

        struct PrinterProps
        {
            public String portName {get;set;}
            public String _monitorName { get; set; }
            public String driverName { get; set; }

            public static PrinterProps getProps(string printerName)
            {
                return new PrinterProps
                {
                    portName = string.Format("{0}:", printerName),
                    _monitorName = string.Format("{0} Monitor", printerName),
                    driverName = string.Format("{0} Driver", printerName),
                };
            }
        }


        public void RemoveVPrinter(string printerName)
        {
            var printerProps = PrinterProps.getProps(printerName);

            //for removal we try to go ahead even if there are some failures
            
            //5 - Configure Virtual Port
            removeVirtualPort(printerProps._monitorName, printerProps.portName);
            _logHelper.Log("removeVirtualPort Completed");


            //4 - Add Printer
            try
            {
                var printerDefaults = new PrinterDefaults
                {
                    DesiredAccess = PrinterAccess.PrinterAllAccess,  //0x000F000C,//PRINTER_ALL_ACCESS
                    pDataType = IntPtr.Zero,
                    pDevMode = IntPtr.Zero
                };
                IntPtr printerHandle;
                if (!OpenPrinter(printerName, out printerHandle, ref printerDefaults))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                _logHelper.Log("OpenPrinter Completed");

                try
                {
                    if (!DeletePrinter(printerHandle))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    _logHelper.Log("DeletePrinter Completed");
                }
                finally
                {
                    if (IntPtr.Zero != printerHandle)
                        ClosePrinter(printerHandle);
                }
            }
            catch (Exception ex)
            {
                _logHelper.Log("Failed to renove printer : " + ex.Message);
            }


            //3 - Add Printer Driver
            if (DeletePrinterDriver(null, null, printerProps.driverName) == 0)
            {
                var ex = new Win32Exception(Marshal.GetLastWin32Error());
                _logHelper.Log("ERROR in custom action AddDeletePrinterPort : " + ex.ToString());
                //
            }
            _logHelper.Log("DeletePrinterDriver Completed");

            //2 - Add Printer Port
            try
            {
                AddDeletePrinterPort(printerProps.portName, printerProps._monitorName, true);
                _logHelper.Log("AddDeletePrinterPort Completed");
            }
            catch (Exception ex)
            {
                _logHelper.Log("ERROR in custom action AddDeletePrinterPort : " + ex.ToString());
            }

            
            //1 - Add Printer Monitor
            RemovePrinterMonitor(printerProps._monitorName);
            _logHelper.Log("RemovePrinterMonitor Completed");

            //6 - Restart Spool Service
            _logHelper.Log("Restarting Spool Service");
            GenericResult restartSpoolResult = RestartSpoolService();
            if (restartSpoolResult.Success == false)
                throw restartSpoolResult.Exception;


        }



        public void AddVPrinter(string printerName, string Execpath)
        {
            var printerProps = PrinterProps.getProps(printerName);
            string key = printerName;

            string driverFileName = "PSCRIPT5.DLL";
            
            //string dataFileName = "PRINTTOEVO.PPD";
            string dataFileName = "ghostpdf.ppd";
            
            string configFileName = "PS5UI.DLL";
            string helpFileName = "PSCRIPT.HLP";

            
            string driverPath = @"C:\WINDOWS\system32\spool\drivers\w32x86\PSCRIPT5.DLL";

            string dataPath = @"C:\WINDOWS\system32\spool\drivers\w32x86\" + dataFileName;
            
            string configPath = @"C:\WINDOWS\system32\spool\drivers\w32x86\PS5UI.DLL";
            string helpPath = @"C:\WINDOWS\system32\spool\drivers\w32x86\PSCRIPT.HLP";

            //0 - Set Printer Driver Path and Files
            _logHelper.Log("Setting Driver Path and Files.");
            GenericResult printerDriverPath = GetPrinterDirectory();
            if (printerDriverPath.Success == true)
            {
                driverPath = string.Format("{0}\\{1}", printerDriverPath.Message, driverFileName);
                dataPath = string.Format("{0}\\{1}", printerDriverPath.Message, dataFileName);
                configPath = string.Format("{0}\\{1}", printerDriverPath.Message, configFileName);
                helpPath = string.Format("{0}\\{1}", printerDriverPath.Message, helpFileName);
            }

            //1 - Add Printer Monitor
            _logHelper.Log("Adding Printer Monitor.");
            AddPrinterMonitor(printerProps._monitorName);

                   
            //2 - Add Printer Port
            _logHelper.Log("Adding Printer Port.");
            AddDeletePrinterPort(printerProps.portName, printerProps._monitorName);
                   
            //3 - Add Printer Driver
            _logHelper.Log("Adding Printer Driver.");
            AddPrinterDriver(printerProps.driverName, driverPath, dataPath, configPath, helpPath);
                   
            //4 - Add Printer
            _logHelper.Log("Adding Printer");
            AddPrinter(printerName, printerProps.portName, printerProps.driverName);
            
            //5 - Configure Virtual Port
            _logHelper.Log("Configuring Virtual Port");
            ConfigureVirtualPort(printerProps._monitorName, printerProps.portName, Execpath);
            
            //6 - Restart Spool Service
            _logHelper.Log("Restarting Spool Service");
            GenericResult restartSpoolResult = RestartSpoolService();
            if (restartSpoolResult.Success == false)
                throw restartSpoolResult.Exception;

            _logHelper.Log("AddVPrinter Success");
        }


        public class GenericResult
        {
            public GenericResult(string method)
            {
                Success = false;
                Message = string.Empty;
                Exception = null;
                _method = method;
            }
            public bool Success { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
            private string _method;
            public string Method
            {
                get { return _method; }
            }
        }
    }
}
