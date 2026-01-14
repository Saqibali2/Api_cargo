using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Models
{
    public class CreateRouteDto
    {
        public Routes route { get; set; }
        public Checkpoints[] cps { get; set; }
    }
}