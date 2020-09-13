using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryConsole
{
    class Command
    {
        public string show;
        public string @goto;
        public Select select;
        public int? sleep;
        public string exec;
        public dynamic @break;
        public dynamic @continue;

        public string @if;
        public string @elseif;
        public Command[] @else;
        public string @while;
        public Command[] then;
    }
}
