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
        ImageView imageView;
        Bitmap curBitmap;
        byte[] blackBitmap;

        public void SetText2(string _message)
        {
            //RunOnUiThread(() => textNumber.Text += _message + "\n");
        }

        public void BtI(byte[] compressed)
        {
            //int compressionBufferLength = 1920 * 1080 * 4;
            //int backbufSize = LZ4.LZ4Codec.MaximumOutputLength(compressionBufferLength) + 4;

            //byte[] decompressedXOR = new byte[LZ4.LZ4Codec.MaximumOutputLength(compressed.Length)];
            //decompressedXOR = LZ4.LZ4Codec.Decode(compressed, 0, compressed.Length, 1920*1080*4);

                  //outbmp.UnlockBits(outbmpData);





            Bitmap bitmap = BitmapFactory.DecodeByteArray(blackBitmap, 0, blackBitmap.Length);
            RunOnUiThread(() => imageView.SetImageBitmap(bitmap));

        }

        public void fillBlack()
        {
            blackBitmap = new byte[1920*1080*4];
            for (int i = 0; i < 1920 * 1080 * 4-4; i+=4)
            {
                blackBitmap[i] = 255;
                blackBitmap[i+1] = 0;
                blackBitmap[i+2] = 0;
                blackBitmap[i+3] = 0;
            }
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {

            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

           // textNumber = FindViewById<TextView>(Resource.Id.textBoxMessageOutput);
            imageView = FindViewById<ImageView>(Resource.Id.imageView);

            FindViewById<Button>(Resource.Id.buttonConnect).Click += (o, e) =>
            {
                try
                {
                    client = new CommClient(IPAddress.Parse(FindViewById<EditText>(Resource.Id.textBoxIP).Text), ConnectionType.Connect, SetText2);
                    fillBlack();

                    client.byteArrayAction = BtI;
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
            public DecompressScreen(byte[] compressed)
            {
                decompressed = new byte[LZ4.LZ4Codec.MaximumOutputLength(compressed.Length)];
                LZ4.LZ4Codec.Decode(compressed, 0, compressed.Length,
                    decompressed, 0, decompressed.Length);

            }

            public static void DC(byte[] compressed, byte[] decompressed)
            {
                decompressed = new byte[LZ4.LZ4Codec.MaximumOutputLength(compressed.Length)];
                LZ4.LZ4Codec.Decode(compressed, 0, compressed.Length,
                    decompressed, 0, decompressed.Length);
            }


            //public void BtI(byte[] input)
            //{

            //    Bitmap bitmap = BitmapFactory.DecodeByteArray(input, 0, input.Length);
            //    imageView.setImageBitmap(bitmap);


            //}

        }
    }
}