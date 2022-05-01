using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lion.CryptoCurrency.Ethereum
{
    public class Ethereum
    {
        public const string ERC20_METHOD_TOTALSUPPLY = "0x18160ddd";
        public const string ERC20_METHOD_BALANCEOF = "0x70a08231";
        public const string ERC20_METHOD_TRANSFER = "0xa9059cbb";
        public const string ERC20_METHOD_TRANSFERFROM = "0x23b872dd";
        public const string ERC20_METHOD_APPROVE = "0x095ea7b3";
        public const string ERC20_METHOD_ALLOWANCE = "0xdd62ed3e";

        public const uint CHAIN_ID_MAINNET = 1;
        public const uint CHAIN_ID_ROPSTEN = 3;
        public const uint CHAIN_ID_RINKEBY = 4;
        public const uint CHAIN_ID_GOERLI = 5;
        public const uint CHAIN_ID_KOVAN = 42;
        public const uint CHAIN_ID_PRIVATE = 1337;

        public const string EIP1155_METHOD_BALANCEOF = "0x00fdd58e";
        public const string EIP1155_METHOD_BALANCEOFBATCH = "0x4e1273f4";
        public const string EIP1155_METHOD_BURN = "0xf5298aca";
        public const string EIP1155_METHOD_BURNBATCH = "0x6b20c454";
        public const string EIP1155_METHOD_MINT = "0x731133e9";
        public const string EIP1155_METHOD_MINTBATCH = "0x1f7fdffa";
        public const string EIP1155_METHOD_SAFEBATCHTRANSFERFROM = "0x2eb2c2d6";
        public const string EIP1155_METHOD_SAFETRANSFERFROM = "0xf242432a";
    }
}
