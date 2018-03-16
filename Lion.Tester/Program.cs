using System;
using System.Collections.Generic;
using Lion.SDK.Ethereum;

namespace Lion.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            ContractABI _abi = new ContractABI("0x12345678");
            _abi.Add(new int[] { 1, 2, 3, 4, 5 });
            _abi.Add(new uint[] { 1, 2, 3, 4, 5 });


            Console.WriteLine(_abi.ToData());

            Console.ReadLine();
        }
    }
}
