using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.exceptions
{
    class external_drive_libexception : System.IO.IOException
    {
        public external_drive_libexception(string s) : base(s) {
            
        }
        public external_drive_libexception(string s, Exception inner) : base(s, inner) {
            
        }
    }
}
