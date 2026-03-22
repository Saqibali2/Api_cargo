using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class ShipmentController : ApiController
    {
        CargoConnectEntities3 db = new CargoConnectEntities3();

        [Route("api/shipments/draft/{customerId}")]
        [HttpPost]
        public IHttpActionResult CreateOrGetDraft(int customerId)
        {
            var customer = db.Customer.FirstOrDefault(c => c.customer_id == customerId);
            if (customer == null)
                return BadRequest("Customer not found");

            var draft = db.Shipments
                .FirstOrDefault(s => s.customer_id == customerId && s.status == "Draft");

            if (draft != null)
                return Ok(new { shipmentId = draft.shipment_id });

            var shipment = new Shipments
            {
                customer_id = customerId,
                sender_name = customer.first_name,
                sender_contact = customer.contact_no,
                package_count = 0,
                total_weight = 0,
                status = "Draft"
            };

            db.Shipments.Add(shipment);
            db.SaveChanges();

            return Ok(new { shipmentId = shipment.shipment_id });
        }


        [Route("api/shipments/add/package")]
        [HttpPost]
        public IHttpActionResult AddPackage(PackageWithMapping request)
        {
            if (request?.Package == null)
                return BadRequest("Invalid package data");

            var shipment = db.Shipments
                .FirstOrDefault(s => s.shipment_id == request.Package.shipment_id);

            if (shipment == null)
                return BadRequest("Shipment not found");

            if (shipment.status != "Draft")
                return BadRequest("Cannot modify non-draft shipment");

            db.Packages.Add(request.Package);
            db.SaveChanges();

            if (request.AttributeIds != null)
            {
                foreach (var attributeId in request.AttributeIds)
                {
                    db.PackageAttributeMapping.Add(new PackageAttributeMapping
                    {
                        package_id = request.Package.package_id,
                        attribute_id = attributeId
                    });
                }
            }

            // Update totals
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

        [Route("api/shipments/delete/package/{id}")]
        [HttpDelete]
        public IHttpActionResult DeletePackage(int id)
        {
            var package = db.Packages.FirstOrDefault(p => p.package_id == id);
            if (package == null)
                return NotFound();

            var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == package.shipment_id);
            if (shipment.status != "Draft")
                return BadRequest("Cannot modify non-draft shipment");

            var mappings = db.PackageAttributeMapping.Where(m => m.package_id == id).ToList();
            foreach (var m in mappings)
                db.PackageAttributeMapping.Remove(m);

            db.Packages.Remove(package);
            db.SaveChanges();

            shipment.package_count = db.Packages.Count(p => p.shipment_id == shipment.shipment_id);
            shipment.total_weight = db.Packages
                .Where(p => p.shipment_id == shipment.shipment_id)
                .Sum(p => p.weight ?? 0);

            db.SaveChanges();

            return Ok("Package deleted successfully");
        }

        [Route("api/shipments/complete")]
        [HttpPost]
        public IHttpActionResult CompleteShipment(CompleteShipmentDto model)
        {
            if (model == null)
                return BadRequest("Invalid data");

            var shipment = db.Shipments
                .FirstOrDefault(s => s.shipment_id == model.shipment_id);

            if (shipment == null)
                return BadRequest("Shipment not found");

            if (shipment.status != "Draft")
                return BadRequest("Shipment already processed");


            var hasPackages = db.Packages
                .Any(p => p.shipment_id == model.shipment_id);

            if (!hasPackages)
                return BadRequest("Add at least one package before completing shipment");

            if (string.IsNullOrWhiteSpace(model.recipient_fname) ||
                string.IsNullOrWhiteSpace(model.recipient_contact))
                return BadRequest("Recipient details are required");

            var existingRecipient = db.RecipientDetails
                .FirstOrDefault(r => r.shipment_id == model.shipment_id);

            if (existingRecipient != null)
                return BadRequest("Recipient already exists");


            shipment.pickup_lat = model.pickup_lat;
            shipment.pickup_long = model.pickup_long;
            shipment.pickup_address = model.pickup_address;

            shipment.delivery_lat = model.delivery_lat;
            shipment.delivery_long = model.delivery_long;
            shipment.delivery_address = model.delivery_address;

            shipment.strict = model.strict;
            shipment.status = "Pending";

            var recipient = new RecipientDetails
            {
                shipment_id = model.shipment_id,
                recipient_fname = model.recipient_fname,
                recipient_lname = model.recipient_lname,
                recipient_contact = model.recipient_contact,
                instructionsMessage = model.instructionsMessage
            };

            db.RecipientDetails.Add(recipient);

            db.SaveChanges();

            return Ok(new
            {
                message = "Shipment completed successfully",
                shipmentId = shipment.shipment_id,
                status = shipment.status
            });
        }
        [Route("api/shipments/edit/recipient/{id}")]
        [HttpPut]
        public IHttpActionResult EditRecipient(int id, RecipientDetails recipient)
        {
            var existing = db.RecipientDetails
                .FirstOrDefault(r => r.recipient_detail_id == id);

            if (existing == null)
                return NotFound();

            var shipment = db.Shipments
                .FirstOrDefault(s => s.shipment_id == existing.shipment_id);

            if (shipment.status != "Draft")
                return BadRequest("Cannot edit after submission");

            existing.recipient_fname = recipient.recipient_fname;
            existing.recipient_lname = recipient.recipient_lname;
            existing.recipient_contact = recipient.recipient_contact;
            existing.instructionsMessage = recipient.instructionsMessage;

            db.SaveChanges();

            return Ok("Recipient updated successfully");
        }

        [Route("api/shipments/packages/{shipmentId}")]
        [HttpGet]
        public IHttpActionResult GetPackagesByShipment(int shipmentId)
        {
            var packages = db.Packages
                .Where(p => p.shipment_id == shipmentId)
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
                .ToList();

            return Ok(packages);
        }

        [Route("api/shipments/customer/{customerId}")]
        [HttpGet]
        public IHttpActionResult GetShipmentsByCustomer(int customerId)
        {
            var shipments = db.Shipments
                .Where(s => s.customer_id == customerId)
                .Select(s => new
                {
                    s.shipment_id,
                    s.status,
                    s.pickup_address,
                    s.delivery_address,
                    s.total_weight,
                    s.package_count
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