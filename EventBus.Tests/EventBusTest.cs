using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;

namespace EventBus.Tests
{
    [TestFixture]
    internal class EventBusTest
    {
        [Test]
        public void container_can_subscribe()
        {
            var eventBus = new EventBus();
            var container = new Container();

            Message message = null;
            container.SubscriberExecuted += msg => { message = msg; };

            eventBus.Register(container);
            eventBus.Post(new Message { Content = "helloworld" });
            Assert.NotNull(message);
            Assert.AreEqual(message.Content, "helloworld");
        }

        [Test]
        public void action_can_subscribe()
        {
            var eventBus = new EventBus();
            Message message = null;
            eventBus.Register<Message>(msg => { message = msg; });
            eventBus.Post(new Message { Content = "helloworld" });
            Assert.NotNull(message);
            Assert.AreEqual(message.Content, "helloworld");
        }

        [Test]
        public void container_can_unsubscribe()
        {
            var eventBus = new EventBus();
            var container = new Container();

            Message message = null;
            container.SubscriberExecuted += msg => { message = msg; };
            eventBus.Register(container);
            eventBus.Unregister(container);
            eventBus.Post(new Message { Content = "helloworld" });
            Assert.Null(message);
        }

        [Test]
        public void action_can_unsubscribe()
        {
            var eventBus = new EventBus();
            Message message = null;
            Action<Message> action = msg => { message = msg; };
            eventBus.Register(action);
            eventBus.Unregister(action);
            eventBus.Post(new Message { Content = "helloworld" });
            Assert.Null(message);
        }

        [Test]
        public void threadmode_is_post__post_in_same_caller_thread_if_called_from_a_non_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            int subscriberThreadId = 0;
            eventBus.Register<Message>(msg =>
            {
                subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                message = msg;
            }, ThreadMode.Post);

