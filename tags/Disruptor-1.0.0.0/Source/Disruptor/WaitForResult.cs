﻿namespace Disruptor
{
    ///<summary>
    ///</summary>
    public struct WaitForResult
    {
        ///<summary>
        /// True if the <see cref="IConsumerBarrier{T}"/> was alerted, false otherwise.
        ///</summary>
        public readonly bool IsAlerted;

        /// <summary>
        /// The sequence up to which is available
        /// </summary>
        public readonly long AvailableSequence;

        ///<summary>
        ///</summary>
        ///<param name="availableSequence"></param>
        ///<param name="isAlerted"></param>
        public WaitForResult(long availableSequence, bool isAlerted)
        {
            IsAlerted = isAlerted;
            AvailableSequence = availableSequence;
        }

        ///<summary>
        /// Create a new WaitForResult in alerted state 
        ///</summary>
        public static WaitForResult AlertedResult
        {
            get
            {
                return new WaitForResult(RingBufferConvention.InitialCursorValue, true);
            }
        }
    }
}