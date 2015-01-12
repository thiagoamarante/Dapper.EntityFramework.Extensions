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
using System.Diagnostics;
using System.Collections;

namespace Dapper
{
    public static class Extensions
    {       
        private static bool _Init = false;       
        private static Dictionary<Type, DbContext> _CacheContext = new Dictionary<Type, DbContext>();
        
        public static Int64 Insert<TEntity>(this DbSet<TEntity> source, object entity)
            where TEntity : class
        {
#if DEBUG
            var watch = LogStart("INSERT");
#endif
            var context = source.GetDbContext();
#if DEBUG
            LogElapsed(watch, "GetDbContext");
#endif
            string table = typeof(TEntity).Name;
            string[] properties = entity.GetType().GetProperties().Where(o => o.Name != "Id").Select(o => o.Name).ToArray();
            string sql = string.Concat("INSERT INTO [", table, "] (",
                string.Join(", ", properties.Select(o => string.Concat("[", o, "]"))),
                ") VALUES(",
                string.Join(", ", properties.Select(o => "@" + o)), "); SELECT CAST(SCOPE_IDENTITY() as bigint)");
            var propertyId = entity.GetType().GetProperty("Id");
#if DEBUG
            LogElapsed(watch, "Parse");
#endif
            Int64 result = context.Database.Connection.Query<Int64>(sql, entity).FirstOrDefault();
#if DEBUG
            LogElapsed(watch, "Execution");
#endif
            if (propertyId != null)
                propertyId.SetValue(entity, result);
            return result;
        }

        public static int Update<TEntity>(this DbSet<TEntity> source, object entity, Expression<Func<TEntity, bool>> where = null)
            where TEntity : class
        {
#if DEBUG
            var watch = LogStart("UPDATE");
#endif
            var context = source.GetDbContext();
#if DEBUG
            LogElapsed(watch, "GetDbContext");
#endif
            DynamicParameters parameter = new DynamicParameters();
            parameter.AddDynamicParams(entity);            
            string table = typeof(TEntity).Name;
            Dictionary<string, Type> properties = entity.GetType().GetProperties().ToDictionary(o => o.Name, o => o.PropertyType);
            string sql = string.Concat("UPDATE [", table, "] SET ",
                string.Join(", ", properties.Where(o => o.Key != "Id").Select(o => string.Concat("[", o.Key, "]") + " = @" + o.Key)));
#if DEBUG
            LogElapsed(watch, "Parse update");
#endif
            if (where != null)
            {
                sql += " " + ProcessWhere<TEntity>(parameter, source, where);
#if DEBUG
                LogElapsed(watch, "Parse where");
#endif
            }
            else if (properties.Any(o => o.Key == "Id"))
                sql += " WHERE [Id] = @Id";
            var result =  context.Database.Connection.Execute(sql, parameter);
#if DEBUG
            LogElapsed(watch, "Execution");
#endif
            return result;
        }

        public static int Delete<TEntity>(this DbSet<TEntity> source, Expression<Func<TEntity, bool>> where = null)
            where TEntity : class
        {
#if DEBUG
            var watch = LogStart("DELETE");
#endif
            var context = source.GetDbContext();
#if DEBUG
            LogElapsed(watch, "GetDbContext");
#endif
            DynamicParameters parameter = new DynamicParameters();
            string table = typeof(TEntity).Name;          
            string sql = string.Concat("DELETE FROM [", table, "]");
#if DEBUG
            LogElapsed(watch, "Parse");
#endif
            if (where != null)
            {
                sql += " " + ProcessWhere<TEntity>(parameter, source, where);
#if DEBUG
                LogElapsed(watch, "Parse where");
#endif
            }
            var result = context.Database.Connection.Execute(sql, parameter);
#if DEBUG
            LogElapsed(watch, "Execution");
#endif
            return result;
        }

