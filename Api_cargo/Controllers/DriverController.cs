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

     
        [HttpPost]
        [Route("api/trucks/available")]
        public IHttpActionResult GetDriversByAvailability(int shipmentId)
        {
            try
            {
                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == shipmentId);
                if (shipment == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, "Shipment not found");

                if (shipment.pickup_lat == null || shipment.delivery_lat == null)
                    return Content(System.Net.HttpStatusCode.BadRequest, "Shipment location missing");

                var isStrict = shipment.strict ?? false;
                var radius = shipment.shipment_radius ?? 10;

                if (radius >= 100)
                    radius /= 1000;

                double pickupLat = shipment.pickup_lat.Value;
                double pickupLong = shipment.pickup_long.Value;
                double destLat = shipment.delivery_lat.Value;
                double destLong = shipment.delivery_long.Value;
                DateTime requestedDate = shipment.pickup_date ?? DateTime.Now;

                double MaxDistanceKm = radius;

                var allSchedules = db.RouteSchedule.ToList();
                var allCheckpoints = db.Checkpoints.ToList();

                var allRoutes = db.Routes
                    .Where(r => r.is_active == true || r.is_next_route == true)
                    .ToList();

                var matchedRouteIds = new List<int>();

                foreach (var route in allRoutes)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking route: {route.route_id}");
                    var checkpoints = allCheckpoints
                        .Where(c => c.route_id == route.route_id)
                        .OrderBy(c => c.sequence_no)
                        .ToList();

                    int pickupIndex = -1;
                    int dropIndex = -1;

                    for (int i = 0; i < checkpoints.Count; i++)
                    {
                        var cp = checkpoints[i];
                        if (cp.latitude == null || cp.longitude == null) continue;

                        if (pickupIndex == -1 &&
                            CalculateDistance(pickupLat, pickupLong, cp.latitude.Value, cp.longitude.Value) <= MaxDistanceKm)
                            pickupIndex = i;

                        if (dropIndex == -1 &&
                            CalculateDistance(destLat, destLong, cp.latitude.Value, cp.longitude.Value) <= MaxDistanceKm)
                            dropIndex = i;
                        System.Diagnostics.Debug.WriteLine($"Route {route.route_id} pickupIndex: {pickupIndex}, dropIndex: {dropIndex}");
                    }

                    if (pickupIndex == -1 || dropIndex == -1 || pickupIndex >= dropIndex)
                        continue;

                    var schedule = allSchedules.FirstOrDefault(s => s.route_id == route.route_id);
                    if (schedule == null) continue;

                    if (schedule.departureDate == null)
                        continue;

                    DateTime dep = schedule.departureDate.Value;

                    if (isStrict && requestedDate.Date != dep.Date)
                        continue;

                    if (!isStrict && dep.Date < requestedDate.Date)
                        continue;
                    System.Diagnostics.Debug.WriteLine($"Route {route.route_id} dep: {dep}, requestedDate: {requestedDate}");

                    matchedRouteIds.Add(route.route_id);
                }

                var result = BuildAvailableTruckDtos(matchedRouteIds, shipmentId, pickupLat, pickupLong, destLat, destLong, requestedDate, isStrict);
                return Ok(result.OrderByDescending(x => x.Rating).ToList());
            }
            catch (Exception ex)
            {
                return Content(System.Net.HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        private List<AvailabilityDto> BuildAvailableTruckDtos(List<int> routeIds, int shipmentId, double pickupLat, double pickupLong, double destLat, double destLong, DateTime requestedDate, bool isStrict)
        {
            var result = new List<AvailabilityDto>();

            var routes = db.Routes
                .Where(r => routeIds.Contains(r.route_id))
                .ToList();

            var driverIds = routes.Select(r => r.driver_id).Distinct().ToList();

            var drivers = db.Driver
                .Where(d => driverIds.Contains(d.driver_id) && d.is_available == true)
                .ToList();

            var vehicles = db.Vehicle
                .Where(v => driverIds.Contains(v.driver_id.Value))
                .ToList();

            double distance = CalculateDistance(pickupLat, pickupLong, destLat, destLong);
            double price = distance * 50;

            foreach (var route in routes)
            {
                var driver = drivers.FirstOrDefault(d => d.driver_id == route.driver_id);
                if (driver == null) continue;

                var vehicle = vehicles.FirstOrDefault(v => v.driver_id == route.driver_id);
                if (vehicle == null) continue;

                var checkpoints = db.Checkpoints
                    .Where(c => c.route_id == route.route_id)
                    .OrderBy(c => c.sequence_no)
                    .ToList();

                if (checkpoints.Count == 0) continue;

                var pickup = checkpoints.First();
                var drop = checkpoints.Last();

                var schedule = db.RouteSchedule.FirstOrDefault(s => s.route_id == route.route_id);

                var ratingData = GetDriverRating(driver.driver_id);

                bool canAccommodate = CanDriverAccommodateShipment(driver.driver_id, shipmentId, requestedDate);

                double totalCapacity = (vehicle.length ?? 0) * (vehicle.width ?? 0) * (vehicle.height ?? 0);

                result.Add(new AvailabilityDto
                {
                    shipmentId = shipmentId,
                    pickupLat = pickupLat,
                    pickupLong = pickupLong,
                    destLat = destLat,
                    destLong = destLong,
                    requestedDate = requestedDate,
                    isStrict = isStrict,

                    DriverId = driver.driver_id,
                    DriverName = driver.first_name + " " + driver.last_name,
                    ContactNo = driver.contact_no,

                    TruckModel = vehicle.model ?? "Unknown",
                    LicenseNo = driver.licence_no ?? "Unknown",
                    TotalCapacity = Math.Round(totalCapacity, 2),

                    PickupCity = pickup.name ?? "Unknown",
                    DestinationCity = drop.name ?? "Unknown",

                    Price = Math.Round(price, 0),
                    IsFull = !canAccommodate,
                    RouteId = route.route_id,
                    Distance = Math.Round(distance, 2),
                    Rating = ratingData.rating,
                    TotalReviews = ratingData.totalReviews,

                    DepartureDate = schedule?.departureDate,
                    ArrivalDate = schedule?.arrivalDate,
                });
            }

            return result;
        }

        private bool CanDriverAccommodateShipment(int driverId, int newShipmentId, DateTime requestedDate)
        {
            var vehicle = db.Vehicle.FirstOrDefault(v => v.driver_id == driverId);
            if (vehicle == null) return false;

            double maxWeight = vehicle.weight_capacity ?? 0;
            double maxVolume = (vehicle.length ?? 0) * (vehicle.width ?? 0) * (vehicle.height ?? 0);

            // Get BOTH active and next routes for this driver
            var driverRoutes = db.Routes
                .Where(r => r.driver_id == driverId && (r.is_active == true || r.is_next_route == true))
                .Select(r => r.route_id)
                .ToList();

            if (!driverRoutes.Any()) return false;

            // Sum bookings across both routes on the requested date
            var activeBookings = db.Bookings
                .Where(b =>
                    driverRoutes.Contains(b.route_id) &&
                    b.status == "Confirmed" &&
                    b.pickup_date <= requestedDate &&
                    b.delivery_date >= requestedDate)
                .ToList();

            double usedWeight = 0;
            double usedVolume = 0;

            foreach (var booking in activeBookings)
            {
                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == booking.shipment_id);
                if (shipment == null) continue;

                usedWeight += shipment.total_weight ?? 0;
                usedVolume += CalculateShipmentVolume(booking.shipment_id);
            }

            var newShipment = db.Shipments.FirstOrDefault(s => s.shipment_id == newShipmentId);
            if (newShipment == null) return false;

            double newWeight = newShipment.total_weight ?? 0;
            double newVolume = CalculateShipmentVolume(newShipmentId);

            bool weightOk = (usedWeight + newWeight) <= maxWeight;
            bool volumeOk = (usedVolume + newVolume) <= maxVolume;

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
        private (double rating, int totalReviews) GetDriverRating(int driverId)
        {
            var driver = db.Driver.FirstOrDefault(d => d.driver_id == driverId);
            if (driver == null) return (0, 0);

            int userId = driver.user_id;

            var reviews = db.Reviews
                .Where(r => r.target_user_id == userId)
                .ToList();

            if (reviews.Count == 0)
                return (0, 0);

            double avgRating = reviews.Average(r => (double)(r.rating ?? 0));
            int total = reviews.Count;

            return (Math.Round(avgRating, 1), total);
        }
        [HttpPost]
        [Route("api/request/send")]
        public IHttpActionResult SendRequest(int shipmentId, int driverId, int routeId, decimal fare)
        {

            var exists = db.Requests.FirstOrDefault(r =>
                r.shipment_id == shipmentId &&
                r.driver_id == driverId
            );

            if (exists != null)
                return Ok("Already sent");

            var request = new Requests
            {
                shipment_id = shipmentId,
                driver_id = driverId,
                route_id = routeId,
                fare = fare,         
                status = "Pending"  
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

        [HttpGet]
        [Route("api/drivers/{id}/requests/pending")]
        public IHttpActionResult GetDriverPendingRequests(int id)
        {
            return GetDriverRequestsByStatus(id, "pending");
        }

        [HttpGet]
        [Route("api/drivers/{id}/requests/accepted")]
        public IHttpActionResult GetDriverAcceptedRequests(int id)
        {
            return GetDriverRequestsByStatus(id, "accepted");
        }

        [HttpGet]
        [Route("api/drivers/{id}/requests/declined")]
        public IHttpActionResult GetDriverDeclinedRequests(int id)
        {
            return GetDriverRequestsByStatus(id, "declined");
        }

        private IHttpActionResult GetDriverRequestsByStatus(int id, string status)
        {
            var requests = db.Requests
                .Where(r => r.driver_id == id && r.status == status)
                .Select(r => new
                {
                    r.request_id,
                    r.shipment_id,
                    r.status,
                    r.route_id,   
                    r.fare,
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
    }
}
