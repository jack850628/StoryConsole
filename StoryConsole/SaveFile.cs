using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryConsole
{
    class SaveFile
    {
        public string stoeyName;
        public int[] floorsLine;
        public Variable[] globalVariable;
    }

    class Variable
    {
        public string name;
        public string type;
        public dynamic value;
    }
}
