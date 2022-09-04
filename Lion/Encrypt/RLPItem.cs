using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Encrypt
{
    public class RLPItem : IRLPItem
    {
        private readonly byte[] data;

        public RLPItem(byte[] _data)
        {
            this.data = _data;
        }

        public byte[] Data
        {
            get
            {
                return this.Data;
            }
        }

    }
}
