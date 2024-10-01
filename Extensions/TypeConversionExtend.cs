using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wukong_PBData_ReadWriter_GUI.Extensions
{
    public static class TypeConversionExtend
    {
        public static bool ToBool(this object obj)
        {
            try
            {
                return bool.Parse(obj.ToString());
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
