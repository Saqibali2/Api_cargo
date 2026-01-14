using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.Entity;

namespace Api_cargo.Controllers
{
    public class RouteController : ApiController
    {
        CargoConnectEntities2 db = new CargoConnectEntities2();

        [HttpGet]
        [Route("api/routes/status")]
        public IHttpActionResult GetRouteStatus()
        {
            return Ok("SUCCESS: Route connection successful.");
        }

        [HttpGet]
        [Route("api/routes")]
        public IHttpActionResult GetAllRoutes()
        {
            var routes = db.Routes.Select(r => new
            {
                r.route_id,
                r.driver_id,
                r.from_checkpoint,
                r.to_checkpoint,
                r.distance_km,

            }).ToList();
            return Ok(routes);
        }

        [HttpGet]
        [Route("api/routes/{id}")]
        public IHttpActionResult GetRoute(int id)
        {
            var result = db.Routes.Where(r => r.route_id == id).Select(R => new
            {
                R.route_id,
                R.driver_id,
                R.from_checkpoint,
                R.to_checkpoint,
                R.distance_km,

            });
            if (result == null)
                return BadRequest("ERROR: No entires found.");
            else
                return Ok(result);
        }

        [HttpGet]
        [Route("api/routes/driver/{driverId}")]
        public IHttpActionResult GetRoutesByDriverId(int driverId)
        {
            var routes = db.Routes.Where(r => r.driver_id == driverId).Select(d => new
            {
                d.route_id,
                d.driver_id,
                d.from_checkpoint,
                d.to_checkpoint,
                d.distance_km,

            }).ToList();
            if (routes == null || routes.Count == 0)
            {
                return NotFound();
            }
            return Ok(routes);
        }

        [HttpGet]
        [Route("api/routes/{routeID}/checkpoints")]
        public IHttpActionResult GetRouteCheckpoints(int routeID)
        {
            var checkpoints = db.RouteCheckpoints
                .Where(rc => rc.route_id == routeID)
                .OrderBy(rc => rc.sequence_no)
                .Select(rc => new
                {
                    rc.route_detail_id,
                    rc.sequence_no,
                    rc.checkpoint_id,
                    rc.reached
                }).ToList();

            return Ok(checkpoints);
        }
        [HttpPost]
        [Route("api/routes/create")]
        public IHttpActionResult CreateRoute(CreateRouteDto dto)
        {
            if (dto.route == null)
                return BadRequest("ERROR: Route data is null.");

            if (dto.cps == null || !dto.cps.Any())
                return BadRequest("ERROR: Checkpoints are missing.");

            // Save route first
            db.Routes.Add(dto.route);
            db.SaveChanges();

            // Save checkpoints (EF 5 safe)
            int sequence = 1;
            foreach (var cp in dto.cps)
            {
                db.RouteCheckpoints.Add(new RouteCheckpoints
                {
                    route_id = dto.route.route_id,
                    checkpoint_id = cp.checkpoint_id,
                    sequence_no = sequence++,
                    reached = false
                });
            }

            db.SaveChanges();

            return Ok("SUCCESS: Route created successfully.");
        }

        [HttpDelete]
        [Route("api/routes/delete/{id}")]
        public IHttpActionResult DeleteRoute(int id)
        {
            var route = db.Routes.FirstOrDefault(r => r.route_id == id);
            if (route == null)
                return NotFound();

            var routeCheckpoints = db.RouteCheckpoints
                                     .Where(rc => rc.route_id == id)
                                     .ToList();

            // EF 5 compatible delete
            foreach (var rc in routeCheckpoints)
            {
                db.RouteCheckpoints.Remove(rc);
            }

            db.Routes.Remove(route);
            db.SaveChanges();

            return Ok("SUCCESS: Route deleted successfully.");
        }


