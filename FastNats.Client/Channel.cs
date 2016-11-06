using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastNats.Client
{
    public class Channel<T> : IChannel<T> where T : class
    {
        private BlockingCollection<T> _blockingQueue = new BlockingCollection<T>();
        private T _flag = default(T);

        public int Count
        {
            get
            {
                return _blockingQueue.Count;
            }
        }

        public void Add(T item)
        {
            _blockingQueue.Add(item);
        }

        public void Close()
        {
            _blockingQueue.Add(_flag);
        }

        public T Get(int timeout)
        {
            T item;

            _blockingQueue.TryTake(out item, timeout);

            if (item == _flag)
            {
                return default(T);
            }

            return item;
        }
    }
}
