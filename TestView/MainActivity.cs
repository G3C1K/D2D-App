using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Android.Media;
using System.IO;

namespace TestView
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        ImageView imageView;

        public void BtI(byte[] input)
        {

            Bitmap bitmap = BitmapFactory.DecodeByteArray(input, 0, input.Length);
            imageView.SetImageBitmap(bitmap);


        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            // Wstawianie do imageView
            DecompressScreen zdekompresowane = new DecompressScreen(skompresowane);
            fab.Click += (o, e) =>
            {
                BtI(zdekompresowane.decompressed);
            };
            
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            View view = (View) sender;
            Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
                .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();
        }
	}

    public class CompressedScreen
    {
        public int Size;
        public byte[] Data;
        public CompressedScreen(int size)
        {
            this.Data = new byte[size];
            this.Size = 4;
        }
    }

    public class DecompressScreen
    {
        public byte[] decompressed;
        public DecompressScreen(CompressedScreen compressed)
        {
            decompressed = new byte[LZ4.LZ4Codec.MaximumOutputLength(compressed.Data.Length)];
            LZ4.LZ4Codec.Decode(compressed.Data, 0, compressed.Size,
                decompressed, 0, decompressed.Length);
        }


        //public void BtI(byte[] input)
        //{

        //    Bitmap bitmap = BitmapFactory.DecodeByteArray(input, 0, input.Length);
        //    imageView.setImageBitmap(bitmap);


        //}

    }
}

