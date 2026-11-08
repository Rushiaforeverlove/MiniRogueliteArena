using System;
using System.Collections.Generic;

namespace AbyssBar.Enemies
{
    // No Unity/physics dependencies: caps and guaranteed introductions can be
    // tested separately from spawning. Only successful spawns update counts.
    public sealed class EnemyPoolSelection
    {
        public sealed class Rule
        {
            public string id;
            public double weight;
            public int cap, minimum;
        }
        private readonly Rule[] rules;
        private readonly Dictionary<string, int> alive = new Dictionary<string, int>();
        private readonly Dictionary<string, int> spawned = new Dictionary<string, int>();
        public EnemyPoolSelection(Rule[] source)
        {
            if (source == null || source.Length == 0) throw new ArgumentException("Empty pool.");
            rules = new Rule[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var r = source[i];
                if (r == null || string.IsNullOrWhiteSpace(r.id) || alive.ContainsKey(r.id)
                    || r.weight <= 0 || double.IsNaN(r.weight) || double.IsInfinity(r.weight)
                    || r.cap < 1 || r.minimum < 0) throw new ArgumentException("Invalid pool rule.");
                rules[i] = new Rule { id = r.id, weight = r.weight, cap = r.cap, minimum = r.minimum };
                alive.Add(r.id, 0); spawned.Add(r.id, 0);
            }
        }
        public int Alive(string id) => alive.TryGetValue(id, out int n) ? n : 0;
        public int Spawned(string id) => spawned.TryGetValue(id, out int n) ? n : 0;
        public int Select(double unit)
        {
            if (double.IsNaN(unit) || double.IsInfinity(unit)) return -1;
            for (int i = 0; i < rules.Length; i++)
                if (Alive(rules[i].id) < rules[i].cap && Spawned(rules[i].id) < rules[i].minimum) return i;
            double total = 0;
            foreach (var r in rules) if (Alive(r.id) < r.cap) total += r.weight;
            if (total <= 0) return -1;
            double pick = Math.Max(0, Math.Min(.999999999, unit)) * total;
            int last = -1;
            for (int i = 0; i < rules.Length; i++)
            {
                var r = rules[i]; if (Alive(r.id) >= r.cap) continue;
                last = i; pick -= r.weight; if (pick < 0) return i;
            }
            return last;
        }
        public bool RegisterSpawn(int index)
        {
            if (index < 0 || index >= rules.Length) return false;
            var r = rules[index]; if (Alive(r.id) >= r.cap) return false;
            alive[r.id]++; spawned[r.id]++; return true;
        }
        public bool RegisterDefeat(string id)
        {
            if (Alive(id) <= 0) return false;
            alive[id]--; return true;
        }
    }
}
