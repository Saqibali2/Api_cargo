using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class ShipmentDto
    {
        public int shipment_id { get; set; }
        public string pickup_address { get; set; }
        public string delivery_address { get; set; }
        public string status { get; set; }
        public string sender_name { get; set; }
        public string sender_contact { get; set; }
        public double? total_weight { get; set; }
    }
}