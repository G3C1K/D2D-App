using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Support.V4.App;
using Android.Support.V4.Media.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{
    [Activity(Label = "NotificationListenerActivity")]
    public class NotificationListenerActivity : AppCompatActivity
    {
        TextView text;
        int i = 0;
        static readonly string CHANNEL_ID = "location_notification";
        



        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.notificationlistener_submenu);

            text = FindViewById<TextView>(Resource.Id.textViewNL);

            Intent intent = new Intent("android.settings.ACTION_NOTIFICATION_LISTENER_SETTINGS");
            StartActivity(intent);
            NotificationListener nl = new NotificationListener();
            StartService(new Intent(this, typeof(NotificationListener)));
            
            //Notification:
            var channel = new NotificationChannel(CHANNEL_ID, "powiadomienia", NotificationImportance.Default)
            {
                Description = "opis"
            };
            var notificationManager = (NotificationManager)GetSystemService(NotificationService);
            notificationManager.CreateNotificationChannel(channel);


            Button createNotificationButton = FindViewById<Button>(Resource.Id.buttonCreateNotification);

            createNotificationButton.Click += (o, r) =>
            {
                var builder = new Notification.Builder(this, CHANNEL_ID)
                                .SetAutoCancel(true)
                                .SetNumber(i)
                                .SetContentTitle("My notification")
                                .SetSmallIcon(Resource.Drawable.abc_ic_star_black_16dp)
                                .SetContentText("Test notification");

                var notificationManager2 = NotificationManagerCompat.From(this);
                notificationManager2.Notify(1000, builder.Build());
                i++;
                StatusBarNotification[] sbn = nl.GetActiveNotifications();
            };




        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.basic_submenu_toolbar, menu);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            return base.OnCreateOptionsMenu(menu);
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                //client.ClosePM();
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }
}