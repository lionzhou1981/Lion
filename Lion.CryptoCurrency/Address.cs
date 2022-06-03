namespace Lion.CryptoCurrency
{
    public class Address
    {
        public Address(string _text)
        {
            this.text = _text;
        }

        private string text = "";
        public virtual string Text { get => text; set => text = value; }

        private string pub = "";
        public virtual string Public { get => pub; set => pub = value; }

        private string priv = "";
        public virtual string Private { get => priv; set => priv = value; }
    }
}
