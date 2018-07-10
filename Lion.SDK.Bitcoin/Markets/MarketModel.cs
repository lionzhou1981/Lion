using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lion.SDK.Bitcoin.Markets
{
    #region Enum
    public enum HttpCallMethod { Get, Json, Form }
    public enum MarketSide { Bid, Ask }
    public enum KLineType { M1, M5, M15, M30, H1, H4, H6, H8, H12, D1, D7, D14, MM, YY }
    public enum OrderType { Market, Limit }
    public enum OrderStatus { New, Filling, Filled, Canceled, Finished }
    #endregion

    #region Books
    public class Books: ConcurrentDictionary<string, BookItems>
    {
        #region this[pair,side]
        public BookItems this[string _pair, MarketSide _side]
        {
            get
            {
                if (this.TryGetValue(_pair + ":" + _side.ToString(), out BookItems _collection))
                {
                    return _collection;
                }
                return null;
            }
            set
            {
                BookItems _items = value;
                _items.Pair = _pair;
                this.AddOrUpdate(_pair + ":" + _side.ToString(), _items, (k, v) => _items);
            }
        }
        #endregion

        #region Clear
        public new void Clear()
        {
            base.Clear();
        }
        #endregion
    }
    #endregion

    #region BookItems
    public class BookItems : ConcurrentDictionary<string, BookItem>
    {
        public string Pair;
        public MarketSide Side;
        public BookItems(MarketSide _side) { this.Side = _side; }

        public new BookItem[] ToArray()
        {
            if (this.Side == MarketSide.Ask) { return this.Values.OrderBy(i => i.Price).ToArray(); }
            if (this.Side == MarketSide.Bid) { return this.Values.OrderByDescending(i => i.Price).ToArray(); }
            return null;
        }

        public new BookItem this[string _id]
        {
            get
            {
                if (!this.TryGetValue(_id, out BookItem _item)) { return null; }
                return _item;
            }
        }

        #region Insert
        public BookItem Insert(string _id, decimal _price, decimal _amount)
        {
            BookItem _item = new BookItem(this.Pair, this.Side, _price, _amount, _id);
            this.AddOrUpdate(_id, _item, (k, v) => _item);
            return _item;
        }
        #endregion

        #region Update
        public BookItem Update(string _id, decimal _amount)
        {
            BookItem _item;
            if (!this.TryGetValue(_id, out _item)) { return null; }

            _item.Amount = _amount;
            return _item;
        }
        #endregion

        #region Delete
        public BookItem Delete(string _id)
        {
            BookItem _item;
            if(!this.TryRemove(_id, out _item)) { return null; }
            return _item;
        }
        #endregion

        #region Resize
        public void Resize(int _size)
        {
            BookItem[] _list = this.ToArray();
            for(int i = _size; i < _list.Length; i++)
            {
                BookItem _removed;
                this.TryRemove(_list[i].Id, out _removed);
            }
        }
        #endregion

        #region GetPrice
        public decimal GetPrice(decimal _amount)
        {
            BookItem[] _list = this.ToArray();
            decimal _count = 0M;
            foreach (BookItem _item in _list)
            {
                _count += _item.Amount;
                if (_count >= _amount) { return _item.Price; }
            }
            return 0M;
        }
        #endregion

        #region GetAmount
        public decimal GetAmount(decimal _price)
        {
            BookItem[] _list = this.ToArray();
            decimal _count = 0M;

            foreach (BookItem _item in _list)
            {
                if (this.Side == MarketSide.Ask)
                {
                    if(_item.Price > _price)
                    {
                        return _count;
                    }
                    else
                    {
                        _count += _item.Amount;
                    }
                }
                if (this.Side == MarketSide.Bid)
                {
                    if (_item.Price < _price)
                    {
                        return _count;
                    }
                    else
                    {
                        _count += _item.Amount;
                    }
                }
            }
            return 0M;
        }
        #endregion
    }
    #endregion

    #region BookItem
    public class BookItem
    {
        public string Id;
        public string Pair;
        public MarketSide Side;
        public decimal Price;
        public decimal Amount;

        public BookItem(string _pair, MarketSide _side, decimal _price, decimal _amount, string _id = "")
        {
            this.Pair = _pair;
            this.Side = _side;
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
                if (this.TryGetValue(_id, out OrderItem _item)) { return _item; }
                return null;
            }
        }
        #endregion

        #region ToArray
        public new OrderItem[] ToArray()
        {
            return this.Values.ToArray();
        }
        #endregion
    }
    #endregion

    #region OrderItem
    public class OrderItem
    {
        public string Id;
        public string Pair;
        public string Side;
        public decimal Price;
        public decimal Amount;
        public decimal FilledPrice;
        public decimal FilledAmount;
        public OrderStatus Status;
        public DateTime CreateTime;

        public OrderItem(string _id, string _pair, string _side, decimal _price, decimal _amount, OrderStatus _status = OrderStatus.New)
        {
            this.Id = _id;
            this.Pair = _pair;
            this.Side = _side;
            this.Price = _price;
            this.Amount = _amount;
            this.FilledPrice = 0M;
            this.FilledAmount = 0M;
            this.Status = _status;
            this.CreateTime = DateTime.UtcNow;
        }
    }
    #endregion

    #region Balances
    public class Balances : ConcurrentDictionary<string, BalanceItem>
    {
        public new BalanceItem this[string _symbol]
        {
            get
            {
                if (this.TryGetValue(_symbol, out BalanceItem _balance)) { return _balance; }
                return null;
            }
            set
            {
                this.AddOrUpdate(_symbol.ToUpper(), value, (k, v) => value);
            }
        }
    }
    public class BalanceItem
    {
        public string Symbol;
        public decimal Free;
        public decimal Lock;
        public decimal Total { get => this.Free + this.Lock; }
    }
    #endregion

    #region Ticker
    public class Ticker
    {
        public string Pair;
        public decimal Last;
        public decimal LastAmount = 0M;
        public decimal BidPrice = 0M;
        public decimal BidAmount = 0M;
        public decimal AskPrice = 0M;
        public decimal AskAmount = 0M;
        public decimal Open24H;
        public decimal High24H;
        public decimal Low24H;
        public decimal Volume24H;
        public decimal Volume24H2 = 0M;
        public decimal Change24H = 0M;
        public decimal ChangeRate24H = 0M;
    }
    #endregion

    #region Pairs
    public class Pairs : ConcurrentDictionary<string, PairItem>
    {
        public new PairItem this[string _pair]
        {
            get
            {
                if (this.TryGetValue(_pair, out PairItem _item)) { return _item; }
                return null;
            }
            set
            {
                this.AddOrUpdate(_pair.ToUpper(), value, (k, v) => value);
            }
        }
    }
    public class PairItem
    {
        public string Code;
        public string SymbolFrom;
        public string SymbolTo;
        public int PriceDecimal;
        public int AmountDecimal;
    }
    #endregion

    #region Trade
    public class Trade
    {
        public string Id;
        public string Pair;
        public MarketSide Side;
        public decimal Price;
        public decimal Amount;
        public DateTime DateTime;
    }
    #endregion

    #region KLine
    public class KLine
    {
        public DateTime DateTime;
        public string Pair;
        public KLineType Type;
        public decimal Open;
        public decimal Close;
        public decimal High;
        public decimal Low;
        public decimal Count;
        public decimal Volume;
        public decimal Volume2;
    }
    #endregion
}
