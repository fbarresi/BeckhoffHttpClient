using System;
using System.Linq;
using System.Threading.Tasks;
using TwinCAT.Ads;

namespace TFU001.Extensions
{
    public static class AdsClientExtensions
    {
        public static Task WriteAsync<T>(this TcAdsClient client, string variablePath, T value)
        {
            var handle = client.CreateVariableHandle(variablePath);
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var targetType = symbolInfo.DataType.ManagedType;
            var targetValue = targetType != null ? Convert.ChangeType(value, targetType) : value;
            return Task.Run(() => client.WriteAny(handle, targetValue));
        }

        public static Task<T> ReadAsync<T>(this TcAdsClient client, string variablePath)
        {
            var handle = client.CreateVariableHandle(variablePath);
            var symbolInfo = (ITcAdsSymbol5)client.ReadSymbolInfo(variablePath);
            var targetType = symbolInfo.DataType.ManagedType;
            return Task.Run(() =>
            {
                var obj = client.ReadAny(handle, targetType);
                return (T) Convert.ChangeType(obj, typeof(T));
            });
        }
    }
}