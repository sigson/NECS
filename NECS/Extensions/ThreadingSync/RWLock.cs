using System;
using System.Threading;
using KPreisser;
using NECS.Core.Logging;

namespace NECS.Extensions.ThreadingSync
{
    public class RWLock : IDisposable
    {
        public abstract class LockToken : IDisposable
        {
            //public SynchronizedList<string> exitLockLog = new SynchronizedList<string>();
            protected bool TokenMockLock = false;
            public abstract void ExitLock();
            public void Dispose() => ExitLock();
        }
        public class WriteLockToken : LockToken
        {
            private readonly IReaderWriterLockSlim lockobj;
            public WriteLockToken(IReaderWriterLockSlim @lock)
            {
                this.lockobj = @lock;
                if (this.lockobj.IsReadLockHeld)
                {
                    if (!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error("HALT! DEADLOCK ESCAPE! You tried to enter write lock while read lock is held!");
                    return;
                }
                if (!this.lockobj.IsWriteLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
                {
                    try
                    {
                        lockobj.EnterWriteLock();
                    }
                    catch (Exception e)
                    {
                        if (!Defines.IgnoreNonDangerousExceptions)
                            NLogger.Error(e);
                    }
                }
                else
                {
                    TokenMockLock = true;
                }
            }
            override public void ExitLock()
            {
                try
                {
                    if (this.lockobj.IsWriteLockHeld)
                    {
                        //exitLockLog.Add($"{new StackTrace()}");
                        lockobj.ExitWriteLock();
                    }
                    else if(TokenMockLock)
                    {
                        TokenMockLock = false;
                    }
                    else
                    {
                        if (!Defines.IgnoreNonDangerousExceptions)
                            NLogger.Error("You tried to exit read lock, but read lock for this thread already free");
                    }
                }
                catch (Exception e)
                {
                    if (!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error(e);
                }
            }
        }

        public class ReadLockToken : LockToken
        {
            private readonly IReaderWriterLockSlim lockobj;
            public ReadLockToken(IReaderWriterLockSlim @lock)
            {
                this.lockobj = @lock;
                if (this.lockobj.IsWriteLockHeld)
                {
                    if (!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error("HALT! DEADLOCK ESCAPE! You tried to enter read lock inner write locked thread!");
                    return;
                }
                if (!this.lockobj.IsReadLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
                {
                    try
                    {
                        lockobj.EnterReadLock();
                    }
                    catch (Exception e)
                    {
                        if (!Defines.IgnoreNonDangerousExceptions)
                            NLogger.Error(e);
                    }
                }
                else
                {
                    TokenMockLock = true;
                }
            }
            override public void ExitLock()
            {
                try
                {
                    if (this.lockobj.IsReadLockHeld)
                    {
                        //exitLockLog.Add($"{new StackTrace()}");
                        lockobj.ExitReadLock();
                    }
                    else if(TokenMockLock)
                    {
                        TokenMockLock = false;
                    }
                    else
                    {
                        if (!Defines.IgnoreNonDangerousExceptions)
                            NLogger.Error("You tried to exit read lock, but read lock for this thread already free");
                    }
                }
                catch (Exception e)
                {
                    if (!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error(e);
                }
            }
        }

        public readonly IReaderWriterLockSlim lockobj;

        public ReadLockToken ReadLock() => new ReadLockToken(lockobj);
        public WriteLockToken WriteLock() => new WriteLockToken(lockobj);

        public void ExecuteReadLocked(Action action)
        {
            using (this.ReadLock())
            {
                action();
            }
        }

        public void ExecuteWriteLocked(Action action)
        {
            using (this.WriteLock())
            {
                action();
            }
        }

        public RWLock()
        {
            if(Defines.OneThreadMode)
            {
                lockobj = new MockReaderWriterLockSlim();
            }
            else
            {
                if (Defines.ThreadsMode)
                {
                    lockobj = new LocalReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
                }
                else
                {
#if NET || UNITY || GODOT4
                    lockobj = new AsyncReaderWriterLockSlim();
#else
                    NLogger.Error("AsyncReaderWriterLockSlim not supported, enable Defines.ThreadsMode or Defines.OneThread");
                    #endif
                }
            }
        }

        public void Dispose() => lockobj.Dispose();
    }
}
