using System;
using System.Collections.Generic;
using System.Text;
using UniRx;

namespace Proffeine.Status
{
    public partial class Status
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
        private ReactiveCollection<float> status;

        //event
        /// <summary>
        /// key, old, new
        /// </summary>
        public event Action<Key, float, float> OnStatChanged;

        //method
        public Status()
        {
            status = new();
            for (int i = 0; i < (int)Key.End; i++)
                status.Add(0);

            status
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
            return status[(int)key];
        }

        public IReadOnlyList<float> GetStatus()
        {
            return status;
        }

        /// <summary>
        /// Set stat
        /// </summary>
        /// <param name="key">target stat key</param>
        /// <param name="modifier">parameter: current value, return: target value</param>
        public void SetStat(Key key, Func<float, float> modifier)
        {
            status[(int)key] = modifier(status[(int)key]);
        }

        public Status Clone()
        {
            var status = new Status();
            status.ChangeFrom(this);
            return status;
        }

        public void ChangeFrom(Status target)
        {
            var status = target.GetStatus();
            for (int i = 0; i < status.Count; i++)
                SetStat((Key)i, x => status[i]);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var stat in status)
                sb.Append(stat.ToString()).Append('\n');
            return sb.ToString();
        }
    }
}