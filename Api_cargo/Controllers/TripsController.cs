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
            private readonly CargoConnectEntities3 db = new CargoConnectEntities3();

            [HttpGet]
            [Route("api/trips/status")]
            public IHttpActionResult GetTripsStatus()
            {
                return Ok("SUCCESS: Trips connection successful.");
            }

            [HttpGet]
            [Route("api/trucks/{regNo}/shipments")]
            public IHttpActionResult GetShipmentsByTruck(string regNo)
            {
                var shipments = db.Trips
                    .Where(t => t.vehicle_reg_no == regNo && t.status == "In Transit")
                    .Select(t => t.shipment_id)
                    .ToList();
                return Ok(shipments);
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
            [Route("api/bookings/active")]
            public IHttpActionResult GetActiveBookings()
            {
                var activeBookings = db.Bookings.Where(b => b.status == "Active" || b.status == "Confirmed").ToList();
                return Ok(activeBookings);
            }

            [HttpGet]
            [Route("api/bookings/future")]
            public IHttpActionResult GetFutureBookings()
            {
                var future = db.Bookings.Where(b => b.pickup_date > DateTime.Today).ToList();
                return Ok(future);
            }

            [HttpPut]
            [Route("api/trips/{id}/stats")]
            public IHttpActionResult UpdateTripStats(int id, TripStats stats)
            {
                var existing = db.TripStats.FirstOrDefault(s => s.trip_id == id);
                if (existing == null) return NotFound();

                existing.weight = stats.weight;
                existing.length = stats.length;
                existing.width = stats.width;
                existing.height = stats.height;

                db.SaveChanges();
                return Ok("SUCCESS: Trip Stats Updated.");
            }

            [HttpGet]
            [Route("api/trips/{id}/stats")]
            public IHttpActionResult GetTripStats(int id)
            {
                var stats = db.TripStats.FirstOrDefault(s => s.trip_id == id);
                if (stats == null) return NotFound();
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
            [Route("api/bookings/upcoming-pickups")]
            public IHttpActionResult GetUpcomingPickups()
            {
                var upcoming = db.Bookings
                    .Where(b => b.pickup_date >= DateTime.Today && b.status != "Cancelled")
                    .OrderBy(b => b.pickup_date)
                    .ToList();

                return Ok(upcoming);
            }


            [HttpGet]
            [Route("api/trips/driver/{driverId}")]
            public IHttpActionResult GetDriverTrips(int driverId)
            {
                var trips = db.Trips.Where(t => t.driver_id == driverId).OrderByDescending(t => t.start_time).ToList();
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
                var trip = db.Trips.Find(id);
                if (trip == null) return NotFound();
                var lastCp = db.TripCheckpoints.Where(tc => tc.trip_id == id).OrderByDescending(tc => tc.sequence_no).FirstOrDefault();
                var delays = db.TripDelays.Where(td => td.trip_id == id).ToList();
                return Ok(new { CurrentTrip = trip, LastReached = lastCp, ActiveDelays = delays });
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
            [Route("api/trips/{id}/booking")]
            public IHttpActionResult GetBookingByTrip(int id)
            {
                var booking = db.Bookings.FirstOrDefault(b => b.trip_id == id);
                return booking == null ? (IHttpActionResult)NotFound() : Ok(booking);
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

