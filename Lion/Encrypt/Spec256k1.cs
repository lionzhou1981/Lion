using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Lion.Encrypt
{
    public static class Secp256k1
    {
        public static readonly BigInteger P = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F", System.Globalization.NumberStyles.HexNumber);
        public static readonly ECPoint G = ECPoint.DecodePoint(HexPlus.HexStringToByteArray("0479BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8"));
        public static readonly BigInteger N = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141", System.Globalization.NumberStyles.HexNumber);
        public static readonly BigInteger HalfN = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141", System.Globalization.NumberStyles.HexNumber) / 2;


        public static byte[] PrivateKeyToPublicKey(string _hexPrivateKey, bool _compressed = false)
        {
            return PrivateKeyToPublicKey(BigInteger.Parse("0" + _hexPrivateKey, System.Globalization.NumberStyles.HexNumber), _compressed);
        }

        public static byte[] PrivateKeyToPublicKey(BigInteger _privateKey,bool _compressed = false)
        {
            return G.Multiply(_privateKey).EncodePoint(_compressed);
        }
    }
}
