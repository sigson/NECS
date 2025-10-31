using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
                    else if (TokenMockLock)
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
                    else if (TokenMockLock)
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
            if (Defines.OneThreadMode)
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


    public class RWLockLogging : IDisposable
    {
        // --- НОВОЕ ПОЛЕ ---
        /// <summary>
        /// Словарь для отслеживания активных блокировок.
        /// Ключ - экземпляр токена, Значение - стектрейс входа.
        /// </summary>
        private readonly ConcurrentDictionary<LockToken, string> _activeLocks;
        // --- КОНЕЦ НОВОГО ПОЛЯ ---

        public abstract class LockToken : IDisposable
        {
            // --- НОВОЕ ПОЛЕ ---
            /// <summary>
            /// Ссылка на родительский RWLock, содержащий словарь _activeLocks.
            /// </summary>
            protected readonly RWLockLogging _parent;
            // --- КОНЕЦ НОВОГО ПОЛЯ ---

            protected bool TokenMockLock = false;
            public abstract void ExitLock();
            public void Dispose() => ExitLock();

            // --- НОВЫЙ КОНСТРУКТОР ---
            /// <summary>
            /// Базовый конструктор для токена блокировки.
            /// </summary>
            /// <param name="parent">Экземпляр RWLock, создавший этот токен.</param>
            protected LockToken(RWLockLogging parent)
            {
                _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            }
            // --- КОНЕЦ НОВОГО КОНСТРУКТОРА ---
        }

        public class WriteLockToken : LockToken
        {
            private readonly IReaderWriterLockSlim lockobj;

            // --- ИЗМЕНЕННЫЙ КОНСТРУКТОР ---
            public WriteLockToken(IReaderWriterLockSlim @lock, RWLockLogging parent) : base(parent)
            {
                this.lockobj = @lock;
                if (this.lockobj.IsReadLockHeld)
                {
                    if (true || !Defines.IgnoreNonDangerousExceptions)
                        NLogger.LogErrorLocking("HALT! DEADLOCK ESCAPE! You tried to enter write lock while read lock is held!");
                    return;
                }
                if (!this.lockobj.IsWriteLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
                {
                    // --- НОВАЯ ЛОГИКА ---
                    string stackTrace = "Stack trace capture failed";
                    try
                    {
                        // 1. Захватываем StackTrace ПЕРЕД попыткой входа
                        stackTrace = new StackTrace(true).ToString();
                    }
                    catch (Exception stEx)
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking($"Failed to capture stack trace: {stEx.Message}");
                    }
                    // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

                    try
                    {
                        // 2. Входим в блокировку
                        lockobj.EnterWriteLock();

                        // --- НОВАЯ ЛОГИКА ---
                        // 3. Добавляем себя в словарь ПОСЛЕ успешного входа
                        _parent._activeLocks.TryAdd(this, stackTrace);
                        // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
                    }
                    catch (Exception e)
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking(e);
                    }
                }
                else
                {
                    TokenMockLock = true;
                }
            }
            // --- КОНЕЦ ИЗМЕНЕННОГО КОНСТРУКТОРА ---

            override public void ExitLock()
            {
                try
                {
                    if (this.lockobj.IsWriteLockHeld)
                    {
                        // 1. Выходим из блокировки
                        lockobj.ExitWriteLock();

                        // --- НОВАЯ ЛОГИКА ---
                        // 2. Удаляем себя из словаря ПОСЛЕ выхода
                        _parent._activeLocks.TryRemove(this, out _);
                        // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
                    }
                    else if (TokenMockLock)
                    {
                        TokenMockLock = false;
                    }
                    else
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking("You tried to exit write lock, but write lock for this thread already free"); // Исправлена опечатка
                    }
                }
                catch (Exception e)
                {
                    if (true || !Defines.IgnoreNonDangerousExceptions)
                        NLogger.LogErrorLocking(e);
                }
            }
        }

        public class ReadLockToken : LockToken
        {
            private readonly IReaderWriterLockSlim lockobj;

            // --- ИЗМЕНЕННЫЙ КОНСТРУКТОР ---
            public ReadLockToken(IReaderWriterLockSlim @lock, RWLockLogging parent) : base(parent)
            {
                this.lockobj = @lock;
                if (this.lockobj.IsWriteLockHeld)
                {
                    if (true || !Defines.IgnoreNonDangerousExceptions)
                        NLogger.LogErrorLocking("HALT! DEADLOCK ESCAPE! You tried to enter read lock inner write locked thread!");
                    return;
                }
                if (!this.lockobj.IsReadLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
                {
                    // --- НОВАЯ ЛОГИКА ---
                    string stackTrace = "Stack trace capture failed";
                    try
                    {
                        // 1. Захватываем StackTrace ПЕРЕД попыткой входа
                        stackTrace = new StackTrace(true).ToString();
                    }
                    catch (Exception stEx)
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking($"Failed to capture stack trace: {stEx.Message}");
                    }
                    // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

                    try
                    {
                        // 2. Входим в блокировку
                        lockobj.EnterReadLock();

                        // --- НОВАЯ ЛОГИКА ---
                        // 3. Добавляем себя в словарь ПОСЛЕ успешного входа
                        _parent._activeLocks.TryAdd(this, stackTrace);
                        // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
                    }
                    catch (Exception e)
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking(e);
                    }
                }
                else
                {
                    TokenMockLock = true;
                }
            }
            // --- КОНЕЦ ИЗМЕНЕННОГО КОНСТРУКТОРА ---

            override public void ExitLock()
            {
                try
                {
                    if (this.lockobj.IsReadLockHeld)
                    {
                        // 1. Выходим из блокировки
                        lockobj.ExitReadLock();

                        // --- НОВАЯ ЛОГИКА ---
                        // 2. Удаляем себя из словаря ПОСЛЕ выхода
                        _parent._activeLocks.TryRemove(this, out _);
                        // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
                    }
                    else if (TokenMockLock)
                    {
                        TokenMockLock = false;
                    }
                    else
                    {
                        if (true || !Defines.IgnoreNonDangerousExceptions)
                            NLogger.LogErrorLocking("You tried to exit read lock, but read lock for this thread already free");
                    }
                }
                catch (Exception e)
                {
                    if (true || !Defines.IgnoreNonDangerousExceptions)
                        NLogger.LogErrorLocking(e);
                }
            }
        }

        public readonly IReaderWriterLockSlim lockobj;

        // --- ИЗМЕНЕННЫЕ МЕТОДЫ-ФАБРИКИ ---
        public ReadLockToken ReadLock() => new ReadLockToken(lockobj, this);
        public WriteLockToken WriteLock() => new WriteLockToken(lockobj, this);
        // --- КОНЕЦ ИЗМЕНЕННЫХ МЕТОДОВ ---

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

        public RWLockLogging()
        {
            // --- НОВАЯ ЛОГИКА ---
            // Инициализируем словарь
            _activeLocks = new ConcurrentDictionary<LockToken, string>();
            // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

            if (Defines.OneThreadMode)
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

        // --- НОВЫЙ МЕТОД (Опционально) ---
        /// <summary>
        /// Возвращает потокобезопасную копию словаря активных блокировок для отладки.
        /// </summary>
        public IReadOnlyDictionary<LockToken, string> GetActiveLocks()
        {
            // Возвращаем копию, чтобы избежать проблем с перечислением
            // во время модификации коллекции в другом потоке.
            return new Dictionary<LockToken, string>(_activeLocks);
        }
        // --- КОНЕЦ НОВОГО МЕТОДА ---
    }
    
    // public class RWLock : IDisposable
    // {
    //     // --- НОВОЕ ПОЛЕ ---
    //     /// <summary>
    //     /// Словарь для отслеживания активных блокировок.
    //     /// Ключ - экземпляр токена, Значение - стектрейс входа.
    //     /// </summary>
    //     private readonly ConcurrentDictionary<LockToken, string> _activeLocks;
    //     // --- КОНЕЦ НОВОГО ПОЛЯ ---

    //     public abstract class LockToken : IDisposable
    //     {
    //         // --- НОВОЕ ПОЛЕ ---
    //         /// <summary>
    //         /// Ссылка на родительский RWLock, содержащий словарь _activeLocks.
    //         /// </summary>
    //         protected readonly RWLock _parent;
    //         // --- КОНЕЦ НОВОГО ПОЛЯ ---

    //         protected bool TokenMockLock = false;
    //         public abstract void ExitLock();
    //         public void Dispose() => ExitLock();

    //         // --- НОВЫЙ КОНСТРУКТОР ---
    //         /// <summary>
    //         /// Базовый конструктор для токена блокировки.
    //         /// </summary>
    //         /// <param name="parent">Экземпляр RWLock, создавший этот токен.</param>
    //         protected LockToken(RWLock parent)
    //         {
    //             _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    //         }
    //         // --- КОНЕЦ НОВОГО КОНСТРУКТОРА ---
    //     }

    //     public class WriteLockToken : LockToken
    //     {
    //         private readonly IReaderWriterLockSlim lockobj;

    //         // --- ИЗМЕНЕННЫЙ КОНСТРУКТОР ---
    //         public WriteLockToken(IReaderWriterLockSlim @lock, RWLock parent) : base(parent)
    //         {
    //             this.lockobj = @lock;
    //             if (this.lockobj.IsReadLockHeld)
    //             {
    //                 if (true || !Defines.IgnoreNonDangerousExceptions)
    //                     NLogger.LogErrorLocking("HALT! DEADLOCK ESCAPE! You tried to enter write lock while read lock is held!");
    //                 return;
    //             }
    //             if (!this.lockobj.IsWriteLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
    //             {
    //                 // --- НОВАЯ ЛОГИКА ---
    //                 string stackTrace = "Stack trace capture failed";
    //                 try
    //                 {
    //                     // 1. Захватываем StackTrace ПЕРЕД попыткой входа
    //                     stackTrace = new StackTrace(true).ToString();
    //                 }
    //                 catch (Exception stEx)
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking($"Failed to capture stack trace: {stEx.Message}");
    //                 }
    //                 // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

    //                 try
    //                 {
    //                     // 2. Входим в блокировку
    //                     lockobj.EnterWriteLock();

    //                     // --- НОВАЯ ЛОГИКА ---
    //                     // 3. Добавляем себя в словарь ПОСЛЕ успешного входа
    //                     _parent._activeLocks.TryAdd(this, stackTrace);
    //                     // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
    //                 }
    //                 catch (Exception e)
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking(e);
    //                 }
    //             }
    //             else
    //             {
    //                 TokenMockLock = true;
    //             }
    //         }
    //         // --- КОНЕЦ ИЗМЕНЕННОГО КОНСТРУКТОРА ---

    //         override public void ExitLock()
    //         {
    //             try
    //             {
    //                 if (this.lockobj.IsWriteLockHeld)
    //                 {
    //                     // 1. Выходим из блокировки
    //                     lockobj.ExitWriteLock();

    //                     // --- НОВАЯ ЛОГИКА ---
    //                     // 2. Удаляем себя из словаря ПОСЛЕ выхода
    //                     _parent._activeLocks.TryRemove(this, out _);
    //                     // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
    //                 }
    //                 else if (TokenMockLock)
    //                 {
    //                     TokenMockLock = false;
    //                 }
    //                 else
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking("You tried to exit write lock, but write lock for this thread already free"); // Исправлена опечатка
    //                 }
    //             }
    //             catch (Exception e)
    //             {
    //                 if (true || !Defines.IgnoreNonDangerousExceptions)
    //                     NLogger.LogErrorLocking(e);
    //             }
    //         }
    //     }

    //     public class ReadLockToken : LockToken
    //     {
    //         private readonly IReaderWriterLockSlim lockobj;

    //         // --- ИЗМЕНЕННЫЙ КОНСТРУКТОР ---
    //         public ReadLockToken(IReaderWriterLockSlim @lock, RWLock parent) : base(parent)
    //         {
    //             this.lockobj = @lock;
    //             if (this.lockobj.IsWriteLockHeld)
    //             {
    //                 if (true || !Defines.IgnoreNonDangerousExceptions)
    //                     NLogger.LogErrorLocking("HALT! DEADLOCK ESCAPE! You tried to enter read lock inner write locked thread!");
    //                 return;
    //             }
    //             if (!this.lockobj.IsReadLockHeld || this.lockobj.RecursionPolicy == LockRecursionPolicy.SupportsRecursion)
    //             {
    //                 // --- НОВАЯ ЛОГИКА ---
    //                 string stackTrace = "Stack trace capture failed";
    //                 try
    //                 {
    //                     // 1. Захватываем StackTrace ПЕРЕД попыткой входа
    //                     stackTrace = new StackTrace(true).ToString();
    //                 }
    //                 catch (Exception stEx)
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking($"Failed to capture stack trace: {stEx.Message}");
    //                 }
    //                 // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

    //                 try
    //                 {
    //                     // 2. Входим в блокировку
    //                     lockobj.EnterReadLock();

    //                     // --- НОВАЯ ЛОГИКА ---
    //                     // 3. Добавляем себя в словарь ПОСЛЕ успешного входа
    //                     _parent._activeLocks.TryAdd(this, stackTrace);
    //                     // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
    //                 }
    //                 catch (Exception e)
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking(e);
    //                 }
    //             }
    //             else
    //             {
    //                 TokenMockLock = true;
    //             }
    //         }
    //         // --- КОНЕЦ ИЗМЕНЕННОГО КОНСТРУКТОРА ---

    //         override public void ExitLock()
    //         {
    //             try
    //             {
    //                 if (this.lockobj.IsReadLockHeld)
    //                 {
    //                     // 1. Выходим из блокировки
    //                     lockobj.ExitReadLock();

    //                     // --- НОВАЯ ЛОГИКА ---
    //                     // 2. Удаляем себя из словаря ПОСЛЕ выхода
    //                     _parent._activeLocks.TryRemove(this, out _);
    //                     // --- КОНЕЦ НОВОЙ ЛОГИКИ ---
    //                 }
    //                 else if (TokenMockLock)
    //                 {
    //                     TokenMockLock = false;
    //                 }
    //                 else
    //                 {
    //                     if (true || !Defines.IgnoreNonDangerousExceptions)
    //                         NLogger.LogErrorLocking("You tried to exit read lock, but read lock for this thread already free");
    //                 }
    //             }
    //             catch (Exception e)
    //             {
    //                 if (true || !Defines.IgnoreNonDangerousExceptions)
    //                     NLogger.LogErrorLocking(e);
    //             }
    //         }
    //     }

    //     public readonly IReaderWriterLockSlim lockobj;

    //     // --- ИЗМЕНЕННЫЕ МЕТОДЫ-ФАБРИКИ ---
    //     public ReadLockToken ReadLock() => new ReadLockToken(lockobj, this);
    //     public WriteLockToken WriteLock() => new WriteLockToken(lockobj, this);
    //     // --- КОНЕЦ ИЗМЕНЕННЫХ МЕТОДОВ ---

    //     public void ExecuteReadLocked(Action action)
    //     {
    //         using (this.ReadLock())
    //         {
    //             action();
    //         }
    //     }

    //     public void ExecuteWriteLocked(Action action)
    //     {
    //         using (this.WriteLock())
    //         {
    //             action();
    //         }
    //     }

    //     public RWLock()
    //     {
    //         // --- НОВАЯ ЛОГИКА ---
    //         // Инициализируем словарь
    //         _activeLocks = new ConcurrentDictionary<LockToken, string>();
    //         // --- КОНЕЦ НОВОЙ ЛОГИКИ ---

    //         if (Defines.OneThreadMode)
    //         {
    //             lockobj = new MockReaderWriterLockSlim();
    //         }
    //         else
    //         {
    //             if (Defines.ThreadsMode)
    //             {
    //                 lockobj = new LocalReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    //             }
    //             else
    //             {
    // #if NET || UNITY || GODOT4
    //                 lockobj = new AsyncReaderWriterLockSlim();
    // #else
    //                 NLogger.Error("AsyncReaderWriterLockSlim not supported, enable Defines.ThreadsMode or Defines.OneThread");
    // #endif
    //             }
    //         }
    //     }

    //     public void Dispose() => lockobj.Dispose();

    //     // --- НОВЫЙ МЕТОД (Опционально) ---
    //     /// <summary>
    //     /// Возвращает потокобезопасную копию словаря активных блокировок для отладки.
    //     /// </summary>
    //     public IReadOnlyDictionary<LockToken, string> GetActiveLocks()
    //     {
    //         // Возвращаем копию, чтобы избежать проблем с перечислением
    //         // во время модификации коллекции в другом потоке.
    //         return new Dictionary<LockToken, string>(_activeLocks);
    //     }
    //     // --- КОНЕЦ НОВОГО МЕТОДА ---
    // }
}
