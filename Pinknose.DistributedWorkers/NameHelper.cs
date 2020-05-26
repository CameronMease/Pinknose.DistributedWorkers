///////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2020 Cameron Mease
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
///////////////////////////////////////////////////////////////////////////////////

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