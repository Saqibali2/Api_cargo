using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
   public class ShipmentController : ApiController
        {
        CargoConnectEntities3 db = new CargoConnectEntities3();

            [HttpGet]
            [Route("api/shipments/status")]
            public IHttpActionResult GetShipmentStatus()
            {
                return Ok("SUCCESS: Shipment Connection successful.");
            }

            [Route("api/shipments/customer/{customerId}/pending")]
            [HttpGet]
            public IHttpActionResult GetPendingShipment(int customerId)
            {
                var pendingShipment = db.Shipments
                    .Where(s => s.customer_id == customerId && s.status == "Pending")
                    .Where(s => !db.RecipientDetails.Any(r => r.shipment_id == s.shipment_id))
                    .OrderByDescending(s => s.shipment_id)
                    .FirstOrDefault();

                if (pendingShipment == null)
                    return Ok(new { shipmentId = (int?)null });

                return Ok(new { shipmentId = pendingShipment.shipment_id });
            }
        [Route("api/shipments/add")]
        [HttpPost]
        public IHttpActionResult AddNewShipment(int customerId)
        {
            var customer = db.Customer
                .FirstOrDefault(c => c.customer_id == customerId);

            if (customer == null)
                return BadRequest("ERROR: Customer not found");

            var existingPending = db.Shipments
                .FirstOrDefault(s =>
                    s.customer_id == customerId &&
                    s.status == "Pending");

            if (existingPending != null)
            {
                return Ok(new
                {
                    shipmentId = existingPending.shipment_id
                });
            }

            var shipment = new Shipments
            {
                customer_id = customerId,
                sender_name = customer.first_name,
                sender_contact = customer.contact_no,
                package_count = 0,
                total_weight = 0,
                status = "Pending",
                pickup_address = null,
                delivery_address = null
            };

            db.Shipments.Add(shipment);
            db.SaveChanges();

            return Ok(new
            {
                shipmentId = shipment.shipment_id
            });
        }


        [Route("api/shipments/add/package")]
            [HttpPost]
            public IHttpActionResult AddNewPackage(PackageWithMapping request)
            {
                if (request == null || request.Package == null)
                {
                    return BadRequest("ERROR: Invalid Package data.");
                }

                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == request.Package.shipment_id);
                if (shipment == null)
                {
                    return BadRequest("ERROR: Shipment not found");
                }

                db.Packages.Add(request.Package);
                db.SaveChanges();

                if (request.AttributeIds != null && request.AttributeIds.Any())
                {
                    foreach (var attributeId in request.AttributeIds)
                    {
                        var mapping = new PackageAttributeMapping
                        {
                            package_id = request.Package.package_id,
                            attribute_id = attributeId
                        };
                        db.PackageAttributeMapping.Add(mapping);
                    }
                }
                shipment.package_count = db.Packages.Count(p => p.shipment_id == shipment.shipment_id);
                shipment.total_weight = db.Packages
                    .Where(p => p.shipment_id == shipment.shipment_id)
                    .Sum(p => p.weight ?? 0);

                db.SaveChanges();

                return Ok(new
                {
                    packageId = request.Package.package_id,
                    shipmentId = shipment.shipment_id
                });
            }

            [Route("api/shipments/add/recipient")]
            [HttpPost]
            public IHttpActionResult AddRecipient(RecipientDetails recipient)
            {
                if (recipient == null)
                {
                    return BadRequest("ERROR: Invalid Recipient Details");
                }

                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == recipient.shipment_id);
                if (shipment == null)
                {
                    return BadRequest("ERROR: No shipment found");
                }

                var existingRecipient = db.RecipientDetails
                    .FirstOrDefault(r => r.shipment_id == recipient.shipment_id);

                if (existingRecipient != null)
                {
                    return BadRequest("ERROR: Recipient already exists for this shipment");
                }

                db.RecipientDetails.Add(recipient);
                db.SaveChanges();

                return Ok("SUCCESS: Recipient Added");
            }

            [Route("api/shipments/delete/{id}")]
            [HttpDelete]
            public IHttpActionResult DeleteShipment(int id)
            {
                var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == id);
                if (shipment == null)
                    return NotFound();

                var recipient = db.RecipientDetails.FirstOrDefault(s => s.shipment_id == id);
                if (recipient != null)
                    db.RecipientDetails.Remove(recipient);

                db.Shipments.Remove(shipment);
                db.SaveChanges();

                return Ok("SUCCESS: Shipment deleted successfully.");
            }

            [Route("api/shipments/edit/{id}")]
            [HttpPut]
            public IHttpActionResult EditShipment(int id, Shipments shipment)
            {
                var Edit = db.Shipments.FirstOrDefault(s => s.shipment_id == id);
                if (Edit == null)
                    return NotFound();

                Edit.sender_name = shipment.sender_name;
                Edit.sender_contact = shipment.sender_contact;
                Edit.status = shipment.status;

                // Update coordinates instead of checkpoints
                Edit.pickup_lat = shipment.pickup_lat;
                Edit.pickup_long = shipment.pickup_long;
                Edit.pickup_address = shipment.pickup_address;
                Edit.delivery_lat = shipment.delivery_lat;
                Edit.delivery_long = shipment.delivery_long;
                Edit.delivery_address = shipment.delivery_address;

                Edit.customer_id = shipment.customer_id;
                Edit.strict = shipment.strict;
                Edit.package_count = shipment.package_count;
                Edit.total_weight = shipment.total_weight;

                db.SaveChanges();
                return Ok("Shipment updated successfully.");
            }

            [Route("api/shipments/edit/recipient/{id}")]
            [HttpPut]
            public IHttpActionResult EditRecipient(int id, RecipientDetails recipient)
            {
                var editrecpt = db.RecipientDetails.FirstOrDefault(r => r.recipient_detail_id == id);
                if (editrecpt == null)
                    return NotFound();

                editrecpt.recipient_fname = recipient.recipient_fname;
                editrecpt.recipient_lname = recipient.recipient_lname;
                editrecpt.recipient_contact = recipient.recipient_contact;
                editrecpt.instructionsMessage = recipient.instructionsMessage;

                db.SaveChanges();
                return Ok("Recipient details updated successfully.");
            }

            [Route("api/shipments/edit/package/{id}")]
            [HttpPut]
            public IHttpActionResult EditPackage(int id, Packages p)
            {
                var EditPkg = db.Packages.FirstOrDefault(ed => ed.package_id == id);
                if (EditPkg == null)
                    return NotFound();

                EditPkg.name = p.name;
                EditPkg.weight = p.weight;
                EditPkg.length = p.length;
                EditPkg.width = p.width;
                EditPkg.height = p.height;
                EditPkg.quantity = p.quantity;
                EditPkg.color = p.color;
                EditPkg.tagNo = p.tagNo;

                db.SaveChanges();
                return Ok("Package updated successfully.");
            }

            [Route("api/shipments/delete/package/{id}")]
            [HttpDelete]
            public IHttpActionResult DeletePackage(int id)
            {
                var package = db.Packages.FirstOrDefault(p => p.package_id == id);
                if (package == null)
                    return NotFound();

                // Delete associated attribute mappings
                var mappings = db.PackageAttributeMapping.Where(m => m.package_id == id).ToList();
                foreach (var mapping in mappings)
                {
                    db.PackageAttributeMapping.Remove(mapping);
                }

                db.Packages.Remove(package);
                db.SaveChanges();

                return Ok("Package deleted successfully.");
            }

        [Route("api/shipments/packages/{shipmentId}")]
        [HttpGet]
        public IHttpActionResult GetPackagesByShipment(int shipmentId)
        {
            var shipmentExists = db.Shipments
                .Any(s => s.shipment_id == shipmentId);

            if (!shipmentExists)
                return BadRequest("No shipment found. Please add a package to create a shipment.");

            var packages = db.Packages
                .Where(p => p.shipment_id == shipmentId)
                .Select(p => new
                {
                    p.package_id,
                    p.shipment_id,
                    p.name,
                    p.weight,
                    p.length,
                    p.width,
                    p.height,
                    p.quantity,
                    p.color,
                    p.tagNo
                })
                .ToList();

            return Ok(packages);
        }


        [Route("api/shipments/package/{id}")]
            [HttpGet]
            public IHttpActionResult GetPackageById(int id)
            {
                var package = db.Packages.Where(p => p.package_id == id)
                    .Select(s => new
                    {
                        s.package_id,
                        s.shipment_id,
                        s.name,
                        s.weight,
                        s.length,
                        s.width,
                        s.height,
                        s.quantity,
                        s.color,
                        s.tagNo
                    });

                if (package == null)
                    return NotFound();

                return Ok(package);
            }
      
        [Route("api/shipments/customer/{customerId}/details")]
            [HttpGet]
            public IHttpActionResult GetShipmentDetailsByCustomer(int customerId)
            {
                bool customerExists = db.Customer.Any(c => c.customer_id == customerId);
                if (!customerExists)
                    return BadRequest("Customer not found.");

                var shipments = db.Shipments
                    .Where(s => s.customer_id == customerId)
                    .AsEnumerable()
                    .Select(s => new
                    {
                        s.shipment_id,
                        s.sender_name,
                        s.sender_contact,
                        s.status,
                        // Return coordinates instead of checkpoints
                        s.pickup_lat,
                        s.pickup_long,
                        s.pickup_address,
                        s.delivery_lat,
                        s.delivery_long,
                        s.delivery_address,
                        s.strict,
                        s.total_weight,
                        Packages = db.Packages
                            .Where(p => p.shipment_id == s.shipment_id)
                            .Select(p => new
                            {
                                p.package_id,
                                p.name,
                                p.weight,
                                p.length,
                                p.width,
                                p.height,
                                p.quantity,
                                p.color,
                                p.tagNo
                            })
                            .ToList(),
                        PackageCount = db.Packages.Count(p => p.shipment_id == s.shipment_id),
                        Recipient = db.RecipientDetails
                            .Where(r => r.shipment_id == s.shipment_id)
                            .Select(r => new
                            {
                                r.recipient_detail_id,
                                r.recipient_fname,
                                r.recipient_lname,
                                r.recipient_contact,
                                r.instructionsMessage
                            })
                            .FirstOrDefault()
                    })
                    .ToList();

                return Ok(shipments);
            }
        }

        public class PackageWithMapping
        {
            public Packages Package { get; set; }
            public List<int> AttributeIds { get; set; }
        }
    }