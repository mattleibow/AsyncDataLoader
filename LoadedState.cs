namespace AsyncDataLoader
{
    public enum LoadedState
    {
        /// <summary>
        /// There is no data at all.
        /// </summary>
        Empty,
        
        /// <summary>
        /// The low-res data is currently being loaded.
        /// </summary>
        LoadingLowRes,
        /// <summary>
        /// The low-res data is now stored and ready to use.
        /// </summary>
        LowResComplete, 
        
        /// <summary>
        /// The hi-res version of the data is being loaded. 
        /// Depending on whether the low-res data was loaded first, the data may be the low-res 
        /// version that can be used in the meantime.
        /// </summary>
        LoadingNormal,
        /// <summary>
        /// The hi-res version of the data is loaded and ready to be used.
        /// </summary>
        NormalComplete
    }
}