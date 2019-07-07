using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;

namespace TFU001.Extensions
{
    public static class AdsClientExtensions
    {
        public static Task WriteAsync<T>(this TcAdsClient client, string variablePath, T value)
        {
            return Task.Run(() =>
            {
                var handle = client.CreateVariableHandle(variablePath);
                var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
                var targetType = symbolInfo.DataType.ManagedType;
                var targetValue = targetType != null ? Convert.ChangeType(value, targetType) : value;
                client.WriteAny(handle, targetValue);
            });
        }

        public static Task<T> ReadAsync<T>(this TcAdsClient client, string variablePath)
        {
            return Task.Run(() =>
            {
                var handle = client.CreateVariableHandle(variablePath);
                var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
                var targetType = symbolInfo.DataType.ManagedType;
                var obj = client.ReadAny(handle, targetType);
                return (T) Convert.ChangeType(obj, typeof(T));
            });
        }

        public static JObject ReadJson(this TcAdsClient client, string variablePath)
        {
            return client.ReadRecursive(variablePath, true);
        }

        public static JObject ReadRecursive(this TcAdsClient client, string variablePath, bool force = false)
        {
            var jObject = new JObject();
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var dataType = symbolInfo.DataType;
            if (symbolInfo.HasJsonName() || force)
            {
                if (dataType.ManagedType == null)
                {
                    if (dataType.SubItems.Any())
                    {
                        jObject.Add(symbolInfo.GetJsonName(), new JObject(dataType.SubItems.Where(si => si.HasJsonName()).Select(si => client.ReadRecursive(variablePath + "." + si.SubItemName))));
                    }
                }
                else
                {
                    var obj = client.ReadSymbol(symbolInfo);
                    jObject.Add(symbolInfo.GetJsonName(), new JValue(obj));
                }
            }

            return jObject;
        }

        public static string GetVaribleNameFromFullPath(this string variablePath)
        {
            return variablePath.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        public static string GetJsonName(this ITcAdsSymbol5 symbol)
        {
            return symbol.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value ?? symbol.Name.GetVaribleNameFromFullPath();
        }
        public static string GetJsonName(this ITcAdsSubItem subItem)
        {
            return subItem.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value;
        }

        public static bool HasJsonName(this ITcAdsSymbol5 symbol)
        {
            return symbol.Attributes.Any(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool HasJsonName(this ITcAdsSubItem subItem)
        {
            return subItem.Attributes.Any(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase));
        }

    }
}