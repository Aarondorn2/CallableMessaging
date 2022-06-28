using System;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This class validates various ICallable types to ensure parameters are set correctly
    /// </summary>
    internal static class Validator
    {
        internal static void Validate(IConcurrentCallable concurrentCallable)
        {
            if (concurrentCallable.ConcurrencyCount() < 1)
            {
                throw new Exception("IConcurrentCallable ConcurrencyCount must be greater than 0.");
            }

            if (string.IsNullOrEmpty(concurrentCallable.ConcurrentTypeKey()))
            {
                throw new Exception("IDebounceCallable not set with TypeKey.");
            }
        }

        internal static void Validate(IDebounceCallable debounceCallable)
        {
            if (debounceCallable.DebounceInterval().TotalMilliseconds <= 0)
            {
                throw new Exception("IDebounceCallable DebounceInterval must be greater than 0.");
            }

            if (string.IsNullOrEmpty(debounceCallable.DebounceInstanceKey)
                || string.IsNullOrEmpty(debounceCallable.DebounceTypeKey()))
            {
                throw new Exception($"IDebounceCallable not set with InstanceKey ({debounceCallable.DebounceInstanceKey}) " +
                    $"and TypeKey ({debounceCallable.DebounceTypeKey()}).");
            }
        }

        internal static void Validate(IRateLimitCallable limitCallable)
        {
            if (limitCallable.RateLimitPeriod().TotalMilliseconds <= 0)
            {
                throw new Exception("IRateLimitCallable RateLimitPeriod must be greater than 0.");
            }

            if (limitCallable.RateLimitPerPeriod() < 1)
            {
                throw new Exception("IRateLimitCallable RateLimitPerPeriod must be greater than 0.");
            }
        }

        internal static void Validate(IRepeatedCallable repeatedCallable)
        {
            if (repeatedCallable.RepeatedCurrentCall < 0)
            {
                throw new Exception("IRepeatedCallable RepeatedCurrentCall must be null or greater than or equal to 0.");
            }

            if (repeatedCallable.RepeatedMaxCalls() < 1)
            {
                throw new Exception("IRepeatedCallable RepeatedMaxCalls must be greater than 0.");
            }

            if (repeatedCallable.RepeatedTimeBetweenCalls().TotalMilliseconds <= 0)
            {
                throw new Exception("IRepeatedCallable RepeatedTimeBetweenCalls must be greater than 0.");
            }

            if (repeatedCallable.RepeatedCurrentCall >= repeatedCallable.RepeatedMaxCalls())
            {
                // we should not be putting a RepeatedCallable on the queue if it's already reached MaxTries
                throw new Exception("IRepeatedCallable exceeds MaxTries.");
            }
        }
    }
}
