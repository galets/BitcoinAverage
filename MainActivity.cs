﻿using System;
using System.Linq;
using Android.App;
using Android.Os;
using Android.Widget;
using Dot42;
using Dot42.Manifest;
using Android.Util;
using Org.Json;
using Java.Util;
using System.Threading.Tasks;
using Android.View;
using Java.Util.Concurrent;
using BitcoinAverage._42;
using Android.Graphics;

[assembly: Application("Bitcoin Average", Icon="@drawable/icon")]
[assembly: UsesPermission(Android.Manifest.Permission.INTERNET)]

namespace BitcoinAverage
{
    [Activity]
    public class MainActivity : Activity
    {
        TextView txtLastPrice { get { return FindViewById<TextView>(R.Ids.txtLastPrice); } }
        TextView txtAsk { get { return FindViewById<TextView>(R.Ids.txtAsk); } }
        TextView txtBid { get { return FindViewById<TextView>(R.Ids.txtBid); } }
        TextView txtAvg { get { return FindViewById<TextView>(R.Ids.txtAvg); } }
        TextView txtVolume { get { return FindViewById<TextView>(R.Ids.txtVolume); } }

        TextView txtPricesOnExchanges { get { return FindViewById<TextView>(R.Ids.txtPricesOnExchanges); } }
        TableLayout tblExchanges { get { return FindViewById<TableLayout>(R.Ids.tblExchanges); } }

        TextView[] xExchange { get; set; }
        TextView[] xVolume { get; set; }
        TextView[] xPrice { get; set; }

        Ticker lastTicker = new Ticker();
        ScheduledThreadPoolExecutor updater;
        int originalTextColor;

        protected override void OnCreate(Bundle savedInstance)
        {
            base.OnCreate(savedInstance);

            SetContentView(R.Layouts.MainLayout);

            tblExchanges.SetVisibility(View.INVISIBLE);
            txtPricesOnExchanges.SetVisibility(View.INVISIBLE);

            originalTextColor = txtLastPrice.CurrentTextColor;

            xExchange = new[] 
            {
                FindViewById<TextView>(R.Ids.txtRow0Exchange),
                FindViewById<TextView>(R.Ids.txtRow1Exchange),
                FindViewById<TextView>(R.Ids.txtRow2Exchange),
                FindViewById<TextView>(R.Ids.txtRow3Exchange),
                FindViewById<TextView>(R.Ids.txtRow4Exchange),
                FindViewById<TextView>(R.Ids.txtRow5Exchange),
                FindViewById<TextView>(R.Ids.txtRow6Exchange),
                FindViewById<TextView>(R.Ids.txtRow7Exchange),
                FindViewById<TextView>(R.Ids.txtRow8Exchange),
                FindViewById<TextView>(R.Ids.txtRow9Exchange),
            };

            xVolume = new[] 
            {
                FindViewById<TextView>(R.Ids.txtRow0Volume),
                FindViewById<TextView>(R.Ids.txtRow1Volume),
                FindViewById<TextView>(R.Ids.txtRow2Volume),
                FindViewById<TextView>(R.Ids.txtRow3Volume),
                FindViewById<TextView>(R.Ids.txtRow4Volume),
                FindViewById<TextView>(R.Ids.txtRow5Volume),
                FindViewById<TextView>(R.Ids.txtRow6Volume),
                FindViewById<TextView>(R.Ids.txtRow7Volume),
                FindViewById<TextView>(R.Ids.txtRow8Volume),
                FindViewById<TextView>(R.Ids.txtRow9Volume),
            };

            xPrice = new[] 
            {
                FindViewById<TextView>(R.Ids.txtRow0Price),
                FindViewById<TextView>(R.Ids.txtRow1Price),
                FindViewById<TextView>(R.Ids.txtRow2Price),
                FindViewById<TextView>(R.Ids.txtRow3Price),
                FindViewById<TextView>(R.Ids.txtRow4Price),
                FindViewById<TextView>(R.Ids.txtRow5Price),
                FindViewById<TextView>(R.Ids.txtRow6Price),
                FindViewById<TextView>(R.Ids.txtRow7Price),
                FindViewById<TextView>(R.Ids.txtRow8Price),
                FindViewById<TextView>(R.Ids.txtRow9Price),
            };
        }

        protected override void OnResume()
        {
            base.OnResume();

            new Task(() => DoUpdateTicker(true)).Start();

            updater = new ScheduledThreadPoolExecutor(1);
            updater.ScheduleAtFixedRate(() => DoUpdateTicker(false), 15, 15, TimeUnit.SECONDS);
        }

        protected override void OnPause()
        {
            updater.Shutdown();

            base.OnPause();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(R.Menus.Menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.GetItemId())
            {
                case R.Ids.item1:
                    new Task(() => DoUpdateTicker(true)).Start();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        void SetTextAndColor(TextView view, string format, double value, double lastValue)
        {
            view.Text = value < 0 ? "N/A" : string.Format(format, value);
            if (value >= 0 && lastValue >= 0 && value != lastValue)
            {
                view.SetTextColor(value > lastValue ? Color.GREEN : Color.RED);
            }
            //else
            //{
            //    view.SetTextColor(originalTextColor);
            //}
        }

        void DoUpdateTicker(bool showPopup)
        {
            ProgressDialog pd = null;
            try
            {
                if (showPopup)
                {
                    RunOnUiThread(() =>
                    {
                        pd = new ProgressDialog(this);
                        pd.SetTitle("Getting data...");
                        pd.Show();
                    });
                }

                var content = Utility.WebGet("https://api.bitcoinaverage.com/ticker/USD");
                var tkr = new Ticker(content);

                RunOnUiThread(() =>
                {
                    SetTextAndColor(txtLastPrice, "{0:C}", tkr.last, lastTicker.last);
                    SetTextAndColor(txtAsk, "{0:C}", tkr.ask, lastTicker.ask);
                    SetTextAndColor(txtBid, "{0:C}", tkr.bid, lastTicker.bid);
                    SetTextAndColor(txtAvg, "{0:C}", tkr.avg, lastTicker.avg);
                    SetTextAndColor(txtVolume, "฿{0:0,000.00}", tkr.total_vol, lastTicker.total_vol);
                });

                lastTicker = tkr;

                content = Utility.WebGet("https://api.bitcoinaverage.com/exchanges/USD");
                var xch = Exchange.ReadJson(content)
                    .OrderByDescending(x => x.volume_percent)
                    .Take(xExchange.Length)
                    .ToArray();

                RunOnUiThread(() =>
                {
                    for (int i = 0; i < xExchange.Length; ++i)
                    {
                        if (i >= xch.Length)
                        {
                            xExchange[i].Text = "";
                            xVolume[i].Text = "";
                            xPrice[i].Text = "";
                        }
                        else
                        {
                            xExchange[i].Text = xch[i].name;
                            xVolume[i].Text = string.Format("{0:0.00}%", xch[i].volume_percent);
                            xPrice[i].Text = string.Format("{0:C}", xch[i].last);
                        }
                    }
                    txtPricesOnExchanges.SetVisibility(View.VISIBLE);
                    tblExchanges.SetVisibility(View.VISIBLE);
                });


            }
            catch (Exception ex)
            {
                Log.E("Main", "DoUpdateTicker failed", ex);

                RunOnUiThread(() =>
                {
                    var toast = new Toast(this);
                    toast.SetText("DoUpdateTicker failed");
                    toast.Show();
                });
            }
            finally
            {
                if (pd != null)
                {
                    RunOnUiThread(() => pd.Dismiss());
                }
            }
        }
    }
}
