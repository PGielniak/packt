using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using NorthwindWeb;
using System.Linq;
using Packt.Shared;
using Microsoft.AspNetCore.Mvc;

namespace NorthwindWeb.Pages
{
    public class SuppliersModel : PageModel
    {
        private Northwind db;

        /* #region Constructor */
        public SuppliersModel(Northwind injectedContext)
                {
                    db=injectedContext;
                }

        /* #endregion */
 
        /* #region Properties */
            public IEnumerable<string> Suppliers{get;set;}

                [BindProperty]
                public Supplier Supplier{get;set;}
        /* #endregion */
       
        

        public void OnGet()
        {
            ViewData["Title"] = "Northwind Website - Suppliers";

            Suppliers= db.Suppliers.Select(s=>s.CompanyName);
        }

        public IActionResult OnPost()
        {
            if(ModelState.IsValid)
            {
                db.Suppliers.Add(Supplier);
                db.SaveChanges();
                return RedirectToPage("/suppliers");
            }

            return Page();
        }

    }
}