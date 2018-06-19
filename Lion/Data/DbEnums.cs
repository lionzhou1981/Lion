using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lion.Data
{
    #region TSQLType
    /// <summary>
    /// Sql命令的类型
    /// </summary>
    public enum TSQLType
    {
        /// <summary>
        /// 新增
        /// </summary>
        Insert,
        /// <summary>
        /// 更新
        /// </summary>
        Update,
        /// <summary>
        /// 查询
        /// </summary>
        Select,
        /// <summary>
        /// 删除
        /// </summary>
        Delete,
        /// <summary>
        /// 存储过程
        /// </summary>
        Procedures,
        /// <summary>
        /// 创建（暂时无用）
        /// </summary>
        Create,
        /// <summary>
        /// 彻底删除（暂时无用）
        /// </summary>
        Drop,
        /// <summary>
        /// 修改结构（暂时无用）
        /// </summary>
        Alert,
        /// <summary>
        /// 删除表中的所有行
        /// </summary>
        Truncate
    }
    #endregion

    #region OrderMode
    /// <summary>
    /// 排序方式
    /// </summary>
    public enum OrderMode
    {
        /// <summary>
        /// 正序
        /// </summary>
        Asc,
        /// <summary>
        /// 逆序
        /// </summary>
        Desc
    }
    #endregion

    #region OrderType
    /// <summary>
    /// 排序种类
    /// </summary>
    public enum OrderType
    {
        /// <summary>
        /// 字段
        /// </summary>
        Field,
        /// <summary>
        /// 表达式
        /// </summary>
        Value
    }
    #endregion

    #region TableRelation
    /// <summary>
    /// 表之间的关系
    /// </summary>
    public enum TableRelation
    {
        /// <summary>
        /// 两表交集
        /// </summary>
        Inner,
        /// <summary>
        /// 左表全集
        /// </summary>
        LeftOuter,
        /// <summary>
        /// 右表全集
        /// </summary>
        RightOuter,
        /// <summary>
        /// 两表全集
        /// </summary>
        FullOuter,
        /// <summary>
        /// 两表多对多相连
        /// </summary>
        Cross
    }
    #endregion

    #region WhereRelation
    /// <summary>
    /// 条件之间的关系
    /// </summary>
    public enum WhereRelation
    {
        /// <summary>
        /// 与
        /// </summary>
        And,
        /// <summary>
        /// 或
        /// </summary>
        Or
    }
    #endregion

    #region ExpressionRelation
    /// <summary>
    /// 表达式中的关系 (是用字段1-3个)
    /// </summary>
    public enum ExpressionRelation
    {
        /// <summary>
        /// 等于 (Field1,Field2)
        /// </summary>
        Equal,
        /// <summary>
        /// 不等于 (Field1,Field2)
        /// </summary>
        NotEqual,
        /// <summary>
        /// 大于 (Field1,Field2)
        /// </summary>
        MoreThen,
        /// <summary>
        /// 小于 (Field1,Field2)
        /// </summary>
        LessThen,
        /// <summary>
        /// 大于等于 (Field1,Field2)
        /// </summary>
        MoreEqualThen,
        /// <summary>
        /// 小于等于 (Field1,Field2)
        /// </summary>
        LessEqualThen,
        /// <summary>
        /// 为空 (Field1)
        /// </summary>
        Null,
        /// <summary>
        /// 不为空 (Field1)
        /// </summary>
        NotNull,
        /// <summary>
        /// Like (Field1,Field2)
        /// </summary>
        Like,
        /// <summary>
        /// Not Like (Field1,Field2)
        /// </summary>
        NotLike,
        /// <summary>
        /// In (Field1,Field2)
        /// </summary>
        In,
        /// <summary>
        /// Not In (Field1,Field2)
        /// </summary>
        NotIn,
        /// <summary>
        /// Between (Field1,Field2,Field3)
        /// </summary>
        Between,
        /// <summary>
        /// Not Between (Field1,Field2,Field3)
        /// </summary>
        NotBetween,
        /// <summary>
        /// 自定义 (Field1)
        /// </summary>
        Custom
    }
    #endregion

    #region ExpressionMode
    /// <summary>
    /// 条件表达式的形式 
    /// </summary>
    public enum ExpressionMode
    {
        /// <summary>
        /// 字段间比较 (两个字段比较的用这个)
        /// </summary>
        FieldVsField,
        /// <summary>
        /// 字段 对 值 比较 (两个字段比较的用这个)
        /// </summary>
        FieldVsValue,
        /// <summary>
        /// 字段间比较 (两三个字段比较的都用这个)
        /// </summary>
        FieldVsFieldVsField,
        /// <summary>
        /// 字段 对 字段 对 值 比较 (主要用于BETWEEN等有三个对象比较的)
        /// </summary>
        FieldVsFieldVsValue,
        /// <summary>
        /// 字段 对 值 对 值 比较 (两个对象比较的请使用这个，也可用于三个比较的)
        /// </summary>
        FieldVsValueVsValue,
        /// <summary>
        /// 字段 对 值 对 字段比较 (主要用于BETWEEN等有三个对象比较的)
        /// </summary>
        FieldVsValueVsField,
        /// <summary>
        /// 值 对 值 对 值比较 (主要用于BETWEEN等有三个对象比较的)
        /// </summary>
        ValueVsValueVsValue,
        /// <summary>
        /// 自动定义Sql语句比较 (Sql语句请放在Field1里)
        /// </summary>
        Custom
    }
    #endregion

    #region FieldType
    /// <summary>
    /// 字段种类
    /// </summary>
    public enum FieldType
    {
        /// <summary>
        /// 普通字段
        /// </summary>
        SelectField,
        /// <summary>
        /// 普通字段
        /// </summary>
        ValueField,
        /// <summary>
        /// 自定义字段
        /// </summary>
        Custom,
        /// <summary>
        /// 行数正序
        /// </summary>
        RowNumberAsc,
        /// <summary>
        /// 行数倒序
        /// </summary>
        RowNumberDesc
    }
    #endregion

    #region GroupType
    /// <summary>
    /// 字段种类
    /// </summary>
    public enum GroupType
    {
        /// <summary>
        /// 字段
        /// </summary>
        Field,
        /// <summary>
        /// 值
        /// </summary>
        Value,
    }
    #endregion

    #region OutputType
    public enum OutputType
    {
        /// <summary>
        /// 插入的值
        /// </summary>
        Inserted,
        /// <summary>
        /// 删除的值
        /// </summary>
        Deleted
    }
    #endregion
}
