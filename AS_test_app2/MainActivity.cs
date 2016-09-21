using System;
using System.Data;
using System.Data.SqlClient;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Graphics;
using Java.Net;
using Java.IO;
using System.Net;
using System.IO;
using Android.Webkit;
using Android.Util;
using System.Collections.ObjectModel;
using System.Collections.Generic;
//using MySql.Data; //need to install this package via NuGet and then add reference
// it will need System.Drawing.dll from .NET framework, which is Windows library...
//using MySql.Data.MySqlClient;

//Xamarin mySql plugin
//https://components.xamarin.com/gettingstarted/MySQL-Plugin
using MySql.Data;
using MySql.Data.MySqlClient;

namespace AS_test_app2
{
    public class myAdditionalUtils
    {
        //connection string, edit if needed
        static string ConnectionString = "Server=sql7.freesqldatabase.com;Port=3306;database=sql7136722;User Id=sql7136722;Password=VBCmfaScje;";


        public static List<string> fetch_from_server_DB() //fetching from database
        {

            //preparing list that will contain data entries from the database
            List<string> values = new List<string>();
            values.Add("Already subsribed:");

            // to use sqlClient one need to add Reference in solution explorer
            // To make everything correctly I need to check 'West' encoding in project Build options
            // http://stackoverflow.com/questions/37779459/no-data-available-for-encoding-1252-xamarin


            try
            {
                //in a “using” block  SqlConnection closed on return or exception
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    // opening connection
                    connection.Open();
                    // Selecting table1
                    using (MySqlCommand command = new MySqlCommand("select Name from table1", connection))
                    using (MySqlDataReader reader = command.ExecuteReader())
                        while (reader.Read())
                        {
                            string res = reader.GetString(0);//The 0 stands for "the 0'th column", so the first column of the result.
                                                             // Do somthing with this rows string, for example to put them in to a list  
                            values.Add(res);


                        }

                }
            }
            catch (Exception e) { values.Add("Error connecting to server:" + e.ToString()); }

            return values;

        }

