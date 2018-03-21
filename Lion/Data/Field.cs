using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Field
    {
        #region public
        #region Name
        /// <summary>
        /// �ֶ���
        /// </summary>
        public string Name { get; set; }
        #endregion

        #region AsName
        /// <summary>
        /// �ֶα���
        /// </summary>
        public string AsName { get; set; }
        #endregion

        #region Value
        /// <summary>
        /// �ֶ�ֵ
        /// </summary>
        public object Value { get; set; }
        #endregion

        #region DbType
        /// <summary>
        /// �ֶ�����
        /// </summary>
        public SqlDbType DbType { get; set; }
        #endregion
        #endregion

        #region Structure
        /// <summary>
        /// Field(Name,AsName,Value) ���캯��
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        /// <param name="_value">�ֶ�ֵ</param>
        /// <param name="_dbType">�ֶ�����</param>
        public Field(string _name, string _asName, object _value, SqlDbType _dbType)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Value = _value;
            this.DbType = _dbType;
        }
        /// <summary>
        /// Field(Name,AsName,Value) ���캯��
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        /// <param name="_value">�ֶ�ֵ</param>
        public Field(string _name, string _asName, object _value)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Value = _value;
            this.DbType = System.Data.SqlDbType.Variant;
        }
        /// <summary>
        /// Field(Name,AsName) ���캯��
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        public Field(string _name, string _asName)
        {
            this.Name = _name;
            this.AsName = _asName;
            this.Value = null;
            this.DbType = System.Data.SqlDbType.Variant;
        }
        /// <summary>
        /// Field(Name) ���캯��
        /// </summary>
        /// <param name="_customInfo">�Զ�����Ϣ</param>
        public Field(string _customInfo)
        {
            this.Name = _customInfo;
            this.AsName = "_ThisIsCustomField_";
            this.Value = null;
            this.DbType = System.Data.SqlDbType.Variant;
        }
        /// <summary>
        /// Field(Name,AsName) ���캯��
        /// </summary>
        /// <param name="_type">�ֶ�����</param>
        /// <param name="_name">�ֶ�����</param>
        /// <param name="_value">�ֶα������ߴ����ֵ</param>
        public Field(FieldType _type, string _name, object _value)
        {
            switch (_type)
            {
                case FieldType.SelectField:
                    this.Name = _name;
                    this.AsName = _value.ToString();
                    this.Value = null;
                    this.DbType = System.Data.SqlDbType.Variant;
                    break;
                case FieldType.ValueField:
                    this.Name = _name;
                    this.AsName = "";
                    this.Value = _value;
                    this.DbType = System.Data.SqlDbType.Variant;
                    break;
                case FieldType.Custom:
                    this.Name = _name;
                    this.AsName = "_ThisIsCustomField_";
                    this.Value = null;
                    this.DbType = System.Data.SqlDbType.Variant;
                    break;
                case FieldType.RowNumberAsc:
                    this.Name = "ROW_NUMBER() OVER(ORDER BY " + "[" + _name.Replace(".", "].[") + "]" + " Asc) AS " + _value.ToString();
                    this.AsName = "_ThisIsCustomField_";
                    this.Value = null;
                    this.DbType = System.Data.SqlDbType.Variant;
                    break;
                case FieldType.RowNumberDesc:
                    this.Name = "ROW_NUMBER() OVER(ORDER BY " + "[" + _name.Replace(".", "].[") + "]" + " Desc) AS " + _value.ToString();
                    this.AsName = "_ThisIsCustomField_";
                    this.Value = null;
                    this.DbType = System.Data.SqlDbType.Variant;
                    break;
            }
             
        }
        #endregion
    }

    [Serializable]
    public class FieldCollection : List<Field>
    {
        #region Add
        /// <summary>
        /// ���Field��������
        /// </summary>
        /// <param name="_type">�ֶ�����</param>
        /// <param name="_name">�ֶ�����</param>
        /// <param name="_asName">�ֶα���</param>
        public void Add(FieldType _type, string _name, object _value)
        {
            base.Add(new Field(_type, _name, _value));
        }

        /// <summary>
        /// ���Field��������
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        /// <param name="_value">�ֶ�ֵ</param>
        /// <param name="_dbType">�ֶ�����</param>
        public void Add(string _name, string _asName, object _value, SqlDbType _dbType)
        {
            base.Add(new Field(_name, _asName, _value, _dbType));
        }

        /// <summary>
        /// ���Field��������
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        /// <param name="_value">�ֶ�ֵ</param>
        public void Add(string _name, string _asName, object _value)
        {
            base.Add(new Field(_name, _asName, _value));
        }

        /// <summary>
        /// ���Field��������
        /// </summary>
        /// <param name="_name">�ֶ���</param>
        /// <param name="_asName">�ֶα���</param>
        public void Add(string _name, string _asName)
        {
            base.Add(new Field(_name, _asName));
        }

        /// <summary>
        /// ���Field��������
        /// </summary>
        /// <param name="_customInfo">�Զ�����Ϣ</param>
        public void Add(string _customInfo)
        {
            base.Add(new Field(_customInfo));
        }
        #endregion
    }
}