        public static IEnumerable<TEntity> Query<TEntity, TKey>(this DbSet<TEntity> source, Expression<Func<TEntity, bool>> where = null, int? top = null, Expression<Func<TEntity, TKey>> orderBy = null, Expression<Func<TEntity, TKey>> orderByDescending = null)
            where TEntity : class
        {
#if DEBUG
            var watch = LogStart("QUERY");
#endif
            var context = source.GetDbContext();
#if DEBUG
            LogElapsed(watch, "GetDbContext");
#endif

            var objectQuery  = (ObjectQuery<TEntity>)((IObjectContextAdapter)context).ObjectContext.CreateObjectSet<TEntity>();
            if (where != null)
                objectQuery = (ObjectQuery<TEntity>)objectQuery.Where(where);

            if (orderBy != null)
                objectQuery = (ObjectQuery<TEntity>)objectQuery.OrderBy(orderBy);

            if (orderByDescending != null)
                objectQuery = (ObjectQuery<TEntity>)objectQuery.OrderByDescending(orderByDescending);

            string query = objectQuery.ToTraceString();

            if (top != null) 
            {
                query = "SELECT TOP " + top.Value + query.Substring(query.IndexOf("SELECT") + 6);
            }
#if DEBUG
            LogElapsed(watch, "Parse");
#endif
            var result = context.Database.Connection.Query<TEntity>(query);
#if DEBUG
            LogElapsed(watch, "Execution");
#endif
            return result;
        }

        public static IEnumerable<TEntity> ToDapper<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class
        {
#if DEBUG
            var watch = LogStart("ToDapper");
#endif
            var context = source.GetDbContext();
#if DEBUG
            LogElapsed(watch, "GetDbContext");
#endif
            string query = source.ToString();
#if DEBUG
            LogElapsed(watch, "Parse");
#endif
            var result =  context.Database.Connection.Query<TEntity>(query);
#if DEBUG
            LogElapsed(watch, "Execution");
#endif
            return result;
        }

        public static void Initialize(this DbContext context)
        {
#if DEBUG
            var watch = LogStart("Initialize");
#endif
            context.Database.Initialize(true);
#if DEBUG
            LogElapsed(watch, "Database.Initialize");
#endif
            foreach (var property in context.GetType().GetProperties().Where(o => o.PropertyType.Name == "DbSet`1"))
            {               
                var dbSet = property.GetValue(context);
                dbSet.ToString();
            }
#if DEBUG
            LogElapsed(watch, "Cache DbSet");
#endif
        }

        public static void Optimization(this DbContext context)
        {
            context.Configuration.AutoDetectChangesEnabled = false;
            context.Configuration.LazyLoadingEnabled = false;
            context.Configuration.ProxyCreationEnabled = false;
            context.Configuration.ValidateOnSaveEnabled = false;            
        }

        private static DbContext GetDbContext(this IQueryable source)
        {           
            var internalContextProperty = source.Provider.GetType().GetProperty("InternalContext");
            if(internalContextProperty != null)
            {
                var internalContext = internalContextProperty.GetValue(source.Provider);
                var ownerProperty = internalContext.GetType().GetProperty("Owner", BindingFlags.Instance | BindingFlags.Public);
                if (ownerProperty != null)
                {
                    DbContext dbContext = (DbContext)ownerProperty.GetValue(internalContext, null);
                    if (dbContext != null)
                        return dbContext;                    
                    else
                        throw new Exception("Context not found");
                }
                else
                    throw new Exception("Owner not found");
            }
            else
                throw new Exception("InternalContext not found");
        }

        private static string ProcessWhere<TEntity>(DynamicParameters parameters, DbSet<TEntity> source, System.Linq.Expressions.Expression<Func<TEntity, bool>> where)
            where TEntity : class
        {
            var context = source.GetDbContext();
            var objectQuery = ((ObjectQuery<TEntity>)((IObjectContextAdapter)context).ObjectContext.CreateObjectSet<TEntity>().Where(where));
            string query = objectQuery.ToTraceString();
            foreach(var param in objectQuery.Parameters)
                parameters.Add("@" + param.Name, param.Value);
            return query.Substring(query.IndexOf("WHERE")).Replace("[Extent1].", "");
        }

#if DEBUG
        private static Stopwatch LogStart(string message)
        {
            Console.WriteLine(message);
            var watch = new Stopwatch();
            watch.Start();
            return watch;
        }

        private static void LogElapsed(Stopwatch watch, string message)
        {
            Console.WriteLine(watch.ElapsedMilliseconds + "ms - " + message);
            watch.Restart();
        }
#endif
    }
}
