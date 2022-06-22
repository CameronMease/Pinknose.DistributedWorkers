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

using Newtonsoft.Json;
using Pinknose.DistributedWorkers.Keystore;
using System;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// Message sent from the server to alert clients of a new shared encryption key for the system.
    /// </summary>
    public class TrustZoneSharedKeyUpdate : MessageBase
    {
        #region Constructors

        [JsonConstructor]
        public TrustZoneSharedKeyUpdate(TrustZoneSharedKey sharedKey) : base()
        {
            if (sharedKey is null)
            {
                throw new ArgumentNullException(nameof(sharedKey));
            }

            SharedKey = sharedKey;
        }

        #endregion Constructors

        #region Properties

        public TrustZoneSharedKey SharedKey { get; private set; }




        #endregion Properties
    }
}