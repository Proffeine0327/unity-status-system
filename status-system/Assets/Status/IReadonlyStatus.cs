using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proffeine.Status
{
    public interface IReadonlyStatus
    {
        public float this[Status.Key key] { get; }
        public float GetStatus(Status.Key key);
        public IReadOnlyList<float> GetAllStatus();
    }
}