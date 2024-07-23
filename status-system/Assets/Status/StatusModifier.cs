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
                public ReactiveProperty<float> _add = new();
                public ReactiveProperty<float> _percent = new();

                public Info(float add, float percent)
                {
                    this._add.Value = add;
                    this._percent.Value = percent;
                }
            }

            private ReactiveDictionary<string, ReactiveDictionary<Key, Info>> _casterInfo = new();
            private Status _percentValues = new();
            private Status _addValues = new();

            public event Action<Key> onValueChange;

            public Modifier()
            {
                //caster add
                _casterInfo
                    .ObserveAdd()
                    .Subscribe(kvp =>
                    {
                        //key add
                        kvp.Value
                            .ObserveAdd()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var percent = e.Value._percent;
                                var add = e.Value._add;

                                percent
                                    .Pairwise()
                                    .Subscribe(p =>
                                    {
                                        var oldValue = p.Previous;
                                        var newValue = p.Current;
                                        _percentValues.SetStatus(key, x => x + (newValue - oldValue));
                                        // Debug.Log(percentValues.ToString());
                                    });
                                add
                                    .Pairwise()
                                    .Subscribe(p =>
                                    {
                                        var oldValue = p.Previous;
                                        var newValue = p.Current;
                                        _addValues.SetStatus(key, x => x + (newValue - oldValue));
                                        // Debug.Log(addValues.ToString());
                                    });
                                _percentValues.SetStatus(key, x => x + percent.Value);
                                _addValues.SetStatus(key, x => x + add.Value);
                            });

                        //remove
                        kvp.Value
                            .ObserveRemove()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var percent = e.Value._percent.Value;
                                var add = e.Value._add.Value;

                                _percentValues.SetStatus(key, x => x - percent);
                                _addValues.SetStatus(key, x => x - add);

                                if (_casterInfo[kvp.Key].Count == 0)
                                    _casterInfo.Remove(kvp.Key);
                            });

                        //replace
                        kvp.Value
                            .ObserveReplace()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var oldPercent = e.OldValue._percent.Value;
                                var oldAdd = e.OldValue._add.Value;
                                var newPercent = e.NewValue._percent.Value;
                                var newAdd = e.NewValue._add.Value;

                                _percentValues.SetStatus(key, x => x + (newPercent - oldPercent));
                                _addValues.SetStatus(key, x => x + (newAdd - oldAdd));
                            });
                    });

                _casterInfo
                    .ObserveRemove()
                    .Subscribe(kvp =>
                    {
                        foreach (var info in kvp.Value)
                        {
                            _percentValues.SetStatus(info.Key, x => x - info.Value._percent.Value);
                            _addValues.SetStatus(info.Key, x => x - info.Value._add.Value);
                        }
                    });

                _percentValues.onStatChanged += (key, _, _) => onValueChange?.Invoke(key);
                _addValues.onStatChanged += (key, _, _) => onValueChange?.Invoke(key);
            }

            /// <summary>
            /// Add or change the modifier.
            /// If the add and percent values are calculated as 0, the infomation is removed
            /// </summary>
            public void Set
            (
                string caster,
                Key key,
                Func<float, float> percent,
                Func<float, float> add
            )
            {
                if (!_casterInfo.ContainsKey(caster))
                {
                    if (add(0) == 0 && percent(0) == 0) return;
                    _casterInfo.Add(caster, new());
                    _casterInfo[caster].Add(key, new(add(0), percent(0)));
                    Debug.Log($"{add(0)} {percent(0)}");
                    return;
                }

                if (!_casterInfo[caster].ContainsKey(key))
                {
                    if (add(0) == 0 && percent(0) == 0) return;
                    _casterInfo[caster].Add(key, new(add(0), percent(0)));
                    Debug.Log($"{add(0)} {percent(0)}");
                    return;
                }

                var info = _casterInfo[caster][key];
                if (add(info._add.Value) == 0 && percent(info._percent.Value) == 0)
                {
                    _casterInfo[caster].Remove(key);
                    return;
                }

                info._add.Value = add(info._add.Value);
                info._percent.Value = percent(info._percent.Value);
                Debug.Log($"{info._add.Value} {info._percent.Value}");
            }

            /// <summary>
            /// Apply modified value to target status <br/>
            /// </summary>
            /// <param name="target">target apply status</param>
            /// <param name="base">base status</param>
            public void CalculateAll(Status target, Status @base)
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
            public void Calculate(Key key, Status target, Status @base)
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