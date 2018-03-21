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
        /// ������
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        #endregion

        #region AsName
        /// <summary>
        /// �����
        /// </summary>
        public string AsName
        {
            get { return this.asName; }
            set { this.asName = value; }
        }
        #endregion

        #region Relation
        /// <summary>
        /// ��ǰ���ϵ
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
        /// Table(Name) ���캯��
        /// </summary>
        /// <param name="_name">����</param>
        public Table(string _name)
        {
            this.Name = _name;
            this.AsName = "";
            this.Relation = TableRelation.Inner;
            this.Conditions = new List<Where>();
        }
        /// <summary>
        /// Table(Name,AsName) ���캯��
        /// </summary>
        /// <param name="_name">����</param>
        /// <param name="_asName">�����</param>
        public Table(string _name, string _asName)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Relation = TableRelation.Inner;
            this.Conditions = new List<Where>();
        }
        /// <summary>
        /// Table(Name,AsName) ���캯��
        /// </summary>
        /// <param name="_name">����</param>
        /// <param name="_asName">�����</param>
        /// <param name="_relation">����Ĺ�ϵ</param>
        /// <param name="_conditions">�����ϵ������</param>
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
