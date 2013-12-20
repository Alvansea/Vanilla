using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Data
{
    public interface ICacheAdapter
    {
        void Add(string key, object value, TimeSpan slidingExpiration);
        void Remove(string key);
        object Get(string key);
    }
}
