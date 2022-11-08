using Google.Protobuf;
using Lion.CryptoCurrency.Tron.TransactionInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lion.CryptoCurrency.Tron
{
    internal class Transaction
    {
        public static string BuildRawTransaction(string _from,string _to,string _refBlockBytes,string _refBlockHash,long _expTime,long _timeStamp)
        {
            using MemoryStream _msFrom = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_from));
            using MemoryStream _msTo = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_to));
            using MemoryStream _refBB = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockBytes));
            using MemoryStream _refBH = new MemoryStream(Lion.HexPlus.HexStringToByteArray(_refBlockHash));

            Lion.CryptoCurrency.Tron.TransactionInfo.Transaction tr = new Lion.CryptoCurrency.Tron.TransactionInfo.Transaction();
            tr.RawData = new TransactionInfo.Transaction.Types.raw();
            tr.RawData.Contract.Add(
                new TransactionInfo.Transaction.Types.Contract()
            {
                Type = TransactionInfo.Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter = Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract()
                {
                    Amount = 1000,
                    OwnerAddress = Google.Protobuf.ByteString.FromStream(_msFrom),
                    ToAddress = Google.Protobuf.ByteString.FromStream(_msTo)
                }),
            });
            tr.RawData.RefBlockHash = ByteString.FromStream(_refBH);
            tr.RawData.RefBlockBytes = ByteString.FromStream(_refBB);
            tr.RawData.Expiration = 1667899389000;
            tr.RawData.Timestamp = 1667899330766;
            byte[] _re = tr.ToByteArray();
            return Lion.HexPlus.ByteArrayToHexString(_re);
        }
    }
}
