using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Support.Design.Widget;
using Android.Webkit;
using System.IO;

namespace D2DUIv3
{
    [Activity(Label = "FileTransferActivity")]
    public class FileTransferActivity : AppCompatActivity
    {
        CommClientAndroid client;
        LinearLayout transferLayout;

        readonly string[] PermissionsStorage =
        {
            Android.Manifest.Permission.ReadExternalStorage,
            Android.Manifest.Permission.WriteExternalStorage
        };

        const int RequestStorageId = 0;

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

                        const string permission = Android.Manifest.Permission.WriteExternalStorage;
                        if (CheckSelfPermission(permission) == (int)Permission.Granted)
                        {
                            CommClientAndroid client = ClientHolder.Client;
                            client.FileReceivedAction = FileReceivedDelegate;
                            client.DownloadFile(oo.Text);

                            client.RemoveFile(oo.Text);
                            ViewGroup parent = (ViewGroup)oo.Parent;
                            parent.RemoveView(oo);
                        }
                        else
                        {
                            Toast.MakeText(this, "Unable to download file. Insufficient permissions.", ToastLength.Long).Show();
                        }

                       
                    };

                    transferLayout.AddView(ACTVItem);
                }
            });
        }

        public void FileReceivedDelegate(string fileName, string fileDescription,string filePath, int fileSize)
        {
            MimeTypeMap mime = MimeTypeMap.Singleton;
            string ext = fileName.ToLower();
            string extension = MimeTypeMap.GetFileExtensionFromUrl(ext);
            string type = null;
            if (extension != null)
            {
                type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(extension);
            }
            if(type == null)
            {
                type = "application/octet-stream";
            }

            DownloadManager downloadManager = DownloadManager.FromContext(Android.App.Application.Context);
            downloadManager.AddCompletedDownload(fileName, fileDescription, true, type, filePath, fileSize, true);
        }

        private void TryGetStorage()
        {
            if ((int)Build.VERSION.SdkInt < 23)
            {
                return;
            }

            GetStoragePermission();
        }

        private void GetStoragePermission()
        {
            const string permission = Android.Manifest.Permission.WriteExternalStorage;
            if (CheckSelfPermission(permission) == (int)Permission.Granted)
            {
                return;
            }

            if (ShouldShowRequestPermissionRationale(permission))
            {
                Snackbar snackbar = Snackbar.Make(transferLayout, "Storage access is required to download files.", Snackbar.LengthIndefinite);
                snackbar.SetAction("OK", v => RequestPermissions(PermissionsStorage, RequestStorageId));
                snackbar.Show();

                return;
            }

            RequestPermissions(PermissionsStorage, RequestStorageId);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.filetransfer_submenu);

            TryGetStorage();

            transferLayout = FindViewById<LinearLayout>(Resource.Id.linearLayout_filetransfer);


            client = ClientHolder.Client;
            client.FileListReceivedAction = FileListReceivedDelegate;

            client.BrokenFileAction = delegate (string input)
            {
                transferLayout.Post(delegate
                {
                    Toast.MakeText(this, input, ToastLength.Short).Show();
                });
            };

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