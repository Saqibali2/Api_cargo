using System;
using System.Collections.Generic;

namespace Api_cargo.Models
{
    public class CreateRouteRequest
    {
        public int DriverId { get; set; }

        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }

        public bool ActivateNow { get; set; }
        public decimal BaseFare { get; set; }
        public List<RoutePointDto> Points { get; set; }

        public bool IsFragile { get; set; }
        public bool IsLiquid { get; set; }
        public bool IsFlammable { get; set; }
        public bool KeepUpright { get; set; }
        public string ShipmentType { get; set; }
    }

    public class RoutePointDto
    {
        public string Name { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int SequenceNo { get; set; }
    }
}