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
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            
            CreateLogger();
            var adsClient = await ConnectToBeckhoff();
            var callAddress = Address.IsValidUrl() ? Address : await adsClient.ReadAsync<string>(Address);            

            var restClient = new RestClient();
            var request = new RestRequest(callAddress, Method, DataFormat.Json);
            //todo: read body and create json
            //request.AddJsonBody()

            var response = await restClient.ExecuteTaskAsync(request);

            Console.WriteLine(response.Content);
            
            await adsClient.WriteAsync(ResponseCode, response.StatusCode);

            //todo: write response
            //var jsonObject = JsonConvert.DeserializeObject(response.Content);

            adsClient.Disconnect();
            adsClient.Dispose();
        }

        private Task<TcAdsClient> ConnectToBeckhoff()
        {
            var adsClient = new TcAdsClient { Synchronize = false, };
            adsClient.Connect(AdsNetId, AdsPort);
            return Task.FromResult(adsClient);
        }

        
    }
}
