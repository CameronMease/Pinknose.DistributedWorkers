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

using System;

namespace Pinknose.DistributedWorkers.Messages
{
    internal enum AnnounceResponse
    {
        Accepted,
        Rejected
    }

    [Serializable]
    internal sealed class ClientAnnounceResponseMessage : MessageBase
    {
        #region Constructors

        internal ClientAnnounceResponseMessage(AnnounceResponse response, int systemSharedKeyId, byte[] systemSharedKey/*PublicKeystore publicKeystore, */) : base()
        {
            Response = response;
            //ServerPublicKey = key.Export(CngKeyBlobFormat.EccFullPublicBlob);
            SystemSharedKey = systemSharedKey;
            SystemSharedKeyId = systemSharedKeyId;
            //PublicKeystore = publicKeystore;
        }

        #endregion Constructors

        #region Properties

        public override Guid MessageTypeGuid => new Guid("6B6B9D9B-2B78-425B-91DE-7FCEFADD757C");

        public AnnounceResponse Response { get; private set; }

        public byte[] SystemSharedKey { get; private set; }

        public int SystemSharedKeyId { get; private set; }

        #endregion Properties

        // Initialization Vector for asymmetric encryption.
        //public byte[] Iv { get; private set; }

        //public PublicKeystore PublicKeystore { get; private set; }
    }
}