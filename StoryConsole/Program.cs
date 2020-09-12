using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StoryConsole
{
    class Program
    {
        const string VERSION = "1.0.0912";
        const string SAVE_DIR = @"\save";
        const string STORY_DIR = @"\story";
        enum GameStatus
        {
            RUN, STOP
        }
        static GameStatus gameStatus = GameStatus.STOP;

        static void Main(string[] args)
        {
            var storyTitle = JsonConvert.DeserializeObject<StoryTitle>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\story.json"));
            var option = new StorySelectOption[]{
                new StorySelectOption(){text = "開始遊戲"},
                new StorySelectOption(){text = "繼續遊戲"},
                new StorySelectOption(){text = "人物介紹"},
                new StorySelectOption(){text = "關於"},
                new StorySelectOption(){text = "結束"},
            };
            while (true)
            {
                switch (Select("     " + storyTitle.name + "               ", option))
                {
                    case 1:
                        Load(storyTitle.startFrom, 0);
                        break;
                    case 2:
                        var saveFile = LoadSave();
                        if (saveFile != null)
                        {
                            Load(saveFile.stoeyName, saveFile.textLine);
                        }
                        break;
                    case 3:
                        Character();
                        break;
                    case 4:
                        About();
                        break;
                    default:
                        return;
                }
            }
        }

        static void Load(string nextStoryName, int textLine)
        {
            gameStatus = GameStatus.RUN;
            while (gameStatus == GameStatus.RUN)
            {
                nextStoryName = RunStory(nextStoryName, textLine);
                textLine = 0;
            }
        }

        static string RunStory(string storyName, int textLine)
        {
            if (string.IsNullOrEmpty(storyName))
            {
                gameStatus = GameStatus.STOP;
                return "";
            }
            var story = JsonConvert.DeserializeObject<Story>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\" + storyName + ".json"));
            for (var i = textLine; i < story.story.Length; i++)
            {
                ShowStoreItem(story.story[i], storyName, i);
                if (gameStatus != GameStatus.RUN) return "";
            }
            if (story.@goto != null)
            {
                return story.@goto;
            }
            else if (story.select == null)
            {
                gameStatus = GameStatus.STOP;
                return "";
            }
            return story.select.option[Select(story.select.title, story.select.option) - 1].@goto;
        }

        static void ShowStoreItem(StoryTextItem item, string storyName, int textLine)
        {
            while (gameStatus == GameStatus.RUN)
            {
                Console.Write(item.text);
                wait(item.sleep * 1000);
                Console.WriteLine();
                Console.Write("                         按0選項:");
                try
                {
                    int selected = Convert.ToInt32(Console.ReadLine());
                    if (selected == 0)
                    {
                        var option = new StorySelectOption[]{
                            new StorySelectOption(){text = "返回"},
                            new StorySelectOption(){text = "存檔"},
                            new StorySelectOption(){text = "回主選單"},
                        };
                        while (true)
                        {
                            selected = Select("    選項      ", option);
                            if (selected == 1) break;
                            else if (selected == 2) 
                            { 
                                Save(storyName, textLine);
                                break;
                            }
                            else if (selected == 3)
                            {
                                gameStatus = GameStatus.STOP;
                                break;
                            }
                        }
                    }
                }
                catch (FormatException e)
                {
                    break;
                }
            }
        }

        static void Save(string storyName , int textLine)
        {
            var option = new StorySelectOption[]{
                new StorySelectOption(){text = "記錄檔1"},
                new StorySelectOption(){text = "記錄檔2"},
                new StorySelectOption(){text = "記錄檔3"},
                new StorySelectOption(){text = "記錄檔4"},
                new StorySelectOption(){text = "記錄檔5"},
                new StorySelectOption(){text = "返回"},
            };
            var fileItem = Select("    選擇記錄檔       ", option);
            if (fileItem == 6) return;
            var saveFile = new SaveFile()
            {
                stoeyName = storyName,
                textLine = textLine
            };
            File.WriteAllText(
                Directory.GetCurrentDirectory() + SAVE_DIR + @"\save" + fileItem + ".json",
                JsonConvert.SerializeObject(saveFile)
            );

            option = new StorySelectOption[]{
                new StorySelectOption(){text = "繼續遊戲"},
                new StorySelectOption(){text = "離開遊戲"},
            };
            switch (Select("請問您現在要?     ", option))
            {
                case 1:
                    return;
                default:
                    gameStatus = GameStatus.STOP;
                    return;
            }
        }

        static SaveFile LoadSave(){
            while(true){
                var option = new StorySelectOption[]{
                    new StorySelectOption(){text = "記錄檔1"},
                    new StorySelectOption(){text = "記錄檔2"},
                    new StorySelectOption(){text = "記錄檔3"},
                    new StorySelectOption(){text = "記錄檔4"},
                    new StorySelectOption(){text = "記錄檔5"},
                    new StorySelectOption(){text = "返回"},
                };
                var fileItem = Select("    選擇記錄檔       ", option);
                if (fileItem == 6) return null;
                try
                {
                    return JsonConvert.DeserializeObject<SaveFile>(File.ReadAllText(Directory.GetCurrentDirectory() + SAVE_DIR + @"\save" + fileItem + ".json"));
                }
                catch (FileNotFoundException e)
                {
                    Console.WriteLine("沒有紀錄！");
                    Console.ReadLine();
                }
            }
        }

        static int Select(string title, StorySelectOption[] option)
        {
            int selection = -1;

            Console.WriteLine(title);
            Console.WriteLine(new String('-', title.Length));
            for (var i = 0 ; i < option.Length ; i++)
                Console.WriteLine("{0}. {1}", i + 1, option[i].text);
            Console.WriteLine(new String('-', title.Length));
            do
            {
                Console.Write(">");
                try
                {
                    selection = Convert.ToInt32(Console.ReadLine());
                }
                catch (FormatException e)
                {
                    selection = -1;
                }
            }
            while (selection < 1 || selection > option.Length);
            return selection;
        }

        static void wait(int time)
        {
            while (time > 0)
            {
                Console.Write(".");
                Thread.Sleep(1000);
                time -= 1000;
            }
        }

        static void Character()
        {
            var character = JsonConvert.DeserializeObject<Character[]>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\character.json"));
            var option = new List<StorySelectOption>();
            foreach (var i in character)
            {
                option.Add(new StorySelectOption() { text = i.name });
            }
            option.Add(new StorySelectOption() { text = "反回" });
            while (true)
            {
                var selected = Select("    人物介紹      ", option.ToArray());
                if (1 <= selected && selected < option.Count)
                {
                    Console.WriteLine("--------------------------------------");
                    Console.WriteLine("             " + character[selected - 1].name);
                    Console.WriteLine(character[selected - 1].detailed);
                    Console.WriteLine("--------------------------------------");
                    Console.ReadLine();
                }
                else if (selected == option.Count)
                {
                    return;
                }
            }
        }

        static void About()
        {
            Console.WriteLine("                 關於");
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine("              StoryConsole");
            Console.WriteLine("這是一套作者作好玩的Console文字冒險遊戲框架");
            Console.WriteLine("作者：jack850628");
            Console.WriteLine("版本：" + VERSION);
            Console.WriteLine("-------------------------------------------");
            Console.ReadLine();
        }
    }
}
