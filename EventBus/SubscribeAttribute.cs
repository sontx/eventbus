using System;
using System.Reflection;

namespace EventBus
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscribeAttribute : Attribute
    {
        public ThreadMode ThreadMode { get; set; } = ThreadMode.Post;
    }

    /// <summary>
    /// Messages can be posted in threads different from the posting thread, so
    /// this enum defines the thread that will call the message handling method.
    /// </summary>
    public enum ThreadMode
    {
        /// <summary>
        /// Run the subscriber at the same thread as caller.
        /// </summary>
        Post = 0,

        /// <summary>
        /// Run the subscriber in the background thread. If the caller is already running in the background thread
        /// it will use <see cref="Post"/> instead, otherwise it will create new thread.
        /// </summary>
        Thread,

        /// <summary>
        /// Same as <see cref="Thread"/> but it always creates new thread to execute the subscriber.
        /// </summary>
        Async,

        /// <summary>
        /// Run in the main thread (it usually is UI thread) and wait for done.
        /// </summary>
        Main,

        /// <summary>
        /// Run in the main thread (it usually is UI thread) but return immediately.
        /// </summary>
        MainOrder
    }

    internal class Description
    {
        public ThreadMode ThreadMode { get; set; }
        public Type ParameterType { get; set; }

        public static Description FromMethodInfo(MethodInfo methodInfo)
        {
            var attribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();
            return new Description
            {
                ThreadMode = attribute.ThreadMode,
                ParameterType = methodInfo.GetParameters()[0].ParameterType
            };
        }

        public static Description FromGeneric<T>(ThreadMode threadMode)
        {
            return new Description
            {
                ThreadMode = threadMode,
                ParameterType = typeof(T)
            };
        }
    }
}