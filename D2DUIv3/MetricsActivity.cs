using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{
    [Activity(Label = "MetricsActivity")]
    public class MetricsActivity : AppCompatActivity
    {

        public CommClientAndroid client;
        TextView textViewMetrics;
        bool stillAsk = false;

        public void PMReadyDelegate(string input)
        {
            Thread pingerThread = new Thread(() =>
            {
                Pinger(1000);
            });
            pingerThread.Start();
        }

        public void PMDataReceivedDelegate(string input)
        {
            try
            {
                textViewMetrics.Post(() =>
                {
                    textViewMetrics.Text = input;
                });
            }
            catch
            {

            }
        }

        public void Pinger(int interval)
        {

            while (stillAsk == true)
            {
                client.AskForPM();
                Thread.Sleep(interval); 
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            stillAsk = true;
            SetContentView(Resource.Layout.metrics_submenu);

            textViewMetrics = FindViewById<TextView>(Resource.Id.textView_pms5);
            textViewMetrics.Text += " test instancji\n";

            client = ClientHolder.Client;
            client.PMReadyAction = PMReadyDelegate;
            client.PMDataReceivedAction = PMDataReceivedDelegate;

            client.InstantiatePMClient();

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
                stillAsk = false;
                client.ClosePM();
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }
}