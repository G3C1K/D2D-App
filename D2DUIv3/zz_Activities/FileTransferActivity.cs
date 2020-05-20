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

namespace D2DUIv3
{
    [Activity(Label = "FileTransferActivity")]
    public class FileTransferActivity : AppCompatActivity
    {
        CommClientAndroid client;
        LinearLayout transferLayout;


        public void FileListReceivedDelegate(List<string> fileList)
        {
            TextView ACTVItem;

            transferLayout = FindViewById<LinearLayout>(Resource.Id.linearLayout_filetransfer);
            transferLayout.Post(() => { int i = 2; });

            transferLayout.Post(() =>
            {
                Toast.MakeText(this, fileList.Count.ToString(), ToastLength.Short).Show();
                transferLayout.RemoveAllViews();

                foreach(string item in fileList)
                {
                    ACTVItem = new TextView(this);
                    ACTVItem.SetTextAppearance(Android.Resource.Style.TextAppearanceSmall);
                    float factor = this.Resources.DisplayMetrics.Density;
                    LinearLayout.LayoutParams paramss = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, (int)(40 * factor));
                    paramss.SetMargins(2, 2, 2, 2);
                    ACTVItem.LayoutParameters = paramss;
                    ACTVItem.TextAlignment = TextAlignment.Center;
                    ACTVItem.Clickable = true;
                    ACTVItem.Focusable = true;
                    ACTVItem.Background = GetDrawable(Resource.Drawable.ACTextViewBackground);
                    ACTVItem.Gravity = GravityFlags.Center;

                    ACTVItem.Text = item;

                    ACTVItem.Click += (o, e) =>
                    {
                        TextView oo = o as TextView;
                        CommClientAndroid client = ClientHolder.Client;
                        client.RemoveFile(oo.Text);



                        ViewGroup parent = (ViewGroup)oo.Parent;
                        parent.RemoveView(oo);
                    };

                    transferLayout.AddView(ACTVItem);
                }
            });
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.filetransfer_submenu);

            transferLayout = FindViewById<LinearLayout>(Resource.Id.linearLayout_filetransfer);


            client = ClientHolder.Client;
            client.FileListReceivedAction = FileListReceivedDelegate;
            client.InstantiateTransfer();



            // Create your application here
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
                //client.ClosePM();
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }
    }
}