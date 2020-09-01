using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NorthwindMvc.Models;
using Packt.Shared;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;


namespace NorthwindMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private Northwind db;

        private readonly IHttpClientFactory clientFactory;

        public HomeController(ILogger<HomeController> logger, Northwind injectedContext, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            db=injectedContext;
            clientFactory=httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var model= new HomeIndexViewModel
            {
                VisitorCount=(new Random()).Next(1,1001),
                Categories= await db.Categories.ToListAsync(),
                Products= await db.Products.ToListAsync()
            };

            return View(model);
            
        }

        [Route("private")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> ProductDetail(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound("You must pass a product ID in the route");
            }

            var model= await db.Products.SingleOrDefaultAsync(p=>p.ProductID==id);

            if (model is null)
            {
                return NotFound($"Product with id {id} not found");
            }

            return View(model);
        }

         public async Task<IActionResult> CategoryDetail(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound("You must pass a category ID in the route");
            }

            var model= await db.Categories.Include(p=>p.Products.Select(p=>p.ProductName))
            .SingleOrDefaultAsync(p=>p.CategoryID==id);

            if (model is null)
            {
                return NotFound($"Category with id {id} not found");
            }

            return View(model);
        }


    
        public IActionResult ModelBinding()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ModelBinding(Thing thing)
        {
           var model=new HomeModelBindingViewModel
           {
               Thing = thing,
               HasErrors= !ModelState.IsValid,
               ValidationErrors=ModelState.Values.SelectMany(state=>state.Errors).Select(error=>error.ErrorMessage)
           };

           return View(model);
        }

        public IActionResult ProductsThatCostMoreThan(decimal? price)
        {
            if (!price.HasValue)
            {
                return NotFound("You must pass a product price in the query string");
            }

            IEnumerable<Product> model= db.Products
            .Include(p=>p.Category)
            .Include(p=>p.Supplier)
            .AsEnumerable()
            .Where(p=>p.UnitPrice>price);

            if (model.Count()==0)
            {
                return NotFound($"No products cost more than {price:C}");
            }

            ViewData["MaxPrice"]= price.Value.ToString("C");

            return View(model);
        }

        public async Task<IActionResult> Customers(string country)
        {

            string uri; 
            
            if (string.IsNullOrEmpty(country))
            {
                ViewData["Title"]= "All customers worldwide";
                uri="api/customers/";
            }
            else
            {
                ViewData["Title"]=$"Customers in {country}";
                uri=$"api/customers/?country={country}";
            }

            var client = clientFactory.CreateClient(name: "NorthwindService");

            var request= new HttpRequestMessage(method: HttpMethod.Get, requestUri: uri);

            HttpResponseMessage responseMessage= await client.SendAsync(request);

            string jsonString= await responseMessage.Content.ReadAsStringAsync();

            IEnumerable<Customer> model= JsonConvert.DeserializeObject<IEnumerable<Customer>>(jsonString);

            return View(model);
        }

        public async Task<IActionResult> CreateCustomer([FromForm] Customer c)
        {
            string uri = $"api/customers";

            

            string jsonString=JsonConvert.SerializeObject(c);

            var client=clientFactory.CreateClient(name: "NorthwindService");
            var request= new HttpRequestMessage(method: HttpMethod.Post, requestUri: uri);
            request.Content= new StringContent(jsonString, Encoding.UTF8,"application/json");

            HttpResponseMessage responseMessage= await client.SendAsync(request);

            return RedirectToAction(nameof(Index));

        }

        public async Task<IActionResult> DeleteCustomer(string id)
        {
            string uri = $"api/customers/{id}";

             var client=clientFactory.CreateClient(name: "NorthwindService");
            var request= new HttpRequestMessage(method: HttpMethod.Delete, requestUri: uri);

            HttpResponseMessage responseMessage= await client.SendAsync(request);

            return RedirectToAction(nameof(Index));

        }
    }
}
