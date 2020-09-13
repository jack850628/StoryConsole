using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryConsole
{
    class Select
    {
        public string title;
        public SelectOption[] option;
    }

    class SelectOption
    {
        public string text, @goto;
    }
}
