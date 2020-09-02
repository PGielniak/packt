using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using NorthwindML.Models;
using Packt.Shared;

namespace NorthwindML.Controllers
{
    public class HomeController : Controller
    {
        private readonly static string datasetName = "dataset.txt";
        private readonly static string[] countries=
        new[]
        {"Germany", "UK", "USA"};
        private readonly ILogger<HomeController> _logger;
        private readonly Northwind db;
        private readonly IWebHostEnvironment webHostEnvironment;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = CreateHomeIndexViewModel();
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetDataPath(string file)
        {
            return Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot", "Data", file);
        }

        private HomeIndexViewModel CreateHomeIndexViewModel()
        {
            return new HomeIndexViewModel
            {
                Categories=db.Categories.Include(category=>category.Products),

                GermanyDataSetExists=System.IO.File.Exists(GetDataPath("germany-dataset.txt")),

                UKDataSetExists=System.IO.File.Exists(GetDataPath("uk-dataset.txt")),

                USADataSetExists=System.IO.File.Exists(GetDataPath("usa-dataset.txt")),
                
            };
        }
        
        public IActionResult GenerateDatasets()
        {
            foreach (string country in countries)
            {
                IEnumerable<Order> ordersInCountry= db.Orders
                .Where(order=>order.Customer.Country==country)
                .Include(order=>order.OrderDetails)
                .AsEnumerable();

                IEnumerable<ProductCoBought> coboughtProducts =
                ordersInCountry.SelectMany(order=>from lineItem1 in order.OrderDetails
                from lineItem2 in order.OrderDetails
                select new ProductCoBought
                {
                    ProductID=(uint)lineItem1.ProductID,
                    CoboughtProductID=(uint)lineItem2.ProductID
                })
                .Where(p=>p.ProductID!=p.CoboughtProductID)
                .GroupBy(p=>new{p.ProductID, p.CoboughtProductID})
                .Select(p=>p.FirstOrDefault())
                .OrderBy(p=>p.ProductID)
                .ThenBy(p=>p.CoboughtProductID);

                StreamWriter datasetFile= System.IO.File.CreateText(path: GetDataPath($"{country.ToLower()}-{datasetName}"));

               
                datasetFile.WriteLine("ProductID\tCoboughtProductID");

                 foreach (var item in coboughtProducts)
                {
                    datasetFile.WriteLine($"{item.ProductID}\t{item.CoboughtProductID}");
                }

                datasetFile.Close();
            }
            var model= CreateHomeIndexViewModel();
            return View("Index", model);
        }

         public IActionResult TrainModels()
    {
      var stopWatch = Stopwatch.StartNew();

      foreach (string country in countries)
      {
        var mlContext = new MLContext();

        IDataView dataView = mlContext.Data.LoadFromTextFile(
          path: GetDataPath($"{country}-{datasetName}"),
          columns: new[]
          {
            new TextLoader.Column(
              name:     "Label",
              dataKind: DataKind.Double,
              index:    0),

            // The key count is the cardinality i.e. maximum valid
            // value. This column is used internally when training
            // the model. When results are shown, the columns are
            // mapped to instances of our model which could have a
            // different cardinality but happen to have the same.
            new TextLoader.Column(
              name:     nameof(ProductCoBought.ProductID),
              dataKind: DataKind.UInt32,
              source:   new [] { new TextLoader.Range(0) },
              keyCount: new KeyCount(77)),

            new TextLoader.Column(
              name:     nameof(ProductCoBought.CoboughtProductID),
              dataKind: DataKind.UInt32,
              source:   new [] { new TextLoader.Range(1) },
              keyCount: new KeyCount(77))
            },
            hasHeader: true,
            separatorChar: '\t');

        var options = new MatrixFactorizationTrainer.Options
        {
          MatrixColumnIndexColumnName =
            nameof(ProductCoBought.ProductID),
          MatrixRowIndexColumnName =
            nameof(ProductCoBought.CoboughtProductID),
          LabelColumnName = "Label",
          LossFunction = MatrixFactorizationTrainer
            .LossFunctionType.SquareLossOneClass,
          Alpha = 0.01,
          Lambda = 0.025,
          C = 0.00001
        };

        MatrixFactorizationTrainer mft = mlContext.Recommendation()
          .Trainers.MatrixFactorization(options);

        ITransformer trainedModel = mft.Fit(dataView);

        mlContext.Model.Save(trainedModel,
          inputSchema: dataView.Schema,
          filePath: GetDataPath($"{country}-model.zip"));
      }

      stopWatch.Stop();

      var model = CreateHomeIndexViewModel();
      model.Milliseconds = stopWatch.ElapsedMilliseconds;

      return View("Index", model);
    }

     public IActionResult Cart(int? id)
    {
      // the current cart is stored as a cookie
      string cartCookie = Request.Cookies["nw_cart"] ?? string.Empty;

      // if visitor clicked Add to Cart button
      if (id.HasValue)
      {
        if (string.IsNullOrWhiteSpace(cartCookie))
        {
          cartCookie = id.ToString();
        }
        else
        {
          string[] ids = cartCookie.Split('-');

          if (!ids.Contains(id.ToString()))
          {
            cartCookie = string.Join('-', cartCookie, id.ToString());
          }
        }

        Response.Cookies.Append("nw_cart", cartCookie);
      }

      var model = new HomeCartModelView
      {
        Cart = new Cart
        {
          Items = Enumerable.Empty<CartItem>()
        },
        Recommendations = new List<EnrichedRecommendation>()
      };

      if (cartCookie.Length > 0)
      {
        model.Cart.Items = cartCookie.Split('-').Select(item =>
          new CartItem
          {
            ProductID = int.Parse(item),
            ProductName = db.Products.Find(
              int.Parse(item)).ProductName
          });
      }

      if (System.IO.File.Exists(GetDataPath("germany-model.zip")))
      {
        var mlContext = new MLContext();

        ITransformer modelGermany;

        using (var stream = new FileStream(
          path: GetDataPath("germany-model.zip"),
          mode: FileMode.Open,
          access: FileAccess.Read,
          share: FileShare.Read))
        {
          modelGermany = mlContext.Model.Load(stream, out DataViewSchema schema);
        }

        var predictionEngine = mlContext.Model.CreatePredictionEngine
          <ProductCoBought, Recommendation>(modelGermany);

        var products = db.Products.ToArray();

        foreach (var item in model.Cart.Items)
        {
          var topThree = products
            .Select(product =>
              predictionEngine.Predict(
                new ProductCoBought
                {
                  ProductID = (uint)item.ProductID,
                  CoboughtProductID = (uint)product.ProductID
                })
              ) // returns IEnumerable<Recommendation>
            .OrderByDescending(x => x.Score)
            .Take(3)
            .ToArray();

          model.Recommendations.AddRange(topThree
            .Select(rec => new EnrichedRecommendation
            {
              CoboughtProductID = rec.CoboughtProductID,
              Score = rec.Score,
              ProductName = db.Products.Find(
                (int)rec.CoboughtProductID).ProductName
            }));
        }

        // show the best three product recommendations
        model.Recommendations = model.Recommendations
          .OrderByDescending(rec => rec.Score)
          .Take(3)
          .ToList();
      }

      return View(model);
    }

    }
}
