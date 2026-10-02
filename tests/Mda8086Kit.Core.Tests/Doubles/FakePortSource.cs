using System.Collections.Generic;
using Mda8086Kit.Core;

namespace Mda8086Kit.Core.Tests.Doubles
{
    public class FakePortSource : IPortSource
    {
        private readonly Queue<List<PortData>> _queue = new Queue<List<PortData>>();

        public void EnqueuePollResult(List<PortData> result)
        {
            _queue.Enqueue(result);
        }

        public IEnumerable<PortData> Poll(long currentUs)
        {
            if (_queue.Count > 0)
            {
                return _queue.Dequeue();
            }
            return new List<PortData>();
        }

        public void Reset()
        {
            _queue.Clear();
        }
    }
}
