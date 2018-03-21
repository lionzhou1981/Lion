using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data.SqlClient
{
    [Serializable]
    public class TSQL
    {
        #region public
        #region Fields
        /// <summary>
        /// TSQL语句中Field的集合
        /// </summary>
        public FieldCollection Fields { get; set; }
        #endregion

        #region Groups
        /// <summary>
        /// TSQL语句中聚合的集合
        /// </summary>
        public GroupCollection Groups { get; set; }
        #endregion

        #region Havings
        /// <summary>
        /// TSQL语句中Having的集合
        /// </summary>
        public WhereCollection Havings { get; set; }
        #endregion

        #region IsWith
        /// <summary>
        /// TSQL语句是否使用With语句
        /// </summary>
        public bool IsWith { get; set; }
        #endregion

        #region Orders
        /// <summary>
        /// TSQL语句中排序的集合
        /// </summary>
        public OrderCollection Orders { get; set; }
        #endregion

        #region Outputs
        /// <summary>
        /// TSQL语句中Output的集合
        /// </summary>
        public OutputCollection Outputs { get; set; }
        #endregion

        #region Parameters
        /// <summary>
        /// TSQL语句中参数的集合
        /// </summary>
        public IList<Parameter> Parameters { get; set; }
        #endregion

        #region Procedures
        /// <summary>
        /// 存储过程的名称，在Type为Procedures时有用
        /// </summary>
        public string Procedures { get; set; }
        #endregion

        #region Tables
        /// <summary>
        /// TSQL语句中Table的集合
        /// </summary>
        public TableCollection Tables { get; set; }
        #endregion

        #region Top
        /// <summary>
        /// TSQL语句中Top数字的集合，0为所有
        /// </summary>
        public int Top { get; set; }
        #endregion

        #region Type
        /// <summary>
        /// TSQL 语句的类型
        /// </summary>
        public TSQLType Type { get; set; }
        #endregion

        #region Wheres
        /// <summary>
        /// TSQL语句中条件的集合
        /// </summary>
        public WhereCollection Wheres { get; set; }
        #endregion

        #region WithOrders
        /// <summary>
        /// TSQL语句中With的排序集合
        /// </summary>
        public OrderCollection WithOrders { get; set; }
        #endregion

        #region WithWheres
        /// <summary>
        /// TSQL语句中With的条件集合
        /// </summary>
        public WhereCollection WithWheres { get; set; }
        #endregion
        #endregion

        #region Structure
        /// <summary>
        /// TSQL(TSQLType) 构造函数
        /// </summary>
        /// <param name="_type">Sql命令类型</param>
        /// <param name="_tableName">Table名称(选填)</param>
        public TSQL(TSQLType _type, string _tableName = "")
        {
            this.Type = _type;
            this.Tables = new TableCollection();
            if (_tableName != "") { this.Tables.Add(_tableName); }
            this.Fields = new FieldCollection();
            this.Outputs = new OutputCollection();
            this.Wheres = new WhereCollection();
            this.Groups = new GroupCollection();
            this.Havings = new WhereCollection();
            this.Orders = new OrderCollection();
            this.Procedures = "";
            this.Parameters = new List<Parameter>();
            this.Top = 0;
            this.IsWith = false;
            this.WithWheres = new WhereCollection();
            this.WithOrders = new OrderCollection();
        }
        #endregion

        #region ToSqlCommand
        /// <summary>
        /// 构件SqlCommand对象
        /// </summary>
        /// <returns>SqlCommand对象</returns>
        public SqlCommand ToSqlCommand()
        {
            SqlCommand _return = new SqlCommand();

            string _sql = "";
            switch (this.Type)
            {
                case TSQLType.Insert:
                    _sql += "INSERT INTO " + GetSqlTable(this, ref _return) + "(" + GetSqlInsertField(this) + ")";
                    _sql += GetSqlOutput(this);
                    _sql += " VALUES(" + GetSqlInsertValue(this, ref _return) + ")";
                    break;
                case TSQLType.Update:
                    _sql = "UPDATE " + GetSqlTable(this, ref _return) + " SET " + GetSqlUpdateField(this, ref _return);
                    _sql += GetSqlOutput(this);
                    _sql += this.Wheres.Count > 0 ? " WHERE " + GetSqlWhere(this.Wheres, ref _return) : "";
                    break;
                case TSQLType.Delete:
                    _sql = "DELETE " + GetSqlTable(this, ref _return);
                    _sql += GetSqlOutput(this);
                    _sql += this.Wheres.Count > 0 ? " WHERE " + GetSqlWhere(this.Wheres, ref _return) : "";
                    break;
                case TSQLType.Select:
                    if (this.IsWith)
                    {
                        _sql += "WITH WITHTABLE AS (";
                    }
                    _sql += "SELECT " + (this.Top > 0 ? ("top " + this.Top.ToString()) : "") + " ";
                    _sql += (this.Fields.Count > 0 ? GetSqlSelectField(this) : "*") + " FROM " + GetSqlTable(this, ref _return);
                    _sql += this.Wheres.Count > 0 ? " WHERE " + GetSqlWhere(this.Wheres, ref _return) : "";
                    _sql += this.Groups.Count > 0 ? " GROUP BY " + GetSqlGroupBy(this) : "";
                    _sql += this.Havings.Count > 0 ? " HAVING " + GetSqlHaving(this, ref _return) : "";
                    if (this.IsWith)
                    {
                        _sql += ") ";
                        _sql += "SELECT * FROM WITHTABLE";
                        _sql += this.WithWheres.Count > 0 ? " WHERE " + GetSqlWhere(this.WithWheres, ref _return) : "";
                        _sql += this.WithOrders.Count > 0 ? " ORDER BY " + GetSqlOrderBy(this.WithOrders) : "";
                    }
                    else
                    {
                        _sql += this.Orders.Count > 0 ? " ORDER BY " + GetSqlOrderBy(this.Orders) : "";
                    }
                    break;
                case TSQLType.Procedures:
                    _sql += "EXEC " + this.Procedures + " " + GetParameter(this.Parameters, ref _return);
                    break;
                case TSQLType.Truncate:
                    _sql += "TRUNCATE TABLE " + GetSqlTableNameString(this.Tables[0]);
                    break;
            }
            _return.CommandType = System.Data.CommandType.Text;
            _return.CommandText = _sql;

            return _return;
        }
        #endregion

        #region TableString
        #region GetSqlTable
        private string GetSqlTable(TSQL _tsql, ref SqlCommand _sqlCommand)
        {
            string _return = "";
            if (_tsql.Type == TSQLType.Select)
            {
                for (int _index = 0; _index < _tsql.Tables.Count; _index++)
                {
                    Table _table = _tsql.Tables[_index];
                    if (_index > 0)
                    {
                        switch (_table.Relation)
                        {
                            case TableRelation.Inner:
                                _return += " INNER JOIN ";
                                _return += GetSqlTableNameString(_table) + " ON " + GetSqlWhere(_table.Conditions, ref _sqlCommand);
                                break;
                            case TableRelation.LeftOuter:
                                _return += " LEFT OUTER JOIN ";
                                _return += GetSqlTableNameString(_table) + " ON " + GetSqlWhere(_table.Conditions, ref _sqlCommand);
                                break;
                            case TableRelation.RightOuter:
                                _return += " RIGHT OUTER JOIN ";
                                _return += GetSqlTableNameString(_table) + " ON " + GetSqlWhere(_table.Conditions, ref _sqlCommand);
                                break;
                            case TableRelation.FullOuter:
                                _return += " FULL OUTER JOIN ";
                                _return += GetSqlTableNameString(_table) + " ON " + GetSqlWhere(_table.Conditions, ref _sqlCommand);
                                break;
                            case TableRelation.Cross:
                                _return += " CROSS JOIN ";
                                _return += GetSqlTableNameString(_table);
                                break;
                        }
                    }
                    else
                    {
                        _return += GetSqlTableNameString(_table);
                    }
                }
            }
            else
            {
                Table _table = _tsql.Tables[0];
                _return += "[" + _table.Name.Replace(".", "].[") + "]";
            }
            return _return;
        }
        #endregion

        #region GetSqlTableNameString
        private string GetSqlTableNameString(Table _table)
        {
            string _return = "[" + _table.Name.Replace(".", "].[") + "]";
            if (_table.AsName != "")
            {
                _return += " [" + _table.AsName + "]";
            }
            return _return;
        }
        #endregion
        #endregion

        #region SelectFieldString
        private string GetSqlSelectField(TSQL _tsql)
        {
            string _return = "";
            for (int _index = 0; _index < _tsql.Fields.Count; _index++)
            {
                Field _field = _tsql.Fields[_index];
                _return += (_index > 0 ? "," : "");
                if (_field.AsName != "_ThisIsCustomField_")
                {
                    _return += "[" + _field.Name.Replace(".", "].[") + "]" + (_field.AsName != "" ? " AS [" + _field.AsName + "]" : "");
                }
                else
                {
                    _return += _field.Name;
                }
            }
            return _return;
        }
        #endregion

        #region InsertFieldString
        private string GetSqlInsertField(TSQL _tsql)
        {
            string _return = "";
            for (int _index = 0; _index < _tsql.Fields.Count; _index++)
            {
                Field _field = _tsql.Fields[_index];
                _return += (_index > 0 ? "," : "");
                _return += "[" + _field.Name.Replace(".", "].[") + "]";
            }
            return _return;
        }
        private string GetSqlInsertValue(TSQL _tsql, ref SqlCommand _sqlCommand)
        {
            string _return = "";
            for (int _index = 0; _index < _tsql.Fields.Count; _index++)
            {
                Field _field = _tsql.Fields[_index];
                _return += (_index > 0 ? "," : "");
                _return += "@" + _field.Name.Replace(".", "_");
                _sqlCommand.Parameters.AddWithValue("@" + _field.Name.Replace(".", "_"), _field.Value);
            }
            return _return;
        }
        #endregion

        #region UpdateFieldString
        private string GetSqlUpdateField(TSQL _tsql, ref SqlCommand _sqlCommand)
        {
            string _return = "";
            for (int _index = 0; _index < _tsql.Fields.Count; _index++)
            {
                Field _field = _tsql.Fields[_index];
                _return += (_index > 0 ? "," : "");

                if (_field.AsName != "_ThisIsCustomField_")
                {
                    _return += "[" + _field.Name.Replace(".", "].[") + "]=@" + _field.Name.Replace(".", "_");
                    if (_field.DbType == SqlDbType.Variant)
                    {
                        _sqlCommand.Parameters.AddWithValue("@" + _field.Name.Replace(".", "_"), _field.Value);
                    }
                    else
                    {
                        SqlParameter _sqlParameter = _sqlCommand.Parameters.AddWithValue("@" + _field.Name.Replace(".", "_"), _field.DbType);
                        _sqlParameter.Value = _field.Value;
                    }
                }
                else
                {
                    _return += _field.Name;
                }
            }
            return _return;
        }
        #endregion

        #region OutputString
        private string GetSqlOutput(TSQL _tsql)
        {
            if (_tsql.Outputs.Count == 0) { return ""; }

            string _output = " OUTPUT";
            for (int i = 0; i < _tsql.Outputs.Count; i++)
            {
                _output += i == 0 ? " " : ",";
                switch (_tsql.Outputs[i].Type)
                {
                    case OutputType.Inserted: _output += "INSERTED."; break;
                    case OutputType.Deleted: _output += "DELETED."; break;
                }
                _output += "[" + _tsql.Outputs[i].Name + "]";
            }
            return _output;
        }
        #endregion

        #region WhereString
        private string GetSqlWhere(IList<Where> _wheres, ref SqlCommand _sqlCommand)
        {
            string _return = "";
            Random _random = new Random();
            for (int _index = 0; _index < _wheres.Count; _index++)
            {
                Where _where = _wheres[_index];
                if (_index > 0)
                {
                    switch (_where.ConditionRelation)
                    {
                        case ConditionRelation.And:
                            _return += " AND ";
                            break;
                        case ConditionRelation.Or:
                            _return += " OR ";
                            break;
                    }
                }
                if (_where.HasSubCondition)
                {
                    _return += "(" + GetSqlWhere(_where.Conditions, ref _sqlCommand) + ")";
                }
                else
                {
                    _return += GetExpression(_where.ExpressionMode, _where.ExpressionRelation, _where.Field1, _where.Field2, _where.Field3, ref _sqlCommand, _random);
                }
            }
            return _return;
        }
        #endregion

        #region GroupByString
        private string GetSqlGroupBy(TSQL _tsql)
        {
            string _return = "";
            for (int _index = 0; _index < _tsql.Groups.Count; _index++)
            {
                if (_index > 0)
                {
                    _return += ",";
                }
                switch (_tsql.Groups[_index].GroupType)
                {
                    case GroupType.Field:
                        _return += "[" + _tsql.Groups[_index].FieldName.Replace(".", "].[") + "]";
                        break;
                    case GroupType.Value:
                        _return += _tsql.Groups[_index].FieldName;
                        break;
                }

            }
            return _return;
        }
        #endregion

        #region HavingString
        private string GetSqlHaving(TSQL _tsql, ref SqlCommand _sqlCommand)
        {
            return GetSqlWhere(_tsql.Havings, ref _sqlCommand);
        }
        #endregion

        #region OrderByString
        private string GetSqlOrderBy(IList<Order> _orderBys)
        {
            string _return = "";
            for (int _index = 0; _index < _orderBys.Count; _index++)
            {
                Order _orderBy = _orderBys[_index];
                if (_index > 0)
                {
                    _return += ",";
                }
                if (_orderBy.OrderType == OrderType.Value)
                {
                    // 以后需要改成参数的
                    _return += "" + _orderBy.FieldName;
                }
                else
                {
                    _return += "[" + _orderBy.FieldName.Replace(".", "].[") + "]";
                }
                switch (_orderBy.Method)
                {
                    case OrderMode.Asc:
                        _return += " ASC";
                        break;
                    case OrderMode.Desc:
                        _return += " DESC";
                        break;
                }
            }
            return _return;
        }
        #endregion

        #region ParameterString
        private string GetParameter(IList<Parameter> _parameters, ref SqlCommand _sqlCommand)
        {
            string _return = "";
            for (int i = 0; i < _parameters.Count; i++)
            {
                Parameter _parameter = (Parameter)_parameters[i];
                _sqlCommand.Parameters.AddWithValue(_parameter.Name, _parameter.Value);

                if (i > 0)
                {
                    _return += ",";
                }
                _return += "@" + _parameter.Name;
            }
            return _return;
        }
        #endregion

        #region ExpressionString
        private string GetExpression(ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _fieldObject1, object _fieldObject2, object _fieldObject3, ref SqlCommand _sqlCommand, Random _random)
        {
            string _return = "";
            string _field1 = _fieldObject1 == null ? "" : _fieldObject1.ToString();
            string _field2 = _fieldObject2 == null ? "" : _fieldObject2.ToString();
            string _field3 = _fieldObject3 == null ? "" : _fieldObject3.ToString();
            if (_expressionRelation != ExpressionRelation.Custom)
            {
                switch (_expressionMode)
                {
                    case ExpressionMode.FieldVsField:
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _field2 = "[" + _field2.Replace(".", "].[") + "]";
                        _field3 = "[" + _field3.Replace(".", "].[") + "]";
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.FieldVsFieldVsField:
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _field2 = "[" + _field2.Replace(".", "].[") + "]";
                        _field3 = "[" + _field3.Replace(".", "].[") + "]";
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.FieldVsFieldVsValue:
                        _field3 = "@" + _field1.Replace(".", "_") + _random.Next().ToString();
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _field2 = "[" + _field2.Replace(".", "].[") + "]";
                        _sqlCommand.Parameters.AddWithValue(_field3, _fieldObject3);
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.FieldVsValue:
                        _field2 = "@" + _field1.Replace(".", "_") + _random.Next().ToString();
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _field3 = "[" + _field1.Replace(".", "].[") + "]";
                        if (_expressionRelation == ExpressionRelation.Like)
                        {
                            SqlParameter _sqlParameter = new SqlParameter(_field2, SqlDbType.NVarChar);
                            _sqlParameter.Value = _fieldObject2;
                            _sqlCommand.Parameters.Add(_sqlParameter);
                        }
                        else if (_expressionRelation == ExpressionRelation.In || _expressionRelation == ExpressionRelation.NotIn)
                        {
                            object[] _array = (object[])_fieldObject2;
                            string _field2Value = "";
                            foreach (object _value in _array)
                            {
                                _field2Value += _field2Value == "" ? "" : ",";
                                string _field2Name = _field2 + _random.Next().ToString();
                                _field2Value += _field2Name;
                                _sqlCommand.Parameters.AddWithValue(_field2Name, _value);
                            }
                            _field2 = _field2Value;
                        }
                        else if (_fieldObject2.GetType() == Guid.NewGuid().GetType())
                        {
                            SqlParameter _sqlParameter = new SqlParameter(_field2, SqlDbType.UniqueIdentifier);
                            _sqlParameter.Value = _fieldObject2;
                            _sqlCommand.Parameters.Add(_sqlParameter);
                        }
                        else
                        {
                            _sqlCommand.Parameters.AddWithValue(_field2, _fieldObject2);
                        }
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.FieldVsValueVsField:
                        _field2 = "@" + _field1.Replace(".", "_") + _random.Next().ToString();
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _field3 = "[" + _field1.Replace(".", "].[") + "]";
                        _sqlCommand.Parameters.AddWithValue(_field2, _fieldObject2);
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.FieldVsValueVsValue:
                        _field2 = "@" + _field1.Replace(".", "_") + "1" + _random.Next().ToString();
                        _field3 = "@" + _field1.Replace(".", "_") + "2" + _random.Next().ToString();
                        _field1 = "[" + _field1.Replace(".", "].[") + "]";
                        _sqlCommand.Parameters.AddWithValue(_field2, _fieldObject2);
                        _sqlCommand.Parameters.AddWithValue(_field3, _fieldObject3);
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.ValueVsValueVsValue:
                        _field1 = "@" + _field1.Replace(".", "_") + "1" + _random.Next().ToString();
                        _field2 = "@" + _field1.Replace(".", "_") + "2" + _random.Next().ToString();
                        _field3 = "@" + _field1.Replace(".", "_") + "3" + _random.Next().ToString();
                        _sqlCommand.Parameters.AddWithValue(_field1, _fieldObject1);
                        _sqlCommand.Parameters.AddWithValue(_field2, _fieldObject2);
                        _sqlCommand.Parameters.AddWithValue(_field3, _fieldObject3);
                        _return = GetExpresstionString(_expressionRelation, _field1, _field2, _field3);
                        break;
                    case ExpressionMode.Custom:
                        _field1 = _fieldObject1.ToString();
                        _field2 = "";
                        _field3 = "";
                        _return = " " + _field1;
                        break;
                }
            }
            else
            {
                _return = _fieldObject1.ToString();
            }
            return _return;
        }
        private string GetExpresstionString(ExpressionRelation _expressionRelation, string _field1, string _field2, string _field3)
        {
            string _return = "";
            switch (_expressionRelation)
            {
                case ExpressionRelation.Between:
                    _return = _field1 + " BETWEEN " + _field2 + " AND " + _field3;
                    break;
                case ExpressionRelation.Custom:
                    _return = _field1;
                    break;
                case ExpressionRelation.Equal:
                    _return = _field1 + " = " + _field2;
                    break;
                case ExpressionRelation.In:
                    _return = _field1 + " IN (" + _field2 + ")";
                    break;
                case ExpressionRelation.LessEqualThen:
                    _return = _field1 + " <= " + _field2;
                    break;
                case ExpressionRelation.LessThen:
                    _return = _field1 + " < " + _field2;
                    break;
                case ExpressionRelation.Like:
                    _return = _field1 + " LIKE " + _field2 + "";
                    break;
                case ExpressionRelation.MoreEqualThen:
                    _return = _field1 + " >= " + _field2;
                    break;
                case ExpressionRelation.MoreThen:
                    _return = _field1 + " > " + _field2;
                    break;
                case ExpressionRelation.NotBetween:
                    _return = _field1 + " NOT BETWEEN " + _field2 + " AND " + _field3;
                    break;
                case ExpressionRelation.NotEqual:
                    _return = _field1 + " <> " + _field2;
                    break;
                case ExpressionRelation.NotIn:
                    _return = _field1 + " NOT IN (" + _field2 + ")";
                    break;
                case ExpressionRelation.NotLike:
                    _return = _field1 + " NOT LIKE '" + _field2 + "'";
                    break;
                case ExpressionRelation.NotNull:
                    _return = _field1 + " IS NOT NULL";
                    break;
                case ExpressionRelation.Null:
                    _return = _field1 + " IS NULL";
                    break;
            }
            return _return;
        }
        #endregion
    }
}
