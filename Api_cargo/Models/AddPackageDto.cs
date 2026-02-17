using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class AddPackageDto
    {
        public int shipment_id { get; set; }
        public string name { get; set; }
        public double weight { get; set; }
        public int quantity { get; set; }
        public double length { get; set; }
        public double width { get; set; }
        public double height { get; set; }
        public List<int> attribute_ids { get; set; }
    }

}