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

using Pinknose.DistributedWorkers.Clients;
using System;

namespace Pinknose.DistributedWorkers.Messages
{
    /// <summary>
    /// A message which is passed between client and server.
    /// </summary>
    [Serializable]
    public abstract partial class MessageBase
    {
        #region Constructors

        public MessageBase()
        {
        }

        #endregion Constructors

        #region Properties

        public string MessageText { get; set; }

        //TODO: How to restrict access to set but not break serialization?
        //public string ClientName { get; internal set; }

        //public MessageTagCollection Tags { get; private set; } = new MessageTagCollection();

        //public bool IsEncrypted { get; private set; } = false;

        //public Dictionary<string, object> CustomProperties { get; private set; } = new Dictionary<string, object>();

        [field: NonSerializedAttribute()]
        public SignatureVerificationStatus SignatureVerificationStatus { get; private set; } = SignatureVerificationStatus.SignatureUnverified;

        #endregion Properties
    }
}