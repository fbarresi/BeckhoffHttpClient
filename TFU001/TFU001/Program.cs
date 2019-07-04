using System;
using System.Xml.Serialization;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;
using TwinCAT.Ads;

namespace TFU001
{
    public class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Argument(0, "Address")]
        public string Address { get; set; } = "https://dog.ceo/api/breeds/image/random";
        [Argument(1, "Method")] public Method Method { get; set; }
        [Argument(3, "ResponseCode")] public string ResponseCode { get; set; }
        [Argument(4, "Body")] public string Body { get; set; }
        [Argument(5, "Response")] public string Response { get; set; }

        [Option(ShortName = "AdsNetId")]public string AdsNetId { get; set; } = "";
        [Option(ShortName = "AdsPort")]public int AdsPort { get; set; } = 851;


        public void OnExecute()
        {
            Console.WriteLine($"ResponseCode : {ResponseCode}");
            var restClient = new RestClient();
            var request = new RestRequest(Address, Method);
            //request.AddParameter("name", "value");
            //request.AddHeader("header", "value");
            var response = restClient.Execute(request);
            Console.WriteLine(response.Content);
            var adsClient = new TcAdsClient { Synchronize = false, };
            adsClient.Connect(AdsNetId, AdsPort);
            var responseCodeHandle = adsClient.CreateVariableHandle(ResponseCode);
            adsClient.WriteAny(responseCodeHandle, (short)response.StatusCode);
            adsClient.Disconnect();
            adsClient.Dispose();
            var jsonObject = JsonConvert.DeserializeObject(response.Content);
            Console.WriteLine($"{response.StatusCode} - {(short)response.StatusCode}" );
        }
    }
}
