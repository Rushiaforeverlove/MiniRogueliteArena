# 局內升級：擴充與維護

## 分工

| 模組 | 負責 | 新增內容時通常改哪裡 |
|---|---|---|
| UpgradeCardData | 一張卡的設計資料 | 名稱、圖、參數、品質、門檻 |
| UpgradeDatabase | 卡庫、已解鎖角色查詢、權重 | 新卡加入 Cards，新角色加入 Roster |
| UpgradeDraft | 品質→分類→來源角色→卡片 | 通常不用修改 |
| UpgradeManager／UpgradeChoiceUI | 多級排隊、暫停、顯示四張與選擇 | 通常不用修改 |
| RunUpgradeRuntime | 本局等級、事件、傷害歸屬、排程、共用造物入口 | 新的共用能力或事件才擴充 |
| RunEffect／UpgradeEffectRegistry | 效果介面、模板工廠、必要參數表 | 全新機制加新類別、註冊 Key |
| General／Item／Scout／Guard／Support 模組 | 目前 77 個內建行為模板 | 既有模板的行為修正 |
| SequentialCharges | 依序回充 | 通常不用修改 |
| CombatBalance | 原資產數值的單次換算 | 統一倍率或未來資料遷移 |
| CharacterProgressionSave | 永久節點進度與滿培發獎 | 局外新功能 |

資料資產使用 ScriptableObject，本局狀態則在玩家元件與效果實例中。JSON 種子只在 Editor 建立卡片資產；Windows 執行時直接讀卡庫資產，不需要網路或 JSON 解析。

Unity 官方參考：[ScriptableObject](https://docs.unity.cn/6000.6/Documentation/Manual/class-ScriptableObject.html)、[JsonUtility.FromJson](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityengine/jsonutility/fromjson)。JSON 匯入目標是可序列化的普通 Seed 類別，再明確建立 ScriptableObject 資產。

## ID 與效果模板

- `id` 是卡片身分；不要使用顯示名稱作為存檔或比較依據。
- `ownerRoleId` 是角色身分，改成人名時保留原本 ID。
- `effectKey` 選擇行為模板，例如 `item.orbiting_blades`。
- 內建模組中的 `Code` 是註冊工廠指定的行為代號，不是從新卡的 ID 推測行為。新卡可使用新的 ID、沿用舊 Effect Key 與不同參數。
- 全新行為可以直接繼承 RunEffect，無需持續擴大既有角色模組的分支。

## 新增效果範例

以下為完整的額外模組範例，尚未加入卡庫：消耗 E 充能後回復生命。建立獨立 `.cs`：

```csharp
using AbyssBar.Upgrades;

public sealed class HealAfterEEffect : RunEffect
{
    public override void AfterAction(RunAction action)
    {
        if (action.kind != RunActionKind.E || action.free) return;
        C.Health.Heal(Data.Number("heal", Rank));
    }
}
```

在 UpgradeEffectRegistry 的 factories 加入：

```csharp
{ "heal.after_e", () => new HealAfterEEffect() },
```

在 required 參數表加入：

```csharp
{ "heal.after_e", new string[] { "heal" } },
```

然後建立卡資產，Effect Key 填 `heal.after_e`，Numbers 新增 `heal`、Values 填 `100, 150, 200`；Max Rank = 3。UI、抽選、角色存檔服務均不需要新增對應分支。

Rank 是目前卡片等級。參數表表示目前效果或每次購買增量，須依參數語意與 Description 一致處理。

## 事件與傷害規約

- 普攻一次射擊是一個 RunAction。多顆子彈共用動作，避免「每三次普攻」被三顆彈直接觸發。
- E／Q 只有成功施放才建立動作；NoTarget、Paused、SequenceActive 等失敗不消耗免費機會或計入施放次數。
- `free` 區分免費施放與實際扣充能；E 接 Q、Q 接 E 等回充卡只看實際消耗。
- `generated = false` 表示玩家直接普攻／E／Q；道具、反擊、側射、回聲、衛星等追加傷害是 generated。
- generated 傷害可擊殺並計入擊殺卡，但不再觸發命中追加攻擊、吸血與直接命中事件。
- 用 `RunAction.Once` 做每動作一次，`TargetOnce` 做每敵人每動作一次。普通穿透的 targetOrdinal 代表該發子彈第幾名不同敵人。
- `C.Hit` 回傳實際扣血量；顯示傷害與實際扣血量分開，避免溢傷灌入吸血。
- 死亡歸屬只領取一次；同幀多彈、命中追加傷害與擊殺連動不會重複掉經驗或計擊殺。
- 屏障吸收、屏障被打空、實際生命受傷是不同事件；自然到期不算被打空。
- 共用減傷上限 60%，屏障上限最大生命 50%；同源減速不累加，多來源取當時最強值，暈眩有連續 1.5 秒上限與 1 秒抗性。
- 未來敵人若要推玩家，呼叫 PlayerController.TryApplyKnockback；它會遵守閃避、穩定狀態及牆壁碰撞。

## 排程與造物

用 C.Later 安排延遲，用 C.Zone 建立可替換的限時區域，用 C.Shot 發射追加子彈。所有者結束戰鬥時會取消排程、區域和所屬造物。

Zone 的 key 表示同一來源的造物組，重建同 key 會替換舊組；不會觸發舊組的自然期滿爆炸。定時區域在建立時觸發第一個 tick，期滿時不多補最後一個 tick；卡頓時補算應有且尚未期滿的 tick。

環繞造物使用數量、半徑、週期、命中半徑、係數、每敵人冷卻，檢查沿途軌跡而不是整圈無差別扣血。穿牆傷害在接觸檢查入口阻擋。

目前子彈採用掃過的線段檢查牆和敵人，不會因一幀移動超過薄牆就略過牆。實際碰撞範圍、複合碰撞器與大型敵人仍應在對應場景做回歸測試。

## 資料與存檔相容

局內卡片目前不跨局存檔。永久節點沿用 v2 TrainingSteps，從最多 6 次擴充至 7 次，不重編已有數值與技能列舉。

節點五滿培獎勵使用獨立的 v3 領取標記；欄位未指定時保留待發獎資格。原造型與貨幣 key 保留，與節點進度一起在同一次 PlayerPrefs.Save 請求寫入。

目前原資產保持舊單位，由 CombatBalance 換算；卡片 Numbers 已是戰鬥單位。日後要全面改成資產直接填戰鬥單位，應做一次可回復的資料遷移，再將倍率設為 1，避免同時混用兩種數值輸入方式。

## 後續版本更新

新增同機制卡片或新角色時，通常只新增資料與圖片；新機制追加模組與測試。Prepare Data Assets 不覆寫既有卡片，避免更新時把手調平衡清掉。

正式發佈更新應將最終卡片資產隨 Unity 建置交付。這版是隨遊戲更新的資料庫，沒有將任意外部腳本或網路內容載入玩家遊戲的機制。

大量敵人與造物的效能仍需量測；目前敵人有活躍名單、普通索敵為單次掃描、經驗寶石快取玩家參考、跳字有上限 96 個的本局物件池。其餘造物的產生入口集中，可在後續替換成物件池或空間查詢，不必改寫每張卡。
