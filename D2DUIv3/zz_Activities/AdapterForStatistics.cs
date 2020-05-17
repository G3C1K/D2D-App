using System;

using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using Android.Runtime;

namespace D2DUIv3
{
    class AdapterForStatistics : RecyclerView.Adapter
    {
        public event EventHandler<AdapterForStatisticsClickEventArgs> ItemClick;
        public event EventHandler<AdapterForStatisticsClickEventArgs> ItemLongClick;
        public string[] items;
        DataViewHolder viewHolderV2;

        public AdapterForStatistics(string[] data)
        {
            items = data;
        }

        public void setItems(string[] data)
        {
            this.items = data;
            foreach(var item in items)
            {
                ((DataViewHolder)viewHolderV2).setDataDetails(item);
            }
            NotifyDataSetChanged();
        }

        class DataViewHolder : RecyclerView.ViewHolder
        {
            private TextView name;
            private TextView stat;

            public DataViewHolder(View itemView) : base(itemView)
            {
                name = itemView.FindViewById<TextView>(Resource.Id.textView_left);
                stat = itemView.FindViewById<TextView>(Resource.Id.textView_right);
            }

            public DataViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public void setDataDetails(string item)
            {
                var HardwareSeparated = item.Split(':');

                name.Text = HardwareSeparated[0];
                stat.Text = HardwareSeparated[1];
            }

            public DataViewHolder(View itemView, Action<AdapterForStatisticsClickEventArgs> clickListener,
                            Action<AdapterForStatisticsClickEventArgs> longClickListener) : base(itemView)
            {
                name = itemView.FindViewById<TextView>(Resource.Id.textView_left);
                stat = itemView.FindViewById<TextView>(Resource.Id.textView_right);
                itemView.Click += (sender, e) => clickListener(new AdapterForStatisticsClickEventArgs { View = itemView, Position = AdapterPosition });
                itemView.LongClick += (sender, e) => longClickListener(new AdapterForStatisticsClickEventArgs { View = itemView, Position = AdapterPosition });
            }
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {

            //Setup your layout here
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.my_recycler_item, parent, false);
            //var id = Resource.Layout.__YOUR_ITEM_HERE;
            //itemView = LayoutInflater.From(parent.Context).
            //       Inflate(id, parent, false);

            var vh = new DataViewHolder(itemView, OnClick, OnLongClick);
            return vh;
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            string item = items[position];
            viewHolderV2 = (DataViewHolder)viewHolder;
            // Replace the contents of the view with that element
            //var holder = (DataViewHolder)viewHolder;
            //holder.TextView.Text = items[position];
            //holder.setDataDetails(item);
            ((DataViewHolder)viewHolder).setDataDetails(item);
        }

        public override int ItemCount => items.Length;

        void OnClick(AdapterForStatisticsClickEventArgs args) => ItemClick?.Invoke(this, args);
        void OnLongClick(AdapterForStatisticsClickEventArgs args) => ItemLongClick?.Invoke(this, args);

    }

    //public class AdapterForStatisticsViewHolder : RecyclerView.ViewHolder
    //{
    //    public View TextView { get; set; }


    //    public AdapterForStatisticsViewHolder(View itemView, Action<AdapterForStatisticsClickEventArgs> clickListener,
    //                        Action<AdapterForStatisticsClickEventArgs> longClickListener) : base(itemView)
    //    {
    //        TextView = itemView.FindViewById(Resource.Id.textView_left);
    //        itemView.Click += (sender, e) => clickListener(new AdapterForStatisticsClickEventArgs { View = itemView, Position = AdapterPosition });
    //        itemView.LongClick += (sender, e) => longClickListener(new AdapterForStatisticsClickEventArgs { View = itemView, Position = AdapterPosition });
    //    }
    //}

    public class AdapterForStatisticsClickEventArgs : EventArgs
    {
        public View View { get; set; }
        public int Position { get; set; }
    }
}