using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Models;
using Dapper;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;

namespace Test
{
    class Program
    {
        static Stopwatch watch = new Stopwatch();
        static void Main(string[] args)
        {
            watch.Restart();
            using (TestEntities context = new TestEntities())
            {
                //context.Optimization();
                context.Initialize();               
            }
            Console.WriteLine("{0}ms - Initialize Context", watch.ElapsedMilliseconds);

            TestsDapper();            
            Tests();
            TestsEF();            
            
            Console.ReadLine();
        }

        public static void Tests()
        {
            Console.WriteLine();
            Console.WriteLine("##### EXTENSIONS");
            Console.WriteLine();
            Insert();
            Update();
            Delete();
            Select();
            ToDapper();
        }

        public static void TestsDapper()
        {
            Console.WriteLine();
            Console.WriteLine("##### DAPPER");
            Console.WriteLine();
            InsertDapper();
            UpdateDapper();
            DeleteDapper();
            SelectDapper();
        }

        public static void TestsEF()
        {
            Console.WriteLine();
            Console.WriteLine("##### ENTITYFRAMEWORK");
            Console.WriteLine();
            InsertEF();
            UpdateEF();
            //Delete();
            //Select();            
        }

        #region Tests Extensions
        public static void Insert()
        {
            using (TestEntities context = new TestEntities())
            {
                var obj = new 
                { 
                    Name = "User Insert", 
                    DateCreate = DateTime.Now, 
                    Gender = Gender.Female 
                };

                watch.Restart();
                context.Users.Insert(obj);
                Console.WriteLine("Insert - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void Update()
        {
            using (TestEntities context = new TestEntities())
            {
                var obj = new
                {
                    Gender = Gender.Male
                };

                watch.Restart();
                context.Users.Update(obj);
                Console.WriteLine("Update All - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Users.Update(new
                {
                    Gender = Gender.Female
                }, o=> o.Id == 1);
                Console.WriteLine("Update With Query - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Users.Update(new
                {
                    Gender = Gender.Female
                }, o => o.Id == 1 && (o.Name == "teste" || o.DateCreate > DateTime.Now));
                Console.WriteLine("Update With Complex Query - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void Delete()
        {
            using (TestEntities context = new TestEntities())
            {
                watch.Restart();
                context.BlogPosts.Delete();
                Console.WriteLine("Delete All - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Users.Delete(o => o.Id == 6);
                Console.WriteLine("Delete With Query - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void Select()
        {
            using (TestEntities context = new TestEntities())
            {
                watch.Restart();
                var result = context.Users.Query(o => o.Id > 1, 2, o => o.Id);
                Console.WriteLine("Select - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void ToDapper()
        {
            using(TestEntities context = new TestEntities())
            {
                object result = null;
                watch.Restart();
                result = (from o in context.Users
                          where o.Id > 1
                          orderby o.Id
                          select o).ToDapper().ToList();
                Console.WriteLine("ToDapper - {0}ms", watch.ElapsedMilliseconds);
            }
        }
        #endregion

        #region Tests Dapper
        public static void InsertDapper()
        {
            using (TestEntities context = new TestEntities())
            {
                var obj = new
                {
                    Name = "User Insert",
                    DateCreate = DateTime.Now,
                    Gender = Gender.Female
                };

                watch.Restart();
                context.Database.Connection.Query<Int64>("insert into [user] (name, datecreate, gender) values (@Name, @DateCreate, @Gender); SELECT CAST(SCOPE_IDENTITY() as bigint)", obj).FirstOrDefault();
                Console.WriteLine("Insert - {0}ms", watch.ElapsedMilliseconds);                
            }
            Console.WriteLine();
        }

        public static void UpdateDapper()
        {
            using (TestEntities context = new TestEntities())
            {
                var obj = new
                {
                    Gender = Gender.Male,
                    Id = 1,
                    Name = "teste",
                    Date = DateTime.Now
                };

                watch.Restart();
                context.Database.Connection.Execute("update [user] set Gender = @Gender", obj);
                Console.WriteLine("Update All - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Database.Connection.Execute("update [user] set Gender = @Gender where Id = @Id", obj);
                Console.WriteLine("Update With Query - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Database.Connection.Execute("update [user] set Gender = @Gender where Id = @Id and (name = @Name or datecreate > @Date)", obj);
                Console.WriteLine("Update With Complex Query Dapper - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void DeleteDapper()
        {
            using (TestEntities context = new TestEntities())
            {
                watch.Restart();
                context.Database.Connection.Execute("delete from [BlogPost]");                
                Console.WriteLine("Delete All - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Database.Connection.Execute("delete from [BlogPost] where id = 6");             
                Console.WriteLine("Delete With Query - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void SelectDapper()
        {
            using (TestEntities context = new TestEntities())
            {
                watch.Restart();
                var result = context.Database.Connection.Query<User>("select top 2 * from [user] where id > 1 order by id");                
                Console.WriteLine("Select - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }
        #endregion

        #region Tests EntityFramework
        public static void InsertEF()
        {
            using (TestEntities context = new TestEntities())
            {
                watch.Restart();
                context.Users.Add(new User() { Name = "User Insert", DateCreate = DateTime.Now, Gender = Gender.Female });
                context.SaveChanges();
                Console.WriteLine("Insert - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }

        public static void UpdateEF()
        {
            using (TestEntities context = new TestEntities())
            {
                User user = new User
                {
                    Gender = Gender.Male,
                    Id = 1,
                    Name = "teste",
                    DateCreate = DateTime.Now
                };

                watch.Restart();
                context.Entry(user).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                Console.WriteLine("Update All - {0}ms", watch.ElapsedMilliseconds);
            }
            Console.WriteLine();
        }
        #endregion
    }
}
