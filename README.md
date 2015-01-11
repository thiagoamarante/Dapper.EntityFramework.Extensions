# Dapper.EntityFramework.Extensions
Extension dapper to EntityFramework


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
}