        [HttpGet]
        [Route("api/routes/active/{driverID}")]
        public IHttpActionResult GetActiveRoute(int driverID)
        {
            var activeRoute = db.ActiveRoute.Select(nr => new
            {
                nr.route_id,
                nr.active_route_id,
                nr.driver_id,
                nr.departure_date,
                nr.arrival_date,
                nr.base_fare,

            }).FirstOrDefault(ar => ar.driver_id == driverID);
            if (activeRoute == null)
                return BadRequest("ERROR: No active route found.");
            return Ok(activeRoute);
        }

        [HttpPost]
        [Route("api/routes/activatenext/{routeID}")]
        public IHttpActionResult ActivateNextRoute(RouteDto dto)
        {
            var route = db.Routes.FirstOrDefault(r => r.route_id == dto.RouteID);
            if (route == null)
                return BadRequest("ERROR: Route not found.");

            var existingNextRoute = db.NextRoute.FirstOrDefault(nr => nr.route_id == dto.RouteID);

            if (existingNextRoute != null)
            {
                existingNextRoute.departure_date = dto.Departure;
                existingNextRoute.arrival_date = dto.Arrival;
            }
            else
            {
                var nextRoute = new NextRoute
                {
                    route_id = dto.RouteID,
                    driver_id = route.driver_id,
                    departure_date = dto.Departure,
                    arrival_date = dto.Arrival
                };
                db.NextRoute.Add(nextRoute);
            }

            db.SaveChanges();
            return Ok("SUCCESS: Next Route activated successfully.");
        }

        [HttpPost]
        [Route("api/routes/activate/{routeID}")]
        public IHttpActionResult ActivateRoute(RouteDto dto)
        {
            var route = db.Routes.FirstOrDefault(r => r.route_id == dto.RouteID);
            if (route == null)
                return BadRequest("ERROR: Route not found.");

            var existingActiveRoute = db.ActiveRoute.FirstOrDefault(ar => ar.driver_id == route.driver_id);

            if (existingActiveRoute != null)
            {
                existingActiveRoute.departure_date = dto.Departure;
                existingActiveRoute.arrival_date = dto.Arrival;
            }
            else
            {
                var activeRoute = new ActiveRoute
                {
                    route_id = dto.RouteID,
                    driver_id = route.driver_id,
                    departure_date = dto.Departure,
                    arrival_date = dto.Arrival
                };
                db.ActiveRoute.Add(activeRoute);
            }

            db.SaveChanges();
            return Ok("SUCCESS: Route activated successfully.");
        }


        [HttpGet]
        [Route("api/routes/next/{driverID}")]
        public IHttpActionResult GetNextRoute(int driverID)
        {
            var nextRoute = db.NextRoute.Select(nr => new
            {
                nr.route_id,
                nr.next_route_id,
                nr.driver_id,
                nr.departure_date,
                nr.arrival_date,
                nr.base_fare,

            }).FirstOrDefault(ar => ar.driver_id == driverID);
            if (nextRoute == null)
                return BadRequest("ERROR: No next route found.");
            return Ok(nextRoute);
        }

        [HttpGet]
        [Route("api/shipments/{id}/packagestest")]
        public IHttpActionResult GetShipmentPackages(int id)
        {
            var packageInclude = db.Shipments.Include("Packages").FirstOrDefault(s => s.shipment_id == id);
            return Ok(packageInclude);
        }

        [HttpPut]
        [Route("api/routes/update/{routeID}")]
        public IHttpActionResult UpdateRoute(int routeID, Routes updatedRoute)
        {
            var route = db.Routes.FirstOrDefault(r => r.route_id == routeID);
            if (route == null)
                return NotFound();
            route.driver_id = updatedRoute.driver_id;
            route.from_checkpoint = updatedRoute.from_checkpoint;
            route.to_checkpoint = updatedRoute.to_checkpoint;
            route.distance_km = updatedRoute.distance_km;
            db.SaveChanges();
            return Ok("SUCCESS: Route updated successfully.");
        }
    }
}
