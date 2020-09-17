using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{
    [Activity]
    public class MetricsActivity : AppCompatActivity
    {

        public CommClientAndroid client;
        TextView textViewMetrics;
        bool stillAsk = false;
        RecyclerView recyclerView_statistics;
        AdapterForStatistics recyclerView_statistics_adapter;
        RecyclerView.LayoutManager recyclerView_statistics_layoutManager;
        String[] HardwareSeparated1D;

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
                //textViewMetrics.Post(() =>
                //{
                //    textViewMetrics.Text = input;
                //});

                recyclerView_statistics.Post(() =>
                {
                    HardwareSeparated1D = input.Split('\n');
                    recyclerView_statistics_adapter.setItems(HardwareSeparated1D);
                });

                //HardwareSeparated1D = input.Split('\n');


                //HardwareSeparated = input.Split('\n').Select(x => x.Split(':')).ToArray();

                //recyclerView_statistics_adapter.setItems(HardwareSeparated1D);

                //recyclerView_statistics_adapter.NotifyDataSetChanged();

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
            this.Title = Resources.GetString(Resource.String.pmetrics);
            stillAsk = true;
            SetContentView(Resource.Layout.metrics_submenu);

            //textViewMetrics = FindViewById<TextView>(Resource.Id.textView_pms5);
            //textViewMetrics.Text += " test instancji\n";
            string[] statOnStart = new string[1];
            //statOnStart[0] = "Loading...: Please wait";
            statOnStart[0] = Resources.GetString(Resource.String.loading);
            HardwareSeparated1D = statOnStart;

            // Recycler view
            recyclerView_statistics = FindViewById<RecyclerView>(Resource.Id.recyclerView_statistics);
            recyclerView_statistics_layoutManager = new LinearLayoutManager(this);
            recyclerView_statistics.SetLayoutManager(recyclerView_statistics_layoutManager);

            recyclerView_statistics_adapter = new AdapterForStatistics(HardwareSeparated1D);
            recyclerView_statistics.SetAdapter(recyclerView_statistics_adapter);

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
                //client.ClosePM();
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }
}