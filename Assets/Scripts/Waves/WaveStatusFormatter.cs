using System;

namespace AbyssBar.Waves
{
    public static class WaveStatusFormatter
    {
        public static string Details(WaveBattleProgress p)
        {
            if (p == null) return "準備出戰";
            switch (p.Phase)
            {
                case WaveBattlePhase.Intermission: return $"第 {p.CurrentWave} 波清場完成\n下一波：{Math.Ceiling(p.RestRemaining):0} 秒";
                case WaveBattlePhase.AwaitingVictory:
                case WaveBattlePhase.Finished: return $"{p.TotalWaves} 波全數清除";
                case WaveBattlePhase.Faulted: return "波次已停止：請查看 Console 設定錯誤";
                case WaveBattlePhase.Stopped: return "戰鬥結束";
                default: return $"剩餘：{p.Remaining}（場上 {p.Alive}／待入場 {p.Pending}）\n"
                    + (p.Phase == WaveBattlePhase.Spawning ? "敵人進場中" : "清除剩餘敵人，完成本波");
            }
        }
    }
}
