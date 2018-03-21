using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Lion.Net.Sockets
{
    internal class SocketEngineBuffer : IDisposable
    {
        private byte[] byteArray;
        private int number = 0;
        private int size = 0;
        private int total = 0;
        private int currentIndex = 0;
        private Stack<int> freeIndexPool;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_number">缓存个数</param>
        /// <param name="_size">每个缓存大小</param>
        public SocketEngineBuffer(Int32 _number, Int32 _size)
        {
            this.number = _number;
            this.size = _size;
            this.total = _number * _size;
        }
        ~SocketEngineBuffer() { this.Dispose(false); }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        public void Init()
        {
            this.byteArray = new byte[this.number * this.size];
            this.freeIndexPool = new Stack<int>(this.number);
        }

        /// <summary>
        /// 释放缓存
        /// </summary>
        /// <param name="_args">SocketAsyncEventArgs对象</param>
        internal void FreeBuffer(SocketAsyncEventArgs _args)
        {
            this.freeIndexPool.Push(_args.Offset);
            _args.SetBuffer(null, 0, 0);
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="_args">SocketAsyncEventArgs对象</param>
        /// <returns>是否设置成功</returns>
        internal bool SetBuffer(SocketAsyncEventArgs _args)
        {
            if (this.freeIndexPool.Count > 0)
            {
                _args.SetBuffer(this.byteArray, this.freeIndexPool.Pop(), this.size);
            }
            else
            {
                if ((this.total - this.size) < this.currentIndex)
                {
                    return false;
                }
                _args.SetBuffer(this.byteArray, this.currentIndex, this.size);
                this.currentIndex += this.size;
            }
            return true;
        }

        public bool Disposed = false;
        protected virtual void Dispose(bool _disposing)
        {
            if (!this.Disposed) // 非托管释放
            {

            }
            if (_disposing) // 托管释放
            {
                this.byteArray = null;
                this.freeIndexPool = null;
            }
            this.Disposed = true;
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
