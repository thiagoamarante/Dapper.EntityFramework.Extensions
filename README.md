# Dapper.EntityFramework.Extensions (1.8.0.1)
Extension for EntityFramework
      Library brings together the best of the EntityFramework with Dapper.
      Basic CRUD operations (Query, Insert, Update, Delete) for your POCOs.
      
      (*) Release 1.8.0.1
      - Support  DbGeography/DbGeometry

      (*) Release 1.7.0.1       
      - Insert optional return identity
      - Insert return identity object type
      - Insert and Update can change PropertyKey
      - Insert and Update automatic removes property of different kind of primitive and enum

      (*) Release 1.7.0.0
      - New method Query and Query with selector
      - Query support top
      - Query support where
      - Query support orderby
      - Query support selector
      
      (*) Release 1.6.0.1
      - Support schemas
      - Support EntityFramework in Transaction
      - Support CommandTimeout DbContext

using Dapper;

public class Example
{

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
                Console.WriteLine("Insert With Anonymous - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                var obj2 = new User()
                {
                    Name = "User Insert",
                    DateCreate = DateTime.Now,
                    Gender = Gender.Male
                };                
                context.Users.Insert(obj2);
                Console.WriteLine("Insert With Class - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();                
                context.Users.Insert(obj, returnIdentity: false);
                Console.WriteLine("Insert Without Return Identity - {0}ms", watch.ElapsedMilliseconds);

                Console.WriteLine();
                watch.Restart();
                context.Users.Insert(obj, propertyKey: "User_Id");
                Console.WriteLine("Insert With Change PropertyKey - {0}ms", watch.ElapsedMilliseconds);           
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
                var result = context.Users.Query(o => o.Id > 1, 2, orderBy => orderBy.Asc(o => o.Id ).Desc(o => o.Name));
                int qtd = result.Count();
                Console.WriteLine("Select - {0}ms", watch.ElapsedMilliseconds);

                watch.Restart();
                var result2 = context.Users.Query(o=> new { o.Id, o.Name }, o => o.Id > 1, 2, orderBy => orderBy.Asc(o => o.Id).Desc(o => o.Name));
                qtd = result2.Count();
                Console.WriteLine("Select With Selector - {0}ms", watch.ElapsedMilliseconds);
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

        public static void Transaction()
        {
            using(TestEntities context = new TestEntities())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        repository.BlogPosts.Delete();
                        repository.Blogs.Delete();
                        transaction.Commit();                        
                    }
                    catch(Exception ex)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        public static void EntityFrameworkAndDapperTogether()
        {
            using(TestEntities context = new TestEntities())
            {
                using (var transaction = context.Database.BeginTransaction())
                {
                    try
                    {
                        repository.BlogPosts.Add(new BlogPost() 
                        {
                            BlogId = 1,
                            Body = "text body",
                            DatePublication = DateTime.Now
                        });
                        repository.SaveChanges();

                        repository.BlogSettings.Update(new { AutoSave = true}, o=> o.BlogId = 1);

                        transaction.Commit();                        
                    }
                    catch(Exception ex)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }
}
