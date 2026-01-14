using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class RouteDto
    {
        public int RouteID { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
    }
}