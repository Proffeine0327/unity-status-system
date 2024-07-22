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
                public ReactiveProperty<float> add = new();
                public ReactiveProperty<float> percent = new();

                public Info(float add, float percent)
                {
                    this.add.Value = add;
                    this.percent.Value = percent;
                }
            }

            private ReactiveDictionary<string, ReactiveDictionary<Key, Info>> casterInfo = new();
            private Status percentValues = new();
            private Status addValues = new();

            public event Action<Key> OnValueChange;

            public Modifier()
            {
                //caster add
                casterInfo
                    .ObserveAdd()
                    .Subscribe(kvp =>
                    {
                        //key add
                        kvp.Value
                            .ObserveAdd()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var percent = e.Value.percent;
                                var add = e.Value.add;

                                percent
                                    .Pairwise()
                                    .Subscribe(p =>
                                    {
                                        var oldValue = p.Previous;
                                        var newValue = p.Current;
                                        percentValues.SetStatus(key, x => x + (newValue - oldValue));
                                        // Debug.Log(percentValues.ToString());
                                    });
                                add
                                    .Pairwise()
                                    .Subscribe(p =>
                                    {
                                        var oldValue = p.Previous;
                                        var newValue = p.Current;
                                        addValues.SetStatus(key, x => x + (newValue - oldValue));
                                        // Debug.Log(addValues.ToString());
                                    });
                                percentValues.SetStatus(key, x => x + percent.Value);
                                addValues.SetStatus(key, x => x + add.Value);
                            });

                        //remove
                        kvp.Value
                            .ObserveRemove()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var percent = e.Value.percent.Value;
                                var add = e.Value.add.Value;

                                percentValues.SetStatus(key, x => x - percent);
                                addValues.SetStatus(key, x => x - add);

                                if (casterInfo[kvp.Key].Count == 0)
                                    casterInfo.Remove(kvp.Key);
                            });

                        //replace
                        kvp.Value
                            .ObserveReplace()
                            .Subscribe(e =>
                            {
                                var key = e.Key;
                                var oldPercent = e.OldValue.percent.Value;
                                var oldAdd = e.OldValue.add.Value;
                                var newPercent = e.NewValue.percent.Value;
                                var newAdd = e.NewValue.add.Value;

                                percentValues.SetStatus(key, x => x + (newPercent - oldPercent));
                                addValues.SetStatus(key, x => x + (newAdd - oldAdd));
                            });
                    });

                casterInfo
                    .ObserveRemove()
                    .Subscribe(kvp =>
                    {
                        foreach (var info in kvp.Value)
                        {
                            percentValues.SetStatus(info.Key, x => x - info.Value.percent.Value);
                            addValues.SetStatus(info.Key, x => x - info.Value.add.Value);
                        }
                    });

                percentValues.OnStatChanged += (key, _, _) => OnValueChange?.Invoke(key);
                addValues.OnStatChanged += (key, _, _) => OnValueChange?.Invoke(key);
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
                if (!casterInfo.ContainsKey(caster))
                {
                    if (add(0) == 0 && percent(0) == 0) return;
                    casterInfo.Add(caster, new());
                    casterInfo[caster].Add(key, new(add(0), percent(0)));
                    Debug.Log($"{add(0)} {percent(0)}");
                    return;
                }

                if (!casterInfo[caster].ContainsKey(key))
                {
                    if (add(0) == 0 && percent(0) == 0) return;
                    casterInfo[caster].Add(key, new(add(0), percent(0)));
                    Debug.Log($"{add(0)} {percent(0)}");
                    return;
                }

                var info = casterInfo[caster][key];
                if (add(info.add.Value) == 0 && percent(info.percent.Value) == 0)
                {
                    casterInfo[caster].Remove(key);
                    return;
                }

                info.add.Value = add(info.add.Value);
                info.percent.Value = percent(info.percent.Value);
                Debug.Log($"{info.add.Value} {info.percent.Value}");
            }

            /// <summary>
            /// Apply modified value to target status <br/>
            /// </summary>
            /// <param name="target">target apply status</param>
            /// <param name="base">base status</param>
            public void CalculateAll(Status target, Status @base)
            {
                target.ChangeFrom(@base);

                var percent = percentValues.GetAllStatus();
                var add = addValues.GetAllStatus();

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
                target.SetStatus(key, x => @base.GetStatus(key) * (percentValues.GetStatus(key) + 1));
                target.SetStatus(key, x => x + addValues.GetStatus(key));
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("percent\n");
                sb.Append(percentValues.ToString()).Append("\n");
                sb.Append("add\n");
                sb.Append(addValues.ToString());
                return sb.ToString();
            }
        }
    }
}