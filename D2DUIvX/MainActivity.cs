using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using Android.Runtime;
using Android.Widget;
using System.Net;
using System;
using Android.Views;
using Android.Graphics;
using Android.Content;
using System.Collections.Generic;
using D2DUIvX;

namespace D2DUIX
{
    
    [Activity(Label = "@string/app_name", Theme = "@style/AppThemeDark", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        CommClientAndroid client;
        AutoConfigAndroid autoConfigClient;
        bool listeningFlag = false;


        public void SetText2(string _message)
        {
            //textNumber.Post(() => textNumber.Text += _message + "\n"); //????
            //RunOnUiThread(() => textNumber.Text += _message + "\n");
        }

        public void DisconnectDelegate(string we)
        {
            Button button = FindViewById<Button>(Resource.Id.buttonConnect);
            button.Post(() =>
                {
                    client.volumeReady = false;
                    client.Close();
                    Toast.MakeText(this, "Disconnected", ToastLength.Short).Show();
                    Intent rtrn = new Intent(this.ApplicationContext, typeof(MainActivity));
                    StartActivity(rtrn);
                }               
            );         
        }

        public void AutoConfigFinished(List<string> outputLista)
        {
            string final = "";
            foreach(string item in outputLista)
            {
                final += item + " ";
            }
            final += "finished";
            TextView textViewForIPS = FindViewById<TextView>(Resource.Id.textView_test);
            textViewForIPS.Post(() =>
            {
                textViewForIPS.Text = final;
            });

        }

        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            TextView testView = FindViewById<TextView>(Resource.Id.textView_test);
            EditText textBoxIP = FindViewById<EditText>(Resource.Id.textBoxIP);
            Button buttonAutoConfig = FindViewById<Button>(Resource.Id.button_autoconfig);

            

            testView.Click += (o, e) =>
            {
                textBoxIP.Text = testView.Text;
            };

            FindViewById<Button>(Resource.Id.buttonConnect).Click += (o, e) =>
            {
                bool isConnected = true;
                IPAddress iPAddress = IPAddress.Parse(FindViewById<EditText>(Resource.Id.textBoxIP).Text);

                try
                {
                    if(listeningFlag == false)
                    {
                        client = new CommClientAndroid(iPAddress, SetText2);
                        client.DisconnectAction = DisconnectDelegate;
                        ClientHolder.Client = client;
                    }
                    else
                    {
                        Toast.MakeText(this, "Listening for hosts...", ToastLength.Short).Show();

                    }
                }
                catch (Exception ex)
                {
                    Toast.MakeText(this, ex.Message, ToastLength.Long).Show();
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

            buttonAutoConfig.Click += (o, e) =>
            {
                autoConfigClient = new AutoConfigAndroid();
                autoConfigClient.FinishAction = AutoConfigFinished;
                autoConfigClient.Listen();
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