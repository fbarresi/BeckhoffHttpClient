using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using log4net;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;
using TFU001.Extensions;
using TwinCAT.Ads;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using TwinCAT.JsonExtension;


namespace TFU001
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Argument(0, "Address")]
        public string Address { get; set; }
        [Argument(1, "Method")] public Method Method { get; set; }
        [Argument(3, "ResponseCode")] public string ResponseCode { get; set; }
        [Argument(4, "Body")] public string Body { get; set; }
        [Argument(5, "Response")] public string Response { get; set; }

        [Option(ShortName = "AdsNetId")] public string AdsNetId { get; set; } = "";
        [Option(ShortName = "AdsPort")] public int AdsPort { get; set; } = 851;

        private static void CreateLogger()
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log.config"));
            LogManager.CreateRepository(Constants.LoggingRepositoryName);
        }
        public async Task OnExecute()
        {
            TcAdsClient adsClient = new TcAdsClient { Synchronize = false, }; ;
            try
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);

                CreateLogger();
                var logger = LoggerFactory.GetLogger();

                logger.Debug("Starting API Call");
                logger.Debug($"Connecting to Beckhoff Port: {AdsPort} - AdsNet: '{AdsNetId}'");
                adsClient.Connect(AdsNetId, AdsPort);

                var callAddress = Address.IsValidUrl() ? Address : await adsClient.ReadAsync<string>(Address);
                logger.Debug($"Url: {callAddress}");
                logger.Debug($"Method: {Method}");

                var restClient = new RestClient();
                var request = new RestRequest(callAddress, Method);
                var jsonBody = adsClient.ReadJson(Body);
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("text/json", jsonBody.ToString(), ParameterType.RequestBody);
                logger.Debug($"Body: {jsonBody}");
                
                logger.Debug($"Executing...");
                var response = await restClient.ExecuteTaskAsync(request);

                logger.Debug($"Response code: {response.StatusCode}");
                logger.Debug($"Response content: {response.Content}");
                
                logger.Debug($"Wrinting status code...");
                await adsClient.WriteAsync(ResponseCode, response.StatusCode);

                var jsonResponse = JObject.Parse(response.Content);
                logger.Debug($"Wrinting json response...");
                await adsClient.WriteJson(Response, jsonResponse);

                adsClient.Disconnect();
                adsClient.Dispose();
            }
            catch (Exception e)
            {

            }
            finally
            {
                adsClient?.Dispose();
            }
        }
    }
}
