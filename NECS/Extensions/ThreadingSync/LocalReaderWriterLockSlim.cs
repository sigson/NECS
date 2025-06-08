using System.Threading;

public class LocalReaderWriterLockSlim : ReaderWriterLockSlim, IReaderWriterLockSlim
{
    public LocalReaderWriterLockSlim(LockRecursionPolicy recursionPolicy) : base(recursionPolicy)
    {
    }

    public LocalReaderWriterLockSlim() : base()
    {
    }
}