using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using D2DUIv3.zz_Activities;
//sing D2DUIv3.zz_Activities;

namespace D2DUIv3
{
    [Activity(Label = "Main menu", Theme = "@style/AppTheme")]
    public class MainMenuActivity : AppCompatActivity
    {

        public CommClientAndroid client;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.main_menu);
            string ip = Intent.GetStringExtra("IP" ?? "not recv");

            client = ClientHolder.Client;

            var txtIP = FindViewById<TextView>(Resource.Id.textIPInfo);
            if (client != null)
            {
                txtIP.Text += " " + ip + " " + client.DownloadPath;
            }
            else
            {
                txtIP.Text = "client not inst";
            }

            var buttonVolume = FindViewById<Button>(Resource.Id.buttonVolume);

            buttonVolume.Click += (o,e) => {
                Intent volumeActivity = new Intent(this, typeof(VolumeActivity));
                StartActivity(volumeActivity);
            };

            Button buttonMetrics = FindViewById<Button>(Resource.Id.buttonMetrics);

            buttonMetrics.Click += (o, e) =>
            {
                Intent metricsActivity = new Intent(this, typeof(MetricsActivity));
                StartActivity(metricsActivity);
            };

            var buttonNumpad = FindViewById<Button>(Resource.Id.buttonNumpad);

            buttonNumpad.Click += (o, e) =>
            {
                Intent numpadActivity = new Intent(this, typeof(NumpadActivity));
                StartActivity(numpadActivity);
            };

            // Create your application here
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main_menu_toolbar, menu);
            return base.OnCreateOptionsMenu(menu);
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Resource.Id.menu_disconnect)
            {
                client.Close();
                this.Finish();
                return true;
            }
            

            return base.OnOptionsItemSelected(item);
        }
    }
}