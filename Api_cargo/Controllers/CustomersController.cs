using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class CustomersController : ApiController
    {
        CargoConnectEntities2 db = new CargoConnectEntities2();

        [HttpGet]
        [Route("api/customers/status")]
        public IHttpActionResult GetCustomersStatus()
        {
            return Ok("SUCCESS: Customers connection successful.");
        }

        [HttpPost]
        [Route("api/customers/create")]
        public IHttpActionResult CreateCustomer(Customer customer)
        {
            if (customer == null)
                return BadRequest("ERROR: Invalid customer data.");

            db.Customer.Add(customer);
            db.SaveChanges();

            return Ok("SUCCESS: Customer created successfully.");
        }

        [HttpGet]
        [Route("api/customers/{id}")]
        public IHttpActionResult GetCustomerById(int id)
        {
            var customer = db.Customer.FirstOrDefault(c => c.customer_id == id);
            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        [HttpPut]
        [Route("api/customers/update/{id}")]
        public IHttpActionResult UpdateCustomer(int id, Customer customer)
        {
            var updatecus = db.Customer.FirstOrDefault(c => c.customer_id == id);
            if (updatecus == null)
                return NotFound();

            updatecus.first_name = customer.first_name;
            updatecus.last_name = customer.last_name;
            updatecus.CNIC = customer.CNIC;
            updatecus.contact_no = customer.contact_no;
            updatecus.street_no = customer.street_no;
            updatecus.city = customer.city;
            updatecus.profile_image_url = customer.profile_image_url;

            db.SaveChanges();
            return Ok("Customer updated successfully.");
        }
        [HttpDelete]
        [Route("api/customers/delete/{id}")]
        public IHttpActionResult DeleteCustomer(int id)
        {
            var customer = db.Customer.Where(c => c.customer_id == id).FirstOrDefault();
            if (customer == null) return NotFound();

            db.Customer.Remove(customer);
            db.SaveChanges();
            return Ok("SUCCESS: Customer removed.");
        }
    }
}
