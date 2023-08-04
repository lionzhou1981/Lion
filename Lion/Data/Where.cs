using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lion.Data
{
    [Serializable]
    public class Where
    {
        #region public
        #region WhereRelation
        /// <summary>
        /// 与前条件的关系
        /// </summary>
        public WhereRelation WhereRelation { get; set; }
        #endregion

        #region Wheres
        /// <summary>
        /// 子条件的内容
        /// </summary>
        public WhereCollection Wheres { get; set; }
        #endregion

        #region ExpressionMode
        /// <summary>
        /// 表达式模式
        /// </summary>
        public ExpressionMode ExpressionMode { get; set; }
        #endregion

        #region ExpressionRelation
        /// <summary>
        /// 与表达是的关系
        /// </summary>
        public ExpressionRelation ExpressionRelation { get; set; }
        #endregion

        #region Field1
        /// <summary>
        /// 条件字段1
        /// </summary>
        public object Field1 { get; set; }
        #endregion

        #region Field2
        /// <summary>
        /// 条件字段2
        /// </summary>
        public object Field2 { get; set; }
        #endregion

        #region Field3
        /// <summary>
        /// 条件字段3
        /// </summary>
        public object Field3 { get; set; }
        #endregion

        #region HasSubWhere
        /// <summary>
        /// 是否有子条件 默认为没有子条件
        /// </summary>
        public bool HasSubWhere { get; private set; }
        #endregion
        #endregion

        #region Structure
        #region Where(WhereRelation,ExpressionMode,ExpressionRelation,object,object,object)
        /// <summary>
        /// Where(WhereRelation,ExpressionMode,ExpressionRelation,object,object,object) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        /// <param name="_expressionMode">表达式模式</param>
        /// <param name="_expressionRelation">表达式关系</param>
        /// <param name="_field1">字段1的内容</param>
        /// <param name="_field2">字段2的内容</param>
        /// <param name="_field3">字段3的内容</param>
        public Where(WhereRelation _whereRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1, object _field2, object _field3)
        {
            this.WhereRelation = _whereRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = _field2;
            this.Field3 = _field3;
            this.HasSubWhere = false;
            this.Wheres = new WhereCollection();
        }
        #endregion

        #region Where(WhereRelation,ExpressionMode,ExpressionRelation,object,object)
        /// <summary>
        /// Where(WhereRelation,ExpressionMode,ExpressionRelation,object,object) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        /// <param name="_expressionMode">表达式模式</param>
        /// <param name="_expressionRelation">表达式关系</param>
        /// <param name="_field1">字段1的内容</param>
        /// <param name="_field2">字段2的内容</param>
        public Where(WhereRelation _whereRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1, object _field2)
        {
            this.WhereRelation = _whereRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = _field2;
            this.Field3 = null;
            this.HasSubWhere = false;
            this.Wheres = new WhereCollection();
        }
        #endregion

        #region Where(WhereRelation,ExpressionMode,ExpressionRelation,object)
        /// <summary>
        /// Where(WhereRelation,ExpressionMode,ExpressionRelation,object) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        /// <param name="_expressionMode">表达式模式</param>
        /// <param name="_expressionRelation">表达式关系</param>
        /// <param name="_field1">字段1的内容</param>
        public Where(WhereRelation _whereRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1)
        {
            this.WhereRelation = _whereRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = null;
            this.Field3 = null;
            this.HasSubWhere = false;
            this.Wheres = new WhereCollection();
        }
        #endregion

        #region Where(WhereRelation,bool)
        /// <summary>
        /// Where(WhereRelation,bool) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        /// <param name="_hasSubCondition">是否有子条件</param>
        public Where(WhereRelation _whereRelation, bool _hasSubCondition)
        {
            this.WhereRelation = _whereRelation;
            this.HasSubWhere = true;
            this.Wheres = new WhereCollection();
        }
        #endregion

        #region Where(WhereRelation,string)
        /// <summary>
        /// Where(WhereRelation) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        /// <param name="_whereString">条件语句</param>
        public Where(WhereRelation _whereRelation, string _whereString)
        {
            this.WhereRelation = _whereRelation;
            this.HasSubWhere = false;
            this.ExpressionRelation = ExpressionRelation.Custom;
            this.Field1 = _whereString;
            this.Wheres = new WhereCollection();
        }
        #endregion

        #region Where(WhereRelation)
        /// <summary>
        /// Where(WhereRelation) 构造函数
        /// </summary>
        /// <param name="_whereRelation">与前条件关系</param>
        public Where(WhereRelation _whereRelation)
        {
            this.WhereRelation = _whereRelation;
            this.HasSubWhere = false;
            this.Wheres = new WhereCollection();
        }
        #endregion
        #endregion

        #region Method
        public void Add(Where _where) => this.Wheres.Add(_where);
        public void And(params object[] _values) => this.Wheres.And(_values);
        public void Or(params object[] _values) => this.Wheres.Or(_values);
        #endregion
    }

    [Serializable]
    public class WhereCollection : List<Where>
    {
        /// <summary>
        /// 添加一个与前一个条件是And关系的条件
        /// </summary>
        /// <param name="_values">如果是字段请用[]中括弧包括，否则视为表达式</param>
        /// <example>
        /// 如果是字段请用[]中括弧包括，否则视为表达式
        /// 1 自定义条件(string) or 是否有自条件(bool) or 条件对象(Condition)
        /// 2 单一字段条件 NULL,NOTNULL
        /// 3 两个字段条件 =,&lt;&gt;,&gt;,&gt;=,&lt;,&lt;=,LIKE,NOTLIKE
        /// O 三个以上字段条件 BETWEEN,IN,NOTIN
        /// </example>
        public void And(params object[] _values) => this.Add(WhereRelation.And, _values);

        public void Or(params object[] _values) => this.Add(WhereRelation.Or, _values);

        private void Add(WhereRelation _relation, object[] _values)
        {
            bool[] _fields = new bool[_values.Length];
            for (int i = 0; i < _fields.Length; i++)
            {
                _fields[i] = _values[i].GetType() == typeof(string) && Regex.IsMatch(_values[i].ToString(), @"\[.+\]");
                if (_fields[i])
                {
                    _values[i] = _values[i].ToString().Substring(1, _values[i].ToString().Length - 2);
                }
            }

            switch (_values.Length)
            {
                case 1:
                    #region 1 自定义条件(string) or 是否有自条件(bool) or 条件对象(Condition)
                    {
                        if (_values[0].GetType() == typeof(Where))
                        {
                            base.Add((Where)_values[0]);
                        }
                        else if (_values[0].GetType() == typeof(bool))
                        {
                            base.Add(new Where(_relation, (bool)_values[0]));
                        }
                        else
                        {
                            base.Add(new Where(_relation, _values[0].ToString()));
                        }
                        break;
                    }
                    #endregion
                case 2:
                    #region 2 单一字段条件 NULL,NOTNULL
                    {
                        switch (_values[1].ToString())
                        {
                            case "NULL": base.Add(new Where(_relation, ExpressionMode.FieldVsField, ExpressionRelation.Null, _values[0])); break;
                            case "NOTNULL": base.Add(new Where(_relation, ExpressionMode.FieldVsField, ExpressionRelation.NotNull, _values[0])); break;
                        }
                        break;
                    }
                    #endregion
                case 3:
                    #region 3 两个字段条件 =,<>,>,>=,<,<=,LIKE,NOTLIKE,IN,NOTIN
                    {
                        ExpressionMode _mode = _fields[2] ? ExpressionMode.FieldVsField : ExpressionMode.FieldVsValue;
                        switch (_values[1].ToString())
                        {
                            case "=": base.Add(new Where(_relation, _mode, ExpressionRelation.Equal, _values[0], _values[2])); break;
                            case "<>": base.Add(new Where(_relation, _mode, ExpressionRelation.NotEqual, _values[0], _values[2])); break;
                            case ">": base.Add(new Where(_relation, _mode, ExpressionRelation.MoreThen, _values[0], _values[2])); break;
                            case ">=": base.Add(new Where(_relation, _mode, ExpressionRelation.MoreEqualThen, _values[0], _values[2])); break;
                            case "<": base.Add(new Where(_relation, _mode, ExpressionRelation.LessThen, _values[0], _values[2])); break;
                            case "<=": base.Add(new Where(_relation, _mode, ExpressionRelation.LessEqualThen, _values[0], _values[2])); break;
                            case "LIKE": base.Add(new Where(_relation, _mode, ExpressionRelation.Like, _values[0], _values[2])); break;
                            case "NOTLIKE": base.Add(new Where(_relation, _mode, ExpressionRelation.NotLike, _values[0], _values[2])); break;
                            case "IN":
                                #region IN
                                {
                                    object[] _valueList;
                                    if (_values[2].GetType() == typeof(Array) || _values[2].GetType().BaseType == typeof(Array))
                                    {
                                        List<object> _objects = new List<object>();
                                        Array _array = (Array)_values[2];
                                        for (int i = 0; i < _array.Length;i++)
                                        {
                                            _objects.Add(_array.GetValue(i));
                                        }
                                        _valueList = _objects.ToArray();
                                    }
                                    else
                                    {
                                        _valueList = new object[] { _values[2] };
                                    }
                                    base.Add(new Where(_relation, ExpressionMode.FieldVsValue, ExpressionRelation.In, _values[0], _valueList));
                                    break;
                                }
                                #endregion
                            case "NOTIN":
                                #region NOTIN
                                {
                                    object[] _valueList;
                                    if (_values[2].GetType() == typeof(Array) || _values[2].GetType().BaseType == typeof(Array))
                                    {
                                        _valueList = (object[])_values[2];
                                    }
                                    else
                                    {
                                        _valueList = new object[] { _values[2] };
                                    }
                                    base.Add(new Where(_relation, ExpressionMode.FieldVsValue, ExpressionRelation.NotIn, _values[0], _valueList));
                                    break;
                                }
                                #endregion
                        }
                        break;
                    }
                    #endregion
                default:
                    #region 三个以上字段条件 BETWEEN,NOTBETWEEN,IN,NOTIN
                    {
                        if ((_values[1].ToString() == "BETWEEN" || _values[1].ToString() == "NOTBETWEEN") && _fields.Length == 4)
                        {
                            ExpressionRelation _expressRelation = _values[1].ToString() == "BETWEEN" ? ExpressionRelation.Between : ExpressionRelation.NotBetween;
                            ExpressionMode _mode = ExpressionMode.FieldVsValueVsValue;
                            if (_fields[2] && _fields[3]) { _mode = ExpressionMode.FieldVsFieldVsField; }
                            if (_fields[2] && !_fields[3]) { _mode = ExpressionMode.FieldVsFieldVsValue; }
                            if (!_fields[2] && _fields[3]) { _mode = ExpressionMode.FieldVsValueVsField; }
                            if (!_fields[2] && !_fields[3]) { _mode = ExpressionMode.FieldVsValueVsValue; }
                            base.Add(new Where(_relation, _mode, _expressRelation, _values[0], _values[2], _values[3]));
                        }
                        else if (_values[1].ToString() == "IN" || _values[1].ToString() == "NOTIN")
                        {
                            ExpressionRelation _expressRelation = _values[1].ToString() == "IN" ? ExpressionRelation.In : ExpressionRelation.NotIn;
                            if (_values[2].GetType() == typeof(Array) || _values[2].GetType().BaseType == typeof(Array))
                            {
                                base.Add(new Where(_relation, ExpressionMode.FieldVsValue, _expressRelation, _values[0], _values[2]));
                            }
                            else
                            {
                                object[] _valueList = new object[_values.Length - 2];
                                Array.Copy(_values, 2, _valueList, 0, _valueList.Length);
                                base.Add(new Where(_relation, ExpressionMode.FieldVsValue, _expressRelation, _values[0], _valueList));
                            }
                        }
                        break;
                    }
                    #endregion
            }
        }
    }
}
