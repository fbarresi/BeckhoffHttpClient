﻿using System;
using System.Collections.Generic;
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
            var adsClient = new AdsClient();
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            CreateLogger();
            var logger = LoggerFactory.GetLogger();
            var header = GetOrCreateHeader();

            try
            {

                logger.Debug("Starting API Call");
                logger.Debug($"Connecting to Beckhoff Port: {AdsPort} - AdsNet: '{AdsNetId}'");
                
                if (string.IsNullOrEmpty(AdsNetId))
                {
                    adsClient.Connect(AdsPort);
                }
                else
                {
                    adsClient.Connect(AdsNetId, AdsPort);
                }
                
                logger.Debug($"Beckhoff connection state: {adsClient.ConnectionState}");

                var callAddress = Address.IsValidUrl() ? Address : await adsClient.ReadAsync<string>(Address);
                logger.Debug($"Url: {callAddress}");
                logger.Debug($"Method: {Method}");

                var restClient = new RestClient();
                var request = new RestRequest(callAddress, Method);

                foreach (var item in header)
                {
                    logger.Debug($"Adding header: \"{item.Key}\" : \"{item.Value}\"");
                    request.AddHeader(item.Key, item.Value);
                }

                var jsonBody = !string.IsNullOrEmpty(Body) ? await adsClient.ReadJson(Body) : new JObject();
                request.RequestFormat = DataFormat.Json;
                request.AddParameter("application/json", jsonBody.ToString(), ParameterType.RequestBody);
                logger.Debug($"Body: {jsonBody}");
                
                logger.Debug($"Executing...");
                var response = await restClient.ExecuteTaskAsync(request);

                logger.Debug($"Response code: {response.StatusCode}");
                logger.Debug($"Response content: {response.Content}");

                if (!string.IsNullOrEmpty(ResponseCode))
                {
                    logger.Debug($"Writing status code into {ResponseCode}...");
                    await adsClient.WriteAsync(ResponseCode, response.StatusCode);
                    logger.Debug($"Written status code into {ResponseCode}.");
                }

                //Try parsing object or array from response content
                logger.Debug($"Writing json response into {Response}...");
                try
                {
                    var objectResponse = JObject.Parse(response.Content);
                    await adsClient.WriteJson(Response, objectResponse);
                    logger.Debug($"Written response into {Response}.");
                }
                catch (JsonReaderException exception)
                {
                    logger.Error("Unable to write response content parsing a json object - 2nd try with json array", exception);
                    
                    try
                    {
                        var arrayResponse = JArray.Parse(response.Content);
                        await adsClient.WriteJson(Response, arrayResponse);
                        logger.Debug($"Written response into {Response}.");
                    }
                    catch (JsonReaderException innerException)
                    {
                        logger.Error("Unable to write response content parsing a json array", innerException);
                    }
                }
                logger.Debug($"Disconnecting to Beckhoff");
                adsClient.Disconnect();
            }
            catch (Exception e)
            {
                logger.Error($"Error while calling API: {e}", e);
                logger.Error($"Stack Trace:\n{e.StackTrace}");
            }
            finally
            {
                adsClient?.Dispose();
            }
        }

        private Dictionary<string, string> GetOrCreateHeader()
        {
            var logger = LoggerFactory.GetLogger();
            var header = new Dictionary<string, string>();
            var headerfile = "header.json";

            if (File.Exists(headerfile))
            {
                logger.Debug("Reading header file (header.json)...");
                try
                {
                    var text = File.ReadAllText(headerfile);
                    header = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                }
                catch (Exception e)
                {
                    logger.Error("Error while reading the header file - skipping header parsing", e);
                }
            }
            else
            {
                logger.Debug("No header file found...");
                logger.Debug("Writing an example header");
                var example = new Dictionary<string, string>{{"api_key","D9C80CF1-C910-41F4-BD7B-D51B72B573AA"},{"Authentication", "api_key"}};
                var exampleFile = JsonConvert.SerializeObject(example, Formatting.Indented);
                File.WriteAllText("header_example.json", exampleFile);
            }

            return header;
        }
    }
}
