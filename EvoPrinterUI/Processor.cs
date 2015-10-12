using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using com.labizbille.evoSDK;
using Newtonsoft.Json;

namespace EvoPrinterUI
{
    class StateHolder : Modelbase
    {
        /// <summary>
        /// We trigger a change on this to start height/width animation
        /// </summary>
        public object dummy { get; set; }

        public processorBase currDisplay
        {
            get { return GetValue(() => currDisplay); }
            set
            {
                var curr = GetValue(() => currDisplay);
                if (curr == value)
                    return;

                if (null != curr)
                    curr.NextStateEvent -= OnNextStateEvent;

                SetValue(() => currDisplay, value);

                Task.Run(async () =>
                {
                    await Task.Delay(100);
                    NotifyPropertyChanged(() => dummy);
                });

                value.NextStateEvent += OnNextStateEvent;

            }
        }

        void OnNextStateEvent(processorBase obj)
        {
            currDisplay = obj;
        }

    };

    abstract class processorBase : Modelbase
    {
        public bool isWindowTopMost
        {
            get { return GetValue(() => isWindowTopMost); }
            set { SetValue(() => isWindowTopMost, value); }
        }

        public String Status
        {
            get { return GetValue(() => Status); }
            set { SetValue(() => Status, value); }
        }

      
        public String Error
        {
            get { return GetValue(() => Error); }
            set { SetValue(() => Error, value); }
        }


        readonly double _DesignHeight;
        public double DesignHeight { get { return _DesignHeight; } }

        readonly double _DesignWidth;
        public double DesignWidth { get { return _DesignWidth; } }


        public event Action<processorBase> NextStateEvent;

        protected void SetNextState(processorBase next)
        {
            if(null != NextStateEvent)
                NextStateEvent(next);
        }

        public processorBase(double designHeight = 300, double designWidth = 400)
        {
            _DesignHeight = designHeight;
            _DesignWidth = designWidth;
        }

    }

    class ProcessorResult : processorBase
    {

        public String indexURL 
        {
            get { return GetValue(() => indexURL); }
            set { SetValue(() => indexURL, value); }
        }

    }

    class ProcessorWindow : processorBase
    {
        readonly String _loginToken;
        public ProcessorWindow(String LoginToken) : base(550, 600)
        {
            _loginToken = LoginToken;
        }

        public String loginToken { get { return _loginToken; } }

        public String indexURL
        {
            get { return GetValue(() => indexURL); }
            set { SetValue(() => indexURL, value); }
        }

        public void OnLoadError(String error)
        {
            SetNextState(new ProcessorResult
            {
                Error = error
            });
        }

    }



    class Processor : processorBase, IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
       /* readonly static String _tmpFolder = 
        */
        readonly static String _tmpFolder;

        static Processor()
        {
            _tmpFolder = Properties.Settings.Default.tmpfileFolder;
            if(String.IsNullOrWhiteSpace(_tmpFolder))
                _tmpFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EvoPrinterUI");

            log.Info("Using _tmpFolder " + _tmpFolder);

            //clean up any stuff left
            if (!Directory.Exists(_tmpFolder))
            {
                log.Info("Creating Folder " + _tmpFolder);
                Directory.CreateDirectory(_tmpFolder);
                return;
            }

            var di = new DirectoryInfo(_tmpFolder);

            foreach (FileInfo file in di.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch { }
            }
        }

        public event Action OnCLoseEvent;
        readonly TimeSpan _resultDuration = TimeSpan.FromMinutes(5);
        //readonly TimeSpan _resultDuration = TimeSpan.FromSeconds(5);
        void CLoseit()
        {
            Task.Run(async () =>
            {
                await Task.Delay(_resultDuration);
                if (null != OnCLoseEvent)
                    OnCLoseEvent();
            });
        }


        public bool discoveryCompleted
        {
            get { return GetValue(() => discoveryCompleted); }
            set { SetValue(() => discoveryCompleted, value); }
        }



        Uri[] _evoServers = new Uri[] { };
        public Uri[] evoServers
        {
            get { return _evoServers; }
            set
            {
                _evoServers = value;


                if (null != selectedServer &&
                        null == evoServers.FirstOrDefault(u => u.DnsSafeHost == selectedServer.DnsSafeHost
                                                    && u.Port == selectedServer.Port))
                {
                    _evoServers = _evoServers.Concat(new[] { selectedServer }).ToArray();
                }

                NotifyPropertyChanged(() => evoServers);

                if (null == selectedServer)
                    selectedServer = _evoServers.FirstOrDefault();
            }
        }


