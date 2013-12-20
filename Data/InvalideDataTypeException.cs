using System;
using System.Collections.Generic;
using System.Text;

namespace Vanilla.Data
{
    public class InvalideDataTypeException : Exception
    {
        public InvalideDataTypeException(string dataType) : base(string.Format("Cannot find data type of {0} in DataDictionary."))
        {
        }
    }
}
