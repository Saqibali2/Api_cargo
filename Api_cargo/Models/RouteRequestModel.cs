using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class RouteRequestModel
    {
        public int DriverId { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ArrivalDate { get; set; }
        public List<CheckpointModel> RouteData { get; set; }
    }

    public class CheckpointModel
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SequenceNo { get; set; }
    }
}