        [Required]
        public Uri selectedServer
        {
            get { return GetValue(() => selectedServer); }
            set { SetValue(() => selectedServer, value); }
        }

        public bool postBtnAvailable 
        {
            get { return GetValue(() => postBtnAvailable); }
            set { SetValue(() => postBtnAvailable, value); }
        }

        [Required]
        public String UserName 
        {
            get { return GetValue(() => UserName); }
            set { SetValue(() => UserName, value); }
        }

        public String Password
        {
            get { return GetValue(() => Password); }
            set { SetValue(() => Password, value); }
        }

        public String indexURL 
        {
            get { return GetValue(() => indexURL); }
            set { SetValue(() => indexURL, value); }
        }

        public String ghostMessage 
        {
            get { return GetValue(() => ghostMessage); }
            set { SetValue(() => ghostMessage, value); }
        }


        readonly Task _ghostTask;
        readonly String _outputPdfPath;
        public Processor()
        {
#if RELEASE
            isWindowTopMost = true;
#endif

            postBtnAvailable = true;
            _outputPdfPath = Path.Combine(_tmpFolder,
                                 string.Format("Page-{0}.pdf", Guid.NewGuid()));

            
            _ghostTask = GhostIT();
            Task.WhenAll(Task.Run(()=> findEvo()), _ghostTask);
            
        }

        bool _bIsDisposed = false;
        public void Dispose()
        {
            try
            {
                if (File.Exists(_outputPdfPath))
                    File.Delete(_outputPdfPath);

                _bIsDisposed = true;
            }catch{}
        }

        ~Processor()
        {
            if (!_bIsDisposed)
                Dispose();
        }

        OperationContextScope credsCall(IEVOService_v1  evo)
        {
            var ret = new OperationContextScope((IContextChannel)evo);
            {
                var creds = System.Text.Encoding.UTF8.GetBytes(
                            String.Format("{0}:{1}", UserName.Trim(), Password.Trim()));
                WebOperationContext.Current.OutgoingRequest.Headers.Add("Authorization",
                                                        "Basic " + System.Convert.ToBase64String(creds));
            }
            return ret;
        }


