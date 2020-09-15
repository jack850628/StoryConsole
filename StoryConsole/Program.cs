using Jint;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace StoryConsole
{
    class Program
    {
        public const string VERSION = "1.1.0915";
        const string SAVE_DIR = @"\save";
        const string STORY_DIR = @"\story";
        static Engine jsEngine = null;
        readonly static List<FloorInformation> floorsLine = new List<FloorInformation>();

        enum GameStatus
        {
            RUN, STOP, BREAK, CONTINUE, GOTO
        }
        static GameStatus gameStatus = GameStatus.STOP;

        enum IfStatus
        {
            CONDITION_NOT_MET, CONDITION_MET
        }

        static void Main(string[] args)
        {
            var storyTitle = JsonConvert.DeserializeObject<Story>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\story.json"));
            var option = new SelectOption[]{
                new SelectOption(){text = "開始遊戲"},
                new SelectOption(){text = "繼續遊戲"},
                new SelectOption(){text = "人物介紹"},
                new SelectOption(){text = "關於"},
                new SelectOption(){text = "結束"},
            };
            while (true)
            {
                switch (Select("     " + storyTitle.name + "               ", option))
                {
                    case 1:
                        var globalVariable = JsonConvert.DeserializeObject<Variable[]>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\globalVariable.json"));
                        Load(storyTitle.startFrom, globalVariable);
                        break;
                    case 2:
                        var saveFile = LoadSave();
                        if (saveFile != null)
                        {
                            floorsLine.AddRange(saveFile.floorsLine);
                            Load(saveFile.stoeyName, saveFile.globalVariable);
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

        static void Load(string nextStoryName, Variable[] globalVariable)
        {
            jsEngine = new Engine();

            var SC = new Dictionary<string, object>();
            foreach (var v in globalVariable)
            {
                switch (v.type)
                {
                    case "string":
                    case "number":
                    case "boolean":
                    case "object":
                        SC.Add(v.name, v.value);
                        break;
                    case "array":
                        var val = v.value as JArray;
                        var arr = new object[val.Count];
                        for (var i = 0; i < arr.Length; i++)
                            arr[i] = val[i];
                        SC.Add(v.name, arr);
                        break;
                    default:
                        throw new Exception("變數型態錯誤");
                }
            }
            jsEngine.SetValue("SC", SC);

            gameStatus = GameStatus.RUN;
            while (gameStatus != GameStatus.STOP)
            {
                var commands = JsonConvert.DeserializeObject<Command[]>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\" + nextStoryName + ".json"));
                nextStoryName = RunStory(commands, nextStoryName, 0);

                if (string.IsNullOrEmpty(nextStoryName))
                {
                    gameStatus = GameStatus.STOP;
                }
                else if(gameStatus == GameStatus.GOTO)
                {
                    gameStatus = GameStatus.RUN;
                }
            }
        }

        static string RunStory(Command[] commands, string storyName, int floor)
        {
            var ifStatus = IfStatus.CONDITION_NOT_MET;

            try
            {
                if(floor >= floorsLine.Count) floorsLine.Add(new FloorInformation(){ line = 0 });
                for (; floorsLine[floor].line < commands.Length; floorsLine[floor].line++)
                {
                    var command = commands[floorsLine[floor].line];
                    if (command.show != null)
                    {
                        Show(command.show, storyName);
                        if (gameStatus == GameStatus.STOP) return "";
                    }
                    else if (command.sleep != null)
                    {
                        long sec;
                        if (command.sleep is long)
                        {
                            sec = (long)command.sleep;
                        }
                        else if (command.sleep is string)
                        {
                            sec = Convert.ToInt64(jsEngine.Execute(command.sleep as string).GetCompletionValue().AsNumber());
                        }
                        else
                        {
                            throw new Exception("sleep時間型別錯誤");
                        }
                        wait(sec * 1000);
                    }
                    else if (command.select != null)
                    {
                        if(floor == floorsLine.Count - 1)
                            floorsLine[floor].selecteOptionItem = Select(command.select.title, command.select.option, true) - 1;
                        var result = RunStory(
                            command.select.option[(int)floorsLine[floor].selecteOptionItem].then, 
                            storyName, 
                            floor + 1
                        );
                        floorsLine[floor].selecteOptionItem = null;
                        if (
                            gameStatus == GameStatus.STOP ||
                            gameStatus == GameStatus.BREAK ||
                            gameStatus == GameStatus.CONTINUE ||
                            gameStatus == GameStatus.GOTO
                        )
                        {
                            return result;
                        }
                    }
                    else if (command.exec != null)
                    {
                        Exec(command.exec);
                    }
                    else if (command.@goto != null)
                    {
                        gameStatus = GameStatus.GOTO;
                        return command.@goto;
                    }
                    else if (command.@continue != null)
                    {
                        gameStatus = GameStatus.CONTINUE;
                        return null;
                    }
                    else if (command.@break != null)
                    {
                        gameStatus = GameStatus.BREAK;
                        return null;
                    }
                    else if (command.@if != null)
                    {
                        if (floor < floorsLine.Count - 1 || jsEngine.Execute(command.@if).GetCompletionValue().AsBoolean())
                        {
                            ifStatus = IfStatus.CONDITION_MET;
                            var result = RunStory(command.then, storyName, floor + 1);
                            if (
                                gameStatus == GameStatus.STOP ||
                                gameStatus == GameStatus.BREAK ||
                                gameStatus == GameStatus.CONTINUE ||
                                gameStatus == GameStatus.GOTO
                            )
                            {
                                return result;
                            }
                        }
                        else
                        {
                            ifStatus = IfStatus.CONDITION_NOT_MET;
                        }
                    }
                    else if (command.elseif != null)
                    {
                        if (floor < floorsLine.Count - 1 || ifStatus == IfStatus.CONDITION_NOT_MET && jsEngine.Execute(command.elseif).GetCompletionValue().AsBoolean())
                        {
                            ifStatus = IfStatus.CONDITION_MET;
                            var result = RunStory(command.then, storyName, floor + 1);
                            if (
                                gameStatus == GameStatus.STOP ||
                                gameStatus == GameStatus.BREAK ||
                                gameStatus == GameStatus.CONTINUE ||
                                gameStatus == GameStatus.GOTO
                            )
                            {
                                return result;
                            }
                        }
                    }
                    else if (command.@else != null)
                    {
                        if (ifStatus == IfStatus.CONDITION_NOT_MET)
                        {
                            var result = RunStory(command.@else, storyName, floor + 1);
                            if (
                                gameStatus == GameStatus.STOP || 
                                gameStatus == GameStatus.BREAK || 
                                gameStatus == GameStatus.CONTINUE || 
                                gameStatus == GameStatus.GOTO
                            )
                            {
                                return result;
                            }
                        }
                    }
                    else if (command.@while != null)
                    {
                        while (true) {
                            if (floor < floorsLine.Count - 1 || jsEngine.Execute(command.@while).GetCompletionValue().AsBoolean())
                            {
                                var result = RunStory(command.then, storyName, floor + 1);
                                if (gameStatus == GameStatus.BREAK)
                                {
                                    gameStatus = GameStatus.RUN;
                                    break;
                                }
                                else if (gameStatus == GameStatus.STOP || gameStatus == GameStatus.GOTO)
                                {
                                    return result;
                                }
                                else if (gameStatus == GameStatus.CONTINUE)
                                {
                                    gameStatus = GameStatus.RUN;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                return null;
            }
            finally
            {
                floorsLine.RemoveAt(floor);
            }
        }

        static void Show(string text, string storyName)
        {
            while (gameStatus == GameStatus.RUN)
            {
                Console.WriteLine(jsEngine.Execute(text).GetCompletionValue());
                Console.Write("                         按0選項:");
                try
                {
                    int selected = Convert.ToInt32(Console.ReadLine());
                    if (selected == 0)
                    {
                        var option = new SelectOption[]{
                            new SelectOption(){text = "返回"},
                            new SelectOption(){text = "存檔"},
                            new SelectOption(){text = "回主選單"},
                        };
                        while (true)
                        {
                            selected = Select("    選項      ", option);
                            if (selected == 1) break;
                            else if (selected == 2) 
                            { 
                                Save(storyName);
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

        static void Exec(string text)
        {
            jsEngine.Execute(text);
        }

        static void Save(string storyName)
        {
            var option = new SelectOption[]{
                new SelectOption(){text = "記錄檔1"},
                new SelectOption(){text = "記錄檔2"},
                new SelectOption(){text = "記錄檔3"},
                new SelectOption(){text = "記錄檔4"},
                new SelectOption(){text = "記錄檔5"},
                new SelectOption(){text = "返回"},
            };
            var fileItem = Select("    選擇記錄檔       ", option);
            if (fileItem == 6) return;
            var globalVariable = new List<Variable>();
            foreach (var name in jsEngine.GetValue("SC").ToObject() as Dictionary<string, object>)
            {
                switch (name.Value)
                {
                    case var v when v is string:
                        globalVariable.Add(new Variable()
                            {
                                name = name.Key,
                                type = "string",
                                value = v as string
                            }
                        );
                        break;
                    case var v when v is double || v is long:
                        globalVariable.Add(new Variable()
                            {
                                name = name.Key,
                                type = "number",
                                value = v
                            }
                        );
                        break;
                    case var v when v is bool:
                        globalVariable.Add(new Variable()
                            {
                                name = name.Key,
                                type = "boolean",
                                value = (bool)v
                            }
                        );
                        break;
                    case var v when v is object[]:
                        globalVariable.Add(new Variable()
                            {
                                name = name.Key,
                                type = "array",
                                value = v
                            }
                        );
                        break;
                    case var v when v == null || v is object:
                        globalVariable.Add(new Variable()
                            {
                                name = name.Key,
                                type = "object",
                                value = v
                            }
                        );
                        break;
                }
            }
            var saveFile = new SaveFile()
            {
                stoeyName = storyName,
                floorsLine = floorsLine.ToArray(),
                globalVariable = globalVariable.ToArray()
            };
            File.WriteAllText(
                Directory.GetCurrentDirectory() + SAVE_DIR + @"\save" + fileItem + ".json",
                JsonConvert.SerializeObject(saveFile)
            );

            option = new SelectOption[]{
                new SelectOption(){text = "繼續遊戲"},
                new SelectOption(){text = "離開遊戲"},
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
                var option = new SelectOption[]{
                    new SelectOption(){text = "記錄檔1"},
                    new SelectOption(){text = "記錄檔2"},
                    new SelectOption(){text = "記錄檔3"},
                    new SelectOption(){text = "記錄檔4"},
                    new SelectOption(){text = "記錄檔5"},
                    new SelectOption(){text = "返回"},
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

        static int Select(string title, SelectOption[] option, bool useJsOption = false)
        {
            int selection = -1;

            Console.WriteLine(useJsOption ? jsEngine.Execute(title).GetCompletionValue() : title);
            Console.WriteLine(new String('-', title.Length));
            for (var i = 0 ; i < option.Length ; i++)
                Console.WriteLine("{0}. {1}", i + 1, useJsOption ? jsEngine.Execute(option[i].text).GetCompletionValue() : option[i].text);
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

        static void wait(long time)
        {
            while (time > 0)
            {
                Console.Write(".");
                Thread.Sleep(1000);
                time -= 1000;
            }
            Console.WriteLine('\n');
        }

        static void Character()
        {
            var character = JsonConvert.DeserializeObject<Character[]>(File.ReadAllText(Directory.GetCurrentDirectory() + STORY_DIR + @"\character.json"));
            var option = new List<SelectOption>();
            foreach (var i in character)
            {
                option.Add(new SelectOption() { text = i.name });
            }
            option.Add(new SelectOption() { text = "反回" });
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
