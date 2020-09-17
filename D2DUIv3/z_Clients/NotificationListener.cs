using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Service.Notification;

namespace D2DUIv3
{
    [Service(Label="NotificationListenerService2", Permission ="android.permission.BIND_NOTIFICATION_LISTENER_SERVICE")]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService"})]
    class NotificationListener : NotificationListenerService
    {
        public override void OnCreate()
        {
            base.OnCreate();
            Toast.MakeText(Application.Context, "Created", ToastLength.Short).Show();
            System.Diagnostics.Debug.WriteLine("oncreate jest");
        }
        public override IBinder OnBind(Intent intent)
        {
            System.Diagnostics.Debug.WriteLine("onbind jest");
            return base.OnBind(intent);
        }

        public override bool OnUnbind(Intent intent)
        {
            return base.OnUnbind(intent);
        }
        public override void OnListenerConnected()
        {
            Toast.MakeText(Application.Context, "Listener connected", ToastLength.Short).Show();
            base.OnListenerConnected();
        }

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            string packageName = sbn.PackageName;
            string content = sbn.Notification.TickerText.ToString();
            System.Diagnostics.Debug.WriteLine(packageName);
            System.Diagnostics.Debug.WriteLine(content);
            System.Diagnostics.Debug.WriteLine("Nowa notyfikacja ");
            base.OnNotificationPosted(sbn);
            Toast.MakeText(Application.Context, "The notification was posted", ToastLength.Short).Show();
            //System.Diagnostics.Debug.WriteLine("Nowa notyfikacja ");
        }

        public override void OnNotificationRemoved(StatusBarNotification sbn)
        {
            base.OnNotificationRemoved(sbn);
            Toast.MakeText(Application.Context, "The notification was removed", ToastLength.Short).Show();
        }


        class NLServiceReceiver : BroadcastReceiver
        {
            public override void OnReceive(Context context, Intent intent)
            {
                //if (intent.GetStringExtra("command").Equals("clearall"))
                //{
                //    NotificationListener.NotificationService.
                //}
                //if (intent.GetStringExtra("command").Equals("list"))
                //{
                //    Intent i1 = new Intent("NotificationListener");
                //    i1.PutExtra("notification_event", "=====================");
                //    context.SendBroadcast(i1);
                //}

            }
        }




    }
}