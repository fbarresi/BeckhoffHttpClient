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
                var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
                var targetType = symbolInfo.DataType.ManagedType;
                var targetValue = targetType != null ? Convert.ChangeType(value, targetType) : value;
                client.WriteSymbol(symbolInfo, targetValue);
            });
        }

        public static Task<T> ReadAsync<T>(this TcAdsClient client, string variablePath)
        {
            return Task.Run(() =>
            {
                var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
                var obj = client.ReadSymbol(symbolInfo);
                return (T) Convert.ChangeType(obj, typeof(T));
            });
        }

        public static Task WriteJson(this TcAdsClient client, string variablePath, JObject obj)
        {
            return WriteRecursive(client, variablePath, obj, string.Empty);
        }

        public static async Task WriteRecursive(this TcAdsClient client, string variablePath, JObject parent, string jsonName)
        {
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var dataType = symbolInfo.DataType;
            {
                if (dataType.Category == DataTypeCategory.Array)
                {
                    var array = parent.SelectToken(jsonName) as JArray;
                    var elementCount = array.Count < dataType.Dimensions.ElementCount ? array.Count : dataType.Dimensions.ElementCount;
                    for (int i = 0; i < elementCount; i++)
                    {
                        if (dataType.BaseType.ManagedType != null)
                            await WriteAsync(client, variablePath + $"[{i + dataType.Dimensions.LowerBounds.First()}]", array[i]);
                        else
                        {
                            await WriteRecursive(client, variablePath + $"[{i + dataType.Dimensions.LowerBounds.First()}]", parent, jsonName + $"[{i}]");
                        }
                    }
                }
                else if (dataType.ManagedType == null)
                {
                    if (dataType.SubItems.Any())
                    {
                        foreach (var subItem in dataType.SubItems)
                        {
                            if (HasJsonName(subItem))
                            {
                                await WriteRecursive(client, variablePath + "." + subItem.SubItemName, parent, string.IsNullOrEmpty(jsonName) ? GetJsonName(subItem) : jsonName + "." + GetJsonName(subItem));
                            }
                        }
                    }
                }
                else
                {
                    await WriteAsync(client, symbolInfo.Name, parent.SelectToken(jsonName));
                }
            }

        }
        public static Task<JObject> ReadJson(this TcAdsClient client, string variablePath)
        {
            return Task.Run(() => ReadRecursive(client, variablePath, new JObject(), GetVaribleNameFromFullPath(variablePath)));
        }

        public static JObject ReadRecursive(TcAdsClient client, string variablePath, JObject parent, string jsonName, bool isChild = false)
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
                        if (isChild)
                            parent.Add(jsonName, child);
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