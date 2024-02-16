using System;

namespace Unifind.Internal
{
    public class UnifindAssertException : Exception
    {
        public UnifindAssertException(string message)
            : base(message) { }

        public UnifindAssertException(string format, params object[] formatArgs)
            : base(string.Format(format, formatArgs)) { }
    }

    public static class Assert
    {
        public static void That(bool condition)
        {
            if (!condition)
            {
                throw CreateException("Assert hit!");
            }
        }

        public static void That(bool condition, string message)
        {
            if (!condition)
            {
                throw CreateException(message);
            }
        }

        public static void That<T>(bool condition, string message, T arg1)
        {
            if (!condition)
            {
                throw CreateException(message, arg1!);
            }
        }

        static UnifindAssertException CreateException()
        {
            return new UnifindAssertException("Assert hit!");
        }

        public static UnifindAssertException CreateException(string message, params object[] args)
        {
            return new UnifindAssertException("Assert hit!  Details: {0}", string.Format(message, args));
        }
    }
}
