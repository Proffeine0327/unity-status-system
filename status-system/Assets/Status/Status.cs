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

            End //'End' must be last.
        }

        //field
        private ReactiveCollection<float> _status;

        //event
        /// <summary>
        /// key, old, new
        /// </summary>
        public event Action<Key, float, float> onStatChanged;

        //method
        public Status()
        {
            _status = new();
            for (int i = 0; i < (int)Key.End; i++)
                _status.Add(0);

            _status
                .ObserveReplace()
                .Subscribe(e => onStatChanged?.Invoke((Key)e.Index, e.OldValue, e.NewValue));
        }

        public float this[Key key]
        {
            get => GetStatus(key);
            set => SetStatus(key, x => value);
        }

        public float GetStatus(Key key)
        {
            return _status[(int)key];
        }

        public IReadOnlyList<float> GetAllStatus()
        {
            return _status;
        }

        /// <summary>
        /// Set stat
        /// </summary>
        /// <param name="key">target stat key</param>
        /// <param name="modifier">parameter: current value, return: target value</param>
        public void SetStatus(Key key, Func<float, float> modifier)
        {
            _status[(int)key] = modifier(_status[(int)key]);
        }

        public Status Clone()
        {
            var status = new Status();
            status.ChangeFrom(this);
            return status;
        }

        public void ChangeFrom(Status target)
        {
            var status = target.GetAllStatus();
            for (int i = 0; i < status.Count; i++)
                SetStatus((Key)i, x => status[i]);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var stat in _status)
                sb.Append(stat.ToString()).Append('\n');
            return sb.ToString();
        }
    }
}