namespace AsyncDataLoader
{
    /// <summary>
    ///   The struct to contain information needed to be passed to the loading thread.
    /// </summary>
    public class LoadingData
    {
        internal bool IsEmpty()
        {
            return this.Index < 0;
        }

        internal static readonly LoadingData Empty = new LoadingData(-1);

        internal readonly int Index;

        internal LoadingData(int index)
        {
            this.Index = index;
        }
    }
}