            using (var waitHandle = new ManualResetEvent(false))
            {
                int postThreadId = -1;

                new Thread(() =>
                {
                    postThreadId = Thread.CurrentThread.ManagedThreadId;
                    eventBus.Post(new Message { Content = "helloworld" });
                    waitHandle.Set();
                }).Start();

                waitHandle.WaitOne(5000);

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_post__post_in_same_caller_thread_if_called_from_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            using (var waitHandle = new ManualResetEvent(false))
            {
                int subscriberThreadId = 0;
                eventBus.Register<Message>(msg =>
                {
                    subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                    message = msg;
                    waitHandle.Set();
                }, ThreadMode.Post);

                int postThreadId = Thread.CurrentThread.ManagedThreadId;
                eventBus.Post(new Message { Content = "helloworld" });
                waitHandle.WaitOne(5000);

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_thread__post_in_same_caller_thread_if_called_from_a_non_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            int subscriberThreadId = 0;
            eventBus.Register<Message>(msg =>
            {
                subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                message = msg;
            }, ThreadMode.Thread);

            using (var waitHandle = new ManualResetEvent(false))
            {
                int postThreadId = -1;

                new Thread(() =>
                {
                    postThreadId = Thread.CurrentThread.ManagedThreadId;
                    eventBus.Post(new Message { Content = "helloworld" });
                    waitHandle.Set();
                }).Start();

                waitHandle.WaitOne(5000);

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_thread__post_in_background_thread_if_called_from_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            using (var waitHandle = new ManualResetEvent(false))
            {
                int subscriberThreadId = 0;
                eventBus.Register<Message>(msg =>
                {
                    subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                    message = msg;
                    waitHandle.Set();
                }, ThreadMode.Thread);

                int postThreadId = Thread.CurrentThread.ManagedThreadId;
                eventBus.Post(new Message { Content = "helloworld" });
                waitHandle.WaitOne(5000);

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreNotEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_async__post_in_background_thread_if_called_from_a_non_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            using (var waitHandle1 = new ManualResetEvent(false))
            using (var waitHandle2 = new ManualResetEvent(false))
            {
                int subscriberThreadId = 0;
                eventBus.Register<Message>(msg =>
                {
                    subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                    message = msg;
                    waitHandle1.Set();
                }, ThreadMode.Async);

                int postThreadId = -1;

                new Thread(() =>
                {
                    postThreadId = Thread.CurrentThread.ManagedThreadId;
                    eventBus.Post(new Message { Content = "helloworld" });
                    waitHandle2.Set();
                }).Start();

                WaitHandle.WaitAll(new WaitHandle[] { waitHandle1, waitHandle2 });

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreNotEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_async__post_in_background_thread_if_called_from_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            using (var waitHandle = new ManualResetEvent(false))
            {
                int subscriberThreadId = 0;
                eventBus.Register<Message>(msg =>
                {
                    subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                    message = msg;
                    waitHandle.Set();
                }, ThreadMode.Async);

                int postThreadId = Thread.CurrentThread.ManagedThreadId;
                eventBus.Post(new Message { Content = "helloworld" });
                waitHandle.WaitOne(5000);

                Assert.NotNull(message);
                Assert.AreEqual(message.Content, "helloworld");
                Assert.AreNotEqual(subscriberThreadId, postThreadId);
            }
        }

        [Test]
        public void threadmode_is_main__post_in_main_thread_if_called_from_main_thread()
        {
            var eventBus = new EventBus();
            eventBus.Initialize();
            Message message = null;

            int subscriberThreadId = 0;
            eventBus.Register<Message>(msg =>
            {
                subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                message = msg;
            }, ThreadMode.Main);

            int postThreadId = Thread.CurrentThread.ManagedThreadId;
            eventBus.Post(new Message { Content = "helloworld" });

            Assert.NotNull(message);
            Assert.AreEqual(message.Content, "helloworld");
            Assert.AreEqual(subscriberThreadId, postThreadId);
        }

        [Test]
        public void threadmode_is_main__post_in_main_thread_if_called_from_background_thread()
        {
            using (var waitInitHandle = new ManualResetEvent(false))
            {
                var eventBus = new EventBus();
                var form = new Form();
                int postThreadId = -1;
                form.Load += (sender, args) => {
                    eventBus.Initialize();
                    postThreadId = Thread.CurrentThread.ManagedThreadId;
                    waitInitHandle.Set();
                };
                var uiThread = new Thread(() => Application.Run(form));
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
                waitInitHandle.WaitOne(5000);

                Message message = null;

                int subscriberThreadId = 0;
                eventBus.Register<Message>(msg =>
                {
                    subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                    message = msg;
                }, ThreadMode.Main);

                using (var waitHandle = new ManualResetEvent(false))
                {
                    new Thread(() =>
                    {
                        eventBus.Post(new Message {Content = "helloworld"});
                        waitHandle.Set();
                    }).Start();

                    waitHandle.WaitOne(5000);

                    Assert.NotNull(message);
                    Assert.AreEqual(message.Content, "helloworld");
                    Assert.AreEqual(subscriberThreadId, postThreadId);
                }
            }
        }

        [Test]
        public void threadmode_is_mainorder__post_in_main_thread_if_called_from_background_thread()
        {
            using (var waitInitHandle = new ManualResetEvent(false))
            {
                var eventBus = new EventBus();
                var form = new Form();
                int postThreadId = -1;
                form.Load += (sender, args) => {
                    eventBus.Initialize();
                    postThreadId = Thread.CurrentThread.ManagedThreadId;
                    waitInitHandle.Set();
                };
                var uiThread = new Thread(() => Application.Run(form));
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
                waitInitHandle.WaitOne(5000);

                Message message = null;

                using (var waitSubscriber = new ManualResetEvent(false))
                {
                    int subscriberThreadId = 0;
                    eventBus.Register<Message>(msg =>
                    {
                        subscriberThreadId = Thread.CurrentThread.ManagedThreadId;
                        message = msg;
                        waitSubscriber.Set();
                    }, ThreadMode.MainOrder);

                    using (var waitHandle = new ManualResetEvent(false))
                    {
                        new Thread(() =>
                        {
                            eventBus.Post(new Message {Content = "helloworld"});
                            waitHandle.Set();
                        }).Start();
                        waitSubscriber.WaitOne(1000);
                        waitHandle.WaitOne(5000);

                        Assert.NotNull(message);
                        Assert.AreEqual(message.Content, "helloworld");
                        Assert.AreEqual(subscriberThreadId, postThreadId);
                    }
                }
            }
        }

        private class Container
        {
            public delegate void MessageDelegate(Message msg);

            public event MessageDelegate SubscriberExecuted;

            [Subscribe]
            private void SubscriberMethod(Message msg)
            {
                SubscriberExecuted?.Invoke(msg);
            }
        }

        private class Message
        {
            public string Content { get; set; }
        }
    }
}