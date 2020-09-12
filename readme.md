# StoryConsole
Console文字冒險故事框架 \
<img src="截圖1.png" width="300"></img>

### 檔案結構
```
|-StoryConsole.exe -> 框架執行檔
|-Newtonsoft.Json.dll -> JSON函式庫
|-save -> 存檔資料夾
|-story -> 故事資料夾
    |-story.json -> 故事標題與故事根進入點
    |-character.json -> 角色介紹
```
### JSON檔案說明
#### story.json
```
{
	"name": "故事名稱",
	"startFrom": "故事的第一個檔案(不包含副檔名且必須放在story資料夾中)"
}
```
#### character.json
```
[
	{
		"name": "角色名稱",
		"detailed": "角色介紹"
	},
	...
]
```
#### 故事的JSON檔
```
{
	"story": [
		{
			"text": "故事內容",
			"sleep": 過場等待時間(整數且以秒為單位)
		},
		...
	],
	"select": {
		"title": "選項標題",
		"option": [
			{
				"text": "選項",
				"goto": "要跳至的故事JSON檔(不包含副檔名且必須放在story資料夾中)"
			},
			...
		]
	},
  "goto": "當story內的故事內容跑完後要跳至的故事JSON檔(不包含副檔名且必須放在story資料夾中)"
}
```
  注意！ \
  select與goto皆為可選， \
  當有定義goto時會忽略select， \
  當select與goto皆沒有定義時會直接回到主選單。
