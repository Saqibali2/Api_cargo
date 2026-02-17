using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class CreateRouteRequest
    {
        public int DriverId { get; set; }

        public string DepartureDate { get; set; }
        public string ArrivalDate { get; set; }

        public bool ActivateNow { get; set; }

        public List<RoutePointDto> Points { get; set; }
    }

    public class RoutePointDto
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SequenceNo { get; set; }
    }
}