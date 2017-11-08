using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace external_drive_lib.exceptions
{
    class exception : System.IO.IOException
    {
        public exception(string s) : base(s) {
            
        }
        public exception(string s, Exception inner) : base(s, inner) {
            
        }
    }
}
