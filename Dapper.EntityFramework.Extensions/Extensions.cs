using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using System.Dynamic;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Threading;

namespace Dapper
{
    public static class Extensions
    {
        private static AssemblyName _AssemblyName;
        private static AssemblyBuilder _AssemblyBuilder;
        private static ModuleBuilder _Module;
        private static bool _Init = false;
        private static Dictionary<string, Type> _Cache = new Dictionary<string, Type>();

        private static void Init()
        {
            if (!_Init)
            {
                _AssemblyName = new AssemblyName("FactorialAssembly");
                _AssemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(_AssemblyName, AssemblyBuilderAccess.RunAndCollect);
                _Module = _AssemblyBuilder.DefineDynamicModule("DynamicModule");
                _Init = true;
            }
        }

        public static Int64 Insert<TEntity>(this DbSet<TEntity> source, object entity)
            where TEntity : class
        {
            Init();
            var context = source.GetContext();
            string table = typeof(TEntity).Name;
            string[] properties = entity.GetType().GetProperties().Where(o => o.Name != "Id").Select(o => o.Name).ToArray();
            string sql = string.Concat("INSERT INTO [", table, "] (",
                string.Join(", ", properties.Select(o => string.Concat("[", o, "]"))),
                ") VALUES(",
                string.Join(", ", properties.Select(o => "@" + o)), "); SELECT CAST(SCOPE_IDENTITY() as bigint)");
            var propertyId = entity.GetType().GetProperty("Id");
            Int64 result = context.Database.Connection.Query<Int64>(sql, entity).FirstOrDefault();
            if (propertyId != null)
                propertyId.SetValue(entity, result);
            return result;
        }

        public static int Update<TEntity>(this DbSet<TEntity> source, object entity, object where = null)
            where TEntity : class
        {
            Init();
            object parameter = entity;
            var context = source.GetContext();
            string table = typeof(TEntity).Name;
            Dictionary<string, Type> properties = entity.GetType().GetProperties().ToDictionary(o => o.Name, o => o.PropertyType);
            Dictionary<string, Type> propertiesWhere = null;
            string sql = string.Concat("UPDATE [", table, "] SET ",
                string.Join(", ", properties.Where(o => o.Key != "Id").Select(o => string.Concat("[", o.Key, "]") + " = @" + o.Key)));
            if (where != null)
            {
                propertiesWhere = where.GetType().GetProperties().ToDictionary(o => o.Name, o => o.PropertyType);
                sql += " WHERE " + string.Join(" AND ", propertiesWhere.Select(o => string.Concat("[", o.Key, "]") + " = @" + o.Key));
            }
            else if (properties.Any(o => o.Key == "Id"))
                sql += " WHERE [Id] = @Id";
            if (where != null)
            {
                Type newType = null;
                if (_Cache.ContainsKey(sql.ToLower()))
                    newType = _Cache[sql.ToLower()];
                else
                {
                    newType = CreateNewType("", properties.Union(propertiesWhere).Distinct().ToDictionary(o => o.Key, o => o.Value));
                    _Cache.Add(sql.ToLower(), newType);
                }
                parameter = Activator.CreateInstance(newType);
                foreach (var property in properties)
                {
                    parameter.GetType().GetProperty(property.Key).SetValue(parameter, entity.GetType().GetProperty(property.Key).GetValue(entity));
                }
                foreach (var property in propertiesWhere)
                {
                    parameter.GetType().GetProperty(property.Key).SetValue(parameter, where.GetType().GetProperty(property.Key).GetValue(where));
                }
            }
            return context.Database.Connection.Execute(sql, parameter);
        }

        public static int Delete<TEntity>(this DbSet<TEntity> source, object where = null)
            where TEntity : class
        {
            Init();
            var context = source.GetContext();
            string table = typeof(TEntity).Name;
            string[] propertiesWhere = where.GetType().GetProperties().Select(o => o.Name).ToArray();
            string sql = string.Concat("DELETE FROM [", table, "]");
            if (where != null)
            {
                if (propertiesWhere.Any(o => o == "Id"))
                    sql += " WHERE [Id] = @Id";
                else
                    sql += " WHERE " + string.Join(" AND ", propertiesWhere.Select(o => string.Concat("[", o, "]") + " = @" + o));
            }
            return context.Database.Connection.Execute(sql, where);
        }

        public static IEnumerable<TEntity> ToDapper<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class
        {
            Init();
            var context = source.GetContext();
            return context.Database.Connection.Query<TEntity>(source.ToString());
        }

        private static DbContext GetContext<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : class
        {
            object internalSet = dbSet
                .GetType()
                .GetField("_internalSet", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(dbSet);
            object internalContext = internalSet
                .GetType()
                .BaseType
                .GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(internalSet);
            return (DbContext)internalContext
                .GetType()
                .GetProperty("Owner", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(internalContext, null);
        }

        private static DbContext GetContext<TEntity>(this IQueryable<TEntity> dbQueryable)
            where TEntity : class
        {
            object internalQuery = dbQueryable
                .GetType()
                .GetField("_internalQuery", BindingFlags.NonPublic | BindingFlags.Instance)
                //.GetProperty("InternalQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(dbQueryable);

            //dynamic internalContext = (internalQuery as dynamic).InternalContext;
            object internalContext = internalQuery
                .GetType()
                .GetField("_internalContext", BindingFlags.NonPublic | BindingFlags.Instance)
                //    .GetType().GetProperty("InternalContext");
                .GetValue(internalQuery);

            return (DbContext)internalContext
                .GetType()
                .GetProperty("Owner", BindingFlags.Instance | BindingFlags.Public)
                .GetValue(internalContext, null);
        }

        private static Type CreateNewType(string typeName, Dictionary<string, Type> columns)
        {
            Guid guid = Guid.NewGuid();
            typeName = string.Concat(typeName, "_", guid.ToString().Substring(0, 11).Replace("-", ""));
            TypeBuilder typeBuilder = _Module.DefineType(typeName, TypeAttributes.Public);
            foreach (KeyValuePair<string, Type> column in columns)
            {
                string key = column.Key;
                FieldBuilder fieldBuilder = typeBuilder.DefineField(string.Concat("_", key), column.Value, FieldAttributes.Private);
                Type value = column.Value;
                Type[] typeArray = new Type[] { column.Value };
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(key, PropertyAttributes.None, value, typeArray);
                MethodAttributes methodAttribute = MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Public | MethodAttributes.HideBySig;
                MethodBuilder methodBuilder = typeBuilder.DefineMethod("get_value", methodAttribute, column.Value, Type.EmptyTypes);
                ILGenerator lGenerator = methodBuilder.GetILGenerator();
                lGenerator.Emit(OpCodes.Ldarg_0);
                lGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
                lGenerator.Emit(OpCodes.Ret);
                typeArray = new Type[] { column.Value };
                MethodBuilder methodBuilder1 = typeBuilder.DefineMethod("set_value", methodAttribute, null, typeArray);
                ILGenerator lGenerator1 = methodBuilder1.GetILGenerator();
                lGenerator1.Emit(OpCodes.Ldarg_0);
                lGenerator1.Emit(OpCodes.Ldarg_1);
                lGenerator1.Emit(OpCodes.Stfld, fieldBuilder);
                lGenerator1.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(methodBuilder);
                propertyBuilder.SetSetMethod(methodBuilder1);
            }
            return typeBuilder.CreateType();
        }  
    }
}
