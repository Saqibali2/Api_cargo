using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Api_cargo.Controllers
{
    public class NotificationsController : ApiController
    {
        private readonly CargoConnectEntities2 db = new CargoConnectEntities2();

        [HttpGet]
        [Route("api/notifications/status")]
        public IHttpActionResult GetNotificationsStatus()
        {
            return Ok("SUCCESS: Notifications connection successful.");
        }
        [HttpGet]
        [Route("api/users/{userId}/notifications")]
        public IHttpActionResult GetNotificationsByUser(int userId)
        {
            var notifications = db.Notifications
                .Where(n => n.user_id == userId)
                .OrderByDescending(n => n.created_at)
                .ToList();

            return Ok(notifications);
        }

        [HttpPost]
        [Route("api/notifications")]
        public IHttpActionResult PostNotification(Notifications notification)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!db.Users.Any(u => u.user_id == notification.user_id))
                return BadRequest("Target user does not exist.");

            if (notification.created_at == null)
                notification.created_at = DateTime.Now;

            db.Notifications.Add(notification);

            db.SaveChanges();

            return Ok(notification);
        }

        [HttpDelete]
        [Route("api/notifications/{id}")]
        public IHttpActionResult DeleteNotification(int id)
        {
            var notification = db.Notifications.Where(n => n.notification_id == id).FirstOrDefault();
            if (notification == null) return NotFound();

            db.Notifications.Remove(notification);
            db.SaveChanges();

            return Ok("SUCCESS: Notification removed.");
        }
    }
}
