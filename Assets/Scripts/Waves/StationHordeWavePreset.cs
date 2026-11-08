namespace AbyssBar.Waves
{
    /// <summary>
    /// Initial values for a new station horde plan. Runtime uses the editable
    /// BattleWavePlan asset; this factory never resets an existing asset.
    /// </summary>
    public static class StationHordeWavePreset
    {
        public const string PlanId = "station_horde_ten_v1";
        public const float IntermissionSeconds = 5f;

        public static BattleWaveDefinition[] CreateWaves()
        {
            return new[]
            {
                Wave(20,  .70f, 20),
                Wave(30,  .65f, 24),
                Wave(45,  .60f, 30),
                Wave(65,  .55f, 36),
                Wave(90,  .50f, 42),
                Wave(120, .45f, 50),
                Wave(155, .40f, 60),
                Wave(195, .35f, 70),
                Wave(240, .30f, 80),
                Wave(290, .25f, 90)
            };
        }

        private static BattleWaveDefinition Wave(int count, float interval, int cap)
            => new BattleWaveDefinition {
                enemyCount = count, spawnInterval = interval, maximumAlive = cap
            };
    }
}
