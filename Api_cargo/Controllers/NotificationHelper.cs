using Api_cargo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Api_cargo.Controllers
{
    public class NotificationHelper
    {
        public static void Send(CargoConnectEntities4 db, int userId, string message)
        {
            var notification = new Notifications
            {
                user_id = userId,
                message = message,
                created_at = DateTime.Now,
                is_read = false
            };

            db.Notifications.Add(notification);
            db.SaveChanges();
        }
    }
}