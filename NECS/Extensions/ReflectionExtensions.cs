﻿using System.Collections.Generic;
using System.Reflection;
using System.ArrayExtensions;

namespace System
{
    public static class ReflectionCopy
    {

        /// <summary>
        /// Makes a shallow copy of the object
        /// </summary>
        /// <param name="Object">Object to copy</param>
        /// <param name="SimpleTypesOnly">If true, it only copies simple types (no classes, only items like int, string, etc.), false copies everything.</param>
        /// <returns>A copy of the object</returns>
        public static object MakeShallowCopy(object Object, bool SimpleTypesOnly)
        {
            try
            {
                Type ObjectType = Object.GetType();
                PropertyInfo[] Properties = ObjectType.GetProperties();
                FieldInfo[] Fields = ObjectType.GetFields();
                object ClassInstance = Activator.CreateInstance(ObjectType);

                foreach (PropertyInfo Property in Properties)
                {
                    try
                    {
                        if (SimpleTypesOnly)
                        {
                            SetPropertyifSimpleType(Property, ClassInstance, Object);
                        }
                        else
                        {
                            SetProperty(Property, ClassInstance, Object);
                        }
                    }
                    catch { }
                }

                foreach (FieldInfo Field in Fields)
                {
                    try
                    {
                        if (SimpleTypesOnly)
                        {
                            SetFieldifSimpleType(Field, ClassInstance, Object);
                        }
                        else
                        {
                            SetField(Field, ClassInstance, Object);
                        }
                    }
                    catch { }
                }

                return ClassInstance;
            }
            catch { throw; }
        }

        /// <summary>
        /// Copies a field value
        /// </summary>
        /// <param name="Field">Field object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetField(FieldInfo Field, object ClassInstance, object Object)
        {
            try
            {
                SetField(Field, Field, ClassInstance, Object);
            }
            catch { }
        }

        /// <summary>
        /// Copies a field value
        /// </summary>
        /// <param name="Field">Field object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetFieldifSimpleType(FieldInfo Field, object ClassInstance, object Object)
        {
            try
            {
                SetFieldifSimpleType(Field, Field, ClassInstance, Object);
            }
            catch { }
        }

        /// <summary>
        /// Copies a field value
        /// </summary>
        /// <param name="ChildField">Child field object</param>
        /// <param name="Field">Field object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
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

        /// <summary>
        /// Copies a field value
        /// </summary>
        /// <param name="ChildField">Child field object</param>
        /// <param name="Field">Field object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetFieldifSimpleType(FieldInfo ChildField, FieldInfo Field, object ClassInstance, object Object)
        {
            try
            {
                Type FieldType = Field.FieldType;
                if (Field.FieldType.FullName.StartsWith("System.Collections.Generic.List", StringComparison.CurrentCultureIgnoreCase))
                {
                    FieldType = Field.FieldType.GetGenericArguments()[0];
                }

                if (FieldType.FullName.StartsWith("System"))
                {
                    SetField(ChildField, Field, ClassInstance, Object);
                }
            }
            catch { throw; }
        }

        /// <summary>
        /// Copies a property value
        /// </summary>
        /// <param name="Property">Property object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetPropertyifSimpleType(PropertyInfo Property, object ClassInstance, object Object)
        {
            try
            {
                SetPropertyifSimpleType(Property, Property, ClassInstance, Object);
            }
            catch { }
        }

        /// <summary>
        /// Copies a property value
        /// </summary>
        /// <param name="Property">Property object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetProperty(PropertyInfo Property, object ClassInstance, object Object)
        {
            try
            {
                SetProperty(Property, Property, ClassInstance, Object);
            }
            catch { }
        }

        /// <summary>
        /// Copies a property value
        /// </summary>
        /// <param name="ChildProperty">Child property object</param>
        /// <param name="Property">Property object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
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

        /// <summary>
        /// Copies a property value
        /// </summary>
        /// <param name="ChildProperty">Child property object</param>
        /// <param name="Property">Property object</param>
        /// <param name="ClassInstance">Class to copy to</param>
        /// <param name="Object">Class to copy from</param>
        private static void SetPropertyifSimpleType(PropertyInfo ChildProperty, PropertyInfo Property, object ClassInstance, object Object)
        {
            try
            {
                Type PropertyType = Property.PropertyType;
                if (Property.PropertyType.FullName.StartsWith("System.Collections.Generic.List", StringComparison.CurrentCultureIgnoreCase))
                {
                    PropertyType = Property.PropertyType.GetGenericArguments()[0];
                }

                if (PropertyType.FullName.StartsWith("System"))
                {
                    SetProperty(ChildProperty, Property, ClassInstance, Object);
                }
            }
            catch { throw; }
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

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                ArrayTraverse walker = new ArrayTraverse(array);
                do action(array, walker.Position);
                while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            public int[] Position;
            private int[] maxLengths;

            public ArrayTraverse(Array array)
            {
                maxLengths = new int[array.Rank];
                for (int i = 0; i < array.Rank; ++i)
                {
                    maxLengths[i] = array.GetLength(i) - 1;
                }
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (int i = 0; i < Position.Length; ++i)
                {
                    if (Position[i] < maxLengths[i])
                    {
                        Position[i]++;
                        for (int j = 0; j < i; j++)
                        {
                            Position[j] = 0;
                        }
                        return true;
                    }
                }
                return false;
            }
        }
    }

}