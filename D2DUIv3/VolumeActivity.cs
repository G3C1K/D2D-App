using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using TCPSender;

namespace D2DUIv3
{

    [Activity(Label = "Volume", Theme = "@style/AppTheme")]
    public class VolumeActivity : AppCompatActivity
    {
        public class Slider2 : SeekBar
        {
            public int MASTER_ID;

            public Slider2(Context context) : base(context)
            {
            }

            public Slider2(Context context, IAttributeSet attrs) : base(context, attrs)
            {
            }

            public Slider2(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
            {
            }

            public Slider2(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
            {
            }

            protected Slider2(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }


        }


        public CommClient client;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            client = ClientHolder.Client;
            client.volumeReady = false;
            client.InstantiateVolumeClient();
            
            while(client.volumeReady == false)
            {

            }

            SetContentView(Resource.Layout.volume_submenu);


            var linearLayoutVolume = FindViewById<LinearLayout>(Resource.Id.linearLayoutVolume);

            foreach (VolumeAndroid volume in client.VolumeArrayForAndroid)
            {
                TextView textView = new TextView(this);
                LinearLayout.LayoutParams paramss = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 100);
                textView.LayoutParameters = paramss;
                if (volume.DisplayName != "null" && volume.DisplayName != "")
                {
                    textView.Text = volume.DisplayName;
                }
                else if (volume.ProcessName != "null")
                {
                    textView.Text = volume.ProcessName;
                }
                else textView.Text = "unknown process";
                textView.Text += " " + volume.ProcessID;

                linearLayoutVolume.AddView(textView);

                Slider2 slider = new Slider2(this);
                slider.MASTER_ID = volume.ProcessID;
                slider.Progress = (int)volume.Volume;

                slider.ProgressChanged += (o, e) =>
                {
                    client.ChangeVolumeClient(slider.MASTER_ID, (float)slider.Progress);
                };

                linearLayoutVolume.AddView(slider); 
            }


        }

        private void Slider_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
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
                client.volumeReady = false;
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }

}