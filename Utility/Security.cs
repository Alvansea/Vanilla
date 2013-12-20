using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Vanilla.Utility
{
    public class Security
    {
        public const string DefaultKey = "Pi22@70ken";

        public static byte[] MD5Bytes(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(UTF8Encoding.Default.GetBytes(text));
        }

        public static string MD5Text(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }
            string t = BitConverter.ToString(MD5Bytes(text));
            t = t.Replace("-", "");
            return t;
        }

        public static string GenerateAuthToken(string id, string key)
        {
            return MD5Text(id + key);
        }
    }
}
