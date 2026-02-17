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
        CargoConnectEntities3 db = new CargoConnectEntities3();

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
                        s.delivery_lat,
                        s.delivery_long,
                        s.delivery_address,
                        s.pickup_lat,
                        s.pickup_long,
                        s.pickup_address,
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
            [HttpPost]
            [Route("api/drivers/find")]
            public IHttpActionResult GetDriversByAvailability(AvailabilityDto request)
            {
                try
                {
                    const double MaxDistanceKm = 20.0;

                    var activeRouteIds = db.RouteSchedule
                        .ToList()
                        .Where(rs =>
                        {
                            if (string.IsNullOrEmpty(rs.departureDate) || string.IsNullOrEmpty(rs.arrivalDate))
                                return false;

                            DateTime dep, arr;
                            if (DateTime.TryParse(rs.departureDate.Trim(), out dep) &&
                                DateTime.TryParse(rs.arrivalDate.Trim(), out arr))
                            {
                                return request.requestedDate.Date >= dep.Date && request.requestedDate.Date <= arr.Date;
                            }
                            return false;
                        })
                        .Select(rs => rs.route_id)
                        .Distinct()
                        .ToList();

                    if (!activeRouteIds.Any())
                        return Ok(new List<object>());

                    var checkpointsByRoute = db.Checkpoints
                        .Where(c => c.route_id.HasValue &&
                activeRouteIds.Contains(c.route_id.Value))
                    .ToList()
                    .GroupBy(c => c.route_id)
                    .ToList();

                var matchingDriverIds = new HashSet<int>();

                    foreach (var routeGroup in checkpointsByRoute)
                    {
                        var checkpoints = routeGroup.OrderBy(c => c.sequence_no).ToList();
                        bool pMatch = false;
                        bool dMatch = false;

                        foreach (var cp in checkpoints)
                        {
                            if (cp.latitude.HasValue && cp.longitude.HasValue)
                            {
                                double lat = cp.latitude.Value;
                                double lon = cp.longitude.Value;

                                if (!pMatch && CalculateDistance(request.pickupLat, request.pickupLong, lat, lon) <= MaxDistanceKm)
                                    pMatch = true;

                                if (!dMatch && CalculateDistance(request.destLat, request.destLong, lat, lon) <= MaxDistanceKm)
                                    dMatch = true;
                            }
                        }

                        if (pMatch && dMatch)
                        {
                            var route = db.Routes.FirstOrDefault(r => r.route_id == routeGroup.Key);
                            if (route != null)
                                matchingDriverIds.Add(route.driver_id);
                        }
                    }

                    var drivers = db.Driver
                        .Where(d => matchingDriverIds.Contains(d.driver_id) && d.is_available == true)
                        .Select(d => new
                        {
                            d.driver_id,
                            d.user_id,
                            d.first_name,
                            d.last_name,
                            d.CNIC,
                            d.contact_no,
                            d.licence_no,
                            d.street_no,
                            d.city,
                            d.profile_image_url,
                            d.is_available
                        })
                        .ToList();

                    return Ok(drivers);
                }
                catch (Exception ex)
                {
                    return Content(System.Net.HttpStatusCode.InternalServerError, ex.Message);
                }
            }

            private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
            {
                const double R = 6371;
                var dLat = ToRadians(lat2 - lat1);
                var dLon = ToRadians(lon2 - lon1);
                var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                        Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                        Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                return R * c;
            }

            private double ToRadians(double deg) => deg * (Math.PI / 180);

            /*
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

             [HttpDelete]
             [Route("api/drivers/delete/{id}")]
             public IHttpActionResult DeleteDriver(int id)
             {
                 var driver = db.Driver.FirstOrDefault(d => d.driver_id == id);
                 if (driver == null)
                 {
                     return NotFound();
                 }

                 var user = db.Users.FirstOrDefault(u => u.user_id == driver.user_id);
                 if (user == null)
                 {
                     return NotFound();
                 }

                 var requests = db.Requests.Where(d => d.driver_id == id);
                 db.Requests.RemoveRange(requests);

                 var bookings = db.Bookings.Where(t => t.trip_id == id);
                 db.Bookings.RemoveRange(bookings);

                 var trips = db.Trips.Where(t => t.driver_id == id);
                 db.Trips.RemoveRange(trips);

                 db.Driver.Remove(driver);
                 db.Users.Remove(user);

                 db.SaveChanges();
                 return Ok("SUCCESS: Driver deleted successfully.");
             }*/

        }
    }
