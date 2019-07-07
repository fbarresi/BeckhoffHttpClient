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
            return client.ReadRecursive(variablePath, new JObject(), GetVaribleNameFromFullPath(variablePath));
        }

        public static JObject ReadRecursive(this TcAdsClient client, string variablePath, JObject parent, string jsonName, bool isChild = false)
        {
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var dataType = symbolInfo.DataType;
            {
                if (dataType.ManagedType == null)
                {
                    if (dataType.SubItems.Any())
                    {
                        var child = new JObject();
                        foreach (var subItem in dataType.SubItems)
                        {
                            if (HasJsonName(subItem))
                            {
                                ReadRecursive(client, variablePath + "." + subItem.SubItemName, isChild ? child : parent, GetJsonName(subItem), true);
                            }
                        }
                        if (isChild) parent.Add(jsonName, child);
                    }
                }
                else
                {
                    var obj = client.ReadSymbol(symbolInfo);
                    parent.Add(jsonName, new JValue(obj));
                }
            }

            return parent;
        }

        public static string GetVaribleNameFromFullPath(this string variablePath)
        {
            return variablePath.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries).Last();
        }

        public static string GetJsonName(this ITcAdsSymbol5 symbol)
        {
            return symbol.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value ?? symbol.Name.GetVaribleNameFromFullPath();
        }
        public static string GetJsonName(this ITcAdsDataType dataType)
        {
            return dataType.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value;
        }

        public static bool HasJsonName(this ITcAdsSymbol5 symbol)
        {
            return symbol.Attributes.Any(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase));
        }
        public static bool HasJsonName(this ITcAdsDataType dataType)
        {
            return dataType.Attributes.Any(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool HasJsonName(this ITcAdsSubItem subItem)
        {
            return subItem.Attributes.Any(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase));
        }

    }
}