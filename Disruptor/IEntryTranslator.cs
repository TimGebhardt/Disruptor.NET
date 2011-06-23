/**
 * Implementations translate a other data representations into {@link IEntry}s claimed from the {@link RingBuffer}
 *
 * @param <T> IEntry implementation storing the data for sharing during exchange or parallel coordination of an event.
 */

namespace Disruptor
{
    public interface IEntryTranslator<T> where T : IEntry
    {
        /**
     * Translate a data representation into fields set in given {@link IEntry}
     *
     * @param entry into which the data should be translated.
     * @return the resulting entry after it has been updated.
     */
        T TranslateTo(T entry);
    }
}