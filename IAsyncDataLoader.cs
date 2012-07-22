namespace AsyncDataLoader
{
    using System;
    using System.Drawing;

#if NETFX_CORE
    using Windows.UI.Xaml.Shapes;
#else
    using Android.Drawing;
#endif

    public interface IAsyncDataLoader
    {
        void RequestLoadForItem(int index);
        void DrawItem(int index, Rectangle destRect, Graphics graphics);

        Func<int, bool> IsVisible { get; set; }
        Func<int, bool> IsLoadable { get; set; }
        Func<int, bool> IsClearable { get; set; }
        void SetItemCount(int count);
    }
}