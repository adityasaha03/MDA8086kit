using System;
using System.Collections.Generic;

namespace Mda8086Kit.Core.Abstractions
{
    public class PortBatch
    {
        private readonly List<PortChange> _changes = new List<PortChange>(32);

        public bool IsBulk { get; set; }
        public bool Reseeded { get; set; }
        public int Count => _changes.Count;

        public PortChange this[int index] => _changes[index];

        public void Add(PortChange change)
        {
            _changes.Add(change);
        }

        public void Clear()
        {
            _changes.Clear();
            IsBulk = false;
            Reseeded = false;
        }
    }
}
