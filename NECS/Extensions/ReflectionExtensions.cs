using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Linq;
using NECS.Extensions;
using System.Threading;
using System.Text;

namespace System
{
    public static class ECSAssemblyExtensions
    {
        public static IEnumerable<Type> GetAllSubclassOf(Type parent)
        {
            var allAssembly = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in allAssembly)
                foreach (var t in a.GetTypes())
                    //if (t.IsAssignableTo(parent)) yield return t;
                    if (t.IsSubclassOf(parent))
                    {
                        yield return t;
                        //foreach(var xt in GetAllSubclassOf(t))
                        //    yield return xt;
                    }
        }
    }

    public static class MethodInfoExtensionsEx
    {
        /// <summary>
        /// Return the method signature as a string.
        /// </summary>
        /// <param name="method">The Method</param>
        /// <param name="callable">Return as an callable string(public void a(string b) would return a(b))</param>
        /// <returns>Method signature</returns>
        public static string GetSignature(this MethodInfo method, bool callable = false)
        {
            var firstParam = true;
            var sigBuilder = new StringBuilder();
            if (callable == false)
            {
                if (method.IsPublic)
                    sigBuilder.Append("public ");
                else if (method.IsPrivate)
                    sigBuilder.Append("private ");
                else if (method.IsAssembly)
                    sigBuilder.Append("internal ");
                if (method.IsFamily)
                    sigBuilder.Append("protected ");
                if (method.IsStatic)
                    sigBuilder.Append("static ");
                sigBuilder.Append(TypeName(method.ReturnType));
                sigBuilder.Append(' ');
            }
            sigBuilder.Append(method.Name);

            // Add method generics
            if(method.IsGenericMethod)
            {
                sigBuilder.Append("<");
                foreach(var g in method.GetGenericArguments())
                {
                    if (firstParam)
                        firstParam = false;
                    else
                        sigBuilder.Append(", ");
                    sigBuilder.Append(TypeName(g));
                }
                sigBuilder.Append(">");
            }
            sigBuilder.Append("(");
            firstParam = true;
            var secondParam = false;
            foreach (var param in method.GetParameters())
            {
                if (firstParam)
                {
                    firstParam = false;
                    if (method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
                    {
                        if (callable)
                        {
                            secondParam = true;
                            continue;
                        }
                        sigBuilder.Append("this ");
                    }
                }
                else if (secondParam == true)
                    secondParam = false;
                else
                    sigBuilder.Append(", ");
                if (param.ParameterType.IsByRef)
                    sigBuilder.Append("ref ");
                else if (param.IsOut)
                    sigBuilder.Append("out ");
                if (!callable)
                {
                    sigBuilder.Append(TypeName(param.ParameterType));
                    sigBuilder.Append(' ');
                }
                sigBuilder.Append(param.Name);
            }
            sigBuilder.Append(")");
            return sigBuilder.ToString();
        }

        /// <summary>
        /// Get full type name with full namespace names
        /// </summary>
        /// <param name="type">Type. May be generic or nullable</param>
        /// <returns>Full type name, fully qualified namespaces</returns>
        public static string TypeName(Type type)
        {
            var nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null)
                return nullableType.Name + "?";

            if (!(type.IsGenericType && type.Name.Contains('`')))
                switch (type.Name)
                {
                    case "String": return "string";
                    case "Int32": return "int";
                    case "Decimal": return "decimal";
                    case "Object": return "object";
                    case "Void": return "void";
                    default:
                        {
                            return string.IsNullOrWhiteSpace(type.FullName) ? type.Name : type.FullName;
                        }
                }

            var sb = new StringBuilder(type.Name.Substring(0,
            type.Name.IndexOf('`'))
            );
            sb.Append('<');
            var first = true;
            foreach (var t in type.GetGenericArguments())
            {
                if (!first)
                    sb.Append(',');
                sb.Append(TypeName(t));
                first = false;
            }
            sb.Append('>');
            return sb.ToString();
        }

    }

    public static class DeepCopy
    {
        public static object CopyObject(object Object)
        {
            if(Defines.AOTMode)
            {
                //DeepCloneExtension.DeepCopyByExpressionTree
                return ObjectExtensions.Copy(Object);
            }
            else
            {
                return ObjectExtensions.Copy(Object);
            }
        }
    }

    public static class ReflectionCopy
    {
        public static object MakeReverseShallowCopy(object Object)
        {
            try
            {
                Type ObjectType = Object.GetType();
                PropertyInfo[] Properties = ObjectType.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                FieldInfo[] Fields = ObjectType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                object ClassInstance = Activator.CreateInstance(ObjectType);

                foreach (PropertyInfo Property in Properties)
                {
                    try
                    {
                        if(Property.GetCustomAttributes(false).OfType<NonSerializedAttribute>().Count() != 0)
                            SetProperty(Property, Object, ClassInstance);
                    }
                    catch { }
                }

                foreach (FieldInfo Field in Fields)
                {
                    try
                    {
                        if (Field.GetCustomAttributes(false).OfType<NonSerializedAttribute>().Count() != 0)
                            SetField(Field, Object, ClassInstance);
                    }
                    catch { }
                }

                return Object;
            }
            catch { throw; }
        }
        private static void SetField(FieldInfo Field, object ClassInstance, object Object)
        {
            try
            {
                SetField(Field, Field, ClassInstance, Object);
            }
            catch { }
        }

        private static void SetField(FieldInfo ChildField, FieldInfo Field, object ClassInstance, object Object)
        {
            try
            {
                if (Field.IsPublic && ChildField.IsPublic)
                {
                    ChildField.SetValue(ClassInstance, Field.GetValue(Object));
                }
            }
            catch { }
        }

        private static void SetProperty(PropertyInfo Property, object ClassInstance, object Object)
        {
            try
            {
                SetProperty(Property, Property, ClassInstance, Object);
            }
            catch { }
        }

        private static void SetProperty(PropertyInfo ChildProperty, PropertyInfo Property, object ClassInstance, object Object)
        {
            try
            {
                if (ChildProperty.GetSetMethod() != null && Property.GetGetMethod() != null)
                {
                    ChildProperty.SetValue(ClassInstance, Property.GetValue(Object, null), null);
                }
            }
            catch { }
        }

    }

    public static class ObjectExtensions
    {
        private static readonly MethodInfo CloneMethod = typeof(Object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(String)) return true;
            return (type.IsValueType & type.IsPrimitive);
        }

        public static Object Copy(this Object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<Object, Object>(new ReferenceEqualityComparer()));
        }
        private static Object InternalCopy(Object originalObject, IDictionary<Object, Object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    Array clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) => array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }

            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType, BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }
        public static T Copy<T>(this T original)
        {
            return (T)Copy((Object)original);
        }

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
            System.Reflection.PropertyInfo[] srcProps = typeSrc.GetProperties();
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

    public class ReferenceEqualityComparer : EqualityComparer<Object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }
        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }
}