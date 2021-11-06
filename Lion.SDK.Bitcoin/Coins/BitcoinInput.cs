using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Lion.SDK.Bitcoin.Coins
{

    public class BitcoinInput
    {
        private bool TestNet { get; }
        public string TxId { get; }
        public BigInteger TxIndex { get; }
        public decimal Balance { get; }

        public string PrivateKey { get; }

        private string publicKey;
        public string PublicKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(publicKey))
                    return publicKey;
                publicKey = Lion.SDK.Bitcoin.Coins.Bitcoin.GenerateAddress(out string _, PrivateKey, !TestNet).PublicKey;
                return publicKey;
            }
        }

        private string scriptPubKey;
        public string SciptPubKey
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(scriptPubKey))
                    return scriptPubKey;
                scriptPubKey = BitcoinHelper.PublicKeyToPKSH(PublicKey);
                return scriptPubKey;
            }
        }


        public BitcoinInput(string _txid, int _txIndex, decimal _balance, string _privateKey, bool _testNet = false)
        {
            TxId = _txid;
            TxIndex = _txIndex;
            Balance = _balance;
            PrivateKey = _privateKey;
            TestNet = _testNet;
        }

        public List<byte> BaseInputScript
        {
            get
            {
                List<byte> _scripts = new List<byte>();
                _scripts.AddBytes(Lion.HexPlus.HexStringToByteArray(TxId).Reverse().ToArray());
                _scripts.AddBytesPadRightZero(4, TxIndex.ToByteArray().Reverse().ToArray());
                return _scripts;
            }
        }

        public List<byte> ScriptSig { get; set; }

    }
}
