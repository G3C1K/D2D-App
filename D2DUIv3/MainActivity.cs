using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using TCPSender;
using System.Net;
using System;
using Android.Views;
using Android.Graphics;


namespace D2DUIv3
{
    
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        TextView textNumber;
        CommClient client;
        EditText textOutput;

        public void SetText2(string _message)
        {
            RunOnUiThread(() => textNumber.Text += _message + "\n");
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            textNumber = FindViewById<TextView>(Resource.Id.textBoxMessageOutput);

            FindViewById<Button>(Resource.Id.buttonConnect).Click += (o, e) =>
            {
                try
                {
                    client = new CommClient(IPAddress.Parse(FindViewById<EditText>(Resource.Id.textBoxIP).Text), ConnectionType.Connect, SetText2);
                }
                catch (Exception ex)
                {
                    textNumber.Text =
                    "Exception caught: \n" +
                    "Message: " + ex.Message + "\n" +
                    "Source: " + ex.Source + "\n" +
                    "TargetSite: " + ex.TargetSite + "\n";
                }
            };

            FindViewById<Button>(Resource.Id.buttonSend).Click += (o, e) =>
            {
                textOutput = FindViewById<EditText>(Resource.Id.textBoxMessageInput);
                client.SendMessage(textOutput.Text);
                textOutput.Text = "";
            };

            FindViewById<Button>(Resource.Id.toggleButtonMute).Click += (o, e) =>
            {
                client.SendVolume("mute");
            };

            FindViewById<Button>(Resource.Id.buttonVolumeDown).Click += (o, e) =>
            {
                client.SendVolume("down");
            };

            FindViewById<Button>(Resource.Id.buttonVolumeUp).Click += (o, e) =>
            {
                client.SendVolume("up");
            };

            //do zrobienia:
            //lepsze ui
            //usiniecie/dezaktywacja connectButton po podlaczeniu sie, lub zamiana go na disconnectButton
            //w zakladkach mam kod jak dostac volume poszczegolnych aplikacji
            //moze remote mute mikra
            //
            //moze zaczac wysylanie plikow

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.top_menus, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            Toast.MakeText(this, "Action selected: " + item.TitleFormatted,
                ToastLength.Short).Show();

            
            return base.OnOptionsItemSelected(item);
        }
    }
}