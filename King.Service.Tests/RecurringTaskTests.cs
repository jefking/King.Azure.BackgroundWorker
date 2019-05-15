﻿namespace King.Service.Tests.Unit
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class RecurringTaskTests
    {
        [Test]
        public void Constructor()
        {
            new RecurringHelper(100);
        }

        [Test]
        public void ConstructorPeriodNegative()
        {
            Assert.That(() => new RecurringHelper(-50), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void IsIRunnable()
        {
            Assert.IsNotNull(new RecurringHelper(100) as IRunnable);
        }

        [Test]
        public void IsIDisposable()
        {
            Assert.IsNotNull(new RecurringHelper(100) as IDisposable);
        }

        [Test]
        public void Dispose()
        {
            using (var m = new RecurringHelper(100))
            {
            }
        }

        [Test]
        public void TestDispose()
        {
            var m = new RecurringHelper(100);
            m.TestDispose();
        }

        [Test]
        public void TestDisposeTimer()
        {
            var m = new RecurringHelper(100);
            m.Start();
            m.TestDispose();
        }

        [Test]
        public void Run()
        {
            var m = new RecurringHelper(100);
            m.Run();
        }

        [Test]
        public void RunStateNull()
        {
            var m = new RecurringHelper(100);
            m.Run(null);
        }

        [Test]
        public void RunThrows()
        {
            var m = new RecurringHelper(100)
            {
                Throw = true,
            };

            m.Run(new object());
        }

        [Test]
        public void Start()
        {
            var m = new RecurringHelper(100);
            var success = m.Start();
            Assert.IsTrue(success);
        }

        [Test]
        public void Stop()
        {
            var m = new RecurringHelper(100);
            var success = m.Stop();
            Assert.IsTrue(success);
        }

        [Test]
        public void StartStop()
        {
            var m = new RecurringHelper(100);
            var success = m.Start();
            Assert.IsTrue(success);
            success = m.Stop();
            Assert.IsTrue(success);
        }

        [Test]
        public void StartZeroStop()
        {
            Assert.That(() => new RecurringHelper(0), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ChangeTimingZero()
        {
            using (var tm = new RecurringHelper(100))
            {
                Assert.That(() => tm.ChangeTiming(TimeSpan.Zero), Throws.TypeOf<ArgumentException>());
            }
        }

        [Test]
        public void ChangeTimingWithoutStart()
        {
            using (var tm = new RecurringHelper(100))
            {
                tm.ChangeTiming(TimeSpan.FromSeconds(100));
            }
        }

        [Test]
        public void ChangeTiming()
        {
            using (var tm = new RecurringHelper(100))
            {
                tm.Start();
                tm.ChangeTiming(TimeSpan.FromSeconds(100));
            }
        }
    }
}