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

using Serilog.Events;

namespace Pinknose.DistributedWorkers.MessageTags
{
    public static class SystemTags
    {
        #region Fields

        private const string SerilogTagName = "SerilogEvent";

        #endregion Fields

        #region Properties

        public static MessageTag SerilogAllEvents => new MessageTag(SerilogTagName);

        public static MessageTagValue SerilogDebugEvent => SerilogEvent(LogEventLevel.Debug);

        public static MessageTagValue SerilogErrorEvent => SerilogEvent(LogEventLevel.Error);

        public static MessageTagValue SerilogFatalEvent => SerilogEvent(LogEventLevel.Fatal);

        public static MessageTagValue SerilogInformationEvent => SerilogEvent(LogEventLevel.Information);

        public static MessageTagValue SerilogVerboseEvent => SerilogEvent(LogEventLevel.Verbose);

        public static MessageTagValue SerilogWarningEvent => SerilogEvent(LogEventLevel.Warning);

        #endregion Properties

        #region Methods

        public static MessageTagValue SerilogEvent(LogEventLevel eventLevel) => new MessageTagValue(SerilogTagName, eventLevel);

        #endregion Methods
    }
}