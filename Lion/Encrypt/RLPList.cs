using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Encrypt
{
    public class RLPItemList: List<IRLPItem>, IRLPItem
    {
        public byte[] Data { get; set; }
    }
}
