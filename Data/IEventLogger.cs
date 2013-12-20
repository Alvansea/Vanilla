using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Data
{
    public interface IEventLogger
    {
        void Log(string log);
        void Update(string log);
        void Error(string error, int level);
    }
}
