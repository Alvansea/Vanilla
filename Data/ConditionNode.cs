using System;
using System.Collections.Generic;
using System.Text;

using System.Collections;

namespace Vanilla.Data
{
    public class ConditionNode : IComparable
    {
        #region Properties

        public const int Branch = 1;
        public const int Leaf = 0;

        public const string AND = "AND";
        public const string OR = "OR";

        public static string[] Operators = new string[] { "=", "<>", ">", ">=", "<", "<=", "BETEWEEN", "IN", "LIKE" };

        /// <summary>
        /// 0 = leaf, 1 = branch or root
        /// </summary>
        protected int NodeType;

        /* Leaf properties */
        public string Name = null;
        public object Value = null;
        public string Operator = Operators[0];

        public string ColumnName
        {
            get
            {
                if (this.Name == null)
                {
                    return string.Empty;
                }
                string[] segs = this.Name.Split(new char[] { '.' });
                if (segs.Length == 2)
                {
                    return string.Format("[{0}].[{1}]", segs[0], segs[1]);
                }
                else
                {
                    return string.Format("[{0}]", this.Name);
                }
            }
        }

        /* Subconditions */
        protected List<ConditionNode> Children = new List<ConditionNode>();

        private string logicOperator = string.Empty;
        public string LogicOperator
        {
            set
            {
                if (string.IsNullOrEmpty(this.logicOperator) && !string.IsNullOrEmpty(value))
                {
                    this.logicOperator = value.ToUpper();
                }
            }
            get
            {
                return this.logicOperator;
            }
        }

        #endregion

        #region Constructor & Public Methods

        private ConditionNode()
        {
        }

        /// <summary>
        /// Constructor for leaf type condition
        /// </summary>
        /// <param name="name"></param>
        /// <param name="opt"></param>
        /// <param name="value"></param>
        public ConditionNode(string name, string opt, object value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(opt) || value == null)
            {
                throw new ArgumentException();
            }

            this.NodeType = Leaf;

            this.Name = name.ToUpper();
            this.Value = value;
            this.Operator = opt.ToUpper();
        }

        /// <summary>
        /// Constructor for branch type condition
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="LogicOptr"></param>
        public ConditionNode(string LogicOptr)
        {
            this.NodeType = Branch;
            this.LogicOperator = LogicOptr;
        }

        public void Add(ConditionNode condition)
        {
            if (condition == null)
            {
                return;
            }
            if (this.NodeType == Leaf && this.CompareTo(condition) != 0)
            {
                ConditionNode self = new ConditionNode(this.Name, this.Operator, this.Value);
                this.Children.Add(self);
                this.NodeType = Branch;
            }
            if (this.Children.IndexOf(condition) < 0)
            {
                this.Children.Add(condition);
            }
            this.Children.Sort();
        }

        public ConditionNode And(ConditionNode c)
        {
            if (this.NodeType == Leaf || this.LogicOperator == OR)
            {
                ConditionNode parent = new ConditionNode(AND);
                parent.Add(this);
                parent.Add(c);
                return parent;
            }
            else
            {
                this.Add(c);
                return this;
            }
        }

        public ConditionNode Or(ConditionNode c)
        {
            if (this.NodeType == Leaf || this.LogicOperator == AND)
            {
                ConditionNode parent = new ConditionNode(OR);
                parent.Add(this);
                parent.Add(c);
                return parent;
            }
            else
            {
                this.Add(c);
                return this;
            }
        }

        public string GetSql(string prefix, Dictionary<string, object> param)
        {
            string sql = string.Empty;
            switch (this.NodeType)
            {
                case Leaf:
                    this.Operator = this.Operator.ToUpper();
                    switch (this.Operator)
                    {
                        case "BETWEEN":
                            if (this.Value is IList)
                            {
                                IList list = (IList)this.Value;
                                if (list.Count > 1 && list[0] is IComparable && list[1] is IComparable)
                                {
                                    string param1 = "@" + prefix + "_0";
                                    string param2 = "@" + prefix + "_1";
                                    sql = string.Format("{0} BETWEEN {1} AND {2}", this.ColumnName, param1, param2);
                                    param.Add(param1, list[0]);
                                    param.Add(param2, list[1]);
                                }
                            }
                            break;
                        case "IN":
                            if (this.Value is IList)
                            {
                                IList list = (IList)this.Value;
                                string s = string.Empty;

                                for (int i = 0; i < list.Count; i++)
                                {
                                    string paramName = "@" + prefix + "_" + i;
                                    if (list[i] is IComparable)
                                    {
                                        s += paramName + ",";
                                        param.Add(paramName, list[i]);
                                    }
                                }
                                if (string.IsNullOrEmpty(s))
                                {
                                    s = "NULL";
                                }
                                else
                                {
                                    s = s.Trim(new char[] { ',' });
                                }
                                sql = string.Format("{0} IN ({1})", this.ColumnName, s);
                            }
                            break;
                        default:
                            if (this.Value is IComparable)
                            {
                                string paramName = "@" + prefix + "_0";
                                sql = this.ColumnName + " " + this.Operator + " " + paramName;
                                param.Add(paramName, this.Value);
                            }
                            break;
                    }
                    break;
                case Branch:
                    for (int i = 0; i < this.Children.Count; i++)
                    {
                        sql += this.Children[i].GetSql(prefix + "_" + i, param);
                        if (i != this.Children.Count - 1)
                        {
                            sql += " " + this.LogicOperator + " ";
                        }
                    }
                    if (!string.IsNullOrEmpty(sql))
                    {
                        sql = "(" + sql + ")";
                    }
                    break;
            }
            return sql;
        }

        #endregion

        #region IComparable

        /// <summary>
        /// Implement IComparable interface
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            try
            {
                ConditionNode node = (ConditionNode)obj;
                int i = this.NodeType - node.NodeType;
                if (i != 0)
                {
                    return i;
                }

                if (this.NodeType == Leaf)
                {
                    i = string.Compare(this.Name, node.Name, true);
                    if (i != 0)
                    {
                        return i;
                    }

                    i = string.Compare(this.Operator, node.Operator, true);
                    if (i != 0)
                    {
                        return i;
                    }

                    return this.CompareValue(this.Value, node.Value);
                }
                else
                {
                    i = string.Compare(this.LogicOperator, node.LogicOperator, true);
                    if (i != 0)
                    {
                        return i;
                    }

                    return this.CompareValue(this.Children, node.Children);
                }
            }
            catch
            {
                return -1;
            }
        }

        private int CompareValue(object x, object y)
        {
            if (x is IList && y is IList)
            {
                return this.CompareValue(x as IList, y as IList);
            }
            else if (x.GetType() == y.GetType())
            {
                if (x is IComparable && y is IComparable)
                {
                    IComparable c1 = (IComparable)x;
                    IComparable c2 = (IComparable)y;
                    return c1.CompareTo(c2);
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                string t1 = x.GetType().ToString();
                string t2 = y.GetType().ToString();
                return t1.CompareTo(t2);
            }
        }

        private int CompareValue(IList x, IList y)
        {
            int result = x.Count - y.Count;
            if (result != 0)
            {
                return result;
            }

            for (int i = 0; i < x.Count; i++)
            {
                result = this.CompareValue(x[i], y[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        #endregion
    }
}
