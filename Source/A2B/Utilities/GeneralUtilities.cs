using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Reflection;
using System.Security.Permissions;

//[assembly:ReflectionPermission(SecurityAction.Demand, Flags=ReflectionPermissionFlag.AllFlags)]

namespace A2B
{

    #region Reference Types

    public class FieldReference<T> {

        public T Value {
            get {
                return (T) field.GetValue(obj);
            }

            set {
                field.SetValue(obj, value);
            }
        }

        private object obj;
        private FieldInfo field;

        public FieldReference(object obj, FieldInfo field) {
            this.obj = obj;
            this.field = field;
        }

        public static implicit operator T(FieldReference<T> fref) {
            return fref.Value;
        }
    }

    public class PropertyReference<T> {

        public T Value {
            get {
                return (T) property.GetValue(obj, null);
            }

            set {
                property.SetValue(obj, value, null);
            }
        }

        private object obj;
        private PropertyInfo property;

        public PropertyReference(object obj, PropertyInfo property) {
            this.obj = obj;
            this.property = property;
        }

        public static implicit operator T(PropertyReference<T> pref) {
            return pref.Value;
        }
    }

    #endregion

    public static class GeneralUtilities
    {

        public static List<T> List<T>(params T[] seq)
        {
            return ((IEnumerable<T>) seq).ToList();
        }

        #region Reflection

        private const BindingFlags MasterKey = 
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

        #region Method Calls

        public static object Call(this object obj, string methodName, params object[] args)
        {
            return Call<object>(obj, methodName, args);
        }

        public static T Call<T>(this object obj, string methodName, params object[] args)
        {
            Type type = obj.GetType();
            MethodInfo method = type.GetMethod(methodName, MasterKey);

            return (T) method.Invoke(obj, args);
        }

        public static object Call(this Type type, string methodName, params object[] args)
        {
            return Call<object>(type, methodName, args);
        }

        public static T Call<T>(this Type type, string methodName, params object[] args)
        {
            MethodInfo method = type.GetMethod(methodName, MasterKey);

            return (T) method.Invoke(null, args);
        }

        #endregion

        #region Property Access

        public static PropertyReference<object> Property(this object obj, string propertyName)
        {
            return Property<object>(obj, propertyName);
        }

        public static PropertyReference<T> Property<T>(this object obj, string propertyName)
        {
            Type type = obj.GetType();
            PropertyInfo property = type.GetProperty(propertyName, MasterKey);
            return new PropertyReference<T>(obj, property);
        }

        public static PropertyReference<object> Property(this Type type, string propertyName)
        {
            return Property<object>(type, propertyName);
        }

        public static PropertyReference<T> Property<T>(this Type type, string propertyName)
        {
            PropertyInfo property = type.GetProperty(propertyName, MasterKey);
            return new PropertyReference<T>(null, property);
        }

        #endregion

        #region Field Access

        public static FieldReference<object> Field(this object obj, string fieldName)
        {
            return Field<object>(obj, fieldName);
        }

        public static FieldReference<T> Field<T>(this object obj, string fieldName)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, MasterKey);

            return new FieldReference<T>(obj, field);
        }

        public static FieldReference<object> Field(this Type type, string fieldName)
        {
            return Field<object>(type, fieldName);
        }

        public static FieldReference<T> Field<T>(this Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, MasterKey);

            return new FieldReference<T>(null, field);
        }

        #endregion

        #endregion

    }
}
