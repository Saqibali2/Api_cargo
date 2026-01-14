using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class DriverController : ApiController
    {
        CargoConnectEntities2 db = new CargoConnectEntities2();
        [HttpGet]
        [Route("api/drivers/status")]
        public IHttpActionResult GetDriverStatus()
        {
            return Ok("SUCCESS: Driver connection successful.");
        }
        [HttpGet]
        [Route("api/drivers")]
        public IHttpActionResult GetAllDrivers()
        {
            var drivers = db.Driver.Select(d => new
            {
                d.driver_id,
                d.user_id,
                d.first_name,
                d.last_name,
                d.CNIC,
                d.contact_no,
                d.licence_no,
                d.city,
                d.street_no,
                d.profile_image_url,
                d.is_available,

            }).ToList();
            return Ok(drivers);
        }
        [HttpGet]
        [Route("api/drivers/{id}")]
        public IHttpActionResult GetDriver(int id)
        {
            var result = db.Driver.Select(d => new
            {
                d.driver_id,
                d.user_id,
                d.first_name,
                d.last_name,
                d.CNIC,
                d.contact_no,
                d.licence_no,
                d.city,
                d.street_no,
                d.profile_image_url,
                d.is_available,

            }).Where(d => d.driver_id == id).FirstOrDefault(d => d.driver_id == id);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/drivers/byuserid/{userID}")]
        public IHttpActionResult GetDriverByUserId(int userId)
        {
            var result = db.Driver.Select(d => new
            {
                d.driver_id,
                d.user_id,
                d.first_name,
                d.last_name,
                d.CNIC,
                d.contact_no,
                d.licence_no,
                d.city,
                d.street_no,
                d.profile_image_url,
                d.is_available,

            }).Where(d => d.user_id == userId).Select(d => new
            {
                d.driver_id,
                d.user_id,
                d.first_name,
                d.last_name,
                d.CNIC,
                d.contact_no,
                d.licence_no,
                d.city,
                d.street_no,
                d.profile_image_url,
                d.is_available,

            }).FirstOrDefault(d => d.user_id == userId);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }

        [HttpGet]
        [Route("api/drivers/{id}/requests")]
        public IHttpActionResult GetDriverRequests(int id)
        {
            var requests = db.Requests
                .Where(r => r.driver_id == id)
                .Select(r => new
                {
                    r.request_id,
                    r.shipment_id,
                    r.status
                })
                .ToList();

            var shipmentIds = requests.Select(r => r.shipment_id).ToList();

            var shipments = db.Shipments
                .Where(s => shipmentIds.Contains(s.shipment_id))
                .Select(s => new
                {
                    s.shipment_id,
                    s.sender_name,
                    s.sender_contact,
                    s.pickup_checkpoint,
                    s.delivery_checkpoint,
                    s.customer_id,
                    s.package_count,
                    s.total_weight
                })
                .ToList();

            return Ok(new
            {
                requestsData = requests,
                totalRequests = requests.Count,
                shipmentData = shipments
            });
        }

        //******************************************************************//
        [HttpGet]
        [Route("api/drivers/find")]
        public IHttpActionResult GetDriversByAvailability(int fromCheckpointId, int toCheckpointId, DateTime departureDate)
        {
            var routeIds = db.RouteCheckpoints
                .Where(rc => rc.checkpoint_id == fromCheckpointId
                          || rc.checkpoint_id == toCheckpointId)
                .GroupBy(rc => rc.route_id)
                .Where(g => g.Select(x => x.checkpoint_id).Distinct().Count() == 2)
                .Select(g => g.Key)
                .ToList();

            DateTime startDate = departureDate.Date;
            DateTime endDate = startDate.AddDays(1);

            var drivers = db.ActiveRoute
                .Where(ar => routeIds.Contains(ar.route_id)
                          && ar.departure_date >= startDate
                          && ar.departure_date < endDate)
                .Select(ar => new
                {
                    ar.Driver.driver_id,
                    ar.Driver.first_name,
                    ar.Driver.last_name,
                    ar.Driver.contact_no,
                    ar.Driver.profile_image_url,
                    ar.Driver.is_available,
                    ar.Driver.city,
                    ar.Driver.street_no
                })
                .Distinct()
                .ToList();

            return Ok(drivers);
        }


        [HttpPut]
        [Route("api/drivers/update/{id}")]
        public IHttpActionResult UpdateDriver(int id, Driver updatedDriver)
        {
            var driver = db.Driver.FirstOrDefault(d => d.driver_id == id);
            if (driver == null)
            {
                return NotFound();
            }
            driver.first_name = updatedDriver.first_name;
            driver.last_name = updatedDriver.last_name;
            driver.CNIC = updatedDriver.CNIC;
            driver.contact_no = updatedDriver.contact_no;
            driver.licence_no = updatedDriver.licence_no;
            driver.city = updatedDriver.city;
            driver.street_no = updatedDriver.street_no;
            driver.profile_image_url = updatedDriver.profile_image_url;
            driver.is_available = updatedDriver.is_available;
            db.SaveChanges();
            return Ok("SUCCESS: Driver information updated successfully.");
        }

       
    }
}
