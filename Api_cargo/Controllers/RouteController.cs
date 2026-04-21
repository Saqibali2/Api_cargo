using Api_cargo.Models;
using System;
using System.Linq;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class RouteController : ApiController
    {
        CargoConnectEntities3 db = new CargoConnectEntities3();

        [HttpGet]
        [Route("api/routes/status")]
        public IHttpActionResult GetRouteStatus()
        {
            return Ok("SUCCESS");
        }

        [HttpPost]
        [Route("api/driver/save-route")]
        public IHttpActionResult SaveRoute(CreateRouteRequest request)
        {
            if (request == null || request.DriverId <= 0 || request.Points == null || !request.Points.Any())
                return BadRequest("Invalid route data.");

            var activeRoute = db.Routes.FirstOrDefault(x =>
                x.driver_id == request.DriverId &&
                x.is_active == true);

            var nextRoute = db.Routes.FirstOrDefault(x =>
                x.driver_id == request.DriverId &&
                x.is_next_route == true);

            bool makeActive = false;
            bool makeNext = false;

            if (activeRoute == null)
                makeActive = true;
            else if (nextRoute == null)
                makeNext = true;

            var route = new Routes
            {
                driver_id = request.DriverId,
                is_active = makeActive,
                is_next_route = makeNext,
                base_fare = request.BaseFare
            };

            db.Routes.Add(route);
            db.SaveChanges();

            db.RouteSchedule.Add(new RouteSchedule
            {
                route_id = route.route_id,
                departureDate = request.DepartureDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                arrivalDate = request.ArrivalDate.ToString("yyyy-MM-ddTHH:mm:ss")
            });

            db.SaveChanges();

            foreach (var cp in request.Points)
            {
                db.Checkpoints.Add(new Checkpoints
                {
                    name = cp.Name,
                    latitude = cp.Latitude,
                    longitude = cp.Longitude,
                    driver_id = request.DriverId,
                    sequence_no = cp.SequenceNo,
                    route_id = route.route_id,
                    reached = false
                });
            }

            db.SaveChanges();

            return Ok(new
            {
                routeId = route.route_id,
                isActive = route.is_active,
                isNext = route.is_next_route
            });
        }

        [HttpGet]
        [Route("api/driver/get-routes/{driverId}")]
        public IHttpActionResult GetRoutes(int driverId)
        {
            var routes = db.Routes
                .Where(x => x.driver_id == driverId)
                .OrderByDescending(x => x.route_id)
                .ToList()
                .Select(r => new
                {
                    routeId = r.route_id,
                    driverId = r.driver_id,
                    fare = r.base_fare,
                    isActive = r.is_active,
                    isNextRoute = r.is_next_route,

                    schedule = db.RouteSchedule
                    .Where(s => s.route_id == r.route_id)
                    .Select(s => new
                    {
                        departureDate = s.departureDate,
                        arrivalDate = s.arrivalDate
                    }).FirstOrDefault(),

                    startPoint = db.Checkpoints
                    .Where(c => c.route_id == r.route_id)
                    .OrderBy(c => c.sequence_no)
                    .Select(c => c.name)
                    .FirstOrDefault(),

                    endPoint = db.Checkpoints
                    .Where(c => c.route_id == r.route_id)
                    .OrderByDescending(c => c.sequence_no)
                    .Select(c => c.name)
                    .FirstOrDefault(),

                    totalStops = db.Checkpoints
                    .Count(c => c.route_id == r.route_id)
                });

            return Ok(routes);
        }

        [HttpGet]
        [Route("api/driver/get-route-detail/{routeId}")]
        public IHttpActionResult GetRouteDetail(int routeId)
        {
            var route = db.Routes.FirstOrDefault(x => x.route_id == routeId);

            if (route == null)
                return BadRequest("Route not found.");

            var schedule = db.RouteSchedule.FirstOrDefault(x => x.route_id == routeId);

            var checkpoints = db.Checkpoints
                .Where(x => x.route_id == routeId)
                .OrderBy(x => x.sequence_no)
                .ToList()
                .Select(c => new
                {
                    checkpointId = c.checkpoint_id,
                    name = c.name,
                    latitude = c.latitude,
                    longitude = c.longitude,
                    sequenceNo = c.sequence_no,
                    reached = c.reached
                });

            return Ok(new
            {
                routeId = route.route_id,
                fare = route.base_fare,
                isActive = route.is_active,
                isNextRoute = route.is_next_route,
                departureDate = schedule?.departureDate,
                arrivalDate = schedule?.arrivalDate,
                checkpoints = checkpoints
            });
        }

        [HttpPost]
        [Route("api/driver/activate-route/{routeId}")]
        public IHttpActionResult ActivateRoute(int routeId)
        {
            var route = db.Routes.FirstOrDefault(x => x.route_id == routeId);

            if (route == null)
                return BadRequest("Route not found.");

            var currentActive = db.Routes.FirstOrDefault(x =>
                x.driver_id == route.driver_id &&
                x.is_active == true);

            if (currentActive != null && currentActive.route_id != routeId)
                return BadRequest("Another route already active.");

            var allNext = db.Routes.Where(x =>
                x.driver_id == route.driver_id &&
                x.is_next_route == true).ToList();

            foreach (var item in allNext)
                item.is_next_route = false;

            route.is_active = true;
            route.is_next_route = false;

            db.SaveChanges();

            return Ok("Activated");
        }

        [HttpPost]
        [Route("api/driver/schedule-next-route/{routeId}")]
        public IHttpActionResult ScheduleNextRoute(int routeId)
        {
            var route = db.Routes.FirstOrDefault(x => x.route_id == routeId);

            if (route == null)
                return BadRequest("Route not found.");

            if (route.is_active == true)
                return BadRequest("Active route cannot be next route.");

            var nextRoute = db.Routes.FirstOrDefault(x =>
                x.driver_id == route.driver_id &&
                x.is_next_route == true &&
                x.route_id != routeId);

            if (nextRoute != null)
                return BadRequest("Next route already exists.");

            route.is_next_route = true;

            db.SaveChanges();

            return Ok("Scheduled");
        }

        [HttpDelete]
        [Route("api/driver/delete-route/{routeId}")]
        public IHttpActionResult DeleteRoute(int routeId)
        {
            var route = db.Routes.FirstOrDefault(x => x.route_id == routeId);

            if (route == null)
                return BadRequest("Route not found.");

            if (route.is_active == true)
                return BadRequest("Active route cannot be deleted.");

            var checkpoints = db.Checkpoints.Where(x => x.route_id == routeId).ToList();
            var schedule = db.RouteSchedule.Where(x => x.route_id == routeId).ToList();

            foreach (var item in checkpoints)
                db.Checkpoints.Remove(item);

            foreach (var item in schedule)
                db.RouteSchedule.Remove(item);

            db.Routes.Remove(route);

            db.SaveChanges();

            return Ok("Deleted");
        }

        [HttpPut]
        [Route("api/driver/edit-route/{routeId}")]
        public IHttpActionResult EditRoute(int routeId, CreateRouteRequest request)
        {
            var route = db.Routes.FirstOrDefault(x => x.route_id == routeId);

            if (route == null)
                return BadRequest("Route not found.");

            if (route.is_active == true)
                return BadRequest("Active route cannot be edited.");

            route.base_fare = request.BaseFare;

            var schedule = db.RouteSchedule.FirstOrDefault(x => x.route_id == routeId);

            if (schedule != null)
            {
                schedule.departureDate = request.DepartureDate.ToString("yyyy-MM-ddTHH:mm:ss");
                schedule.arrivalDate = request.ArrivalDate.ToString("yyyy-MM-ddTHH:mm:ss");
            }

            var oldPoints = db.Checkpoints.Where(x => x.route_id == routeId).ToList();

            foreach (var item in oldPoints)
                db.Checkpoints.Remove(item);

            db.SaveChanges();

            foreach (var cp in request.Points)
            {
                db.Checkpoints.Add(new Checkpoints
                {
                    name = cp.Name,
                    latitude = cp.Latitude,
                    longitude = cp.Longitude,
                    driver_id = route.driver_id,
                    route_id = routeId,
                    sequence_no = cp.SequenceNo,
                    reached = false
                });
            }

            db.SaveChanges();

            return Ok("Updated");
        }
    }
}