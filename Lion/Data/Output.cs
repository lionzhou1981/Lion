using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Output
    {
        #region public
        #region Name
        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; set; }
        #endregion

        #region Type
        /// <summary>
        /// 字段类型
        /// </summary>
        public OutputType Type { get; set; }
        #endregion
        #endregion

        #region Structure
        /// <summary>
        /// Output(Type,Name) 构造函数
        /// </summary>
        /// <param name="_type">字段名</param>
        /// <param name="_name">字段别名</param>
        public Output(OutputType _type, string _name)
        {
            this.Type = _type;
            this.Name = _name;
        }
        #endregion
    }

    [Serializable]
    public class OutputCollection : List<Output>
    {
        #region Add
        /// <summary>
        /// 添加Field到集合中
        /// </summary>
        /// <param name="_type">字段类型</param>
        /// <param name="_name">字段名称</param>
        public void Add(OutputType _type, string _name) => base.Add(new Output(_type, _name));
        #endregion
    }
}
