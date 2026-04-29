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

     join r in db.Routes
         on b.route_id equals r.route_id into routeGroup
     from r in routeGroup.DefaultIfEmpty()

     let fromCp = db.Checkpoints
         .Where(c => r != null && c.route_id == r.route_id)
         .OrderBy(c => c.sequence_no)
         .FirstOrDefault()

     let toCp = db.Checkpoints
         .Where(c => r != null && c.route_id == r.route_id)
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
        [HttpGet]
        [Route("api/customers/{customerId}/pending-shipments-count")]
        public IHttpActionResult GetPendingShipmentsCount(int customerId)
        {
            var count = db.Shipments
                .Where(s => s.customer_id == customerId && s.status == "Pending")
                .Count();

            return Ok(count);
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


     

        [HttpGet]
        [Route("api/checkpoints/{checkpointId}/shipments")]
        public IHttpActionResult GetShipmentsAtCheckpoint(int checkpointId, int driverId)
        {
  
            var checkpoint = db.Checkpoints.FirstOrDefault(c => c.checkpoint_id == checkpointId);
            if (checkpoint == null)
                return BadRequest("Checkpoint not found");

            int routeId = checkpoint.route_id ?? 0;


            var route = db.Routes.FirstOrDefault(r => r.route_id == routeId && r.driver_id == driverId);
            if (route == null)
                return BadRequest("This checkpoint does not belong to the driver's route");


            var bookingsData = (
                from b in db.Bookings
                join t in db.Trips on b.trip_id equals t.trip_id
                join s in db.Shipments on b.shipment_id equals s.shipment_id
                join rd in db.RecipientDetails
                    on b.shipment_id equals rd.shipment_id into recipientGroup
                from r in recipientGroup.DefaultIfEmpty()
                where b.route_id == routeId
                   && t.driver_id == driverId
                   && (b.status == "Assigned" || b.status == "In-Transit")
                   && s.delivery_lat != null
                   && s.delivery_long != null
                   && s.pickup_lat != null
                   && s.pickup_long != null
                select new
                {
                    b.booking_id,
                    b.shipment_id,
                    b.status,
                    b.amount,
                    b.pickup_date,

                    // Shipment
                    s.pickup_address,
                    s.pickup_lat,
                    s.pickup_long,
                    s.delivery_address,
                    s.delivery_lat,
                    s.delivery_long,
                    s.sender_name,
                    s.sender_contact,
                    s.total_weight,
                    s.package_count,

                    // Recipient
                    recipient_name = r != null ? r.recipient_fname + " " + r.recipient_lname : null,
                    recipient_contact = r != null ? r.recipient_contact : null
                }
            ).ToList(); // ✅ only here

            // 4. Fetch all packages in ONE query (no N+1 problem)
            var shipmentIds = bookingsData.Select(x => x.shipment_id).Distinct().ToList();

            var allPackages = db.Packages
                .Where(p => shipmentIds.Contains(p.shipment_id))
                .Select(p => new
                {
                    p.shipment_id,
                    p.name,
                    p.weight,
                    p.length,
                    p.width,
                    p.height,
                    p.quantity,
                    p.color,
                    p.tagNo
                })
                .ToList();

            // 5. Merge packages into bookings
            var allBookings = bookingsData.Select(b => new
            {
                b.booking_id,
                b.shipment_id,
                b.status,
                b.amount,
                b.pickup_date,

                b.pickup_address,
                b.pickup_lat,
                b.pickup_long,
                b.delivery_address,
                b.delivery_lat,
                b.delivery_long,
                b.sender_name,
                b.sender_contact,
                b.total_weight,
                b.package_count,

                b.recipient_name,
                b.recipient_contact,

                packages = allPackages
                    .Where(p => p.shipment_id == b.shipment_id)
                    .ToList()
            }).ToList();

            // 6. Safe coordinate handling
            double checkpointLat = checkpoint.latitude ?? 0;
            double checkpointLng = checkpoint.longitude ?? 0;

            var toDrop = allBookings
          .Where(b =>
              b.status == "In-Transit" &&   // 🔥 IMPORTANT
              b.delivery_lat.HasValue && b.delivery_long.HasValue &&
              Math.Abs(b.delivery_lat.Value - checkpointLat) < 0.02 && // 🔥 reduce tolerance
              Math.Abs(b.delivery_long.Value - checkpointLng) < 0.02
          )
          .ToList();

            var toLoad = allBookings
         .Where(b =>
             b.status == "Assigned" &&   // 🔥 IMPORTANT
             b.pickup_lat.HasValue && b.pickup_long.HasValue &&
             Math.Abs(b.pickup_lat.Value - checkpointLat) < 0.02 &&
             Math.Abs(b.pickup_long.Value - checkpointLng) < 0.02
         )
         .ToList();

            // 9. Final response
            return Ok(new
            {
                checkpoint_id = checkpointId,
                checkpoint_name = checkpoint.name,

                to_load = new
                {
                    total = toLoad.Count,
                    shipments = toLoad
                },

                to_drop = new
                {
                    total = toDrop.Count,
                    shipments = toDrop
                }
            });
        }

        [HttpPost]
        [Route("api/checkpoints/{checkpointId}/confirm")]
        public IHttpActionResult ConfirmCheckpointReached(int checkpointId, int driverId)
        {
          
                var checkpoint = db.Checkpoints.FirstOrDefault(c => c.checkpoint_id == checkpointId);
                if (checkpoint == null)
                    return BadRequest("Checkpoint not found");

                int routeId = checkpoint.route_id ?? 0;

                var route = db.Routes.FirstOrDefault(r => r.route_id == routeId && r.driver_id == driverId);
                if (route == null)
                    return BadRequest("This checkpoint does not belong to the driver's route");

                // Verify all pickups at this checkpoint are done
                var pendingPickups = (
                    from b in db.Bookings
                    join s in db.Shipments on b.shipment_id equals s.shipment_id
                    join t in db.Trips on b.trip_id equals t.trip_id
                    where b.route_id == routeId
                       && t.driver_id == driverId
                       && b.status == "Assigned"
                       && s.pickup_lat != null && s.pickup_long != null
                       && Math.Abs(s.pickup_lat.Value - checkpoint.latitude.Value) < 0.02
                       && Math.Abs(s.pickup_long.Value - checkpoint.longitude.Value) < 0.02
                    select b
                ).ToList();

                if (pendingPickups.Any())
                    return BadRequest($"Please pickup all {pendingPickups.Count} remaining shipment(s) before confirming.");

                // Verify all drop offs at this checkpoint are done
                var pendingDropoffs = (
                    from b in db.Bookings
                    join s in db.Shipments on b.shipment_id equals s.shipment_id
                    join t in db.Trips on b.trip_id equals t.trip_id
                    where b.route_id == routeId
                       && t.driver_id == driverId
                       && b.status == "In-Transit"
                       && s.delivery_lat != null && s.delivery_long != null
                       && Math.Abs(s.delivery_lat.Value - checkpoint.latitude.Value) < 0.02
                       && Math.Abs(s.delivery_long.Value - checkpoint.longitude.Value) < 0.02
                    select b
                ).ToList();

                if (pendingDropoffs.Any())
                    return BadRequest($"Please deliver all {pendingDropoffs.Count} remaining shipment(s) before confirming.");

                // All done — mark checkpoint reached
                checkpoint.reached = true;

                // Check if last checkpoint
                var lastCheckpoint = db.Checkpoints
                    .Where(c => c.route_id == routeId)
                    .OrderByDescending(c => c.sequence_no)
                    .FirstOrDefault();

                if (lastCheckpoint != null && lastCheckpoint.checkpoint_id == checkpointId)
                {
                    var trip = db.Trips.FirstOrDefault(t =>
                        t.route_id == routeId && t.status == "In-Transit");

                    if (trip != null)
                    {
                        trip.end_time = DateTime.Now;
                        trip.status = "Completed";
                    }

                    var activeRoute = db.Routes.FirstOrDefault(r =>
                        r.route_id == routeId && r.is_active == true);

                    if (activeRoute != null)
                    {
                        var nextRoute = db.Routes.FirstOrDefault(r =>
                            r.driver_id == driverId &&
                            r.is_next_route == true &&
                            r.route_id != routeId);

                        activeRoute.is_active = false;
                        activeRoute.is_next_route = false;

                        if (nextRoute != null)
                        {
                            nextRoute.is_active = true;
                            nextRoute.is_next_route = false;
                        }
                    }
                }

                db.SaveChanges();

                return Ok(new
                {
                    message = "Checkpoint confirmed",
                    checkpoint_id = checkpointId,
                    checkpoint_name = checkpoint.name
                });
            
          
        }
        [HttpPost]
        [Route("api/bookings/{bookingId}/pickup")]
        public IHttpActionResult PickupBooking(int bookingId)
        {
            try
            {
                var booking = db.Bookings.FirstOrDefault(b => b.booking_id == bookingId);
                if (booking == null)
                    return BadRequest("Booking not found");

                if (booking.status != "Assigned")
                    return BadRequest("Booking is not in Assigned status");

                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == booking.shipment_id);
                if (shipment == null)
                    return BadRequest("Shipment not found");

                booking.status = "In-Transit";
                shipment.status = "In-Transit";

                db.SaveChanges();

                return Ok(new { message = "Booking picked up", bookingId = bookingId });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("api/bookings/{bookingId}/deliver")]
        public IHttpActionResult DeliverBooking(int bookingId)
        {
            try
            {
                var booking = db.Bookings.FirstOrDefault(b => b.booking_id == bookingId);
                if (booking == null)
                    return BadRequest("Booking not found");

                if (booking.status != "In-Transit")
                    return BadRequest("Booking is not In-Transit");

                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == booking.shipment_id);
                if (shipment == null)
                    return BadRequest("Shipment not found");

                booking.status = "Completed";
                booking.actual_delivery_datetime = DateTime.Now;
                shipment.status = "Delivered";

                db.SaveChanges();

                return Ok(new { message = "Booking delivered", bookingId = bookingId });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        [HttpPost]
        [Route("api/trips/start/{checkpointId}")]
        public IHttpActionResult StartTrip(int checkpointId, int driverId)
        {
            try
            {
                var checkpoint = db.Checkpoints.FirstOrDefault(c => c.checkpoint_id == checkpointId);
                if (checkpoint == null)
                    return BadRequest("Checkpoint not found");

                checkpoint.reached = true;

                int routeId = checkpoint.route_id ?? 0;

                // Verify checkpoint belongs to this driver
                var route = db.Routes.FirstOrDefault(r => r.route_id == routeId && r.driver_id == driverId);
                if (route == null)
                    return BadRequest("This checkpoint does not belong to the driver's route");

                // Update trip status
                var trip = db.Trips.FirstOrDefault(t =>
                    t.route_id == routeId &&
                    t.status == "Scheduled"
                );

                if (trip != null)
                {
                    trip.start_time = DateTime.Now;
                    trip.status = "In-Transit";
                }

                // Set all Assigned bookings + shipments on this route to In-Transit
                var bookingsToUpdate = (
                    from b in db.Bookings
                    join s in db.Shipments on b.shipment_id equals s.shipment_id
                    join t in db.Trips on b.trip_id equals t.trip_id
                    where b.route_id == routeId
                       && t.driver_id == driverId
                       && b.status == "Assigned"
                    select new { booking = b, shipment = s }
                ).ToList();

                //foreach (var item in bookingsToUpdate)
                //{
                //    item.booking.status = "In-Transit";
                //   // item.shipment.status = "In-Transit";
                //}

                db.SaveChanges();

                return Ok(new
                {
                    message = "Trip started",
                    checkpoint_id = checkpointId,
                    checkpoint_name = checkpoint.name,
                    trip_id = trip?.trip_id,
                    updated_bookings = bookingsToUpdate.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
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

        private IHttpActionResult GetBookingsByStatus(int driverId, string status)
        {
            var bookings = (
                from b in db.Bookings
                join t in db.Trips on b.trip_id equals t.trip_id
                join s in db.Shipments on b.shipment_id equals s.shipment_id
                join r in db.RecipientDetails on b.shipment_id equals r.shipment_id
                where t.driver_id == driverId && b.status == status
                select new
                {
                    booking_id = b.booking_id,
                    shipment_id = b.shipment_id,
                    route_id = b.route_id,
                    trip_id = b.trip_id,
                    status = b.status,
                    amount = b.amount,
                    pickup_date = b.pickup_date,

                    pickup_address = s.pickup_address,
                    pickup_lat = s.pickup_lat,
                    pickup_long = s.pickup_long,
                    delivery_address = s.delivery_address,
                    delivery_lat = s.delivery_lat,
                    delivery_long = s.delivery_long,
                    sender_name = s.sender_name,
                    sender_contact = s.sender_contact,
                    total_weight = s.total_weight,
                    package_count = s.package_count,

   
                    recipient_name = r.recipient_fname + " " + r.recipient_lname,
                    recipient_contact = r.recipient_contact,


                    packages = db.Packages
                        .Where(p => p.shipment_id == b.shipment_id)
                        .Select(p => new
                        {
                            p.shipment_id,
                            p.name,
                            p.weight,
                            p.length,
                            p.width,
                            p.height,
                            p.quantity,
                            p.color,
                            p.tagNo
                        }).AsEnumerable()

                }).ToList();

            return Ok(bookings);
        }

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/confirmed")]
        public IHttpActionResult GetConfirmedBookings(int driverId) => GetBookingsByStatus(driverId, "Assigned");

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/in-transit")]
        public IHttpActionResult GetInTransitBookings(int driverId) => GetBookingsByStatus(driverId, "In-Transit");

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/completed")]
        public IHttpActionResult GetCompletedBookings(int driverId) => GetBookingsByStatus(driverId, "Completed");

        [HttpGet]
        [Route("api/drivers/{driverId}/bookings/canceled")]
        public IHttpActionResult GetCanceledBookings(int driverId) => GetBookingsByStatus(driverId, "Canceled");
    }
    }

