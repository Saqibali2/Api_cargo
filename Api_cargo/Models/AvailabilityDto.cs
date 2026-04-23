using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class AvailabilityDto
    {
        public int shipmentId { get; set; }
        public double pickupLat { get; set; }
        public double pickupLong { get; set; }
        public double destLat { get; set; }
        public double destLong { get; set; }
        public DateTime requestedDate { get; set; }

        public bool isStrict { get; set; }
    }
}