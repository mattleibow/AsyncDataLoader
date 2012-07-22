namespace AsyncDataLoader
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
#if NETFX_CORE
    using System.Drawing;
    using Windows.System.Threading;
    using Windows.UI.Xaml.Shapes;
#else
    using System.Drawing;
    using System.Threading;
    using Android.Drawing;
#endif

    /// <summary>
    ///   Asynchronous data loader that allows for loading of any daya type on another thread while storing
    ///   the current load status from empty, loading, low-res or hi-res
    /// </summary>
    /// <typeparam name="T">
    ///   The type to load
    /// </typeparam>
    public class AsyncDataLoader<T> : AsyncDataLoader<T, AsyncDataHolder<T>>
        where T : class
    {
    }

    public partial class AsyncDataLoader<T, THolder> : IAsyncDataLoader, IDisposable
        where T : class
        where THolder : AsyncDataHolder<T>, new()
    {
        private THolder[] _dataArray;
        private readonly LoaderThread _lowResStack;
        private readonly LoaderThread _hiResStack;

        public void Dispose()
        {
            if (this._dataArray != null)
            {
                foreach (IDisposable asyncDataHolder in this._dataArray.Where(x => x is IDisposable))
                {
                    asyncDataHolder.Dispose();
                }
            }

            this._lowResStack.Clear();
            this._hiResStack.Clear();
        }

        public AsyncDataLoader()
        {
            this._lowResStack = new LoaderThread(this);
            this._hiResStack = new LoaderThread(this);

            this._lowResStack.ThreadEnded += delegate
                                            {
                                                //Console.WriteLine("Starting HiRes... " + _hiResStack.GetHashCode());
                                                this._hiResStack.Start();
                                            };
        }

        /// <summary>
        ///   This action is to tell the UI that it needs to update itself.
        /// </summary>
        public Action Invalidate { get; set; }

        /// <summary>
        ///   This function must release the momory allocated for this data item.
        /// </summary>
        /// <returns>
        ///   True if the holder must be cleared or False if the data can be re-used.
        /// </returns>
        public Func<T, int, bool> DoClearDataForItem { get; set; }

        /// <summary>
        ///   This function performs the load operation.
        ///   The holder contains othe information that is used for the loading method.
        /// </summary>
        /// <returns>
        ///   The data that has been loaded. It is assigned to the holder by the caller, 
        ///   allowing you to perform any action on the existing data and then re-use it.
        /// </returns>
        public Func<THolder, int, T> DoLoad { get; set; }

        /// <summary>
        ///   This function allows you to draw the loaded data onto the canvas.
        ///   The holder provides the loaded status whether low-res or hi-res.
        /// </summary>
        public Action<THolder, int, Rectangle, Graphics> DoDraw { get; set; }

        public float MaximumHiResLoadVelocity { get; set; }

        /// <summary>
        ///   Requests the data be loaded for the specified item.
        /// </summary>
        /// <param name = 'index'>
        ///   Index.
        /// </param>
        public void RequestLoadForItem(int index)
        {
            // enqueue the loader using another thread as it may lock the ui
#if NETFX_CORE
            ThreadPool.RunAsync(delegate { this.EnqueueLoadData(index); });
#else
            ThreadPool.QueueUserWorkItem(delegate { this.EnqueueLoadData(index); });
#endif
        }

        private void EnqueueLoadData(int index)
        {
            if (this._dataArray == null)
            {
                return;
            }

            var imageHolder = this._dataArray[index];

            var loadingData = new LoadingData(index);

            lock (imageHolder)
            {
                // don't queue if already out of range or at high speed for hi-res
                var finishedLoading = imageHolder.LoadedState == LoadedState.Empty ||
                                      imageHolder.LoadedState == LoadedState.LowResComplete;
                if (finishedLoading && this.IsLoadable(index))
                {
                    imageHolder.LoadedState = imageHolder.LoadedState == LoadedState.Empty
                                                  ? LoadedState.LoadingLowRes
                                                  : LoadedState.LoadingNormal;
                }
            }

            if (imageHolder.LoadedState == LoadedState.LoadingLowRes)
            {
                this._lowResStack.AddLoadingData(loadingData);
            }
            else
            {
                this._hiResStack.AddLoadingData(loadingData, this._lowResStack.IsRunning);
            }
        }

        /// <summary>
        ///   Draws the item at <paramref name = "index" />.
        /// </summary>
        /// <param name = 'index'>
        ///   Index of the item to draw.
        /// </param>
        /// <param name = 'siteRect'>
        ///   The rectangle of the area to draw in.
        /// </param>
        /// <param name = 'graphics'>
        ///   The graphics to draw on.
        /// </param>
        public void DrawItem(int index, Rectangle siteRect, Graphics graphics)
        {
            if (this._dataArray != null)
            {
                THolder dataHolder = this._dataArray[index];

                if (dataHolder != null)
                {
                    this.DoDraw(dataHolder, index, siteRect, graphics);
                }
            }
        }

        /// <summary>
        ///   Determines whether the data item is visible at the specified index.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this data item is visible at the specified index; otherwise, <c>false</c>.
        /// </returns>
        /// <param index="0">
        ///   The index to check visibility.
        /// </param>
        public Func<int, bool> IsVisible { get; set; }
        public Func<int, bool> IsLoadable { get; set; }
        public Func<int, bool> IsClearable { get; set; }

        public void SetItemCount(int count)
        {
            this.Dispose();

            this._dataArray = new THolder[count];
            for (int i = 0; i < count; i++)
            {
                this._dataArray[i] = new THolder();
            }
        }

        /// <summary>
        /// Release resources of unused items (not visible) 
        /// </summary>
        private void ReleaseData()
        {
            if (this._dataArray == null)
                return;

            for (int i = 0; i < this._dataArray.Length; i++)
            {
                var data = this._dataArray[i];
                if (data != null && this.IsClearable(i) && data.LoadedState != LoadedState.Empty)
                {
                    if (this.DoClearDataForItem(data.ItemData, i))
                    {
                        lock (data)
                        {
                            // mark the data loader as empty
                            data.ItemData = default(T);
                            data.LoadedState = LoadedState.Empty;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///   Gets the data at the specified index in the loader.
        /// </summary>
        /// <returns>
        ///   The data at the specified index.
        /// </returns>
        /// <param name = 'index'>
        ///   Index of the item to load.
        /// </param>
        public THolder GetData(int index)
        {
            return this._dataArray != null && 0 <= index && index < this._dataArray.Length
                       ? this._dataArray[index]
                       : null;
        }

        /// <summary>
        ///   Gets all the data associated with this loader.
        /// </summary>
        /// <returns>
        ///   All the data associated with this loader.
        /// </returns>
        public IEnumerable<THolder> GetData()
        {
            if (this._dataArray != null)
            {
                var query = from data in this._dataArray
                            where data != null && data.ItemData != null
                            select data;

                return new Collection<THolder>(query.ToArray());
            }

            return new Collection<THolder>(new THolder[0]);
        }
    }
}