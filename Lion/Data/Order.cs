using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Order
    {
        #region private
        private OrderType g_Type;
        private OrderMode g_Method;
        private string g_strFieldName;
        #endregion

        #region public
        /// <summary>
        /// 排序的种类
        /// </summary>
        public OrderType OrderType
        {
            get { return this.g_Type; }
            set { this.g_Type = value; }
        }
        /// <summary>
        /// 排序的方法
        /// </summary>
        public OrderMode Method
        {
            get { return this.g_Method; }
            set { this.g_Method = value; }
        }
        /// <summary>
        /// 排序的字段
        /// </summary>
        public string FieldName
        {
            get { return this.g_strFieldName; }
            set { this.g_strFieldName = value; }
        }
        #endregion

        #region Structure
        /// <summary>
        /// OrderBy(FieldName,Method) 构造函数
        /// </summary>
        /// <param name="m_strFieldName">排序的字段</param>
        /// <param name="m_Method">排序的方式</param>
        public Order(string m_strFieldName, OrderMode m_Method)
        {
            this.OrderType = OrderType.Field;
            this.Method = m_Method;
            this.FieldName = m_strFieldName;
        }
        /// <summary>
        /// OrderBy(FieldName,Method) 构造函数
        /// </summary>
        /// <param name="m_strFieldName">排序的字段</param>
        /// <param name="m_Method">排序的方式</param>
        public Order(OrderType m_Type, string m_strFieldName, OrderMode m_Method)
        {
            this.OrderType = m_Type;
            this.Method = m_Method;
            this.FieldName = m_strFieldName;
        }
        #endregion
    }

    [Serializable]
    public class OrderCollection : List<Order>
    {
        public void Add(string _fieldName, OrderMode _method) => base.Add(new Order(OrderType.Field, _fieldName, _method));

        public void Add(OrderType _orderType, string _fieldName, OrderMode _method) => base.Add(new Order(_orderType, _fieldName, _method));

        public void AscField(string _fieldName) => base.Add(new Order(OrderType.Field, _fieldName, OrderMode.Asc));

        public void DescField(string _fieldName) => base.Add(new Order(OrderType.Field, _fieldName, OrderMode.Desc));

        public void AscValue(string _fieldName) => base.Add(new Order(OrderType.Value, _fieldName, OrderMode.Asc));

        public void DescValue(string _fieldName) => base.Add(new Order(OrderType.Value, _fieldName, OrderMode.Desc));
    }
}
