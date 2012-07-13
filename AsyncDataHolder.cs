namespace AsyncDataLoader
{
    public class AsyncDataHolder<T>
    {
        public AsyncDataHolder()
        {
            this.ItemData = default(T);
            this.LoadedState = LoadedState.Empty;
            this.TimeLoaded = 0;
        }

        public T ItemData;
        public LoadedState LoadedState;
        public long TimeLoaded;
    }
}