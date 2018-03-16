using System;
using System.Collections.Generic;
using Lion.SDK.Ethereum;

namespace Lion.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            ContractABI _abi = new ContractABI("X");
            _abi.Add(new int[10]);

            Console.WriteLine(_abi.ToData());

            Console.ReadLine();
        }
    }
}
