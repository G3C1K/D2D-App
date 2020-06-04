using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Security;

namespace D2DUIv3.zz_Activities
{
    [Activity(Label = "NumpadActivity")]
    public class NumpadActivity : AppCompatActivity
    {
        public CommClientAndroid client;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            client = ClientHolder.Client;
            // client.NumpadInstantiate();
            SetContentView(Resource.Layout.numpad_submenu);

            int[] tab =
            {
                Resource.Id.one,
                Resource.Id.two,
                Resource.Id.three,
                Resource.Id.four,
                Resource.Id.five,
                Resource.Id.six,
                Resource.Id.seven,
                Resource.Id.eight,
                Resource.Id.nine,
                Resource.Id.zero,
                Resource.Id.dot,
                Resource.Id.plus,
                Resource.Id.minus,
                Resource.Id.div,
                Resource.Id.razy,
                Resource.Id.equal
            };




           
            foreach (int id in tab)
            {
                Button b = FindViewById<Button>(id);
                b.Click += NumClick;

            }





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
                this.Finish();
                return true;
            }


            return base.OnOptionsItemSelected(item);
        }




        





        
        public void NumClick(object sender, EventArgs e)
        {
            string klawisz = null;
            Button g = (Button)sender;
            
            switch (g.Text)
            {
                case "1":
                    klawisz = "1";
                    break;
                case "2":
                    klawisz = "2";
                    break;
                case "3":
                    klawisz = "3";
                    break;
                case "4":
                    klawisz = "4";
                    break;
                case "5":
                    klawisz = "5";
                    break;
                case "6":
                    klawisz = "6";
                    break;
                case "7":
                    klawisz = "7";
                    break;
                case "8":
                    klawisz = "8";
                    break;
                case "9":
                    klawisz = "9";
                    break;
                case "0":
                    klawisz = "0";
                    break;
                case "+":
                    klawisz = "ADD";
                    break;
                case "-":
                    klawisz = "SUB";
                    break;
                case "/":
                    klawisz = "DIV";
                    break;
                case ".":
                    klawisz = "DEC";
                    break;
                case "*":
                    klawisz = "MUL";
                    break;
                case "=":
                    klawisz = "EQ";
                    break;


            }
            client.SendKey(klawisz);
            
          
            

        } 

    }



}