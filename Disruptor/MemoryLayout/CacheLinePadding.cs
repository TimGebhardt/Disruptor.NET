using System.Runtime.InteropServices;

namespace Disruptor.MemoryLayout
{
    ///<summary>
    /// 
    ///</summary>
    [StructLayout(LayoutKind.Explicit, Size = 7 * 4)]
    public struct CacheLinePadding
    {
        ///<summary>
        ///</summary>
        [FieldOffset(6 * 4)]
        public readonly long Data;
    }
}