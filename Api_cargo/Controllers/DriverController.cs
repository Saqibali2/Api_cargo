using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Transactions;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class DriverController : ApiController
    {
        CargoConnectEntities4 db = new CargoConnectEntities4();

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
            var rawData = db.Requests
                .Where(r => r.driver_id == id && r.status == "Pending")
                .Join(db.Shipments,
                    r => r.shipment_id,
                    s => s.shipment_id,
                    (r, s) => new
                    {
                        r.request_id,
                        r.shipment_id,
                        r.fare,  

                        s.pickup_address,
                        s.delivery_address,
                        s.total_weight,
                        s.package_count,
                        s.pickup_date,

                        s.sender_name,
                        s.sender_contact
                    })
                .ToList();

            var data = rawData.Select(x => new
            {
                x.request_id,
                x.shipment_id,

                Title = "Shipment #" + x.shipment_id,

                PickupCity = x.pickup_address != null
                    ? x.pickup_address.Split(',')[0]
                    : "N/A",

                DestinationCity = x.delivery_address != null
                    ? x.delivery_address.Split(',')[0]
                    : "N/A",

                Weight = x.total_weight ?? 0,
                PackageCount = x.package_count ?? 0,

                PickupDate = x.pickup_date,

                CustomerName = x.sender_name,
                CustomerContact = x.sender_contact,

                Fare = x.fare ?? 0  
            }).ToList();

            return Ok(data);
        }
        [HttpPost]
        [Route("api/drivers/find")]
        public IHttpActionResult GetDriversByAvailability(int shipmentId)
        {
            try
            {
                var request = BuildRequestFromShipment(shipmentId);

                var routes = FilterRoutesByDate(request);

                var matchedRouteIds = MatchRoutesWithCheckpoints(routes, request);

                var result = BuildAvailableTruckDtos(matchedRouteIds, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        private List<AvailabilityDto> BuildAvailableTruckDtos(List<int> routeIds, AvailabilityDto request)
        {
            var result = new List<AvailabilityDto>();

            var routes = db.Routes
                .Where(r => routeIds.Contains(r.route_id))
                .ToList();

            var driverIds = routes.Select(r => r.driver_id).Distinct().ToList();

            var drivers = db.Driver
                .Where(d => driverIds.Contains(d.driver_id) && d.is_available == true)
                .ToList();

            var groupedRoutes = routes.GroupBy(r => r.driver_id);

            foreach (var group in groupedRoutes)
            {
                var driver = drivers.FirstOrDefault(d => d.driver_id == group.Key);
                if (driver == null) continue;

                var route = group.First();

                var checkpoints = db.Checkpoints
                    .Where(c => c.route_id == route.route_id)
                    .OrderBy(c => c.sequence_no)
                    .ToList();

                if (checkpoints.Count == 0) continue;

                var pickup = checkpoints.First();
                var drop = checkpoints.Last();

                double distance = CalculateDistance(
                    request.pickupLat,
                    request.pickupLong,
                    request.destLat,
                    request.destLong
                );

                double price = distance * 50;

                // 🔥 CAPACITY CHECK
                bool canTake = ApplyCapacityCheck(driver.driver_id, request.shipmentId);
                var ratingData = GetDriverRating(driver.driver_id);
                result.Add(new AvailabilityDto
                {
                    // ✅ COPY FROM REQUEST (THIS IS YOUR BUG FIX)
                    shipmentId = request.shipmentId,
                    pickupLat = request.pickupLat,
                    pickupLong = request.pickupLong,
                    destLat = request.destLat,
                    destLong = request.destLong,
                    requestedDate = request.requestedDate,
                    isStrict = request.isStrict,

                    // DRIVER DATA
                    DriverId = driver.driver_id,
                    DriverName = driver.first_name + " " + driver.last_name,
                    ContactNo = driver.contact_no,

                    TruckType = "General",

                    PickupCity = pickup.name ?? "Unknown",
                    DestinationCity = drop.name ?? "Unknown",

                    Price = Math.Round(price, 0),
                    IsFull = !canTake,
                    RouteId = route.route_id,
                    Distance = Math.Round(distance, 2),
                    Rating = ratingData.rating,
                    TotalReviews = ratingData.totalReviews,
                });
            }

            return result;
        }
        private (double rating, int totalReviews) GetDriverRating(int driverId)
        {
            // 🔴 get driver user_id
            var driver = db.Driver.FirstOrDefault(d => d.driver_id == driverId);
            if (driver == null) return (0, 0);

            int userId = driver.user_id;

            // 🔴 get reviews
            var reviews = db.Reviews
                .Where(r => r.target_user_id == userId)
                .ToList();

            if (reviews.Count == 0)
                return (0, 0);

            double avgRating = reviews.Average(r => (double)(r.rating ?? 0));
            int total = reviews.Count;

            return (Math.Round(avgRating, 1), total);
        }
        private bool ApplyCapacityCheck(int driverId, int shipmentId)
        {
            var vehicle = db.Vehicle.FirstOrDefault(v => v.driver_id == driverId);
            if (vehicle == null) return false;

            double maxWeight = vehicle.weight_capacity ?? 0;
            double maxVolume = (vehicle.length ?? 0) * (vehicle.width ?? 0) * (vehicle.height ?? 0);

            var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == shipmentId);
            if (shipment == null) return false;

            double newWeight = shipment.total_weight ?? 0;
            double newVolume = CalculateShipmentVolume(shipmentId);

            bool weightOk = newWeight <= maxWeight;
            bool volumeOk = newVolume <= maxVolume;

            return weightOk && volumeOk;
        }
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371;

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }
        private List<RouteSchedule> FilterRoutesByDate(AvailabilityDto request)
        {
            return db.RouteSchedule
                .ToList()
                .Where(rs =>
                {
                    if (rs.departureDate == null)
                        return false;

                    DateTime dep;
                    if (!DateTime.TryParse(rs.departureDate, out dep))
                        return false;

                    var routeDate = dep.Date;
                    var shipmentDate = request.requestedDate.Date;

                    if (request.isStrict)
                    {
                        return routeDate == shipmentDate;
                    }
                    else
                    {
                        return routeDate >= shipmentDate.AddDays(-2)
                            && routeDate <= shipmentDate.AddDays(2);
                    }

                }).ToList();
        }
        private List<int> MatchRoutesWithCheckpoints(List<RouteSchedule> routes, AvailabilityDto request)
        {
            const double MaxDistanceKm = 20.0;

            var routeIds = routes.Select(r => r.route_id).ToList();

            var checkpointsByRoute = db.Checkpoints
                .Where(c => c.route_id.HasValue && routeIds.Contains(c.route_id.Value))
                .ToList()
                .GroupBy(c => c.route_id.Value)
                .ToList();

            var matchedRouteIds = new List<int>();

            foreach (var group in checkpointsByRoute)
            {
                var checkpoints = group.OrderBy(c => c.sequence_no).ToList();

                int pickupIndex = -1;
                int dropIndex = -1;

                for (int i = 0; i < checkpoints.Count; i++)
                {
                    var cp = checkpoints[i];

                    if (cp.latitude.HasValue && cp.longitude.HasValue)
                    {
                        double lat = cp.latitude.Value;
                        double lon = cp.longitude.Value;

                        if (pickupIndex == -1 &&
                            CalculateDistance(request.pickupLat, request.pickupLong, lat, lon) <= MaxDistanceKm)
                        {
                            pickupIndex = i;
                        }

                        if (dropIndex == -1 &&
                            CalculateDistance(request.destLat, request.destLong, lat, lon) <= MaxDistanceKm)
                        {
                            dropIndex = i;
                        }
                    }
                }

                if (pickupIndex != -1 && dropIndex != -1 && pickupIndex < dropIndex)
                {
                    matchedRouteIds.Add(group.Key);
                }
            }

            return matchedRouteIds;
        }
        private double ToRadians(double angle)
        {
            return angle * Math.PI / 180;
        }
        private double CalculateShipmentVolume(int shipmentId)
        {
            var packages = db.Packages
                .Where(p => p.shipment_id == shipmentId)
                .ToList();

            double totalVolume = 0;

            foreach (var p in packages)
            {
                double volume = (p.length ?? 0) * (p.width ?? 0) * (p.height ?? 0);

                totalVolume += volume * (p.quantity ?? 1);
            }

            return totalVolume;
        }
        private AvailabilityDto BuildRequestFromShipment(int shipmentId)
        {
            var shipment = db.Shipments
                .FirstOrDefault(s => s.shipment_id == shipmentId);

            if (shipment == null)
                throw new Exception("Shipment not found");

            if (shipment.pickup_lat == null || shipment.delivery_lat == null)
                throw new Exception("Shipment location missing");

            return new AvailabilityDto
            {
                shipmentId = shipmentId,
                pickupLat = shipment.pickup_lat.Value,
                pickupLong = shipment.pickup_long.Value,
                destLat = shipment.delivery_lat.Value,
                destLong = shipment.delivery_long.Value,
                requestedDate = DateTime.Now,
                isStrict = false
            };
        }
        ////******************************************************************//
        //[HttpPost]
        //[Route("api/drivers/find")]
        //public IHttpActionResult GetDriversByAvailability(AvailabilityDto request)
        //{
        //    try
        //    {
        //        const double MaxDistanceKm = 20.0;

        //        var activeRouteIds = db.RouteSchedule
        //            .ToList()
        //            .Where(rs =>
        //            {
        //                if (string.IsNullOrEmpty(rs.departureDate) || string.IsNullOrEmpty(rs.arrivalDate))
        //                    return false;

        //                DateTime dep, arr;
        //                if (DateTime.TryParse(rs.departureDate.Trim(), out dep) &&
        //                    DateTime.TryParse(rs.arrivalDate.Trim(), out arr))
        //                {
        //                    return request.requestedDate.Date >= dep.Date && request.requestedDate.Date <= arr.Date;
        //                }
        //                return false;
        //            })
        //            .Select(rs => rs.route_id)
        //            .Distinct()
        //            .ToList();

        //        if (!activeRouteIds.Any())
        //            return Ok(new List<object>());

        //        var checkpointsByRoute = db.Checkpoints
        //            .Where(c => c.route_id.HasValue &&
        //    activeRouteIds.Contains(c.route_id.Value))
        //        .ToList()
        //        .GroupBy(c => c.route_id)
        //        .ToList();

        //    var matchingDriverIds = new HashSet<int>();

        //        foreach (var routeGroup in checkpointsByRoute)
        //        {
        //            var checkpoints = routeGroup.OrderBy(c => c.sequence_no).ToList();
        //            bool pMatch = false;
        //            bool dMatch = false;

        //            foreach (var cp in checkpoints)
        //            {
        //                if (cp.latitude.HasValue && cp.longitude.HasValue)
        //                {
        //                    double lat = cp.latitude.Value;
        //                    double lon = cp.longitude.Value;

        //                    if (!pMatch && CalculateDistance(request.pickupLat, request.pickupLong, lat, lon) <= MaxDistanceKm)
        //                        pMatch = true;

        //                    if (!dMatch && CalculateDistance(request.destLat, request.destLong, lat, lon) <= MaxDistanceKm)
        //                        dMatch = true;
        //                }
        //            }

        //            if (pMatch && dMatch)
        //            {
        //                var route = db.Routes.FirstOrDefault(r => r.route_id == routeGroup.Key);
        //                if (route != null)
        //                    matchingDriverIds.Add(route.driver_id);
        //            }
        //        }

        //        var drivers = db.Driver
        //            .Where(d => matchingDriverIds.Contains(d.driver_id) && d.is_available == true)
        //            .Select(d => new
        //            {
        //                d.driver_id,
        //                d.user_id,
        //                d.first_name,
        //                d.last_name,
        //                d.CNIC,
        //                d.contact_no,
        //                d.licence_no,
        //                d.street_no,
        //                d.city,
        //                d.profile_image_url,
        //                d.is_available
        //            })
        //            .ToList();

        //        return Ok(drivers);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Content(System.Net.HttpStatusCode.InternalServerError, ex.Message);
        //    }
        //}

        //private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        //    {
        //        const double R = 6371;
        //        var dLat = ToRadians(lat2 - lat1);
        //        var dLon = ToRadians(lon2 - lon1);
        //        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
        //                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
        //                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        //        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        //        return R * c;
        //    }




        //private double ToRadians(double deg) => deg * (Math.PI / 180);

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


        /*---------------------------*/
        [HttpPost]
        [Route("api/request/send")]
        public IHttpActionResult SendRequest(int shipmentId, int driverId, int routeId)
        {
            var exists = db.Requests.FirstOrDefault(r =>
                r.shipment_id == shipmentId &&
                r.driver_id == driverId &&
                r.status == "Pending"
            );

            if (exists != null)
                return Ok("Already sent");

            var request = new Requests
            {
                shipment_id = shipmentId,
                driver_id = driverId,
                status = "Pending",
                 route_id = routeId,
            };

            db.Requests.Add(request);
            db.SaveChanges();

            return Ok("Request sent");
        }

        [HttpPost]
        [Route("api/requests/decline")]
        public IHttpActionResult DeclineRequest(int requestId)
        {
            var request = db.Requests.FirstOrDefault(r => r.request_id == requestId);

            if (request == null)
                return NotFound();

            request.status = "Declined";

            db.SaveChanges();

            return Ok(new { message = "Request declined" });
        }



[HttpPost]
    [Route("api/drivers/accept-request")]
    public IHttpActionResult AcceptRequest(int requestId)
    {
        using (var scope = new TransactionScope())
        {
            try
            {
      
                var request = db.Requests.FirstOrDefault(r => r.request_id == requestId);

                if (request == null)
                    return BadRequest("Request not found");

                if (request.status != "Pending")
                    return BadRequest("Request already processed");

                int shipmentId = request.shipment_id;
                int driverId = request.driver_id;

                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == shipmentId);

                if (shipment == null)
                    return BadRequest("Shipment not found");

                request.status = "Accepted";

                var otherRequests = db.Requests
                    .Where(r => r.shipment_id == shipmentId && r.request_id != requestId)
                    .ToList();

                foreach (var r in otherRequests)
                {
                    r.status = "Rejected";
                }

                shipment.status = "Assigned";

         
                var vehicleRegNo = db.Vehicle
                    .Where(v => v.driver_id == driverId)
                    .Select(v => v.vehicle_reg_no)
                    .FirstOrDefault();

         
                if (vehicleRegNo == null)
                    return BadRequest("Driver vehicle not found");

                var routeId = db.Routes
                    .Where(rt => rt.driver_id == driverId)
                    .Select(rt => rt.route_id)
                    .FirstOrDefault();

         
                if (routeId == 0)
                    return BadRequest("Driver route not found");

           
                var trip = new Trips
                {
                    driver_id = driverId,
                    vehicle_reg_no = vehicleRegNo,
                    route_id = routeId,
                    start_time = null,
                    end_time = null,
                    status = "Scheduled"
                };

                db.Trips.Add(trip);
                db.SaveChanges();

 
                var booking = new Bookings
                {
                    shipment_id = shipmentId,
                    customer_id = shipment.customer_id,
                    route_id = routeId,
                    trip_id = trip.trip_id,
                    status = "Assigned",
                    amount = request.fare ?? 0,
                    booking_type = shipment.strict == true ? "Private" : "Shared",
                    pickup_date = shipment.pickup_date,
                    created_at = DateTime.Now
                };

                db.Bookings.Add(booking);

                db.SaveChanges();

     
                scope.Complete();

                return Ok(new
                {
                    message = "Request accepted successfully",
                    tripId = trip.trip_id,
                    bookingId = booking.booking_id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
}
