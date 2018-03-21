using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Parameter
    {
        #region public
        #region Name
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }
        #endregion

        #region Value
        /// <summary>
        /// 参数值
        /// </summary>
        public object Value { get; set; }
        #endregion
        #endregion

        #region Structure
        /// <summary>
        /// Parameter(_name,_value) 构造函数
        /// </summary>
        /// <param name="_name">参数名称</param>
        /// <param name="_value">参数值</param>
        public Parameter(string _name, object _value)
        {
            this.Name = _name;
            this.Value = _value;
        }
        #endregion
    }

    [Serializable]
    public class ParameterCollection : List<Parameter>
    {
        public void Add(string _name, object _value) => base.Add(new Parameter(_name, _value));
    }
}
