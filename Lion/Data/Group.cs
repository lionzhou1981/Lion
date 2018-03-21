using System;
using System.Collections.Generic;
using System.Text;

namespace Lion.Data
{
    [Serializable]
    public class Group
    {

        #region public
        public GroupType GroupType { get; set; }
        /// <summary>
        /// ���ɵ��ֶ�
        /// </summary>
        public string FieldName { get; set; }
        #endregion

        #region Structure
        /// <summary>
        /// GroupBy(FieldName) ���캯��
        /// </summary>
        /// <param name="_fieldName">������ֶ�</param>
        public Group(string _fieldName)
        {
            this.GroupType = GroupType.Field;
            this.FieldName = _fieldName;
        }
        /// <summary>
        /// GroupBy(FieldName) ���캯��
        /// </summary>
        /// <param name="_fieldName">������ֶ�</param>
        public Group(GroupType _groupType, string _fieldName)
        {
            this.GroupType = _groupType;
            this.FieldName = _fieldName;
        }
        #endregion
    }

    [Serializable]
    public class GroupCollection : List<Group>
    {
        public void Add(string _fieldName) => base.Add(new Group(_fieldName));

        public void Add(GroupType _groupType, string _fieldName) => base.Add(new Group(_groupType, _fieldName));
    }
}
