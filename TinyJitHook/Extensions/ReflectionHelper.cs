using System;
using System.Reflection;

namespace TinyJitHook.Extensions
{
    /// <summary>
    /// A general class to help with many things related to reflection.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Get a module scope.
        /// </summary>
        /// <param name="mod">The module to get the scope of.</param>
        /// <returns>The module's scope.</returns>
        public static IntPtr GetScope(this Module mod)
        {
            var obj = mod.ModuleHandle.GetFieldValue<object>("m_ptr");
            if (obj is IntPtr)
            {
                return (IntPtr)obj;
            }
            if (obj.GetType().ToString() == "System.Reflection.RuntimeModule")
            {
                return obj.GetFieldValue<IntPtr>("m_pData");
            }

            throw new Exception("Cannot get scope of module.");
        }
        /// <summary>
        /// Get a field value from a field using reflection.
        /// </summary>
        /// <typeparam name="T">Type of the field (the return type).</typeparam>
        /// <param name="obj">The object to get the field from.</param>
        /// <param name="fieldName">The field name of the field to retrieve value of.</param>
        /// <returns>The value of the field casted to T.</returns>
        public static T GetFieldValue<T>(this object obj, string fieldName)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");
            var rtType = obj.GetType();
           // var fields = rtType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var field = rtType.GetField(fieldName, BindingFlags.Public |
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException("fieldName", "No such field was found.");

            if (!typeof(T).IsAssignableFrom(field.FieldType))
                throw new InvalidOperationException("Field type and requested type are not compatible.");

            return (T)field.GetValue(obj);
        }
        /// <summary>
        /// Get a field value from a field of a specific type using reflection.
        /// </summary>
        /// <typeparam name="T">Type of the field (the return type).</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="fieldName">The field name of the field to retrieve value of.</param>
        /// <param name="theType">The type to use to get the field from.</param>
        /// <returns>The value of the field casted to T</returns>
        public static T GetFieldValue<T>(this object obj, string fieldName, Type theType)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

           // var fields = theType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var field = theType.GetField(fieldName, BindingFlags.Public |
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException("fieldName", "No such field was found.");

            if (!typeof(T).IsAssignableFrom(field.FieldType))
                throw new InvalidOperationException("Field type and requested type are not compatible.");

            return (T)field.GetValue(obj);
        }
        /// <summary>
        /// Set a field's value using reflection.
        /// </summary>
        /// <param name="obj">The type instance that holds the field to set the value of.</param>
        /// <param name="fieldName">The field name.</param>
        /// <param name="data">The data to set the field to, must be the field type.</param>
        public static void SetFieldValue(this object obj, string fieldName, object data)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            var field = obj.GetType().GetField(fieldName, BindingFlags.Public |
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.Instance);

            if (field == null)
                throw new ArgumentException("fieldName", "No such field was found.");

            field.SetValue(obj, data);
        }

        /// <summary>
        /// Get a method in a type with the flags specified, will work with internal and private types.
        /// </summary>
        /// <param name="type">The full name (including namespace) of the type.</param>
        /// <param name="method">The method name to retrieve.</param>
        /// <param name="flags">The binding flags to use to find the method on the type.</param>
        /// <param name="breakOnFind">Whether or not to immediately return when found.</param>
        /// <returns>The method with the name supplied.</returns>
        public static MethodInfo GetMethod(this string type, string method, BindingFlags flags, bool breakOnFind = true)
        {
            MethodInfo found = null;
            foreach (MethodInfo mInfo in typeof(Assembly).Assembly.GetType(type)
                .GetMethods(flags))
            {
                if (mInfo.Name == method)
                {
                    found = mInfo;
                    if (breakOnFind)
                    {
                        break;
                    }
                }
            }
            return found;
        }
    }
}
