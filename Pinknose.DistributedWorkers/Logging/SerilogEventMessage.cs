﻿///////////////////////////////////////////////////////////////////////////////////
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

using Pinknose.DistributedWorkers.Messages;
using Serilog.Events;
using System;

namespace Pinknose.DistributedWorkers.Logging
{
    [Serializable]
    public class SerilogEventMessage : PayloadMessage<LogEvent>
    {
        #region Constructors

        public SerilogEventMessage(LogEvent payload) : base(payload, false, true, false)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            //TODO: How do we do this now?
            //Tags.Add(SystemTags.SerilogEvent(payload.Level));
        }

        #endregion Constructors

        #region Properties

        public override Guid MessageTypeGuid => new Guid("E29E6A36-7D57-4515-B3CA-0ACF288DF8A9");

        #endregion Properties
    }
}