﻿using NECS.Core.Logging;
using NECS.ECS.Types.AtomicType;
using NECS.GameEngineAPI;
using NECS.Harness.Model;
using NECS.Harness.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace NECS
{
    public static class MathEx
    {
        public static double CopySign(double x, double y)
        {
            // This method is required to work for all inputs,
            // including NaN, so we operate on the raw bits.

            long xbits = BitConverter.DoubleToInt64Bits(x);
            long ybits = BitConverter.DoubleToInt64Bits(y);

            // If the sign bits of x and y are not the same,
            // flip the sign bit of x and return the new value;
            // otherwise, just return x

            if ((xbits ^ ybits) < 0)
            {
                return BitConverter.Int64BitsToDouble(xbits ^ long.MinValue);
            }

            return x;
        }

        public static float Rad2Deg => 360f / ((float)Math.PI * 2);
        public static float RadToDeg(float rad)
        {
            return ((rad * MathEx.Rad2Deg) > 360 ? 360 - ((rad * MathEx.Rad2Deg) - 360) : 360 - (rad * MathEx.Rad2Deg)) - 180f;
        }

        public static Quaternion ToQuaternion(Vector3 v)
        {

            float cy = (float)Math.Cos(v.Z * 0.5);
            float sy = (float)Math.Sin(v.Z * 0.5);
            float cp = (float)Math.Cos(v.Y * 0.5);
            float sp = (float)Math.Sin(v.Y * 0.5);
            float cr = (float)Math.Cos(v.X * 0.5);
            float sr = (float)Math.Sin(v.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };

        }

        public static Vector3S ToEulerAngles(Quaternion q)
        {
            Vector3S angles = new Vector3S();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.x = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.y = (float)MathEx.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
    }

    public static class JsonUtil
    {
        public static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        public static T GetObjectByPath<T>(this JObject storage, string path, bool fixPath = true)
        {
            return storage.GetJTokenByPath(path, fixPath).ToObject<T>();
        }

        public static JToken GetJTokenByPath(this JObject storage, string path, bool fixPath = true)
        {
            if (fixPath)
                path = path.Replace(GlobalProgramState.instance.PathAltSeparator, GlobalProgramState.instance.PathSeparator);

            var pathSplit = path.Split(GlobalProgramState.instance.PathSeparator[0]);
            var nowStorage = storage[pathSplit[0]];
            for (int i = 1; i < pathSplit.Length; i++)
            {
                if (!Lambda.TryExecute(() => nowStorage = nowStorage[pathSplit[i]]))
                    if (!Lambda.TryExecute(() => nowStorage = nowStorage[int.Parse(pathSplit[i])]))
                        throw new Exception("Wrong JObject iterator");
            }
            return nowStorage;
        }
    }

    public class Crc32
    {
        private readonly uint[] _table;
        private const uint Poly = 0xedb88320;

        public uint ComputeChecksum(IEnumerable<byte> bytes)
        {
            var crc = 0xffffffff;
            foreach (var t in bytes)
            {
                var index = (byte)((crc & 0xff) ^ t);
                crc = (crc >> 8) ^ _table[index];
            }

            return ~crc;
        }

        public IEnumerable<byte> ComputeChecksumBytes(IEnumerable<byte> bytes)
        {
            return BitConverter.GetBytes(ComputeChecksum(bytes));
        }

        public Crc32()
        {
            _table = new uint[256];
            for (uint i = 0; i < _table.Length; ++i)
            {
                var temp = i;
                for (var j = 8; j > 0; --j)
                    if ((temp & 1) == 1)
                        temp = (temp >> 1) ^ Poly;
                    else
                        temp >>= 1;
                _table[i] = temp;
            }
        }
    }

    public static class ClassEx
    {
		public static float NextFloat(this Random random, float startFloat, float endFloat)
		{
			if (startFloat >= endFloat)
			{
				throw new ArgumentException("startFloat must be less than endFloat");
			}

			// Генерация случайного числа в диапазоне [0, 1)
			float randomValue = (float)random.NextDouble();

			// Масштабирование и смещение для получения числа в диапазоне [startFloat, endFloat)
			return startFloat + randomValue * (endFloat - startFloat);
		}
		
        public static string RandomString(this Random random, int countSymbols)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, countSymbols)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static float FastFloat(this string str)
        {
            return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static long GuidToLongR(this Guid guid)
        {
            return DateTime.UtcNow.Ticks + BitConverter.ToInt64(guid.ToByteArray(), 8);
        }
        public static long GuidToLong(this Guid guid)
        {
            return BitConverter.ToInt64(guid.ToByteArray(), 8);
        }

        //public static T[] Concat<T>(this T[] x, T[] y)
        //{
        //    if (x == null) throw new ArgumentNullException("x");
        //    if (y == null) throw new ArgumentNullException("y");
        //    int oldLen = x.Length;
        //    Array.Resize<T>(ref x, x.Length + y.Length);
        //    Array.Copy(y, 0, x, oldLen, y.Length);
        //    return x;
        //}
    }

    public class RWLock : IDisposable
    {
        public struct WriteLockToken : IDisposable
        {
            private readonly ReaderWriterLockSlim lockobj;
            public WriteLockToken(ReaderWriterLockSlim @lock)
            {
                this.lockobj = @lock;
                if(this.lockobj.IsReadLockHeld)
                {
                    if(!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error("HALT! DEADLOCK ESCAPE! You tried to enter write lock while read lock is held!");
                    return;
                }
                if(!this.lockobj.IsWriteLockHeld)
                    lockobj.EnterWriteLock();
            }
            public void Dispose() => lockobj.ExitWriteLock();
        }

        public struct ReadLockToken : IDisposable
        {
            private readonly ReaderWriterLockSlim lockobj;
            public ReadLockToken(ReaderWriterLockSlim @lock)
            {
                this.lockobj = @lock;
                if(this.lockobj.IsWriteLockHeld)
                {
                    if(!Defines.IgnoreNonDangerousExceptions)
                        NLogger.Error("HALT! DEADLOCK ESCAPE! You tried to enter read lock inner write locked thread!");
                    return;
                }
                if(!this.lockobj.IsReadLockHeld)
                    lockobj.EnterReadLock();
            }
            public void Dispose() => lockobj.ExitReadLock();
        }

        private readonly ReaderWriterLockSlim lockobj = new ReaderWriterLockSlim();

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

        public void Dispose() => lockobj.Dispose();
    }

#if UNITY_5_3_OR_NEWER
    public static class Lambda
    {
        public static UnityEngine.Events.UnityEvent<T> AddListener<T>(this UnityEngine.Events.UnityEvent<T> unityEvent, System.Action<T> action)
        {
            UnityEngine.Events.UnityAction<T> uaction = (T arg) => action(arg);
            unityEvent.AddListener(uaction);
            return unityEvent;
        }

        public static UnityEngine.Events.UnityEvent AddListener(this UnityEngine.Events.UnityEvent unityEvent, System.Action action)
        {
            UnityEngine.Events.UnityAction uaction = () => action();
            unityEvent.AddListener(uaction);
            return unityEvent;
        }

        public static bool TryExecute(Action act)
        {
            try
            {
                act();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static T LineFunction<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        public static T LineFunction<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch(Exception ex)
            {
                NLogger.LogError(ex);
            }
            return default(T);
        }
    }
#else
    public static class Lambda
    {
        public static Action<T> AddListener<T>(this Action<T> unityEvent, System.Action<T> action)
        {
            Action<T> uaction = (T arg) => action(arg);
            unityEvent.AddListener(uaction);
            return unityEvent;
        }

        public static Action AddListener(this Action unityEvent, System.Action action)
        {
            Action uaction = () => action();
            unityEvent.AddListener(uaction);
            return unityEvent;
        }

        public static bool TryExecute(Action act)
        {
            try
            {
                act();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static T LineAction<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }

        public static T LineFunction<T>(Func<T> action)
        {
            return action();
        }
    }

#endif

#if UNITY_5_3_OR_NEWER
    public static class TaskExts
    {
        public static Task LogExceptionIfFaulted(this Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted || t.Exception != null)
                {
                    NetworkingService.instance.ExecuteInstruction(() => UnityEngine.Debug.LogException(t.Exception.Flatten().InnerException));
                }
            });//, TaskScheduler.FromCurrentSynchronizationContext());
            return task;
        }
    }
#endif
    public class TaskEx : Task
    {
        public TaskEx(Action action) : base(action)
        {
        }

        public TaskEx(Action action, CancellationToken cancellationToken) : base(action, cancellationToken)
        {
        }

        public TaskEx(Action action, TaskCreationOptions creationOptions) : base(action, creationOptions)
        {
        }

        public TaskEx(Action<object> action, object state) : base(action, state)
        {
        }

        public TaskEx(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, cancellationToken, creationOptions)
        {
        }

        public TaskEx(Action<object> action, object state, CancellationToken cancellationToken) : base(action, state, cancellationToken)
        {
        }

        public TaskEx(Action<object> action, object state, TaskCreationOptions creationOptions) : base(action, state, creationOptions)
        {
        }

        public TaskEx(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, state, cancellationToken, creationOptions)
        {
        }

        public static void RunAsync(Action action)
        {
#if UNITY_5_3_OR_NEWER
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() => {
                    action();
                }).LogExceptionIfFaulted().ConfigureAwait(false);
            };
            asyncUpd();
#else
            Func<Task> asyncUpd = async () =>
            {
                await Task.Run(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        NLogger.LogError(ex);
                    }
                }).ConfigureAwait(false);
            };
            asyncUpd();
