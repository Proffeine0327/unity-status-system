using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UniRx;
using UnityEngine;

namespace Proffeine.Status
{
    public partial class Status
    {
        public class Modifier
        {
            public class Info
            {
                private ReactiveProperty<Key> _key = new();
                private ReactiveProperty<float> _percent = new();
                private ReactiveProperty<float> _add = new();
                private Action<Key> _onAnyValueChanged;

                public ReactiveProperty<Key> key => _key;
                public ReactiveProperty<float> percent => _percent;
                public ReactiveProperty<float> add => _add;

                public Info(Key key, float percent, float add, Status percentValues, Status addValues, Action<Key> onAnyValueChanged)
                {
                    _key.Value = key;
                    _add.Value = add;
                    _percent.Value = percent;
                    _onAnyValueChanged = onAnyValueChanged;
                    
                    percentValues.SetStatus(key, x => x + percent);
                    addValues.SetStatus(key, x => x + add);
                    onAnyValueChanged?.Invoke(key);

                    _key
                        .Skip(0)
                        .Pairwise()
                        .Subscribe(pair =>
                        {
                            var oldKey = pair.Previous;
                            var newKey = pair.Current;

                            percentValues.SetStatus(oldKey, value => value - _percent.Value);
                            addValues.SetStatus(oldKey, value => value - _add.Value);
                            _onAnyValueChanged?.Invoke(oldKey);

                            percentValues.SetStatus(newKey, value => value + _percent.Value);
                            addValues.SetStatus(newKey, value => value + _add.Value);
                            _onAnyValueChanged?.Invoke(newKey);
                        });

                    _percent
                        .Skip(0)
                        .Pairwise()
                        .Subscribe(pair =>
                        {
                            percentValues.SetStatus(_key.Value, x => x + (pair.Current - pair.Previous));
                            _onAnyValueChanged?.Invoke(_key.Value);
                        });

                    _add
                        .Skip(0)
                        .Pairwise()
                        .Subscribe(pair =>
                        {
                            addValues.SetStatus(_key.Value, x => x + (pair.Current - pair.Previous));
                            _onAnyValueChanged?.Invoke(_key.Value);
                        });
                }
            }

            private ReactiveDictionary<string, Info> _modifiedInfos = new();
            private Status _percentValues = new();
            private Status _addValues = new();

            public event Action<Key> onValueChange;

            /// <summary>
            /// Add or change the modifier.
            /// </summary>
            public void Set
            (
                string caster,
                Key key,
                Func<float, float> percent,
                Func<float, float> add
            )
            {
                if (_modifiedInfos.ContainsKey(caster))
                {
                    _modifiedInfos[caster].key.Value = key;
                    _modifiedInfos[caster].percent.Value = percent(_modifiedInfos[caster].percent.Value);
                    _modifiedInfos[caster].add.Value = add(_modifiedInfos[caster].add.Value);
                }
                else
                {
                    _modifiedInfos.Add(caster, new Info(key, percent(0), add(0), _percentValues, _addValues, onValueChange));
                }
            }

            /// <summary>
            /// Remove caster
            /// </summary>
            public void Remove(string caster)
            {
                _modifiedInfos.Remove(caster);
            }

            /// <summary>
            /// Apply modified value to target status <br/>
            /// </summary>
            /// <param name="target">target apply status</param>
            /// <param name="base">base status</param>
            public void CalculateAll(Status target, IReadOnlyStatus @base)
            {
                target.ChangeFrom(@base);

                var percent = _percentValues.GetAllStatus();
                var add = _addValues.GetAllStatus();

                for (int i = 0; i < percent.Count; i++)
                    target.SetStatus((Key)i, x => x + x * percent[i]);
                for (int i = 0; i < add.Count; i++)
                    target.SetStatus((Key)i, x => x + add[i]);
            }

            /// <summary>
            /// Apply modified value to target status <br/>
            /// If you want to change calculation, modify this method
            /// </summary>
            /// <param name="key">target calculate key</param>
            /// <param name="target">target apply status</param>
            /// <param name="base">base status</param>
            public void Calculate(Key key, Status target, IReadOnlyStatus @base)
            {
                //base + base * percent + add
                target.SetStatus(key, x => @base.GetStatus(key) * (_percentValues.GetStatus(key) + 1));
                target.SetStatus(key, x => x + _addValues.GetStatus(key));
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("percent\n");
                sb.Append(_percentValues.ToString()).Append("\n");
                sb.Append("add\n");
                sb.Append(_addValues.ToString());
                return sb.ToString();
            }
        }
    }
}