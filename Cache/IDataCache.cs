using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Data
{
    public interface IDataCache
    {
        DataObject[] GetItems(Query query);
        void AddItems(Query query, object items);
        void Clear();
    }
}