#endif
        }
    }

    public static class Reflection
    {

        public static bool TryEnterWriteLockAwaiter(this ReaderWriterLockSlim readerWriterLockSlim, int timeout)
        {
            bool executed = false;
            while (!executed)
            {
                try
                {
                    if (!readerWriterLockSlim.IsWriteLockHeld && readerWriterLockSlim.TryEnterWriteLock(timeout))
                    {
                        executed = true;
                        return true;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch { }
                
            }
            return false;
        }

        public static bool TryEnterReadLockAwaiter(this ReaderWriterLockSlim readerWriterLockSlim, int timeout)
        {
            bool executed = false;
            while (!executed)
            {
                if (readerWriterLockSlim.TryEnterReadLock(timeout))
                {
                    executed = true;
                    return true;
                }
            }
            return false;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Extension for 'Object' that copies the properties to a destination object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                if (!(targetProperty.CanRead && targetProperty.GetMethod.IsStatic) ||
                (targetProperty.CanWrite && targetProperty.SetMethod.IsStatic))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }
    }

    public static class FileEx
    {
        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
    }

#if UNITY_5_3_OR_NEWER

    public static class ManagerSpace
    {
        #region Instantiate
        public static UnityEngine.Object InstantiatedProcess(UnityEngine.Object instantiated, IEntityManager entityManagerOwner = null)
        {
            if (entityManagerOwner != null)
            {
                if (instantiated is UnityEngine.GameObject)
                {
                    (instantiated as UnityEngine.GameObject).GetComponentsInChildren<IManagable>().ForEach(x => (x as IManagable).ownerManagerSpace = entityManagerOwner);
                }
            }
            return instantiated;
        }

        public static T Instantiate<T>(T original, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, parent);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }
        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, position, rotation);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Transform parent, bool worldPositionStays, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, parent, worldPositionStays);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, position, rotation, parent);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Transform parent, bool instantiateInWorldSpace, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, parent, instantiateInWorldSpace);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, position, rotation);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static T Instantiate<T>(T original, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null) where T : UnityEngine.Object
        {
            var instantiated = UnityEngine.Object.Instantiate<T>(original, position, rotation, parent);
            return (T)InstantiatedProcess(instantiated, entityManagerOwner);
        }

        public static UnityEngine.Object Instantiate(UnityEngine.Object original, UnityEngine.Transform parent, IEntityManager entityManagerOwner = null)
        {
            var instantiated = UnityEngine.Object.Instantiate(original, parent);
            return InstantiatedProcess(instantiated, entityManagerOwner);
        }
        #endregion
        #region Component

        private static UnityEngine.Component ComponentProcess(UnityEngine.Component component, IEntityManager entityManagerOwner)
        {
            if (component is IManagable && entityManagerOwner != null)
                (component as IManagable).ownerManagerSpace = entityManagerOwner;
            return component;
        }

        public static UnityEngine.Component AddComponent<T>(this UnityEngine.GameObject gameObject, IEntityManager entityManagerOwner) where T : UnityEngine.Component
        {
            return (T)ComponentProcess(gameObject.AddComponent<T>(), entityManagerOwner);
        }
        public static UnityEngine.Component AddComponent(this UnityEngine.GameObject gameObject, Type typeComponent, IEntityManager entityManagerOwner)
        {
            return ComponentProcess(gameObject.AddComponent(typeComponent), entityManagerOwner);
        }
        #endregion
    }
    
