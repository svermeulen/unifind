using System;

namespace Unifind.Internal
{
    public static class Log
    {
        static NullDisposable _nullDisposable = new NullDisposable();

        public static void Info(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public static void Debug(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public static void Trace(string message, params object[] args)
        {
            UnityEngine.Debug.LogFormat(message, args);
        }

        public static void Error(string message, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(message, args);
        }

        public static IDisposable SpanDebug(string message, params object[] args)
        {
            Debug(message, args);
            return _nullDisposable;
        }

        class NullDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
