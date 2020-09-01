using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NorthwindService.Repositories;
using Packt.Shared;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace NorthwindService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController: ControllerBase
    {
        private ICustomerRepository repo;

        public CustomersController(ICustomerRepository repo)
        {
            this.repo=repo;
        }

        [HttpGet]
        [ProducesResponseType(200, Type= typeof(IEnumerable<Customer>))]
        public async Task<IEnumerable<Customer>> GetCustomers(string country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return await repo.RetrieveAllAsync();
            }
            else
            {
                return (await repo.RetrieveAllAsync()).Where(customer=>customer.Country==country);
            }
        }

        [HttpGet ("{id}", Name=nameof(GetCustomer))]
        [ProducesResponseType(200, Type=typeof(Customer))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCustomer(string id)
        {
            Customer c = await repo.RetrieveAsync(id);

            if (c==null)
            {
                return NotFound();
            }

            return Ok(c);
      
        }

        [HttpPost]
        [ProducesResponseType(201, Type=typeof(Customer))]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] Customer c)
        {
            if (c==null)
            {
                return BadRequest();    
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Customer added= await repo.CreateAsync(c);

            return CreatedAtRoute(
                routeName: nameof(GetCustomer),
                routeValues: new{id= added.CustomerID.ToLower()},
                value:added
            );
        }


        // PUT: api/customers/[id]
    // BODY: Customer (JSON, XML) 
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(
      string id, [FromBody] Customer c)
    {
      id = id.ToUpper();
      c.CustomerID = c.CustomerID.ToUpper();

      if (c == null || c.CustomerID != id)
      {
        return BadRequest(); // 400 Bad request
      }

      if (!ModelState.IsValid)
      {
        return BadRequest(ModelState); // 400 Bad request
      }

      var existing = await repo.RetrieveAsync(id);

      if (existing == null)
      {
        return NotFound(); // 404 Resource not found
      }

      await repo.UpdateAsync(id, c);

      return new NoContentResult(); // 204 No content
    }



        // DELETE: api/customers/[id] 
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
      if (id == "bad")
      {
        var problemDetails = new ProblemDetails
        {
          Status = StatusCodes.Status400BadRequest,
          Type = "https://localhost:5001/customers/failed-to-delete",
          Title = $"Customer ID {id} found but failed to delete.",
          Detail = "More details like Company Name, Country and so on.",
          Instance = HttpContext.Request.Path
        };
        return BadRequest(problemDetails); // 400 Bad request
      }

      var existing = await repo.RetrieveAsync(id);
      if (existing == null)
      {
        return NotFound(); // 404 Resource not found
      }

      bool? deleted = await repo.DeleteAsync(id);

      if (deleted.HasValue && deleted.Value) // short circuit AND
      {
        return new NoContentResult(); // 204 No content
      }
      else
      {
        return BadRequest( // 400 Bad request
          $"Customer {id} was found but failed to delete.");
      }
    }
    }
}