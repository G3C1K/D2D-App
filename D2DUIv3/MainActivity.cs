using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using System.Net;
using System;
using Android.Views;
using Android.Graphics;
using Android.Content;

namespace D2DUIv3
{
    
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        TextView textNumber;
        CommClientAndroid client;
        EditText textOutput;

        public void SetText2(string _message)
        {
            textNumber.Post(() => textNumber.Text += _message + "\n"); //????
            //RunOnUiThread(() => textNumber.Text += _message + "\n");
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            textNumber = FindViewById<TextView>(Resource.Id.textBoxMessageOutput);

            FindViewById<Button>(Resource.Id.buttonConnect).Click += (o, e) =>
            {
                bool isConnected = true;
                IPAddress iPAddress = IPAddress.Parse(FindViewById<EditText>(Resource.Id.textBoxIP).Text);

                try
                {
                    client = new CommClientAndroid(iPAddress, SetText2);
                    ClientHolder.Client = client;
                }
                catch (Exception ex)
                {
                    textNumber.Text =
                    "Exception caught: \n" +
                    "Message: " + ex.Message + "\n" +
                    "Source: " + ex.Source + "\n" +
                    "TargetSite: " + ex.TargetSite + "\n";
                    isConnected = false;
                }
                finally
                {
                    if (isConnected)
                    {
                        Intent nextActivity = new Intent(this, typeof(MainMenuActivity));
                        nextActivity.PutExtra("IP", iPAddress.ToString());
                        StartActivity(nextActivity);
                    }
                }
            };

            FindViewById<Button>(Resource.Id.buttonSend).Click += (o, e) =>
            {
                textOutput = FindViewById<EditText>(Resource.Id.textBoxMessageInput);
                client.SendMessage(textOutput.Text);
                textOutput.Text = "";
            };

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.intro_toolbar, menu);
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