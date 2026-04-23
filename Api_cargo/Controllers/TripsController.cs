using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
        public class TripsController : ApiController
        {
            private readonly CargoConnectEntities4 db = new CargoConnectEntities4();

            [HttpGet]
            [Route("api/trips/status")]
            public IHttpActionResult GetTripsStatus()
            {
                return Ok("SUCCESS: Trips connection successful.");
            }

        [HttpGet]
        [Route("api/bookings/{bookingId}")]
        public IHttpActionResult GetBookingById(int bookingId)
        {
            var booking = db.Bookings
                .Where(b => b.booking_id == bookingId)
                .Select(b => new
                {
                    id = b.booking_id,
                    status = b.status,
                    amount = b.amount,
                    bookingType = b.booking_type,
                    pickupDate = b.pickup_date,
                    shipmentId = b.shipment_id,
                    routeId = b.route_id,
                    tripId = b.trip_id
                })
                .FirstOrDefault();

            if (booking == null)
                return NotFound();

            // Shipment
            var shipment = db.Shipments
                .Where(s => s.shipment_id == booking.shipmentId)
                .Select(s => new
                {
                    senderName = s.sender_name,
                    senderContact = s.sender_contact,
                    pickupAddress = s.pickup_address,
                    deliveryAddress = s.delivery_address,
                    totalWeight = s.total_weight,
                    packageCount = s.package_count
                })
                .FirstOrDefault();

            // Route checkpoints
            var fromCheckpoint = db.Checkpoints
                .Where(c => c.route_id == booking.routeId)
                .OrderBy(c => c.sequence_no)
                .Select(c => c.name)
                .FirstOrDefault();

            var toCheckpoint = db.Checkpoints
                .Where(c => c.route_id == booking.routeId)
                .OrderByDescending(c => c.sequence_no)
                .Select(c => c.name)
                .FirstOrDefault();

            // Trip
            var tripStatus = db.Trips
                .Where(t => t.trip_id == booking.tripId)
                .Select(t => t.status)
                .FirstOrDefault();

            // Packages (NOW SAFE)
            var packages = db.Packages
                .Where(p => p.shipment_id == booking.shipmentId)
                .Select(p => new
                {
                    packageId = p.package_id,
                    name = p.name,
                    weight = p.weight,
                    length = p.length,
                    width = p.width,
                    height = p.height,
                    quantity = p.quantity,
                    color = p.color,
                    tagNo = p.tagNo
                })
                .ToList();

            return Ok(new
            {
                booking.id,
                booking.status,
                booking.amount,
                booking.bookingType,
                booking.pickupDate,
                fromCheckpoint,
                toCheckpoint,
                shipment,
                tripStatus,
                packages
            });
        }


        [HttpGet]
        [Route("api/customers/{customerId}/bookings")]
        public IHttpActionResult GetCustomerBookings(int customerId)
        {
            var bookings = (
                from b in db.Bookings
                join r in db.Routes on b.route_id equals r.route_id

                let fromCp = db.Checkpoints
                    .Where(c => c.route_id == r.route_id)
                    .OrderBy(c => c.sequence_no)
                    .FirstOrDefault()

                let toCp = db.Checkpoints
                    .Where(c => c.route_id == r.route_id)
                    .OrderByDescending(c => c.sequence_no)
                    .FirstOrDefault()

                where b.customer_id == customerId

                select new
                {
                    id = b.booking_id,              
                    status = b.status,              
                    amount = b.amount,              
                    bookingType = b.booking_type,   
                    fromCheckpoint = fromCp != null ? fromCp.name : null,
                    toCheckpoint = toCp != null ? toCp.name : null,    
                }
            ).ToList();

            return Ok(bookings);
        }

        [HttpPut]
            [Route("api/bookings/{id}/cancel")]
            public IHttpActionResult CancelBooking(int id, string reason)
            {
                var booking = db.Bookings.Find(id);
                if (booking == null) return NotFound();

                booking.status = "Cancelled";
                booking.cancel_reason = reason;
                booking.updated_at = DateTime.Now;

                db.SaveChanges();
                return Ok("SUCCESS: Booking Cancelled.");
            }

            [HttpGet]
            [Route("api/trips/active")]
            public IHttpActionResult GetActiveTrips()
            {
                var activeTrips = db.Trips.Where(t => t.status == "In Transit").ToList();
                return Ok(activeTrips);
            }

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/active")]
        public IHttpActionResult GetActiveBookings(int driverId)
        {
            var bookings = (
                from b in db.Bookings
                join t in db.Trips on b.trip_id equals t.trip_id
                where t.driver_id == driverId
                && (b.status == "Active" || b.status == "Confirmed")
                select new
                {
                    booking_id = b.booking_id,
                    shipment_id = b.shipment_id,
                    route_id = b.route_id,
                    trip_id = b.trip_id,
                    status = b.status,
                    amount = b.amount,
                    pickup_date = b.pickup_date
                }).ToList();

            return Ok(bookings);
        }

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/future")]
        public IHttpActionResult GetFutureBookings(int driverId)
        {
            var bookings = (
                from b in db.Bookings
                join t in db.Trips on b.trip_id equals t.trip_id
                where t.driver_id == driverId
                && b.pickup_date > DateTime.Today
                select new
                {
                    booking_id = b.booking_id,
                    shipment_id = b.shipment_id,
                    route_id = b.route_id,
                    trip_id = b.trip_id,
                    status = b.status,
                    amount = b.amount,
                    pickup_date = b.pickup_date
                }).ToList();

            return Ok(bookings);
        }

        [HttpPut]
        [Route("api/trips/{id}/stats")]
        public IHttpActionResult UpdateTripStats(int id, TripStats stats)
        {
            if (stats == null)
                return BadRequest("Stats data required");

            var existing = db.TripStats.FirstOrDefault(s => s.trip_id == id);

            if (existing == null)
                return NotFound();

            existing.weight = stats.weight;
            existing.length = stats.length;
            existing.width = stats.width;
            existing.height = stats.height;

            db.SaveChanges();

            return Ok("Trip stats updated");
        }

        [HttpGet]
        [Route("api/trips/{id}/stats")]
        public IHttpActionResult GetTripStats(int id)
        {
            var stats = db.TripStats
                .Where(s => s.trip_id == id)
                .Select(s => new
                {
                    s.trip_id,
                    s.weight,
                    s.length,
                    s.width,
                    s.height
                })
                .FirstOrDefault();

            if (stats == null)
                return NotFound();

            return Ok(stats);
        }

        [HttpPut]
            [Route("api/trips/checkpoints/{checkpointEventId}/reach")]
            public IHttpActionResult ReachCheckpoint(int checkpointEventId)
            {
                var cp = db.TripCheckpoints.Find(checkpointEventId);
                if (cp == null) return NotFound();

                cp.reached_at = DateTime.Now;
                db.SaveChanges();
                return Ok("SUCCESS: Checkpoint reached at " + cp.reached_at);
            }

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/upcoming")]
        public IHttpActionResult GetUpcomingPickups(int driverId)
        {
            var bookings = (
                from b in db.Bookings
                join t in db.Trips on b.trip_id equals t.trip_id
                where t.driver_id == driverId
                && b.pickup_date >= DateTime.Today
                && b.status != "Cancelled"
                orderby b.pickup_date
                select new
                {
                    booking_id = b.booking_id,
                    shipment_id = b.shipment_id,
                    route_id = b.route_id,
                    trip_id = b.trip_id,
                    status = b.status,
                    amount = b.amount,
                    pickup_date = b.pickup_date
                }).ToList();

            return Ok(bookings);
        }


        [HttpGet]
        [Route("api/trips/driver/{driverId}")]
        public IHttpActionResult GetDriverTrips(int driverId)
        {
            var trips = db.Trips
                .Where(t => t.driver_id == driverId)
                .OrderByDescending(t => t.start_time)
                .Select(t => new
                {
                    trip_id = t.trip_id,
                    route_id = t.route_id,
                    driver_id = t.driver_id,
                    status = t.status,
                    start_time = t.start_time,
                    end_time = t.end_time
                })
                .ToList();

            return Ok(trips);
        }


        [HttpPost]
            [Route("api/trips/{id}/checkpoint")]
            public IHttpActionResult AddTripCheckpoint(int id, TripCheckpoints checkpoint)
            {
                checkpoint.trip_id = id;
                checkpoint.reached_at = checkpoint.reached_at ?? DateTime.Now;
                db.TripCheckpoints.Add(checkpoint);
                db.SaveChanges();
                return Ok("Checkpoint added.");
            }


            [HttpGet]
            [Route("api/trips/{id}/delays")]
            public IHttpActionResult GetTripDelays(int id)
            {
                var delays = db.TripDelays.Where(d => d.trip_id == id).ToList();
                return Ok(delays);
            }
        [HttpPut]
        [Route("api/bookings/{id}/complete")]
        public IHttpActionResult CompleteBooking(int id)
        {
            var booking = db.Bookings.Find(id);

            if (booking == null)
                return NotFound();

            booking.status = "Completed";
            booking.updated_at = DateTime.Now;

            db.SaveChanges();

            return Ok("Delivery completed.");
        }
        [HttpPost]
            [Route("api/trips/start")]
            public IHttpActionResult StartTrip(Trips trip)
            {
                trip.start_time = DateTime.Now;
                trip.status = "In Transit";
                db.Trips.Add(trip);
                db.SaveChanges();
                return Ok(trip);
            }

        [HttpGet]
        [Route("api/trips/{id}/track")]
        public IHttpActionResult TrackTrip(int id)
        {
            var trip = db.Trips
                .Where(t => t.trip_id == id)
                .Select(t => new
                {
                    trip_id = t.trip_id,
                    route_id = t.route_id,
                    driver_id = t.driver_id,
                    status = t.status,
                    start_time = t.start_time,
                    end_time = t.end_time
                })
                .FirstOrDefault();

            if (trip == null)
                return NotFound();

            var lastCp = db.TripCheckpoints
     .Where(tc => tc.trip_id == id)
     .OrderByDescending(tc => tc.sequence_no)
     .Select(tc => new
     {
         checkpoint_event_id = tc.checkpoint_event_id,
         checkpoint_id = tc.checkpoint_id,
         sequence_no = tc.sequence_no,
         reached_at = tc.reached_at
     })
     .FirstOrDefault();


            var delays = db.TripDelays
                .Where(td => td.trip_id == id)
                .Select(td => new
                {
                    delay_id = td.delay_id,
                    reason = td.reason,
                    created_at = td.created_at
                })
                .ToList();

            return Ok(new
            {
                CurrentTrip = trip,
                LastReached = lastCp,
                ActiveDelays = delays
            });
        }


        [HttpPost]
            [Route("api/trips/{id}/report-delay")]
            public IHttpActionResult PostDelay(int id, TripDelays delay)
            {
                delay.trip_id = id;
                delay.created_at = DateTime.Now;
                db.TripDelays.Add(delay);
                db.SaveChanges();
                return Ok("Delay reported.");
            }

        [HttpGet]
        [Route("api/trips/{id}/bookings")]
        public IHttpActionResult GetBookingsByTrip(int id)
        {
            var bookings = db.Bookings
                .Where(b => b.trip_id == id)
                .Select(b => new
                {
                    booking_id = b.booking_id,
                    shipment_id = b.shipment_id,
                    status = b.status,
                    amount = b.amount,
                    pickup_date = b.pickup_date
                })
                .ToList();

            return Ok(bookings);
        }
        [HttpGet]
        [Route("api/routes/{routeId}/checkpoints")]
        public IHttpActionResult GetRouteCheckpoints(int routeId)
        {
            var checkpoints = db.Checkpoints
                .Where(c => c.route_id == routeId)
                .OrderBy(c => c.sequence_no)
                .Select(c => new
                {
                    c.checkpoint_id,
                    c.sequence_no,
                    c.name,
                    latitude = c.latitude,
                    longitude = c.longitude
                })
                .ToList();

            return Ok(checkpoints);
        }
        [HttpGet]
            [Route("api/trips")]
            public IHttpActionResult GetAllTrips()
            {
                var trips = db.Trips.Select(t => new
                {
                    t.trip_id,
                    t.route_id,
                    t.driver_id,
                    t.start_time,
                    t.end_time,
                }).ToList();

                return Ok(trips);
            }
        }
    }

