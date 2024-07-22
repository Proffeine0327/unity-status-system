using System;
using System.Collections.Generic;
using System.Text;
using UniRx;

namespace Proffeine.Stats
{
    public partial class Stats
    {
        //define
        public enum Key
        {
            // Write stat key here
            // e.g.
            // Hp, Mp, Attack_Damage, etc

            //'End' must be last.
            End
        }

        //field
        private ReactiveCollection<float> stats;

        //event
        /// <summary>
        /// key, old, new
        /// </summary>
        public event Action<Key, float, float> OnStatChanged;

        //method
        public Stats()
        {
            stats = new();
            for (int i = 0; i < (int)Key.End; i++)
                stats.Add(0);

            stats
                .ObserveReplace()
                .Subscribe(e => OnStatChanged?.Invoke((Key)e.Index, e.OldValue, e.NewValue));
        }

        public float this[Key key]
        {
            get => GetStat(key);
            set => SetStat(key, x => value);
        }

        public float GetStat(Key key)
        {
            return stats[(int)key];
        }

        public IReadOnlyList<float> GetStats()
        {
            return stats;
        }

        /// <summary>
        /// Set stat
        /// </summary>
        /// <param name="key">target stat key</param>
        /// <param name="modifier">parameter: current value, return: target value</param>
        public void SetStat(Key key, Func<float, float> modifier)
        {
            stats[(int)key] = modifier(stats[(int)key]);
        }

        public Stats Clone()
        {
            var stats = new Stats();
            stats.ChangeFrom(this);
            return stats;
        }

        public void ChangeFrom(Stats target)
        {
            var stats = target.GetStats();
            for (int i = 0; i < stats.Count; i++)
                SetStat((Key)i, x => stats[i]);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var stat in stats)
                sb.Append(stat.ToString()).Append('\n');
            return sb.ToString();
        }
    }
}