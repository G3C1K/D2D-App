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
using System.Collections.Generic;

namespace D2DUIv3
{

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        CommClientAndroid client;
        AutoConfigAndroid autoConfigClient;
        bool listeningFlag = false;
        bool canProceedToMainMenu = false;
        IPAddress iPAddress;


        public void SetText2(string _message)
        {
            //textNumber.Post(() => textNumber.Text += _message + "\n"); //????
            //RunOnUiThread(() => textNumber.Text += _message + "\n");
        }

        public void ConnectedDelegate(string message)
        {
            string deviceName = Android.Provider.Settings.Secure.GetString(this.ContentResolver, "device_name");
            if(deviceName != null)
            {
                client.SendDeviceName(deviceName);
            }
            else
            {
                deviceName = Android.OS.Build.Model;
                if (deviceName != null)
                {
                    client.SendDeviceName(deviceName);
                }
                else
                {
                    client.SendDeviceName("unknown device or emulator");
                }
            }

            Button button = FindViewById<Button>(Resource.Id.buttonConnect);
            button.Post(() =>
            {
                Intent nextActivity = new Intent(this, typeof(MainMenuActivity));
                nextActivity.PutExtra("IP", iPAddress.ToString());
                StartActivity(nextActivity);
            });
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
            EditText textBoxIP = FindViewById<EditText>(Resource.Id.textBoxIP);
            LinearLayout ACLayout = FindViewById<LinearLayout>(Resource.Id.linearLayoutAC);

            if (textBoxIP.Text == "acdebug")
            {
                autoConfigClient.listaWykrytychIP.Add("192.168.50.190");
                autoConfigClient.listaWykrytychIP.Add("debug");
                autoConfigClient.listaWykrytychIP.Add("127.0.0.1");
                autoConfigClient.listaWykrytychIP.Add("1.2.3.4");
            }

            //string final = "";
            ////foreach(string item in outputLista)
            ////{
            ////    //final += item + " ";
            ////}
            //final += "finished";
            //TextView textViewForIPS = FindViewById<TextView>(Resource.Id.textView_test);

            //textViewForIPS.Post(() =>
            //{
            //    textViewForIPS.Text = final;
            //});


            TextView ACTVItem;

            ACLayout.Post(() =>
            {
                ACLayout.RemoveAllViews();

                if(outputLista.Count == 0)
                {
                    Toast.MakeText(this, "No clients found!", ToastLength.Short).Show();
                }

                foreach (string item in outputLista)
                {

                    ACTVItem = new TextView(this);
                    ACTVItem.SetTextAppearance(Android.Resource.Style.TextAppearanceMedium);
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
                        textBoxIP.Text = oo.Text;
                    };

                    ACLayout.AddView(ACTVItem);
                }
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

            
            //do szybkiego wpisywania adresu mojego kompa
            testView.Click += (o, e) =>
            {
                textBoxIP.Text = testView.Text;
            };

            //pzycisk connect
            FindViewById<Button>(Resource.Id.buttonConnect).Click += (o, e) =>
            {
                bool isConnected = false;
                EditText IPinputEditText = FindViewById<EditText>(Resource.Id.textBoxIP);

                try//sprawdzam
                {
                    if (ClientUtilities.IsValidIPV4Address(IPinputEditText.Text))   //czy adres jest valid
                    {
                        iPAddress = IPAddress.Parse(IPinputEditText.Text);  //jak tak parsuje i daje pozwolenie na przejscie do menu
                        canProceedToMainMenu = true; 
                    }
                    else
                    {
                        Toast.MakeText(this, "Invalid IP Address", ToastLength.Short).Show();   //jak nie wyswietlam i nie daje pozwolenia
                        canProceedToMainMenu = false;
                    }
                }
                catch (Exception e2)
                {
                    Toast.MakeText(this, e2.Message, ToastLength.Short).Show(); //jak sie crashnie to tez nie pozwalam, ale nie wiem czy jest w stanie sie crashnac. zostawic i tak
                    canProceedToMainMenu = false;
                }

                if (canProceedToMainMenu == true)   //jesli pozwalam na przejscie do menu
                {
                    try
                    {
                        if (listeningFlag == false) //???
                        {
                            listeningFlag = true;

                            client = new CommClientAndroid(iPAddress, SetText2);
                            client.DisconnectAction = DisconnectDelegate;
                            ClientHolder.Client = client;
                            client.ConnectedAction = ConnectedDelegate;
                            isConnected = true;
                        }
                        else
                        {
                            Toast.MakeText(this, "Connecting...", ToastLength.Short).Show();
                            //chyba chodzi o to, ze jak nacisnie sie drugi raz connect podczas connectowania, to nie stworzy sie kolejny klient?
                        }
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(this, ex.Message, ToastLength.Long).Show();  //jak sie nie polaczy wyswietla sie toast
                    }

                    if (isConnected == true)    //jak jest polaczony
                    {
                        //Intent nextActivity = new Intent(this, typeof(MainMenuActivity));
                        //nextActivity.PutExtra("IP", iPAddress.ToString());      
                        //StartActivity(nextActivity);
                    }
                }
            };

            //do autoconfiga
            buttonAutoConfig.Click += (o, e) =>
            {
                autoConfigClient = new AutoConfigAndroid();
                autoConfigClient.FinishAction = AutoConfigFinished;
                autoConfigClient.Listen();
            };

            Button button1234 = FindViewById<Button>(Resource.Id.button_send_password_1234);

            button1234.Click += (o, e) =>
            {
                if (client != null)
                {
                    client.SendPassword("1234");
                }
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