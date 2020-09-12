using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryConsole
{
    class Story
    {
        public StoryTextItem[] story;
        public StorySelect select;
        public string @goto;
    }

    class StoryTextItem
    {
        public string text;
        public int sleep;
    }

    class StorySelect
    {
        public string title;
        public StorySelectOption[] option;
    }

    class StorySelectOption
    {
        public string text, @goto;
    }
}
