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
        CargoConnectEntities2 db = new CargoConnectEntities2();
        [HttpGet]
        [Route("api/shipments/status")]
        public IHttpActionResult GetShipmentStatus()
        {
            return Ok("SUCCESS: Shipment Connection successful.");
        }
        [Route("api/shipments/add")]
        [HttpPost]
        public IHttpActionResult AddNewShipment(Shipments shipment, RecipientDetails recipent)
        {
            if (shipment == null)
            {
                return BadRequest("ERROR: Invalid Shipments data.");
            }
            var customer_id = db.Customer.FirstOrDefault(c => c.customer_id == shipment.customer_id);
            if (customer_id == null)
            {
                return BadRequest("ERROR: Customer not found");
            }
            else
            {
                db.Shipments.Add(shipment);
                db.SaveChanges();
                return Ok("Shipment saved");
            }
        }
        [Route("api/shipments/delete/{id}")]
        [HttpDelete]
        public IHttpActionResult DeleteShipment(int id)
        {
            var shipment = db.Shipments.FirstOrDefault(s => s.shipment_id == id);
            if (shipment == null)
                return NotFound();

            var recipent = db.RecipientDetails.FirstOrDefault(s => s.shipment_id == id);
            db.RecipientDetails.Remove(recipent);
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
            Edit.pickup_checkpoint = shipment.pickup_checkpoint;
            Edit.delivery_checkpoint = shipment.delivery_checkpoint;
            Edit.customer_id = shipment.customer_id;
            Edit.strict = shipment.strict;
            Edit.package_count = shipment.package_count;
            Edit.total_weight = shipment.total_weight;

            db.SaveChanges();
            return Ok("Shipment updated successfully.");
        }

        [Route("api/shipments/add/recipient")]
        [HttpPost]
        public IHttpActionResult AddRecipient(RecipientDetails recipent)
        {
            if (recipent == null)
            {
                return BadRequest("ERROR: Invalid Recipient Details");
            }
            var shipment_id = db.Shipments.FirstOrDefault(s => s.shipment_id == recipent.shipment_id);
            if (shipment_id == null)
            {
                return BadRequest("ERROR: No shipment found");
            }
            else
            {
                db.RecipientDetails.Add(recipent);
                db.SaveChanges();
                return Ok("SUCCESS: Recipient Added");
            }
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

        [Route("api/shipments/add/package")]
        [HttpPost]
        public IHttpActionResult AddNewPackages(Packages package, PackageAttributeMapping mapping)
        {
            if (package == null || mapping == null)
            {
                return BadRequest("ERROR: Invalid Packages  data.");
            }
            var shipment_id = db.Shipments.FirstOrDefault(s => s.shipment_id == package.shipment_id);
            if (shipment_id == null)
            {
                return BadRequest("Shipments not found");
            }
            else
            {
                db.Packages.Add(package);
                db.PackageAttributeMapping.Add(mapping);
                db.SaveChanges();
                return Ok("Packages saved");
            }
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

            db.Packages.Remove(package);
            db.SaveChanges();

            return Ok("Package deleted successfully.");
        }

        [Route("api/shipments/packages/{shipmentId}")]
        [HttpGet]
        public IHttpActionResult GetPackagesByShipment(int shipmentId)
        {
            var packages = db.Packages.Where(p => p.shipment_id == shipmentId)
                .Select(s => new
                {
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

            return Ok(packages);
        }

        [Route("api/shipments/package/{id}")]
        [HttpGet]
        public IHttpActionResult GetPackageById(int id)
        {
            var package = db.Packages.Where(p => p.package_id == id)
                .Select(s => new
                {
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
                    s.pickup_checkpoint,
                    s.delivery_checkpoint,
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
}
