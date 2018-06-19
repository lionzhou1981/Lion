using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lion.SDK.Bitcoin.Markets
{
    #region Books
    public class Books: ConcurrentDictionary<string, BookItems>
    {
        #region this[symbol,side]
        public BookItems this[string _symbol, string _side]
        {
            get
            {
                BookItems _collection;
                if (this.TryGetValue(_symbol + ":" + _side, out _collection))
                {
                    return _collection;
                }
                return null;
            }
            set
            {
                this.AddOrUpdate(_symbol + ":" + _side, value, (k, v) => value);
            }
        }
        #endregion
    }
    #endregion

    #region BookItems
    public class BookItems : ConcurrentDictionary<string, BookItem>
    {
        public string Side;
        public BookItems(string _side) { this.Side = _side; }

        public new BookItem[] ToArray()
        {
            if (this.Side == "ASK") { return this.Values.OrderBy(i => i.Price).ToArray(); }
            if (this.Side == "BID") { return this.Values.OrderByDescending(i => i.Price).ToArray(); }
            return null;
        }

        public void Insert(string _id, decimal _price, decimal _amount)
        {
            BookItem _item = new BookItem(_price, _amount, _id);
            this.AddOrUpdate(_id, _item, (k, v) => _item);
        }

        public bool Update(string _id, decimal _amount)
        {
            BookItem _item;
            if (!this.TryGetValue(_id, out _item)) { return false; }

            _item.Amount = _amount;
            return true;
        }

        public bool Delete(string _id)
        {
            BookItem _item;
            return this.TryRemove(_id, out _item);
        }
    }
    #endregion

    #region BookItem
    public class BookItem
    {
        public string Id;
        public decimal Price;
        public decimal Amount;

        public BookItem(decimal _price,decimal _amount,string _id = "")
        {
            this.Price = _price;
            this.Amount = _amount;
            this.Id = _id == "" ? _price.ToString() : _id;
        }
    }
    #endregion

    #region Orders
    public class Orders : ConcurrentDictionary<string, OrderItem>
    {
        #region this[id]
        public new OrderItem this[string _id]
        {
            get
            {
                OrderItem _item;
                if (this.TryGetValue(_id, out _item)) { return _item; }
                return null;
            }
        }
        #endregion
    }
    #endregion

    #region OrderItem
    public class OrderItem
    {
        public string Id;
        public string Symbol;
        public string Side;
        public decimal Price;
        public decimal Amount;
        public decimal AmountFilled;
        public OrderStatus Status;
        public DateTime CreateTime;

        public OrderItem(string _id, string _symbol, string _side, decimal _price, decimal _amount, OrderStatus _status = OrderStatus.New)
        {
            this.Id = _id;
            this.Symbol = _symbol;
            this.Side = _side;
            this.Price = _price;
            this.Amount = _amount;
            this.AmountFilled = 0;
            this.CreateTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region OrderStatus
    public enum OrderStatus { New, Filling, Filled, Canceled, Finished }
    #endregion

    #region Balance
    public class Balance : ConcurrentDictionary<string, decimal>
    {
        public decimal this[string _symbol]
        {
            get
            {
                decimal _balance;
                if (this.TryGetValue(_symbol, out _balance)) { return _balance; }
                return 0M;
            }
            set
            {
                this.AddOrUpdate(_symbol.ToUpper(), value, (k, v) => value);
            }
        }
    }
    #endregion
}
