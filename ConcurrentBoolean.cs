namespace AsyncDataLoader
{
    using System.Threading;

    internal class ConcurrentBoolean
    {
        private long _value;

        private ConcurrentBoolean(bool val)
        {
            this.Set(val);
        }

        private void Set(bool val)
        {
            Interlocked.Exchange(ref this._value, val ? 1 : 0);
        }

        public static bool operator ==(ConcurrentBoolean left, ConcurrentBoolean right)
        {
            var l = Interlocked.Read(ref left._value);
            var r = Interlocked.Read(ref right._value);

            return l == r;
        }

        public static bool operator !=(ConcurrentBoolean left, ConcurrentBoolean right)
        {
            return !(left == right);
        }

        public static implicit operator ConcurrentBoolean(bool x)
        {
            return x ? True : False;
        }

        // Override the Object.GetHashCode() method:
        public override int GetHashCode()
        {
            return (int)this._value;
        }

        // Override the Object.Equals(object o) method:
        public override bool Equals(object o)
        {
            try
            {
                return this == (ConcurrentBoolean)o;
            }
            catch
            {
                return false;
            }
        }

        public static readonly ConcurrentBoolean True = new ConcurrentBoolean(true);
        public static readonly ConcurrentBoolean False = new ConcurrentBoolean(false);
    }
}