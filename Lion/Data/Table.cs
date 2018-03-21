using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Table
    {
        #region private
        private string name;
        private string asName;
        private TableRelation relation;
        private IList<Where> conditions;
        #endregion

        #region public
        #region Name
        /// <summary>
        /// 表名称
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        #endregion

        #region AsName
        /// <summary>
        /// 表别名
        /// </summary>
        public string AsName
        {
            get { return this.asName; }
            set { this.asName = value; }
        }
        #endregion

        #region Relation
        /// <summary>
        /// 与前表关系
        /// </summary>
        public TableRelation Relation
        {
            get { return this.relation; }
            set { this.relation = value; }
        }
        #endregion

        #region Conditions
        public IList<Where> Conditions
        {
            get { return conditions; }
            set { conditions = value; }
        }
        #endregion
        #endregion

        #region Structure
        /// <summary>
        /// Table(Name) 构造函数
        /// </summary>
        /// <param name="_name">表名</param>
        public Table(string _name)
        {
            this.Name = _name;
            this.AsName = "";
            this.Relation = TableRelation.Inner;
            this.Conditions = new List<Where>();
        }
        /// <summary>
        /// Table(Name,AsName) 构造函数
        /// </summary>
        /// <param name="_name">表名</param>
        /// <param name="_asName">表别名</param>
        public Table(string _name, string _asName)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Relation = TableRelation.Inner;
            this.Conditions = new List<Where>();
        }
        /// <summary>
        /// Table(Name,AsName) 构造函数
        /// </summary>
        /// <param name="_name">表名</param>
        /// <param name="_asName">表别名</param>
        /// <param name="_relation">两表的关系</param>
        /// <param name="_conditions">两表关系的条件</param>
        public Table(string _name, string _asName, TableRelation _relation, IList<Where> _conditions)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Relation = _relation;
            this.Conditions = _conditions;
        }
        #endregion
    }

    [Serializable]
    public class TableCollection : List<Table>
    {
        public void Add(string _name)
        {
            base.Add(new Table(_name));
        }
        public void Add(string _name, string _asName)
        {
            base.Add(new Table(_name,_asName));
        }
    }
}
