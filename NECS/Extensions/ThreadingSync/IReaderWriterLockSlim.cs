namespace System.Threading
{
    /// <summary>
    /// Represents a lock that is used to manage access to a resource, allowing multiple
    /// threads for reading or exclusive access for writing.
    /// </summary>
    public interface IReaderWriterLockSlim : IDisposable
    {
        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in read mode.
        /// </summary>
        int WaitingReadCount { get; }

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in write mode,
        /// as an indication of recursion.
        /// </summary>
        int RecursiveWriteCount { get; }

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in upgradeable
        /// mode, as an indication of recursion.
        /// </summary>
        int RecursiveUpgradeCount { get; }

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in read mode,
        /// as an indication of recursion.
        /// </summary>
        int RecursiveReadCount { get; }

        /// <summary>
        /// Gets a value that indicates the recursion policy for the current lock.
        /// </summary>
        LockRecursionPolicy RecursionPolicy { get; }

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in
        /// write mode.
        /// </summary>
        bool IsWriteLockHeld { get; }

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in
        /// upgradeable mode.
        /// </summary>
        bool IsUpgradeableReadLockHeld { get; }

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in
        /// read mode.
        /// </summary>
        bool IsReadLockHeld { get; }

        /// <summary>
        /// Gets the total number of unique threads that have entered the lock in read mode.
        /// </summary>
        int CurrentReadCount { get; }

        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in upgradeable
        /// mode.
        /// </summary>
        int WaitingUpgradeCount { get; }

        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in write
        /// mode.
        /// </summary>
        int WaitingWriteCount { get; }

        /// <summary>
        /// Tries to enter the lock in read mode.
        /// </summary>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has attempted to acquire
        /// the read lock when it already holds the read or write lock, or the recursion number
        /// would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        void EnterReadLock();

        /// <summary>
        /// Tries to enter the lock in upgradeable mode.
        /// </summary>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter upgradeable mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        void EnterUpgradeableReadLock();

        /// <summary>
        /// Tries to enter the lock in write mode.
        /// </summary>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter write mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        void EnterWriteLock();

        /// <summary>
        /// Reduces the recursion count for read mode, and exits read mode if the resulting
        /// count is 0 (zero).
        /// </summary>
        /// <exception cref="System.Threading.SynchronizationLockException">
        /// The current thread has not entered the lock in read mode.
        /// </exception>
        void ExitReadLock();

        /// <summary>
        /// Reduces the recursion count for upgradeable mode, and exits upgradeable mode
        /// if the resulting count is 0 (zero).
        /// </summary>
        /// <exception cref="System.Threading.SynchronizationLockException">
        /// The current thread has not entered the lock in upgradeable mode.
        /// </exception>
        void ExitUpgradeableReadLock();

        /// <summary>
        /// Reduces the recursion count for write mode, and exits write mode if the resulting
        /// count is 0 (zero).
        /// </summary>
        /// <exception cref="System.Threading.SynchronizationLockException">
        /// The current thread has not entered the lock in write mode.
        /// </exception>
        void ExitWriteLock();

        /// <summary>
        /// Tries to enter the lock in read mode, with an optional time-out.
        /// </summary>
        /// <param name="timeout">The interval to wait, or -1 milliseconds to wait indefinitely.</param>
        /// <returns>true if the calling thread entered read mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or the recursion number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The timeout is negative but not -1 milliseconds, or greater than Int32.MaxValue milliseconds.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterReadLock(TimeSpan timeout);

        /// <summary>
        /// Tries to enter the lock in read mode, with an optional integer time-out.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>true if the calling thread entered read mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or the recursion number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The millisecondsTimeout is negative but not -1, which is the only negative value allowed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterReadLock(int millisecondsTimeout);

        /// <summary>
        /// Tries to enter the lock in upgradeable mode, with an optional integer time-out.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>true if the calling thread entered upgradeable mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter upgradeable mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The millisecondsTimeout is negative but not -1, which is the only negative value allowed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterUpgradeableReadLock(int millisecondsTimeout);

        /// <summary>
        /// Tries to enter the lock in upgradeable mode, with an optional time-out.
        /// </summary>
        /// <param name="timeout">The interval to wait, or -1 milliseconds to wait indefinitely.</param>
        /// <returns>true if the calling thread entered upgradeable mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter upgradeable mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The timeout is negative but not -1 milliseconds, or greater than Int32.MaxValue milliseconds.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterUpgradeableReadLock(TimeSpan timeout);

        /// <summary>
        /// Tries to enter the lock in write mode, with an optional integer time-out.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>true if the calling thread entered write mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter write mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The millisecondsTimeout is negative but not -1, which is the only negative value allowed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterWriteLock(int millisecondsTimeout);

        /// <summary>
        /// Tries to enter the lock in write mode, with an optional time-out.
        /// </summary>
        /// <param name="timeout">The interval to wait, or -1 milliseconds to wait indefinitely.</param>
        /// <returns>true if the calling thread entered write mode, otherwise, false.</returns>
        /// <exception cref="System.Threading.LockRecursionException">
        /// The recursion policy is NoRecursion and the current thread has already entered the
        /// lock, or attempting to enter write mode would create a deadlock, or the recursion
        /// number would exceed the counter capacity.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The timeout is negative but not -1 milliseconds, or greater than Int32.MaxValue milliseconds.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// The lock object has been disposed.
        /// </exception>
        bool TryEnterWriteLock(TimeSpan timeout);
    }
}