using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Packt.Shared;

namespace NorthwindService.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        #region Private Fields
            private static ConcurrentDictionary<string, Customer> customersCache;
        private Northwind db;
        #endregion
        
        #region Constructor
        
             public CustomerRepository(Northwind db)
        {
            this.db = db;

            if (customersCache==null)
            {
                customersCache=new ConcurrentDictionary<string, Customer>(
                    db.Customers.ToDictionary(c=>c.CustomerID)
                );
            }
        } 

        #endregion
       
        #region CreateAsync
              public async Task<Customer> CreateAsync(Customer c)
        {
            c.CustomerID=c.CustomerID.ToUpper();

            EntityEntry<Customer> added= await db.Customers.AddAsync(c);

            int affected= await db.SaveChangesAsync();

            if (affected==1)
            {
                return customersCache.AddOrUpdate(c.CustomerID, c, UpdateCache);
            }
            else
            {
                return null;
            }
        } 
        #endregion
     
        #region DeleteAsync
            public async Task<bool?> DeleteAsync(string id)
        {
            id=id.ToUpper();

            Customer c= db.Customers.Find(id);
            db.Customers.Remove(c);

            int affected= await db.SaveChangesAsync();

            if (affected==1)
            {
                return customersCache.TryRemove(id, out c);
            }
            else
            {
                return null;
            }
        }
        #endregion
        
        #region RetrieveAllAsync
            public Task<IEnumerable<Customer>> RetrieveAllAsync()
                {
                    return Task.Run<IEnumerable<Customer>>(()=>customersCache.Values);
                }
        #endregion
        
        #region RetrieveAsync
              public Task<Customer> RetrieveAsync(string id)
            {
            return Task.Run(()=>{
                id=id.ToUpper();
                Customer c;
                customersCache.TryGetValue(id, out c);
                return c;
            });
            }
        #endregion
      
        #region UpdateCache
                private Customer UpdateCache(string id, Customer c)
            {
                Customer old;

                if (customersCache.TryGetValue(id, out old))
                {
                    if (customersCache.TryUpdate(id,c,old))
                    {
                        return c;
                    }
                }
                return null;
            }
        #endregion
    
        #region UpdateAsync
               public async Task<Customer> UpdateAsync(string id, Customer c)
        {
            id=id.ToUpper();
            c.CustomerID=c.CustomerID.ToUpper();

            db.Customers.Update(c);
            int affected= await db.SaveChangesAsync();

            if (affected==1)
            {
                return UpdateCache(id,c);
            }
            return null;
        }
        #endregion
     
    }
}