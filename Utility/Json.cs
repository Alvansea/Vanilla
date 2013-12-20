using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

using Vanilla.Data;

namespace Vanilla.Utility
{
    public class Json
    {
        public const string EmptyObject = "{}";
        public const string EmptyArray = "[]";

        #region Formatter

        public static string Format(object obj)
        {
            if (obj == null)
            {
                return EmptyObject;
            }
            else if (obj is IList)
            {
                return Format(obj as IList);
            }
            else if (obj is IDictionary)
            {
                return Format(obj as IDictionary);
            }
            else if (obj is NameValueCollection)
            {
                return Format(obj as NameValueCollection);
            }
            else
            {
                return _FormatString(obj);
            }
        }

        private static string _FormatString(object obj)
        {
            String s = obj.ToString();
            s = s.Replace("\\", "\\\\");
            s = s.Replace("'", "\\'");
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\n", "\\n");
            return "'" + s + "'";
        }

        private static string _Format(IList list)
        {
            string output = string.Empty;
            if (list != null && list.Count > 0)
            {
                foreach (object item in list)
                {
                    output += Format(item) + ",";
                }
                output = output.Remove(output.Length - 1);
            }
            output = "[" + output + "]";
            return output;
        }

        private static string _Format(IDictionary dictionary)
        {
            string output = string.Empty;
            if (dictionary != null && dictionary.Count > 0)
            {
                foreach (object key in dictionary.Keys)
                {
                    output += _FormatString(key) + ":" + Format(dictionary[key]) + ",";
                }
                output = output.Remove(output.Length - 1);
            }
            output = "{" + output + "}";
            return output;
        }

        private static string _Format(NameValueCollection input)
        {
            string output = string.Empty;
            if (input != null && input.Count > 0)
            {
                foreach (string key in input.Keys)
                {
                    output += _FormatString(key) + ":" + Format(input[key]) + ",";
                }
                output = output.Remove(output.Length - 1);
            }
            output = "{" + output + "}";
            return output;
        }

        #endregion

        #region Parser

        public static object Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            char[] contents = json.ToCharArray();
            Stack idx = new Stack();
            Stack rep = new Stack();
            foreach (char ch in contents)
            {
                if (_AppendChar(idx, rep, ch))
                {
                    continue;
                }
                switch (ch)
                {
                    case '{':
                        idx.Push("<node>");
                        Dictionary<string, object> node = new Dictionary<string, object>();
                        rep.Push(node);
                        break;
                    case '}':
                        _EndSegment(idx, rep);
                        if (idx.Peek().ToString() == "<node>")
                        {
                            idx.Pop();
                            idx.Push("</node>");
                        }
                        break;
                    case '[':
                        idx.Push("<list>");
                        List<object> list = new List<object>();
                        rep.Push(list);
                        break;
                    case ']':
                        _EndSegment(idx, rep);
                        if (idx.Peek().ToString() == "<list>")
                        {
                            idx.Pop();
                            idx.Push("</list>");
                        }
                        break;
                    case '\'':
                        if (idx.Peek().ToString() == "<string>")
                        {
                            idx.Pop();
                            idx.Push("</string>");
                        }
                        else
                        {
                            idx.Push("<string>");
                            rep.Push(string.Empty);
                        }
                        break;
                    case '\\':
                        if (idx.Peek().ToString() == "<string>")
                        {
                            idx.Push("<esc/>");
                        }
                        break;
                    case ':':
                        if (idx.Peek().ToString() == "</string>")
                        {
                            idx.Pop();
                            idx.Push("</name>");
                        }
                        break;
                    case ',':
                        _EndSegment(idx, rep);
                        break;
                    default:
                        if (idx.Peek().ToString() == "<string>")
                        {
                            string tmp = rep.Pop().ToString() + ch;
                            rep.Push(tmp);
                        }
                        break;
                }
            }
            if (rep.Count > 0)
            {
                return rep.Pop();
            }
            else
            {
                return null;
            }
        }

        private static void _EndSegment(Stack idx, Stack rep)
        {
            if (idx.Peek().ToString() == "</string>" || idx.Peek().ToString() == "</node>" || idx.Peek().ToString() == "</list>")
            {
                idx.Pop();  // pop </string>, </node> or </list>
                if (idx.Peek().ToString() == "</name>")
                {
                    idx.Pop();  // pop </name>
                    object value = rep.Pop();
                    string name = (string)rep.Pop();
                    Dictionary<string, object> node = (Dictionary<string, object>)rep.Pop();
                    node.Add(name, value);
                    rep.Push(node);
                }
                else if (idx.Peek().ToString() == "<list>")
                {
                    object value = rep.Pop();
                    List<object> list = (List<object>)rep.Pop();
                    list.Add(value);
                    rep.Push(list);
                }
            }
        }

        private static bool _AppendChar(Stack idx, Stack rep, char ch)
        {
            if (idx.Count <= 0)
            {
                return false;
            }
            if (idx.Peek().ToString() == "<esc/>")
            {
                idx.Pop(); // pop <escape>
                string tmp = rep.Pop().ToString() + ch;
                rep.Push(tmp);
                return true;
            }
            if (idx.Peek().ToString() == "<string>")
            {
                if (ch != '\'' && ch != '\\')
                {
                    string tmp = rep.Pop().ToString() + ch;
                    rep.Push(tmp);
                    return true;
                }
                return false;
            }
            return false;
        }

        #endregion
    }
}
