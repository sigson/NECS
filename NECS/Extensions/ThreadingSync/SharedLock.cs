using System;
using System.Threading;

namespace NECS.Extensions.ThreadingSync
{
    public class SharedLock
    {
        /// <summary>
        /// Токен блокировки, который освобождает Monitor при вызове Dispose.
        /// </summary>
        public class LockToken : IDisposable
        {
            private readonly object _lockObj;
            private bool _lockTaken;

            public LockToken(object lockObj)
            {
                _lockObj = lockObj;
                _lockTaken = false;

                try
                {
                    // Пытаемся захватить эксклюзивную блокировку
                    if (!Defines.OneThreadMode)
                        Monitor.Enter(_lockObj, ref _lockTaken);
                    else
                        _lockTaken = true;
                }
                catch (Exception e)
                {
                    // Здесь можно добавить ваше логирование NLogger.Error(e);
                    throw;
                }
            }

            public void ExitLock()
            {
                if (_lockTaken)
                {
                    try
                    {
                        if (!Defines.OneThreadMode)
                        {
                            Monitor.Exit(_lockObj);
                            _lockTaken = false;
                        }
                        else
                            _lockTaken = false;
                    }
                    catch (SynchronizationLockException e)
                    {
                        // Попытка освободить лок, которым поток не владеет
                        // NLogger.Error($"Error exiting lock: {e.Message}");
                        throw;
                    }
                    catch (Exception e)
                    {
                        // NLogger.Error(e);
                        throw;
                    }
                }
            }

            public void Dispose() => ExitLock();
        }

        // Объект, на котором происходит блокировка (SyncRoot)
        public readonly object LockObject;

        /// <summary>
        /// Конструктор.
        /// Если передан existingLockObject, использует его.
        /// Если null, создает новый object.
        /// </summary>
        public SharedLock(object existingLockObject = null)
        {
            LockObject = existingLockObject ?? new object();
        }

        /// <summary>
        /// Захватывает блокировку и возвращает Disposable токен.
        /// </summary>
        public LockToken Lock()
        {
            return new LockToken(LockObject);
        }

        /// <summary>
        /// Выполняет действие внутри блокировки (синтаксический сахар).
        /// </summary>
        public void ExecuteLocked(Action action)
        {
            using (Lock())
            {
                action();
            }
        }
    }
}