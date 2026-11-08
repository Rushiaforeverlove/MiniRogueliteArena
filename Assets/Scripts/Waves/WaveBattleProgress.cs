using System;
using System.Collections.Generic;

namespace AbyssBar.Waves
{
    public enum WaveBattlePhase { NotStarted, Spawning, Clearing, Intermission, AwaitingVictory, Finished, Stopped, Faulted }

    // Pure battle state. No scene lookup, Unity clock, spawning, UI or currency.
    public sealed class WaveBattleProgress
    {
        private readonly int[] quotas;
        private readonly float restDuration;
        private readonly HashSet<int> issued = new HashSet<int>();
        private readonly HashSet<int> alive = new HashSet<int>();
        public WaveBattlePhase Phase { get; private set; }
        public int CurrentWave { get; private set; }
        public int TotalWaves => quotas.Length;
        public int CompletedWaves { get; private set; }
        public int Spawned { get; private set; }
        public int Defeated { get; private set; }
        public int Alive => alive.Count;
        public int Target => CurrentWave > 0 ? quotas[CurrentWave - 1] : 0;
        public int Pending => Target - Spawned;
        public int Remaining => Target - Defeated;
        public float RestRemaining { get; private set; }
        public string FaultReason { get; private set; }
        public bool IsWaveActive => Phase == WaveBattlePhase.Spawning || Phase == WaveBattlePhase.Clearing;
        public WaveBattleProgress(int[] waveQuotas, float intermissionSeconds)
        {
            if (waveQuotas == null || waveQuotas.Length == 0) throw new ArgumentException("Empty wave plan.");
            if (intermissionSeconds < 0 || float.IsNaN(intermissionSeconds) || float.IsInfinity(intermissionSeconds)) throw new ArgumentException("Invalid rest duration.");
            quotas = (int[])waveQuotas.Clone();
            foreach (int quota in quotas) if (quota <= 0) throw new ArgumentException("Wave quotas must be positive.");
            restDuration = intermissionSeconds;
        }
        public bool Start()
        {
            if (Phase != WaveBattlePhase.NotStarted) return false;
            CurrentWave = 1; BeginCurrent(); return true;
        }
        private void BeginCurrent()
        {
            issued.Clear(); alive.Clear(); Spawned = Defeated = 0; RestRemaining = 0;
            Phase = WaveBattlePhase.Spawning;
        }
        public bool RegisterSpawn(int token, int wave)
        {
            if (wave != CurrentWave || Phase != WaveBattlePhase.Spawning || Pending <= 0 || !issued.Add(token)) return false;
            alive.Add(token); Spawned++;
            if (Pending == 0) Phase = WaveBattlePhase.Clearing;
            return true;
        }
        public bool RegisterDefeat(int token, int wave)
        {
            if (wave != CurrentWave || !IsWaveActive || !alive.Remove(token)) return false;
            Defeated++;
            if (Pending == 0 && Alive == 0)
            {
                CompletedWaves++;
                if (CompletedWaves == TotalWaves) Phase = WaveBattlePhase.AwaitingVictory;
                else { RestRemaining = restDuration; Phase = WaveBattlePhase.Intermission; }
            }
            return true;
        }
        public bool TickIntermission(float scaledDeltaTime)
        {
            if (Phase != WaveBattlePhase.Intermission || scaledDeltaTime < 0 || float.IsNaN(scaledDeltaTime) || float.IsInfinity(scaledDeltaTime)) return false;
            RestRemaining = Math.Max(0, RestRemaining - scaledDeltaTime);
            if (RestRemaining > 0) return false;
            CurrentWave++; BeginCurrent(); return true;
        }
        public bool ConfirmVictory()
        {
            if (Phase != WaveBattlePhase.AwaitingVictory) return false;
            Phase = WaveBattlePhase.Finished; return true;
        }
        public void Stop()
        { if (Phase != WaveBattlePhase.Finished && Phase != WaveBattlePhase.Faulted) Phase = WaveBattlePhase.Stopped; }
        public void Fault(string reason)
        { if (Phase == WaveBattlePhase.Finished || Phase == WaveBattlePhase.Stopped) return; FaultReason = reason; Phase = WaveBattlePhase.Faulted; }
    }
}
