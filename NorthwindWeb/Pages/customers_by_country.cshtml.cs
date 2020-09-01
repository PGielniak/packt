using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using NorthwindWeb;
using System.Linq;
using Packt.Shared;
using Microsoft.AspNetCore.Mvc;

namespace NorthwindWeb.Pages
{
    public class CustomersModel : PageModel
    {
        private Northwind db;

        /* #region Constructor */
        public CustomersModel(Northwind injectedContext)
                {
                    db=injectedContext;
                }

        /* #endregion */
 
        /* #region Properties */
            public ILookup<string, Customer> Customers{get;set;}

               
        /* #endregion */
       
        

        public void OnGet()
        {
        
            ViewData["Title"] = "Northwind Website - Customers";

            Customers=db.Customers.OrderBy(s=>s.Country).ThenBy(s=>s.CompanyName).ToLookup(s=>s.Country);
        }

       

    }
}