        public static String mySql_inject_DB(String value, String message)
        {

            try
            {
                using (MySqlConnection connection = new MySqlConnection(ConnectionString))
                {
                    // opening connection
                    connection.Open();

                    // writing value into table, allows duplicates for testing
                    using (MySqlCommand command = new MySqlCommand("INSERT INTO table1 (Name,Message) VALUES(\"" + value + "\",\""+message+"\")", connection))
                    {
                        command.ExecuteNonQuery();
                    }

                }
            } catch (Exception e) { return ("Error connecting to server:" + e.ToString()); }

            return ("Thanks! Return and check the database below"); // let it be so for this instance
        }

    }//myAdditionalUtils





    [Activity(Label = "AS_test_app2", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        private WebView web_view1; //login form
        private ImageView user_image;
        private TextView text_view1; //status messages
        private Button RefreshButton;





        //needed to make pages open in App, not in the browser 
        //You must implement WebViewClient class and Override shouldOverrideURLLoading() method in this class.  
        // Because webview just opens your "exactly link", if that link redirects other links, android will open default browser for this action. 
        //http://stackoverflow.com/questions/5561709/opening-webview-not-in-new-browser 
        // web_view3.SetWebViewClient(new WebViewClient()); 


        private class Callback : WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(WebView view, String url)
            {
                //If WebViewClient is provided, return true means the host application handles the url, while return false means the current WebView handles the url.
                return (false);
            }

            //handle FB popup redirect url
            //http://stackoverflow.com/questions/12648099/making-facebook-login-work-with-an-android-webview
            // see one more addition in  MyWebChromeClient
            public override void OnPageFinished(WebView view, String url)
            {
                if (url.StartsWith("https://www.facebook.com/plugins/close_popup"))
                {

                    view.LoadUrl(url);
                    return;

                }
                base.OnPageFinished(view, url);

            }
        }

        // grabbig javascript alert from webview
        // http://stackoverflow.com/questions/3298597/how-to-get-return-value-from-javascript-in-webview-of-android
        // in the onJsAlert call "message" will contain the returned value.
        // Do not intercept alerts, because they stop the page

        sealed class MyWebChromeClient : WebChromeClient
        {
            //needed to store activity to access FindViewById later
            public MainActivity activity;

            public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
            {

                //Log.Debug("LogTag", message);
                //result.Confirm();
                TextView text_view1 = (TextView)activity.FindViewById(Resource.Id.textView1);

                myConsoleResponseHandler(consoleMessage.Message());

                text_view1.Text = consoleMessage.Message();
                return true;
            }
            
            // if Facebook returned data, we handle it
            private void myConsoleResponseHandler(String mydata)
            {
                if (mydata.StartsWith("API"))
                {
                    //we logined in, so we hide Login form
                    WebView web_view1 = activity.FindViewById<WebView>(Resource.Id.webView1);
                    web_view1.Visibility = ViewStates.Invisible;

                    // and show previously hidden fields
                    TextView textView_hidden = activity.FindViewById<TextView>(Resource.Id.textView_hidden);
                    textView_hidden.Visibility = ViewStates.Visible;
                    textView_hidden.Text = "Enter your essage:";  // now it is static, but can be some additional indication

                    // One can have a number of these fields and they can be customized to be numeric(Phone), email, etc.
                    EditText editText1 = activity.FindViewById<EditText>(Resource.Id.editText1);
                    editText1.Visibility = ViewStates.Visible;


                    // unpacking received data
                    String[] separated = mydata.Split(':');

                    //setting picture
                    ImageView user_image = (ImageView)activity.FindViewById(Resource.Id.imageView1);
                    user_image.SetImageBitmap(getFacebookProfilePicture(separated[1]));

                    TextView user_name = (TextView)activity.FindViewById(Resource.Id.textView2);
                    user_name.Text = separated[2];

                    // and show Thanks button
                    Button ThanksButton = activity.FindViewById<Button>(Resource.Id.ThanksButton);
                    ThanksButton.Visibility = ViewStates.Visible;
                    ThanksButton.Click += delegate {
                        //Calling garbage collector, maybe there are obsolete pages, before creating the new one
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

                        Intent intent = new Intent(activity, typeof(ThanksActivity));

                        //Informing user that he/she was added to the database - or caught error                        
                        intent.PutExtra("db_message", myAdditionalUtils.mySql_inject_DB(separated[2], editText1.Text.ToString()));//can transfer data here
                        activity.StartActivity(intent);
                    };                   
                    

                }
            }

            // additional attempt to handle FB popup
            public override void OnCloseWindow(WebView view)
            {
                view.LoadUrl("http://d.cc.ua/misc/socialnetwork_test/fblogin.html");

            }

        }


        //fetch picture from profile

        public static Bitmap getFacebookProfilePicture(String userID)
        {
            String url = "https://graph.facebook.com/" + userID + "/picture?type=large";

            // creating bitmat 1x1; It will be used if user does not have photo
            Bitmap mybitmap = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888); ;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream receiveStream = response.GetResponseStream();

                // read the stream
                mybitmap = BitmapFactory.DecodeStream(receiveStream);

                receiveStream.Close();
                response.Close();
            }
            catch (Exception e) { }

            return mybitmap;
        }

        // initialization or refresh of the webview
        private WebView WebView_initialize(MainActivity activity) {

            // hide our login/error text            
            TextView textView_hidden = activity.FindViewById<TextView>(Resource.Id.textView_hidden);
            textView_hidden.Visibility = ViewStates.Invisible;
            // hide thanks Bitton
            Button ThanksButton = activity.FindViewById<Button>(Resource.Id.ThanksButton);
            ThanksButton.Visibility = ViewStates.Invisible;
            // hide Edittext
            EditText editText1 = activity.FindViewById<EditText>(Resource.Id.editText1);
            editText1.Visibility = ViewStates.Invisible;

            WebView web_view1;
            // initializing login form
            web_view1 = FindViewById<WebView>(Resource.Id.webView1);
            //if button was hidden, unhide it
            web_view1.Visibility = ViewStates.Visible;

            //preparing webview
            web_view1.Settings.JavaScriptEnabled = true;
            web_view1.SetWebViewClient(new Callback());  //http://stackoverflow.com/questions/5561709/opening-webview-not-in-new-browser

            //passing activity methods to webChromeClient
            MyWebChromeClient client = new MyWebChromeClient();
            client.activity = this;
            web_view1.SetWebChromeClient(client);

            //clearing cookies          
            Android.Webkit.CookieManager.Instance.RemoveAllCookie();
            web_view1.ClearCache(true);
            web_view1.ClearFormData();
            //Android.Webkit.CookieManager.Instance.SetAcceptCookie(false); //no login with this


            //web_view1.setSavePassword(false);
            // deprecated: webWiew is no saving passwords anymore 

            // loading fb login form
            web_view1.LoadUrl("http://d.cc.ua/misc/socialnetwork_test/fblogin.html");

            //grabbing data from web_view, see 
            //http://stackoverflow.com/questions/3298597/how-to-get-return-value-from-javascript-in-webview-of-android
            //and
            //http://stackoverflow.com/questions/5264489/how-do-i-pass-return-values-from-a-javascript-function-to-android

            return web_view1;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            web_view1 = WebView_initialize(this);

            //testing



            // ImageViews
            user_image = (ImageView)FindViewById(Resource.Id.imageView1);
            //user_image.SetImageBitmap(getFacebookProfilePicture("137802996677019"));

            //text_view
            text_view1 = (TextView)FindViewById(Resource.Id.textView1);




            //open databese page
            Button button = FindViewById<Button>(Resource.Id.MyButton);

            button.Click += delegate {
                //Calling garbage collector, maybe there are obsolete pages, before creating the new one
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

                Intent intent = new Intent(this, typeof(DatabaseActivity));
                //intent.PutExtra(...) //can transfer data here
                this.StartActivity(intent);
            };



            //refresh Webview
            RefreshButton = FindViewById<Button>(Resource.Id.RefreshButton);
            RefreshButton.Click += delegate { web_view1 = WebView_initialize(this); };

        }
    }//MainActivity




    [Activity(Label = "Database", MainLauncher = true, Icon = "@drawable/icon")]
    public class DatabaseActivity : Activity
    {
        private TextView text_view_db; //title
        private ListView myList;

        protected override void OnCreate(Bundle bundle)
        {

            base.OnCreate(bundle);

            // Set our view from the "dbview" layout resource
            SetContentView(Resource.Layout.dbview);

            text_view_db = FindViewById<TextView>(Resource.Id.textView_db);
            text_view_db.Text = "Database recordings:";

            // initializing listview
            //Listview itself is scrollable.
            myList = (ListView)FindViewById(Resource.Id.lvMain);

            // getting list from mySQL server:
            String[] values_arr = myAdditionalUtils.fetch_from_server_DB().ToArray();
            ArrayAdapter<String> adapter = new ArrayAdapter<String>(this, Resource.Layout.listelem, Resource.Id.ListElem, values_arr);
            myList.Adapter = adapter;


        }

    }//DatabaseActivity

    [Activity(Label = "Thanks", MainLauncher = true, Icon = "@drawable/icon")]
    public class ThanksActivity : Activity
    {
        private TextView thankstxt; //title

        protected override void OnCreate(Bundle bundle)
        {

            base.OnCreate(bundle);
            // Set our view again from the "dbview" layout resource for simplicity
            SetContentView(Resource.Layout.dbview);

            thankstxt = FindViewById<TextView>(Resource.Id.textView_db);
            thankstxt.Text = Intent.GetStringExtra("db_message") ?? "0";
        }

        } //Thanksactivity
}

