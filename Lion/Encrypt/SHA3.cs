using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Lion.Encrypt
{
    public enum SHA3BitType
    {
        [Description("224")]
        S224 = 224,
        [Description("256")]
        S256 = 256,
        [Description("384")]
        S384 = 384,
        [Description("512")]
        S512 = 512,
    }
    public enum SHA3HashType
    {
        Keccak = 0x01,
        Sha3 = 0x06,
        Shake = 0x1f,
        CShake = 0x04
    }

    public class SHA3 : Keccak1600
    {
        public SHA3(SHA3BitType bitType) : base((int)bitType)
        {

        }

        public string Hash(string stringToHash, SHA3HashType _hashType = SHA3HashType.Sha3)
        {
            return Hash(Encoding.ASCII.GetBytes(stringToHash), _hashType);
        }

        public string Hash(byte[] bytesToHash, SHA3HashType _hashType = SHA3HashType.Sha3)
        {
            base.Initialize((int)_hashType);
            base.Absorb(bytesToHash, 0, bytesToHash.Length);
            base.Partial(bytesToHash, 0, bytesToHash.Length);

            var byteResult = base.Squeeze();

            return HexPlus.ByteArrayToHexString(byteResult);
        }
    }
}