#else
    public static class ManagerSpace
    {
        #region Instantiate
        public static EngineApiObjectBehaviour InstantiatedProcess(EngineApiObjectBehaviour instantiated, IEntityManager entityManagerOwner = null)
        {
            if (entityManagerOwner != null)
            {
                if (instantiated is EngineApiObjectBehaviour)
                {
                    (instantiated as EngineApiObjectBehaviour).GetComponentsInChildren<IManagable>().ForEach(x => (x as IManagable).ownerManagerSpace = entityManagerOwner);
                }
            }
            return instantiated;
        }
        #endregion
    }
#endif
    public static class HashExtension
    {
        // Some random MD5 stuff
        public static string MD5(string input)
        {
            if (input == null) input = string.Empty;
            using (MD5 hasher = System.Security.Cryptography.MD5.Create())
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte bit in hasher.ComputeHash(Encoding.UTF8.GetBytes(input)))
                    sb.Append(bit.ToString("x2"));
                return sb.ToString();
            }
        }
        // end some random MD5 stuff

        //static JSONNode HashOptions { get => Program.Config["HashUtilOptions"]; }
        //public static int iterations { get => HashOptions["Iterations"]; }
        //public static int saltSize { get => HashOptions["SaltSize"]; }
        //public static int keySize { get => HashOptions["KeySize"]; }
        //public static string Compute(string password)
        //{
        //    using (Rfc2898DeriveBytes algo = new Rfc2898DeriveBytes(
        //        password,
        //        saltSize,
        //        iterations))
        //    {
        //        string key = Convert.ToBase64String(algo.GetBytes(keySize));
        //        string salt = Convert.ToBase64String(algo.Salt);

        //        return $"{iterations}:{salt}:{key}";
        //    }
        //}

        //public static string Hash(string password)
        //{
        //    using (var algorithm = new Rfc2898DeriveBytes(
        //      password,
        //      saltSize,
        //      iterations))
        //    {
        //        var key = Convert.ToBase64String(algorithm.GetBytes(keySize));
        //        var salt = Convert.ToBase64String(algorithm.Salt);

        //        return $"{iterations}.{salt}.{key}";
        //    }
        //}

        //public static HashUtilCheckResult Check(string hash, string password)
        //{
        //    string[] hashPart = hash.Split(':');

        //    if (hashPart.Length != 3)
        //        throw new FormatException("Parameter 'hash' needs to be formatted as '{iterations}:{salt}:{hash}'");

        //    int iterations = int.Parse(hashPart[0]);
        //    byte[] salt = Convert.FromBase64String(hashPart[1]);
        //    byte[] key = Convert.FromBase64String(hashPart[2]);

        //    bool needsUpgrade = iterations != HashExtension.iterations;

        //    using (Rfc2898DeriveBytes algo = new Rfc2898DeriveBytes(
        //        password,
        //        salt,
        //        iterations))
        //    {
        //        byte[] keyToCheck = algo.GetBytes(keySize);
        //        bool verified = keyToCheck.SequenceEqual(key);
        //        return new HashUtilCheckResult
        //        {
        //            verified = verified,
        //            needsUpgrade = needsUpgrade
        //        };
        //    }
        //}
        //public struct HashUtilCheckResult
        //{
        //    public bool verified;
        //    public bool needsUpgrade;
        //}
    }

    public static partial class DateTimeExtensions
    {
        private static long ServerTime;
        private static long LocalTime;

        public static long TicksToMilliseconds(long ticks) => ticks / 10000;
        public static long MillisecondToTicks(long ms) => ms * 10000;
        public static float TicksToSeconds(long ticks) => (float)Math.Round(Math.Round((double)ticks / 10000) / 1000, 3);
        public static long NowServerTicks => DateTime.Now.Ticks + (ServerTime-LocalTime);

        public static void UpdateServerTime(long ServerTicks)
        {
            ServerTime = ServerTicks;
            LocalTime = DateTime.Now.Ticks;
        }
        private static int DateValue(this DateTime dt)
        {
            return dt.Year * 372 + (dt.Month - 1) * 31 + dt.Day - 1;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2)
        {
            return dt.MonthsBetween(dt2) / 12;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            return dt.MonthsBetween(dt2, includeLastDay) / 12;
        }

        public static int YearsBetween(this DateTime dt, DateTime dt2, bool includeLastDay, out int excessMonths)
        {
            int months = dt.MonthsBetween(dt2, includeLastDay);
            excessMonths = months % 12;
            return months / 12;
        }

        public static int MonthsBetween(this DateTime dt, DateTime dt2)
        {
            int months = (dt2.DateValue() - dt.DateValue()) / 31;
            return Math.Abs(months);
        }

        public static int MonthsBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            if (!includeLastDay) return dt.MonthsBetween(dt2);
            int days;
            if (dt2 >= dt)
                days = dt2.AddDays(1).DateValue() - dt.DateValue();
            else
                days = dt.AddDays(1).DateValue() - dt2.DateValue();
            return days / 31;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2)
        {
            return dt.DaysBetween(dt2) / 7;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            return dt.DaysBetween(dt2, includeLastDay) / 7;
        }

        public static int WeeksBetween(this DateTime dt, DateTime dt2, bool includeLastDay, out int excessDays)
        {
            int days = dt.DaysBetween(dt2, includeLastDay);
            excessDays = days % 7;
            return days / 7;
        }

        public static int DaysBetween(this DateTime dt, DateTime dt2)
        {
            return (dt2.Date - dt.Date).Duration().Days;
        }

        public static int DaysBetween(this DateTime dt, DateTime dt2, bool includeLastDay)
        {
            int days = dt.DaysBetween(dt2);
            if (!includeLastDay) return days;
            return days + 1;
        }
    }
    public class TimerEx : System.Timers.Timer
    {
        private long TimerStart = 0;
        private long TimerPaused = 0;
        private long TimerStopped = 0;
        private double baseInterval = 0f;
        private double interval = 0f;
		public bool inited = false;
        public bool Dead = false;
        public new double Interval
        {
            get
            {
                return interval;
            }
            set
            {
                if(value > 0)
                {
                    baseInterval = value;
                    interval = value;
                    base.Interval = value;
                }
            }
        }

        public TimerEx() : base()
        {
            this.Elapsed += (async (sender, e) => { 
                if(base.AutoReset)
                    this.Interval = this.baseInterval; 
                TimerStart = TimerDateTime.DateTimeNowTicks; TimerPaused = 0;});
            //base.AutoReset = true;
            base.Disposed += new EventHandler(this.OnDisposeTimer);
            this.Disposed += new EventHandler(this.OnDisposeTimer);
        }

        public TimerEx(TimerEx oldTimer) : base()
        {
            this.Elapsed += (async (sender, e) => {
                if (!base.AutoReset)
                    this.Interval = this.baseInterval; 
                TimerStart = TimerDateTime.DateTimeNowTicks; TimerPaused = 0; });
            Interval = oldTimer.Interval;
			inited = true;
            //base.AutoReset = true;
            base.Disposed += new EventHandler(this.OnDisposeTimer);
            this.Disposed += new EventHandler(this.OnDisposeTimer);
        }

        public void OnDisposeTimer(object sender, EventArgs args)
        {
            Dead = true;
        }

        public new void Start()
        {
            TimerStart = TimerDateTime.DateTimeNowTicks;
			inited = true;
            base.Start();
        }

        public new void Stop()
        {
            TimerStopped = TimerDateTime.DateTimeNowTicks;
            base.Stop();
        }

        public void Pause()
        {
            if(this.Enabled)
            {
                this.Stop();
                TimerPaused = TimerDateTime.DateTimeNowTicks;
            }
        }

        public void Resume()
        {
            if(TimerPaused != 0 && !this.Enabled)
            {
                this.interval = TimerPaused - TimerStart;
                base.Interval = TimerPaused - TimerStart;
                TimerPaused = 0;
                TimerStart = TimerDateTime.DateTimeNowTicks;
                this.Start();
            }
        }

        public void Reset()
        {
            this.Interval = baseInterval;
            this.Stop();
            TimerStart = 0;
            TimerPaused = 0;
            TimerStopped = 0;
            this.Start();
        }

        public double RemainingToElapsedTime()
        {
            return baseInterval - TimeSpan.FromTicks((TimerPaused == 0 ? TimerDateTime.DateTimeNowTicks - TimerStart : TimerPaused - TimerStart)).TotalMilliseconds;
        }
    }

    public class TimerDateTime
    {
        public static long DateTimeNowTicks
        {
            get
            {
                if(TickUpdate == null)
                {
                    TickUpdate = new System.Timers.Timer(1);
                    TickUpdate.Elapsed += UpdateTicks;
                    TickUpdate.AutoReset = true;
                    TickUpdate.Enabled = true;
                    Ticks = DateTime.Now.Ticks;
                }
                return Ticks;
            }
        }
        private static long Ticks;
        private static System.Timers.Timer TickUpdate = null;

        private static void UpdateTicks(Object source, ElapsedEventArgs e)
        {
            Ticks += DateTimeExtensions.MillisecondToTicks(1);
        }
    }
}
