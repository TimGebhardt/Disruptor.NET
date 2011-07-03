using System;

namespace Disruptor.Test.Support
{
    public class StubEntry : Entry
    {
        public int Value { get; set; }
        public string teststring { get; set; }

        public StubEntry(int i)
        {
            Value = i;
        }

        public void copy(StubEntry entry)
        {
            Value = entry.Value;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime*result + Value;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if(GetType() != obj.GetType()) return false;
            var other = (StubEntry) obj;

            return Value == other.Value;
        }
    }

    internal class StubFactory : IEntryFactory<StubEntry>
    {
        public StubEntry Create()
        {
            return new StubEntry(-1);
        }
    }
}
