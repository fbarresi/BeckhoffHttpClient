using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

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
            return ReadRecursive(client, variablePath, new JObject(), GetVaribleNameFromFullPath(variablePath));
        }

        private static JObject ReadRecursive(this TcAdsClient client, string variablePath, JObject parent, string jsonName, bool isChild = false)
        {
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var dataType = symbolInfo.DataType;
            {
                if (dataType.Category == DataTypeCategory.Array)
                {
                    if (dataType.BaseType.ManagedType != null)
                    {
                        var obj = client.ReadSymbol(symbolInfo);
                        parent.Add(jsonName, new JArray(obj));
                    }
                    else
                    {
                        var array = new JArray();
                        for (int i = dataType.Dimensions.LowerBounds.First(); i <= dataType.Dimensions.UpperBounds.First(); i++)
                        {
                            var child = new JObject();
                            ReadRecursive(client, variablePath + $"[{i}]", child, jsonName, false);
                            array.Add(child);
                        }
                        parent.Add(jsonName, array);

                    }
                }
                else if (dataType.ManagedType == null)
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

        public static string GetJsonName(ITcAdsSymbol5 symbol)
        {
            var jsonName = symbol.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value;

            return string.IsNullOrEmpty(jsonName) ? GetVaribleNameFromFullPath(symbol.Name) : jsonName;
        }
        public static string GetJsonName(ITcAdsSubItem dataType)
        {
            var jsonName = dataType.Attributes.FirstOrDefault(attribute => attribute.Name.Equals("json", StringComparison.InvariantCultureIgnoreCase))?.Value;
            return string.IsNullOrEmpty(jsonName) ? GetVaribleNameFromFullPath(dataType.SubItemName) : jsonName;
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