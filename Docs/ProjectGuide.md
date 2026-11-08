# 深淵酒吧專案導覽

從 `Assets/Scenes/MainMenu.unity` 開始閱讀。主選單進入 `BarHub`，酒吧右側出口進入 `Main`，結算可回到酒吧。三個場景依這個順序列在 Build Profiles 的 Scene List。

## 檔案放在哪裡

| 目錄 | 內容 |
| --- | --- |
| `Assets/Scenes` | 主選單、酒吧、地鐵站戰鬥 |
| `Assets/Scripts` | 功能程式。先依功能找 `Hub`、`Combat`、`Enemies`、`Waves`、`RunUpgrades` 或 `UI` |
| `Assets/Scripts/Editor` | 編輯器工具，分成預覽、安裝器、診斷和介面建構 |
| `Assets/Data` | Inspector 可調整的角色、敵人、波次、武器、技能與升級卡資料 |
| `Assets/Art` | 角色、敵人、酒吧、地圖、卡圖和介面字型 |
| `Assets/Prefabs` | 可重用的敵人、戰鬥物件、掉落物和澪的專屬物件 |
| `Assets/Resources` | 遊戲用程式路徑載入的角色美術、酒吧配置與卡庫 |
| `Assets/Settings`、`ProjectSettings`、`Packages` | 渲染、建置、專案和套件設定 |
| `Docs` | 使用與維護說明。早期文件在 `Docs/History` |

## 依流程閱讀程式

1. **主選單**從 [MainMenuUI](../Assets/Scripts/UI/Menu/MainMenuUI.cs) 開始，按開始會進入酒吧。
2. **酒吧**由 [HubCharacterSelection](../Assets/Scripts/Hub/HubCharacterSelection.cs) 管理選角和出戰。[HubSceneComposer](../Assets/Scripts/Hub/HubSceneComposer.cs) 根據酒吧配置組合場景美術。
3. **戰鬥**由 [PlayerCharacter](../Assets/Scripts/Combat/Player/PlayerCharacter.cs) 讀入角色設定。角色行動在 `Combat/Player`，敵人與出怪在 `Enemies` 和 `Waves`。
4. **升級**由 [UpgradeManager](../Assets/Scripts/RunUpgrades/UpgradeManager.cs) 開啟選卡畫面。卡庫決定有哪些卡，效果程式處理選卡後的變化。
5. **結算**由 [GameResultUI](../Assets/Scripts/UI/Battle/GameResultUI.cs) 顯示結果、發放獎勵並切換場景。

## 在 Unity 查看和維護

- **Abyss Bar → Scene Preview** 有三個場景入口。開場景後可用 **Frame Current Scene** 查看配置，或切到 **Game** 查看攝影機畫面。
- 酒吧美術由配置資料產生。要調整椅子或設施，從 **Abyss Bar → Hub → Editor Preview → Edit Layout Data** 開啟配置。
- 戰鬥的編輯預覽使用場景預設角色的待機畫面。要看技能、角色選擇、生成敵人或面板互動，從主選單進入 Play。
- 移動 Unity 資產時可在 **Project** 視窗拖曳。從檔案系統搬移時，要將同名 `.meta` 一起搬，場景和 Prefab 才能繼續找到資產。若程式用文字路徑載入資產，也要同步更新該路徑。
- 共用卡片在 `Assets/Data/RunUpgrades/Cards`，澪的卡片在 `Assets/Data/Characters/Mio/Upgrades`。新增卡片後從 `Assets/Resources/RunUpgradeDatabase.asset` 檢查它是否在 Cards 清單內。
- 本機舊場景和 Prefab 保存在專案根目錄的 `Backups/ProjectOrganization/Archive`。正式版本由 Git 記錄。Unity 的快取、個人設定、建置輸出和本機備份由 `.gitignore` 管理。
