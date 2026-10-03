using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace ModUtil
{
    /// <summary>
    /// Reflection helpers for members the game keeps private.
    ///
    /// Why this exists: `GHPC_Data\Managed\publicized_assemblies\Assembly-CSharp-Publicized.dll`
    /// is only a compile-time convenience - the assembly the running game loads is the
    /// official `Assembly-CSharp.dll`, where these members are private. Writing
    /// `vehicle._friendlyName = ...` therefore compiles against the publicized copy and
    /// then throws FieldAccessException in game. Everything non-public goes through here.
    ///
    /// Two traps that are handled explicitly:
    ///  * `Type.GetField(name, ...)` does NOT return private members declared on a base
    ///    class, so every lookup walks BaseType by hand.
    ///  * Failed lookups are cached too - they are logged once instead of once per frame.
    /// </summary>
    public static class Reflect
    {
        private const BindingFlags ANY =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> properties =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private static readonly Dictionary<Type, Dictionary<string, CachedMethod[]>> methods =
            new Dictionary<Type, Dictionary<string, CachedMethod[]>>();
        private static readonly HashSet<string> reported = new HashSet<string>();

        private sealed class CachedMethod
        {
            internal MethodInfo method;
            internal Type[] parameters;
        }

        private static void Miss(string what)
        {
            if (reported.Add(what))
            {
            }
        }

        private static string MemberName(Type type, string name)
        {
            return type.FullName + "::" + name;
        }

        public static FieldInfo FindField(Type type, string name)
        {
            if (type == null)
            {
                return null;
            }

            Dictionary<string, FieldInfo> map;
            if (!fields.TryGetValue(type, out map))
            {
                map = new Dictionary<string, FieldInfo>();
                fields[type] = map;
            }

            FieldInfo cached;
            if (map.TryGetValue(name, out cached))
            {
                return cached;
            }

            FieldInfo found = null;
            for (Type t = type; t != null && found == null; t = t.BaseType)
            {
                found = t.GetField(name, ANY | BindingFlags.DeclaredOnly);
            }

            if (found == null)
            {
                Miss("field " + MemberName(type, name));
            }

            map[name] = found;
            return found;
        }

        public static PropertyInfo FindProperty(Type type, string name)
        {
            if (type == null)
            {
                return null;
            }

            Dictionary<string, PropertyInfo> map;
            if (!properties.TryGetValue(type, out map))
            {
                map = new Dictionary<string, PropertyInfo>();
                properties[type] = map;
            }

            PropertyInfo cached;
            if (map.TryGetValue(name, out cached))
            {
                return cached;
            }

            PropertyInfo found = null;
            for (Type t = type; t != null && found == null; t = t.BaseType)
            {
                found = t.GetProperty(name, ANY | BindingFlags.DeclaredOnly);
            }

            if (found == null)
            {
                Miss("property " + MemberName(type, name));
            }

            map[name] = found;
            return found;
        }

        public static object GetBoxed(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            FieldInfo field = FindField(target.GetType(), name);
            return field == null ? null : field.GetValue(target);
        }

        public static T Get<T>(object target, string name)
        {
            if (target == null)
            {
                return default(T);
            }

            FieldInfo field = FindField(target.GetType(), name);
            return field == null ? default(T) : Read<T>(field, target);
        }

        private static T Read<T>(FieldInfo field, object target)
        {
            if (field.FieldType == typeof(T))
            {
                Func<object, T> getter = Getters<T>.For(field);

                if (getter != null)
                {
                    try
                    {
                        return getter(target);
                    }
                    catch (Exception)
                    {
                        // Some runtimes refuse a compiled delegate for a private member.
                    }
                }
            }

            object value = field.GetValue(target);
            return value == null ? default(T) : (T)value;
        }

        /// <summary>One compiled getter per field and requested type; avoids value-type boxing.</summary>
        private static class Getters<T>
        {
            private static readonly Dictionary<FieldInfo, Func<object, T>> map =
                new Dictionary<FieldInfo, Func<object, T>>();

            internal static Func<object, T> For(FieldInfo field)
            {
                Func<object, T> cached;

                if (map.TryGetValue(field, out cached))
                {
                    return cached;
                }

                try
                {
                    ParameterExpression instance = Expression.Parameter(typeof(object), "target");
                    Expression owner = field.IsStatic
                        ? null
                        : (Expression)Expression.Convert(instance, field.DeclaringType);
                    Expression body = Expression.Convert(Expression.Field(owner, field), typeof(T));
                    cached = Expression.Lambda<Func<object, T>>(body, instance).Compile();
                }
                catch (Exception)
                {
                    cached = null;
                }

                map[field] = cached;
                return cached;
            }
        }

        /// <summary>
        /// Set, for a caller that already knows the value's type. The object overload boxes every value on
        /// the way in, and the aim path calls this once a frame with a Vector2; and looking a field up by
        /// name allocates on every call, which is the larger half of what the per-frame callers pay.
        /// </summary>
        public static void Set<T>(object target, string name, T value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = FindField(target.GetType(), name);
            if (field == null)
            {
                return;
            }

            Action<object, T> setter = field.FieldType == typeof(T) ? Setters<T>.For(field) : null;

            if (setter != null)
            {
                try
                {
                    setter(target, value);
                    return;
                }
                catch (Exception)
                {
                    // A private field the compiled setter cannot reach is still a field.
                }
            }

            field.SetValue(target, value);
        }

        /// <summary>One compiled setter per field, per value type; see Set&lt;T&gt;.</summary>
        private static class Setters<T>
        {
            private static readonly Dictionary<FieldInfo, Action<object, T>> map =
                new Dictionary<FieldInfo, Action<object, T>>();

            internal static Action<object, T> For(FieldInfo field)
            {
                Action<object, T> cached;

                if (map.TryGetValue(field, out cached))
                {
                    return cached;
                }

                try
                {
                    ParameterExpression instance = Expression.Parameter(typeof(object), "target");
                    ParameterExpression argument = Expression.Parameter(typeof(T), "value");
                    Expression body = Expression.Assign(
                        Expression.Field(Expression.Convert(instance, field.DeclaringType), field),
                        Expression.Convert(argument, field.FieldType));
                    cached = Expression.Lambda<Action<object, T>>(body, instance, argument).Compile();
                }
                catch (Exception)
                {
                    cached = null;
                }

                map[field] = cached;
                return cached;
            }
        }

        public static void Set(object target, string name, object value)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo field = FindField(target.GetType(), name);
            if (field == null)
            {
                return;
            }

            field.SetValue(target, value);
        }

        public static object GetStatic(Type type, string name)
        {
            FieldInfo field = FindField(type, name);
            return field == null ? null : field.GetValue(null);
        }

        public static T GetStatic<T>(Type type, string name)
        {
            FieldInfo field = FindField(type, name);
            return field == null ? default(T) : Read<T>(field, null);
        }

        public static void SetStatic(Type type, string name, object value)
        {
            FieldInfo field = FindField(type, name);
            if (field != null)
            {
                field.SetValue(null, value);
            }
        }

        /// <summary>Writes through a property whose setter is not public.</summary>
        public static void SetProperty(object target, string name, object value)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo property = FindProperty(target.GetType(), name);
            if (property == null)
            {
                return;
            }

            MethodInfo setter = property.GetSetMethod(true);
            if (setter == null)
            {
                Miss("setter for " + MemberName(target.GetType(), name));
                return;
            }

            setter.Invoke(target, new object[] { value });
        }

        public static object Call(object target, string name, params object[] args)
        {
            if (target == null)
            {
                return null;
            }

            return Invoke(target.GetType(), target, name, args);
        }

        public static object CallStatic(Type type, string name, params object[] args)
        {
            return Invoke(type, null, name, args);
        }

        private static object Invoke(Type type, object target, string name, object[] args)
        {
            MethodInfo method = FindMethod(type, name, args);
            if (method == null)
            {
                return null;
            }

            return method.Invoke(target, args);
        }

        private static MethodInfo FindMethod(Type type, string name, object[] args)
        {
            if (type == null)
            {
                return null;
            }

            Dictionary<string, CachedMethod[]> map;
            if (!methods.TryGetValue(type, out map))
            {
                map = new Dictionary<string, CachedMethod[]>();
                methods[type] = map;
            }

            CachedMethod[] candidates;
            if (!map.TryGetValue(name, out candidates))
            {
                List<CachedMethod> found = new List<CachedMethod>();

                for (Type t = type; t != null; t = t.BaseType)
                {
                    foreach (MethodInfo candidate in t.GetMethods(ANY | BindingFlags.DeclaredOnly))
                    {
                        if (candidate.Name != name)
                        {
                            continue;
                        }

                        ParameterInfo[] declared = candidate.GetParameters();
                        CachedMethod cached = new CachedMethod();
                        cached.method = candidate;
                        cached.parameters = new Type[declared.Length];

                        for (int i = 0; i < declared.Length; i++)
                        {
                            cached.parameters[i] = declared[i].ParameterType;
                        }

                        found.Add(cached);
                    }
                }

                candidates = found.ToArray();
                map[name] = candidates;
            }

            int count = args == null ? 0 : args.Length;

            for (int candidate_index = 0; candidate_index < candidates.Length; candidate_index++)
            {
                CachedMethod candidate = candidates[candidate_index];

                if (candidate.parameters.Length != count)
                {
                    continue;
                }

                bool matches = true;
                for (int i = 0; i < count; i++)
                {
                    Type parameter = candidate.parameters[i];

                    if (args[i] == null)
                    {
                        if (parameter.IsValueType && Nullable.GetUnderlyingType(parameter) == null)
                        {
                            matches = false;
                            break;
                        }

                        continue;
                    }

                    if (!parameter.IsInstanceOfType(args[i]))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    return candidate.method;
                }
            }

            string signature = name + "(" + string.Join(", ", System.Linq.Enumerable.Select(args ?? new object[0], a => a == null ? "null" : a.GetType().Name)) + ")";
            Miss("method " + MemberName(type, signature) + " - note that an array argument is spread by `params object[]` unless it is wrapped in object[]");
            return null;
        }

        /// <summary>Indexes a generic dictionary through the non-generic IDictionary view (private value types included).</summary>
        public static bool DictHas(object dictionary, object key)
        {
            IDictionary dict = dictionary as IDictionary;
            return dict != null && dict.Contains(key);
        }

        public static object DictGet(object dictionary, object key)
        {
            IDictionary dict = dictionary as IDictionary;
            if (dict == null || !dict.Contains(key))
            {
                return null;
            }

            return dict[key];
        }
    }
}