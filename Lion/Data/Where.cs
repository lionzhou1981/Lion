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
        #region ConditionRelation
        /// <summary>
        /// ��ǰ�����Ĺ�ϵ
        /// </summary>
        public ConditionRelation ConditionRelation { get; set; }
        #endregion

        #region Conditions
        /// <summary>
        /// ������������
        /// </summary>
        public IList<Where> Conditions { get; set; }
        #endregion

        #region ExpressionMode
        /// <summary>
        /// ���ʽģʽ
        /// </summary>
        public ExpressionMode ExpressionMode { get; set; }
        #endregion

        #region ExpressionRelation
        /// <summary>
        /// �����ǵĹ�ϵ
        /// </summary>
        public ExpressionRelation ExpressionRelation { get; set; }
        #endregion

        #region Field1
        /// <summary>
        /// �����ֶ�1
        /// </summary>
        public object Field1 { get; set; }
        #endregion

        #region Field2
        /// <summary>
        /// �����ֶ�2
        /// </summary>
        public object Field2 { get; set; }
        #endregion

        #region Field3
        /// <summary>
        /// �����ֶ�3
        /// </summary>
        public object Field3 { get; set; }
        #endregion

        #region HasSubCondition
        /// <summary>
        /// �Ƿ��������� Ĭ��Ϊû��������
        /// </summary>
        public bool HasSubCondition { get; private set; }
        #endregion
        #endregion

        #region Structure
        #region Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object,object,object)
        /// <summary>
        /// Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object,object,object) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        /// <param name="_expressionMode">���ʽģʽ</param>
        /// <param name="_expressionRelation">���ʽ��ϵ</param>
        /// <param name="_field1">�ֶ�1������</param>
        /// <param name="_field2">�ֶ�2������</param>
        /// <param name="_field3">�ֶ�3������</param>
        public Where(ConditionRelation _conditionRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1, object _field2, object _field3)
        {
            this.ConditionRelation = _conditionRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = _field2;
            this.Field3 = _field3;
            this.HasSubCondition = false;
            this.Conditions = new List<Where>();
        }
        #endregion

        #region Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object,object)
        /// <summary>
        /// Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object,object) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        /// <param name="_expressionMode">���ʽģʽ</param>
        /// <param name="_expressionRelation">���ʽ��ϵ</param>
        /// <param name="_field1">�ֶ�1������</param>
        /// <param name="_field2">�ֶ�2������</param>
        public Where(ConditionRelation _conditionRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1, object _field2)
        {
            this.ConditionRelation = _conditionRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = _field2;
            this.Field3 = null;
            this.HasSubCondition = false;
            this.Conditions = new List<Where>();
        }
        #endregion

        #region Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object)
        /// <summary>
        /// Condition(ConditionRelation,ExpressionMode,ExpressionRelation,object) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        /// <param name="_expressionMode">���ʽģʽ</param>
        /// <param name="_expressionRelation">���ʽ��ϵ</param>
        /// <param name="_field1">�ֶ�1������</param>
        public Where(ConditionRelation _conditionRelation, ExpressionMode _expressionMode, ExpressionRelation _expressionRelation, object _field1)
        {
            this.ConditionRelation = _conditionRelation;
            this.ExpressionMode = _expressionMode;
            this.ExpressionRelation = _expressionRelation;
            this.Field1 = _field1;
            this.Field2 = null;
            this.Field3 = null;
            this.HasSubCondition = false;
            this.Conditions = new List<Where>();
        }
        #endregion

        #region Condition(ConditionRelation,bool)
        /// <summary>
        /// Condition(ConditionRelation,bool) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        /// <param name="_hasSubCondition">�Ƿ���������</param>
        public Where(ConditionRelation _conditionRelation, bool _hasSubCondition)
        {
            this.ConditionRelation = _conditionRelation;
            this.HasSubCondition = true;
            this.Conditions = new List<Where>();
        }
        #endregion

        #region Condition(ConditionRelation,string)
        /// <summary>
        /// Condition(ConditionRelation) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        /// <param name="_conditionString">�������</param>
        public Where(ConditionRelation _conditionRelation, string _conditionString)
        {
            this.ConditionRelation = _conditionRelation;
            this.HasSubCondition = false;
            this.ExpressionRelation = ExpressionRelation.Custom;
            this.Field1 = _conditionString;
            this.Conditions = new List<Where>();
        }
        #endregion

        #region Condition(ConditionRelation)
        /// <summary>
        /// Condition(ConditionRelation) ���캯��
        /// </summary>
        /// <param name="_conditionRelation">��ǰ������ϵ</param>
        public Where(ConditionRelation _conditionRelation)
        {
            this.ConditionRelation = _conditionRelation;
            this.HasSubCondition = false;
            this.Conditions = new List<Where>();
        }
        #endregion
        #endregion

        #region Method
        /// <summary>
        /// ���������
        /// </summary>
        /// <param name="_condition">����</param>
        public void AddCondition(Where _condition)
        {
            this.HasSubCondition = true;
            this.Conditions.Add(_condition);
        }
        #endregion
    }

    [Serializable]
    public class WhereCollection : List<Where>
    {
        /// <summary>
        /// ���һ����ǰһ��������And��ϵ������
        /// </summary>
        /// <param name="_values">������ֶ�����[]������������������Ϊ���ʽ</param>
        /// <example>
        /// ������ֶ�����[]������������������Ϊ���ʽ
        /// 1 �Զ�������(string) or �Ƿ���������(bool) or ��������(Condition)
        /// 2 ��һ�ֶ����� NULL,NOTNULL
        /// 3 �����ֶ����� =,&lt;&gt;,&gt;,&gt;=,&lt;,&lt;=,LIKE,NOTLIKE
        /// O ���������ֶ����� BETWEEN,IN,NOTIN
        /// </example>
        public void And(params object[] _values) => this.Add(ConditionRelation.And, _values);

        public void Or(params object[] _values) => this.Add(ConditionRelation.Or, _values);

        private void Add(ConditionRelation _relation, object[] _values)
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
                    #region 1 �Զ�������(string) or �Ƿ���������(bool) or ��������(Condition)
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
                    #region 2 ��һ�ֶ����� NULL,NOTNULL
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
                    #region 3 �����ֶ����� =,<>,>,>=,<,<=,LIKE,NOTLIKE,IN,NOTIN
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
                    #region ���������ֶ����� BETWEEN,NOTBETWEEN,IN,NOTIN
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
                            if(_values[2] is Array)
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
