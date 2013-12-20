using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;

namespace Vanilla.Data
{
    public class ConditionSet : IEquatable<ConditionSet>
    {
        #region Properties

        private ConditionNode RootNode = null;

        private string sql;
        public string Sql
        {
            get
            {
                if (string.IsNullOrEmpty(this.sql))
                {
                    this.InitSql();
                }
                return this.sql;
            }
        }

        private Dictionary<string, object> parameters;
        public Dictionary<string, object> Parameters
        {
            get
            {
                if (this.parameters == null)
                {
                    this.InitSql();
                }
                return this.parameters;
            }
        }

        #endregion

        public ConditionSet()
        {
        }

        private void InitSql()
        {
            this.parameters = new Dictionary<string, object>();
            if (this.RootNode != null)
            {
                this.sql = this.RootNode.GetSql("0", this.parameters);
            }
        }

        public void And(ConditionNode condition)
        {
            if (this.RootNode == null)
            {
                this.RootNode = condition;
            }
            else
            {
                this.RootNode = this.RootNode.And(condition);
            }
        }

        public void And(string name, string optr, object value)
        {
            this.And(new ConditionNode(name, optr, value));
        }

        public void Or(ConditionNode condition)
        {
            if (this.RootNode == null)
            {
                this.RootNode = condition;
            }
            else
            {
                this.RootNode = this.RootNode.Or(condition);
            }
        }

        public void Or(string name, string optr, object value)
        {
            this.Or(new ConditionNode(name, optr, value));
        }

        #region IEquatable

        /// <summary>
        /// Implement IEquatable
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        public bool Equals(ConditionSet set)
        {
            int result = string.Compare(this.Sql, set.Sql);
            if (result != 0)
            {
                return false;
            }

            result = this.Parameters.Count - set.Parameters.Count;
            if (result != 0)
            {
                return false;
            }

            foreach (string key in this.Parameters.Keys)
            {
                if (!set.Parameters.ContainsKey(key))
                {
                    return false;
                }
                object x = this.Parameters[key];
                object y = set.Parameters[key];
                if (x.GetType() == y.GetType())
                {
                    IComparable c1 = (IComparable)x;
                    IComparable c2 = (IComparable)y;
                    if (!c1.Equals(c2))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        #endregion
    }
}
