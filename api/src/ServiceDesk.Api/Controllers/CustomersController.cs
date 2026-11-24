using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Api.Contracts;
using ServiceDesk.Api.Data;
using ServiceDesk.Api.Models;

namespace ServiceDesk.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(InMemoryStore store) : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Customer>> GetAll() => Ok(store.Customers);
        
        [HttpGet("{id:int}")]
        public ActionResult<Customer> GetById(int id)
        {
            var customer = store.Customers.FirstOrDefault(c => c.Id == id);
            return customer is null ? NotFound() : Ok(customer);
        }

        [HttpPost]
        public ActionResult<Customer> Create(CreateCustomerRequest request)
        {
            var customer = new Customer
            {
                Id = store.NextCustomerId(),
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone
            };
            store.Customers.Add(customer);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, UpdateCustomerRequest request)
        {
            var customer = store.Customers.FirstOrDefault(c => c.Id == id);
            if (customer is null) return NotFound();

            customer.Name = request.Name;
            customer.Email = request.Email;
            customer.Phone = request.Phone;

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var customer = store.Customers.FirstOrDefault(c => c.Id == id);
            if (customer is null) return NotFound();

            store.Customers.Remove(customer);
            return NoContent();
        }
    }
}
