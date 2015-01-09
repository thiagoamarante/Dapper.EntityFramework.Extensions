# Dapper.EntityFramework.Extensions
Extension dapper to EntityFramework


using Dapper;

public class Example
{

    public void Insert()
    {
        using (Repository repository = new Repository(true))
        {
            //Insert and return id
            var id = repository.Customers.Insert(new { Name = "test" });            
        }
    }

    public void Update()
    {
        using (Repository repository = new Repository(true))
        {
            //Id property is the condition update
            repository.Customers.Update(new { Id = 1, Name = "test" });
            
            //Updates all with Status = true
            repository.Customers.Update(new { Status = false }, new { Status = true });
        }
    }

    public void Delete()
    {
        using (Repository repository = new Repository(true))
        {
            //Delete all rows
            repository.Customers.Delete();

            //Delete all with Status = true
            repository.Customers.Delete(new { Status = true });
        }
    }

    public void Query()
    {
        using (Repository repository = new Repository(true))
        {
            //Run query with Dapper
            var result = (from o in repository.Customers
                          where o.Name == "test"
                          select o.Name).ToDapper();
        }
    }
}