        public void Post()
        {
            Error = null;

            if (String.IsNullOrWhiteSpace(UserName))
            {
                Error = "User name is required";
                return;
            }

            if (null == selectedServer)
            {
                Error = "No evo server selected";
                return;
            }

            

            Task.Run(async () =>
                {
                    var oldGhsotMessage = ghostMessage;
                    try
                    {
                        postBtnAvailable = false;

                        //just making sure that ghost task is complete
                        _ghostTask.Wait() ;


                        await Task.Run(() =>
                        {
                            oldGhsotMessage = ghostMessage;
                            ghostMessage = null;
                            Status = "Sending pages";

                            using (var channelFactory =
                                        new ChannelFactory<com.labizbille.evoSDK.IEVOService_v1>(new WebHttpBinding(),
                                            selectedServer.AbsoluteUri))
                            {
                                channelFactory.Endpoint.Behaviors.Add(new WebHttpBehavior());
                                var evo = channelFactory.CreateChannel();

                                dynamic JobContext;

                                using (credsCall(evo))
                                using (var br = new FileStream(_outputPdfPath, FileMode.Open))
                                {
                                    WebOperationContext.Current.OutgoingRequest.Headers.Add("filename", "print.pdf");
                                    
                                    JobContext = new
                                    {
                                        jobName ="NewDocfromCartPages",
                                        cartPages = evo.AddImagesToCart(br)
                                    };
                                }

                                using (credsCall(evo))
                                {
                                    var JobCode = evo.CreateShortCode(new JobCodeRequest
                                    {
                                        JobContext = JsonConvert.SerializeObject(JobContext),
                                        expiration = DateTime.Now + _resultDuration,
                                        AppName="evoPrinter",
                                        AppSecret="not used"
                                    });

                                    CLoseit();

                                    var linkURL = string.Format("http://{0}:{1}/#/docviewer_newDoc?routeContext=shortcode:{2}",
                                            selectedServer.DnsSafeHost, selectedServer.Port, JobCode.code);

                                    //Process.Start(linkURL);

                                    SetNextState(new ProcessorWindow(
                                        System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                                            String.Format("{0}:{1}", UserName.Trim(), JobCode.LoginToken.Trim())) )
                                        )
                                    {
                                        indexURL = linkURL
                                    });

                                    try
                                    {
#if RELEASE
                                        if (File.Exists(_inputPdfPath))
                                            File.Delete(_inputPdfPath);
#endif
                                    }
                                    catch { }

                                }

                            }

                        });
                        
                    }

                    catch (Exception ex)
                    {
                        Error = ex.Message;
                    }
                    finally
                    {
                        postBtnAvailable = true;
                        ghostMessage=oldGhsotMessage;
                        Status = null;

                    }
                });

            
        }

        readonly object _discoveryLock = new object();
        
        void findEvo()
        {
            try
            {
                using (var discovery = new DiscoveryClient(new UdpDiscoveryEndpoint()))
                {
                    discovery.FindProgressChanged += (s, e) =>
                    {
                        try
                        {
                            if (null != e.EndpointDiscoveryMetadata)
                            {
                                var port = e.EndpointDiscoveryMetadata.Address.Uri.Port;
                                var host = e.EndpointDiscoveryMetadata.Address.Uri.DnsSafeHost;

                                lock (_discoveryLock)
                                {
                                    if (null == evoServers.FirstOrDefault(u=>u.DnsSafeHost == host && u.Port == port ) )
                                        evoServers = evoServers.Concat(new[] { e.EndpointDiscoveryMetadata.Address.Uri }).ToArray();
                                }

                                discoveryCompleted = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            var t = ex.Message;
                        }
                    };

                    discovery.FindAsync(new FindCriteria(typeof(com.labizbille.evoSDK.IEVOService_v1)));

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to discover EVO servers : " + ex.Message);
            }
            finally
            {
                discoveryCompleted = true;
            }
        }

        /*
        async Task EnsureGHostScriptInstalled()
        {
             
        }
         */

        String _inputPdfPath = null;

        readonly String _logFile = Path.Combine(_tmpFolder,String.Format("{0}.log", Guid.NewGuid()));

        async Task GhostIT()
        {
            log.Info("Starting GhostIT");

            
            Status = "Processing print job";
            ghostMessage = null;

            /*using(System.IO.StreamWriter LogStream =
                                    new System.IO.StreamWriter(_logFile))*/
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length < 3)
                {
                    //throw new InvalidOperationException("Printer data not received");
                    _inputPdfPath = @"C:\codework\srDocManager\SrDocumentManager\sample data\images\Boeing Purchase Orders.pdf";
                }
                else
                {
                    _inputPdfPath = args[2];
                }
                if (!File.Exists(_inputPdfPath))
                    throw new InvalidOperationException("Printer data file not found");

                if (File.Exists(_outputPdfPath))
                    throw new InvalidOperationException("Output file already exists");

               // Ensure
                log.Info("Starting Process");

                using (var p = new Process())
                {
                    // Redirect the output stream of the child process.
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;

                    //p.StartInfo.WorkingDirectory = @"C:\Program Files\gs\gs9.16\bin";
                    p.StartInfo.WorkingDirectory = Properties.Settings.Default.GSBinDirectory;

                    //"C:\Program Files\gs\gs9.16\lib\ps2pdf14.bat" C:\EvoPrinterQUEUE\Temp\gppd.ps C:\EvoPrinterQUEUE\Temp\gppd.pdf
                    //var execFile = @"C:\Program Files\gs\gs9.16\lib\ps2pdf14.bat";
                    var execFile = Properties.Settings.Default.ps2pdfPath;

                    p.StartInfo.FileName = String.Format("\"{0}\" \"{1}\" \"{2}\" ", execFile, _inputPdfPath, _outputPdfPath);
                    p.Start();

                    try
                    {

                        // Do not wait for the child process to exit before
                        // reading to the end of its redirected stream.
                        // p.WaitForExit();
                        // Read the output stream first and then wait.
                        var output = await p.StandardOutput.ReadToEndAsync();
                        var e = await p.StandardError.ReadToEndAsync();
                        if (!String.IsNullOrWhiteSpace(e))
                        {
                            throw new Exception("Failed to process print job : " + e);
                        }

                       var t = Regex.Matches(output, @"\[Page:(.*)\]");
                       //ghostMessage = String.Format("{0} pages ready to print", t.Count);
                        //9.18 is not sending any std output
                       ghostMessage = String.Format("Ready to print");

                       
                    }
                    finally
                    {
                        p.WaitForExit();
                    }

                }
            }
        
            catch (Exception ex)
            {
                SetNextState(new ProcessorResult
                {
                    Error = "Ghost error : " + ex.Message
                });

                CLoseit();

                throw ex;
            }
            finally
            {
                Status = null;
            }
        
        }



    }
}
