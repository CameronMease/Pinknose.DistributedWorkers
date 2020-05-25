using System;
using System.Collections.Generic;
using System.Text;

namespace Pinknose.DistributedWorkers
{
    internal static class NameHelper
    {
#pragma warning disable CA1308 // Normalize strings to uppercase
        public static string GetWorkQueueName(string systemName) => $"{systemName}-queue-work".ToLowerInvariant();
        public static string GetBroadcastExchangeName(string systemName) => $"{systemName}-exchange-broadcast".ToLowerInvariant();
        public static string GetSubscriptionExchangeName(string systemName) => $"{systemName}-exchange-subscription".ToLowerInvariant();
        public static string GetDedicatedQueueName(string systemName, string clientName) => $"{systemName}-{clientName}-queue-dedicated".ToLowerInvariant();
        public static string GetServerQueueName(string systemName) => GetDedicatedQueueName(systemName, GetServerName());
        public static string GetSubscriptionQueueName(string systemName, string clientName) => $"{systemName}-{clientName}-queue-subscription".ToLowerInvariant();

        public static string GetServerName() => "server";

        //public string LogQueueName(string systemName) => $"{SystemName}-queue-log".ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
    }
}
