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
using TCPSender;

namespace D2DUIv3
{
    [Activity(Label = "Volume", Theme = "@style/AppTheme")]
    public class VolumeActivity : AppCompatActivity
    {

        public CommClient client;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            client = ClientHolder.Client;

            SetContentView(Resource.Layout.volume_submenu);

            var buttonVolumeUp = FindViewById<Button>(Resource.Id.buttonVolumeUp2);
            var buttonVolumeDown = FindViewById<Button>(Resource.Id.buttonVolumeDown2);
            var buttonToggleMute = FindViewById<Button>(Resource.Id.toggleButtonMute2);

            buttonVolumeDown.Click += (o, e) =>
            {
                client.SendVolume("down");
            };

            buttonVolumeUp.Click += (o, e) =>
            {
                client.SendVolume("up");
            };

            buttonToggleMute.Click += (o, e) =>
            {
                client.SendVolume("mute");
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
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }

}