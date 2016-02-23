using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Ship_Game
{
    public sealed class BatchRemovalCollection<T> : List<T>,IDisposable //where T : new()
    {
        //public List<T> pendingRemovals;
        public ConcurrentStack<T> pendingRemovals;
        public ReaderWriterLockSlim thisLock;

        //adding for thread safe Dispose because class uses unmanaged resources
        private bool disposed;

        public BatchRemovalCollection()
        {
            //this.pendingRemovals = new List<T>();
            this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(bool noQueueForRemoval)
        {
            //this.pendingRemovals = new List<T>();
            //this.pendingRemovals = new ConcurrentStack<T>();
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public BatchRemovalCollection(List<T> ListToCopy)
        {
            List<T> list = this as List<T>;
            list = ListToCopy.ToList<T>();
            this.AddRange(list);
            this.thisLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        }
        public void ApplyPendingRemovals()
        {
            T result;            
            while (!this.pendingRemovals.IsEmpty)
            {               
                this.pendingRemovals.TryPop(out result); //out T result);
                this.Remove(result);
            }
        }
        public void ApplyPendingRemovals(bool SaveForPooling)
        {
            if (SaveForPooling)
            {
                foreach(T item in this.pendingRemovals.ToArray())
                {
                    this.Remove(item);
                }
                return;
            }
            T result;
            List<T> removes = new List<T>();
            while (!this.pendingRemovals.IsEmpty)
            {
                
                this.pendingRemovals.TryPop(out result); //out T result);
                //removes.Add(result);
                this.Remove(result);
                
            }
            //this.thisLock.EnterWriteLock();
            //removes = (this as List<T>).Except(removes).ToList();

            //(this as List<T>).Clear();
            //(this as List<T>).AddRange(removes);
            //this.thisLock.ExitWriteLock();
        }
        public void QueuePendingRemoval(T item)
        {
            this.pendingRemovals.Push(item);
        }
        public void ClearPendingRemovals()
        {
            this.pendingRemovals.Clear();
        }
        new public void Add(T item)
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Add(item);
            thisLock.ExitWriteLock();
        }
        public List<T> Get()
        {
            thisLock.EnterReadLock();
            var list = this;
            thisLock.ExitReadLock();
            return this as List<T>;
        }
        new public Enumerator GetEnumerator()
        {
            thisLock.EnterReadLock();
            var result = (this as List<T>).GetEnumerator();
            thisLock.ExitReadLock();
            return result;
        }
        new public void Clear()
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAndRecycle()
        {
            thisLock.EnterWriteLock();
            List<T> test = (this as List<T>);
            this.pendingRemovals =  new ConcurrentStack<T>(test); 
            (this as List<T>).Clear();
            thisLock.ExitWriteLock();
        }
        public void ClearAll()
        {
            thisLock.EnterWriteLock();
            (this as List<T>).Clear();
            thisLock.ExitWriteLock();
            if(this.pendingRemovals !=null)
            {
                this.pendingRemovals.Clear();
            }
        }
        new public void Remove(T item)
        {
            
            thisLock.EnterWriteLock();
            (this as List<T>).Remove(item);
            thisLock.ExitWriteLock();

        }
        new public bool Contains(T item)
        {
            thisLock.EnterReadLock();
            var result = (this as List<T>).Contains(item);
            thisLock.ExitReadLock();
            return result;
        }
        public T RecycleObject()
        {            
            T test;
            
            if (this.pendingRemovals.TryPop(out test))
            {
                if (test is Empire.InfluenceNode)
                    (test as Empire.InfluenceNode).Wipe();
                
            }            
            return test;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BatchRemovalCollection() { Dispose(false); }

        protected void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.thisLock != null)
                        this.thisLock.Dispose();

                }
                this.thisLock = null;
                this.disposed = true;
            }
        }


    }
}