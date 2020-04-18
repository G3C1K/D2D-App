using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace D2DUIv3
{

    [Activity(Label = "Volume", Theme = "@style/AppTheme")]
    public class VolumeActivity : AppCompatActivity
    {
        public class Slider2 : SeekBar      //niepoprawne rozwiązanie
        {
            public int MASTER_ID;
            public string PROCESS_NAME = "null";
            public string DISPLAY_NAME = "null";

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


        public CommClientAndroid client;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            client = ClientHolder.Client;
            client.volumeReady = false;
            client.InstantiateVolumeClient();
            
            while(client.volumeReady == false)
            {
                Thread.Sleep(10);
            }

            SetContentView(Resource.Layout.volume_submenu);


            var linearLayoutVolume = FindViewById<LinearLayout>(Resource.Id.linearLayoutVolume);

            foreach (VolumeAndroid volume in client.VolumeListForAndroid)
            {            
                LinearLayout layout2 = new LinearLayout(this);          //horizontal layout = ikona + text
                LinearLayout.LayoutParams layout2Params = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, 100);
                layout2.LayoutParameters = layout2Params;
                layout2.Orientation = Orientation.Horizontal;

                ImageView iv = new ImageView(this);                     //imageView ikony
                iv.LayoutParameters = new LinearLayout.LayoutParams(100, 100);

                if(volume.ProcessName!="System Volume")
                {
                    Bitmap bmp = BitmapFactory.DecodeByteArray(volume.IconByteArray, 0, volume.IconByteArray.Length);
                    iv.SetImageBitmap(bmp);                                 //wbijanie ikony do imageview
                }
                else
                {
                    Drawable drawable = GetDrawable(Resource.Drawable.baseline_volume_up_white_48);
                    iv.SetImageDrawable(drawable);
                }

                TextView textView = new TextView(this);                 //opis sesji
                LinearLayout.LayoutParams paramss = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.WrapContent, 100);
                textView.LayoutParameters = paramss;

                Slider2 slider = new Slider2(this);

                if (volume.DisplayName != "null" && volume.DisplayName != "")   //ustalanie ktory name bedzie wybrany do opisu sesji
                {
                    textView.Text = volume.DisplayName;
                }
                else if (volume.ProcessName != "null")
                {
                    textView.Text = volume.ProcessName;
                }
                else textView.Text = "unknown process";
                textView.Text += " " + volume.ProcessID;

                if(textView.Text.Contains("AudioSrv", StringComparison.InvariantCultureIgnoreCase))
                {
                    textView.Text = "System sounds";
                }

                //linearLayoutVolume.AddView(textView);
                linearLayoutVolume.AddView(layout2);        //inner horizontal layout
                layout2.AddView(iv);
                layout2.AddView(textView);

                slider.MASTER_ID = volume.ProcessID;        //slider insta
                slider.PROCESS_NAME = volume.ProcessName;

                slider.DISPLAY_NAME = volume.DisplayName;

                slider.Progress = (int)volume.Volume;

                slider.ProgressChanged += Slider_ProgressChanged;      //slider event

                linearLayoutVolume.AddView(slider);         //slider po layout2
                
            }


        }

        private void Slider_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            Slider2 slider = sender as Slider2;
            if(slider != null)
            {
                if (slider.PROCESS_NAME != "null")
                {
                    if(slider.PROCESS_NAME == "System Volume")
                    {
                        client.ChangeVolumeMaster((float)slider.Progress);
                    }
                    else client.ChangeVolumeClientPN(slider.PROCESS_NAME, (float)slider.Progress);

                }
                else if (slider.DISPLAY_NAME != "null")
                {
                    client.ChangeVolumeClientDN(slider.DISPLAY_NAME, (float)slider.Progress);
                }
                else
                {
                    client.ChangeVolumeClient(slider.MASTER_ID, (float)slider.Progress);
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.volume_toolbar, menu);
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

            if(item.ItemId == Resource.Id.volume_refresh)
            {
                client.volumeReady = false;
                this.Finish();
                OverridePendingTransition(0, 0);
                this.StartActivity(this.Intent);
                OverridePendingTransition(0, 0);
            }


            return base.OnOptionsItemSelected(item);
        }
    }

}