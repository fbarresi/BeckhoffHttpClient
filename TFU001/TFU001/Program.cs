using System;
using System.Xml.Serialization;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Serialization.Json;

namespace TFU001
{
    public class Program
    {
        public static int Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        [Argument(0, "Address")]
        public string Address { get; set; } = "https://dog.ceo/api/breeds/image/random";
        [Argument(1, "Call Method")]public Method Method { get; set; }
        [Argument(2, "Headers")] public string Headers { get; set; }
        [Argument(3, "Parameters")] public string Parameters { get; set; }


        public void OnExecute()
        {
            var client = new RestClient();
            var request = new RestRequest(Address, Method);
            //request.AddParameter("name", "value");
            //request.AddHeader("header", "value");
            var response = client.Execute(request);
            Console.WriteLine(response.Content);

            var jsonObject = JsonConvert.DeserializeObject(response.Content);
            Console.WriteLine($"{response.StatusCode} - {(int)response.StatusCode}" );
        }
    }
}
