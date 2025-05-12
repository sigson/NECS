
public class MockReaderWriterLockSlim : IReaderWriterLockSlim
{
    public int WaitingReadCount => throw new NotImplementedException();

    public int RecursiveWriteCount => throw new NotImplementedException();

    public int RecursiveUpgradeCount => throw new NotImplementedException();

    public int RecursiveReadCount => throw new NotImplementedException();

    public LockRecursionPolicy RecursionPolicy => LockRecursionPolicy.SupportsRecursion;

    public bool IsWriteLockHeld => false;

    public bool IsUpgradeableReadLockHeld => throw new NotImplementedException();

    public bool IsReadLockHeld => false;

    public int CurrentReadCount => throw new NotImplementedException();

    public int WaitingUpgradeCount => throw new NotImplementedException();

    public int WaitingWriteCount => throw new NotImplementedException();

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void EnterReadLock()
    {
        
    }

    public void EnterUpgradeableReadLock()
    {
        throw new NotImplementedException();
    }

    public void EnterWriteLock()
    {
        
    }

    public void ExitReadLock()
    {
        
    }

    public void ExitUpgradeableReadLock()
    {
        throw new NotImplementedException();
    }

    public void ExitWriteLock()
    {
        
    }

    public bool TryEnterReadLock(TimeSpan timeout)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterReadLock(int millisecondsTimeout)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterUpgradeableReadLock(int millisecondsTimeout)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterWriteLock(int millisecondsTimeout)
    {
        throw new NotImplementedException();
    }

    public bool TryEnterWriteLock(TimeSpan timeout)
    {
        throw new NotImplementedException();
    }
}