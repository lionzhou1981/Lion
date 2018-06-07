# Insert Example
```c#
TSQL _tsql=new TSQL(TSQLType.Insert,"[TABLE_NAME]");
_tsql.Fields.Add("[Field1]","",[Value1]);
_tsql.Fields.Add("[Field2]","",[Value2]);
_tsql.Fields.Add("[Field3]","",[Value3]);
_tsql.Fields.Add("[Field4]","",[Value4]);
_tsql.Outputs.Add(OutputType.Inserted,"[OuputField]");
```

# Update Example
```c#
TSQL _tsql=new TSQL(TSQLType.Update,"[TABLE_NAME]");
_tsql.Fields.Add("[Field1]","",[Value1]);
_tsql.Fields.Add("[Field2]","",[Value2]);
_tsql.Fields.Add("[Field3]","",[Value3]);
_tsql.Fields.Add("[Field4]","",[Value4]);
_tsql.Wheres.And("[Fieldx]","=",[Valuex]);
```

# Delete Example
```c#
TSQL _tsql=new TSQL(TSQLType.Delete,"[TABLE_NAME]");
_tsql.Wheres.And("[Fieldx]","=",[Valuex]);
```

# Select Example 1
```c#
TSQL _tsql=new TSQL(TSQLType.Select,"[TABLE_NAME]");
_tsql.Top=1;
```
```c#
_tsql.Fields.Add("[Field1]","",[Value1]);
_tsql.Fields.Add("[Field2]","",[Value2]);
_tsql.Fields.Add("[Field3]","",[Value3]);
_tsql.Fields.Add("[Field4]","",[Value4]);
```
or
```c#
_tsql.Fields.Add("*");
```
or
```c#
_tsql.Fields.Add("COUNT(*)");
```
```c#
_tsql.Wheres.And("[Fieldx]","=",[Valuex]);
_tsql.Orders.AscField("[Field]");
_tsql.Groups.Add("[Field]");
```

# Select Example 2 Pager
```c#
_tsql.IsWith = true;
_tsql.Fields.Add(FieldType.RowNumberAsc, "Id", "RowNumber");    // Paging field and order type
_tsql.Fields.Add("*");                                          // Another fields
_tsql.WithConditions.Add(new Condition(ConditionRelation.And, ExpressionMode.FieldVsValueVsValue, ExpressionRelation.Between, "RowNumber", _page * _pageSize + 1, (_page + 1) * _pageSize)); // page start with 0
_tsql.WithOrderBys.Add(new OrderBy("RowNumber", OrderMode.Asc));
```
