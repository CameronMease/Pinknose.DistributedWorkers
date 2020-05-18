using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Pinknose.DistributedWorkers.Messages
{
    public class MessageSerializationHeader
    {
        internal BitVector32 bits;
        internal BitVector32 ints;

        private static int isEncryptedMask = BitVector32.CreateMask();
        private static int containsIVMask = BitVector32.CreateMask(isEncryptedMask);
        private static BitVector32.Section signatureLengthSection = BitVector32.CreateSection(256);
        private static BitVector32.Section ivLengthSection = BitVector32.CreateSection(256, signatureLengthSection);
        private byte[] _signature;
        private byte[] _iv;



        public MessageSerializationHeader(bool isEncrypted, byte[] signature, byte[] iv = null)
        {
            bits = new BitVector32(0);
            ints = new BitVector32(0);

            IsEncrypted = isEncrypted;
            Signature = signature;
            Iv = iv;
        }

        private MessageSerializationHeader()
        {
        }


        internal static MessageSerializationHeader Deserialize(byte[] bytes)
        {
            var header = new MessageSerializationHeader();

            header.bits = new BitVector32(BitConverter.ToInt32(bytes, 0));
            header.ints = new BitVector32(BitConverter.ToInt32(bytes, 4));

            header.Signature = bytes[8..(8 + header.SignatureLength)];
            header.Iv = bytes[^header.IVLength..];

            return header;
        }

        private bool IsEncrypted
        {
            get
            {
                return bits[isEncryptedMask];
            }
            set
            {
                bits[isEncryptedMask] = value;
            }
        }

        private bool ContainsIV
        {
            get
            {
                return bits[containsIVMask];
            }
            set
            {
                bits[containsIVMask] = value;
            }
        }


        private Int32 SignatureLength
        {
            get
            {
                return ints[signatureLengthSection];
            }
            set
            {
                ints[signatureLengthSection] = value;
            }
        }

        private Int32 IVLength
        {
            get
            {
                return ints[ivLengthSection];
            }
            set
            {
                ints[ivLengthSection] = value;
            }
        }

        internal byte[] Signature 
        { 
            get =>_signature;
            private set
            {
                _signature = value;
                SignatureLength = _signature.Length;
            }
        }

        private byte[] Iv
        {
            get => _iv;
            set
            {
                _iv = value;
                ContainsIV = _iv != null;
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[Length];
            this.CopyTo(bytes, 0);
            return bytes;
        }

        public void CopyTo(byte[] byteArray, int index)
        {
            
            BitConverter.GetBytes(bits.Data).CopyTo(byteArray, index + 0);
            BitConverter.GetBytes(ints.Data).CopyTo(byteArray, index + 4);
            Signature.CopyTo(byteArray, index + 8);

            if (Iv != null)
            {
                Iv.CopyTo(byteArray, index + 8 + Signature.Length);
            }
        }

        public int Length => 8 + IVLength + SignatureLength;
    }
}
