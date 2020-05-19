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
            //client.NumpadInstantiate();
            



            SetContentView(Resource.Layout.numpad_submenu);
            // Create your application here
        }


        [Java.Interop.Export("NumClick")]
        public void NumClick(View v)
        {
            
            string klawisz = "nic";
            Button guzik = (Button)v;
            switch (guzik.Text)
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

                
            }
            client.SendKey(klawisz);
           
        } 

    }



}