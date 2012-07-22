namespace AsyncDataLoader
{
    using System;
    using System.Collections.Concurrent;
#if NETFX_CORE
    using Thread = System.Threading.Tasks.Task;
#else
    using System.Threading;
#endif

    public partial class AsyncDataLoader<T, THolder> 
    {
        public class LoaderThread
        {
            private readonly AsyncDataLoader<T, THolder> _loader;

            private readonly Thread _underlyingThread;
            private ConcurrentBoolean _isStarted;

            private readonly ConcurrentStack<LoadingData> _loadStack;
            public event EventHandler ThreadEnded;

            public LoaderThread(AsyncDataLoader<T, THolder> loader)
            {
                this._loader = loader;
                this._isStarted = false;
                this._loadStack = new ConcurrentStack<LoadingData>();

                this._underlyingThread = new Thread(this.DoWork);
                this._underlyingThread.Start();
            }

            public bool IsRunning { get { return this._isStarted == ConcurrentBoolean.True; } }

            private void DoWork()
            {
                //Console.WriteLine("Starting... " + this.GetHashCode());
                this._isStarted = true;

                while (this._loadStack.Count > 0)
                {
                    //Console.WriteLine("Working... " + this.GetHashCode());
                    LoadingData value;
                    if (this._loadStack.TryPop(out value))
                    {
                        var dataHolder = this._loader._dataArray[value.Index];

                        // don't queue if already out of range or at high speed for hi-res
                        var isLoading = dataHolder.LoadedState == LoadedState.LoadingLowRes ||
                                        dataHolder.LoadedState == LoadedState.LoadingNormal;
                        if (!isLoading || !this._loader.IsLoadable(value.Index))
                        {
                            break;
                        }

                        this._loader.ReleaseData();

                        this.PerformDataLoad(dataHolder, value.Index);

                        // Now load the HiRes
                        if (dataHolder.LoadedState != LoadedState.NormalComplete)
                        {
                            this._loader.RequestLoadForItem(value.Index);
                        }
                    }
                }

                //Console.WriteLine("Finished... " + this.GetHashCode());
                if (this._loadStack.Count > 0)
                {
                    //Console.WriteLine("Restarting... " + this.GetHashCode());
                    this.DoWork();
                }
                else
                {
                    //Console.WriteLine("Really ending... " + this.GetHashCode());
                    this._isStarted = false;

                    this.OnThreadEnded(EventArgs.Empty);
                }
            }

            protected virtual void OnThreadEnded(EventArgs e)
            {
                var handler = this.ThreadEnded;

                if (handler != null)
                {
                    handler(this, e);
                }
            }

            private void PerformDataLoad(THolder dataHolder, int index)
            {
                // load the data
                T data = this._loader.DoLoad(dataHolder, index);

                T tempItemData;

                // update the holder
                lock (dataHolder)
                {
                    // keep a temporary reference to the data for disposing of the existing item if loading the hi-res
                    // as the existing data will probably be the low res version
                    tempItemData = dataHolder.ItemData;

                    dataHolder.ItemData = data;
                    dataHolder.LoadedState = dataHolder.LoadedState == LoadedState.LoadingLowRes ? LoadedState.LowResComplete : LoadedState.NormalComplete;
                    dataHolder.TimeLoaded = DateTime.Now.Ticks;
                }

                // refresh the screen
                this._loader.Invalidate();

                // remove the old image from memory
                this._loader.DoClearDataForItem(tempItemData, index);
            }

            public void AddLoadingData(LoadingData data, bool delayed = false)
            {
                this._loadStack.Push(data);

                //Console.WriteLine("Adding: {0} - isStarted: {1} - HASH: {2}", data.Index, _isStarted == Bool.True, this.GetHashCode());

                if (!delayed)
                {
                    this.Start();
                }
            }

            public void Start()
            {
                if (this._isStarted == ConcurrentBoolean.False)
                {
                    this.DoWork();
                }
            }

            public void Clear()
            {
                this._loadStack.Clear();
            }
        }
    }
}