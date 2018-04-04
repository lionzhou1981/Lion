using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Lion
{
    public class CollectionPlus<T> where T : CollectionPlusItem
    {
        private bool resize;
        public object[] items;
        public object Locker;

        #region CollectionPlus
        public CollectionPlus(int _limited, bool _resize)
        {
            this.resize = _resize;
            this.Locker = new object();
            this.items = new object[_limited];
        }

        public CollectionPlus(int _limited) : this(_limited, false) { }
        #endregion

        #region this[int]
        public T this[int _index]
        {
            get
            {
                T _tempItem = (T)(this.items[_index]);
                return _tempItem == null ? null : (_tempItem.Deleted ? null : _tempItem);
            }
            set
            {
                this.items[_index] = value;
            }
        }
        #endregion

        #region Add
        public int Add(T _item)
        {
            lock (this.Locker)
            {
                return this.AddWithoutLockAndCheck(_item);
            }
        }
        #endregion

        #region AddWithoutLockAndCheck
        public int AddWithoutLockAndCheck(T _item)
        {
            int _addIndex = -1;
            for (int i = 0; i < this.items.Length; i++)
            {
                if (this.items[i] != null && this.items[i]==_item)
                {
                    return i;
                }
                if (_addIndex == -1 && (this.items[i] == null || ((T)this.items[i]).Deleted))
                {
                    _addIndex = i;
                }
            }
            if (_addIndex > -1)
            {
                this.items[_addIndex] = _item;
                return _addIndex;
            }
            else
            {
                int _size = this.items.Length;
                Array.Resize(ref this.items, _size * 2);
                this.items[_size] = _item;
                return _size;
            }
        }
        #endregion

        #region Delete
        public void Delete(T _item)
        {
            _item.Deleted = true;
        }
        #endregion

        #region Remove
        /// <summary>
        /// 从数组中移除
        /// </summary>
        /// <param name="_item"></param>
        public void Remove(T _item)
        {
            for (int i = 0; i < this.items.Length; i++)
            {
                if (this.items[i] != null && this.items[i] == _item)
                {
                    this.items[i] = null;
                }
            }
        }
        #endregion

        #region RemoveAll
        public void RemoveAll()
        {
            for (int i = 0; i < this.items.Length; i++)
            {
                this.items[i] = null;
            }
        }
        #endregion

        #region Length
        public int Length
        {
            get
            {
                return this.items.Length;
            }
        }
        #endregion

        #region Count
        public int Count
        {
            get
            {
                int _count = 0;
                for (int i = 0; i < this.items.Length; i++)
                {
                    T _tempItem = (T)(this.items[i]);
                    if (_tempItem != null && _tempItem.Deleted == false) { _count++; }
                }
                return _count;
            }
        }
        #endregion
    }

    public class CollectionPlusItem
    {
        public int IndexByArray = -1;
        public bool Deleted = false;
        public CollectionPlusItem() { }
    }

}