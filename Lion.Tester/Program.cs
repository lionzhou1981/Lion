using System;
using System.Collections.Generic;
using Lion.SDK.Ethereum;

namespace Lion.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            ContractABI _abi = new ContractABI("0x70a08231");
            _abi.Add("dave");
            _abi.Add(true);
            _abi.Add(new int[] { 1, 2, 3 });

            string _data = _abi.ToData();
            Console.WriteLine(_data.Substring(0, 10));
            int _position = 10;
            while(_position<_data.Length)
            {
                Console.WriteLine(_data.Substring(_position, 64));
                _position += 64;
            }

            Console.ReadLine();
        }
    }
}
