﻿namespace King.Service.Tests
{
    using King.Service;
    using King.Service.Scalability;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class AutoScalerTests
    {
        #region Helper
        private class AutoScalerHelper : AutoScaler<object>
        {
            #region Constructors
            /// <summary>
            /// Default Constructor
            /// </summary>
            public AutoScalerHelper(object configuration = null, byte minimum = 1, byte maximum = 2, byte checkScaleInMinutes = 20)
                : base(configuration, minimum, maximum, checkScaleInMinutes)
            {
            }

            /// <summary>
            /// Mockable Constructor
            /// </summary>
            public AutoScalerHelper(IScaler<object> scaler, object configuration = null, byte minimum = 1, byte maximum = 2, byte checkScaleInMinutes = 20)
                : base(scaler, configuration, minimum, maximum, checkScaleInMinutes)
            {
            }
            #endregion

            public object passed = null;

            public override IEnumerable<IScalable> ScaleUnit(object data)
            {
                this.passed = data;
                yield return new AdaptiveHelper();
            }
        }
        #endregion

        [Test]
        public void Constructor()
        {
            new AutoScalerHelper();
        }

        [Test]
        public void ConstructorMinimumGreaterThanMaximum()
        {
            Assert.That(() => new AutoScalerHelper(new object(), 100, 1), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ConstructorMinimumZero()
        {
            new AutoScalerHelper(new object(), 0, 1);
        }

        [Test]
        public void ConstructorScalerNull()
        {
            Assert.That(() => new AutoScalerHelper(null, new object(), 1, 1, 1), Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void IsITaskFactory()
        {
            Assert.IsNotNull(new AutoScalerHelper() as ITaskFactory<object>);
        }

        [Test]
        public void Dispose()
        {
            using (new AutoScalerHelper())
            {
            }
        }

        [Test]
        public void IsRecurringTask()
        {
            Assert.IsNotNull(new AutoScalerHelper() as RecurringTask);
        }

        [Test]
        public void Minimum()
        {
            var scaler = new AutoScalerHelper(new object(), 1, 100);
            Assert.AreEqual(1, scaler.InstanceCount.Minimum);
        }

        [Test]
        public void Maximum()
        {
            var scaler = new AutoScalerHelper(new object(), 100, 150);
            Assert.AreEqual(150, scaler.InstanceCount.Maximum);
        }

        [Test]
        public void InstanceCountDefault()
        {
            var scaler = new AutoScalerHelper();
            Assert.AreEqual(1, scaler.InstanceCount.Minimum);
            Assert.AreEqual(2, scaler.InstanceCount.Maximum);
        }

        [Test]
        public void Tasks()
        {
            var config = new object();
            var scaler = new AutoScalerHelper(config);
            var unit = scaler.Tasks();

            Assert.IsNotNull(unit);
            Assert.AreEqual(1, unit.Count());
            Assert.IsNotNull(unit.First() as AdaptiveHelper);
            Assert.AreEqual(config, scaler.passed);
        }

        [Test]
        public void ScaleUnit()
        {
            var scaler = new AutoScalerHelper();
            var unit = scaler.ScaleUnit(null);

            Assert.IsNotNull(unit);
            Assert.AreEqual(1, unit.Count());
            Assert.IsNotNull(unit.First() as AdaptiveHelper);
        }
        
        [Test]
        public void RunIsFirstRun()
        {
            var s = Substitute.For<IScaler<object>>();
            var config = new object();
            s.IsFirstRun(1).Returns(true);

            var scaler = new AutoScalerHelper(s, config);

            s.Initialize(1, scaler, config, Arg.Any<string>());

            scaler.Run();

            s.Received().IsFirstRun(1);
            s.Received().Initialize(1, scaler, config, Arg.Any<string>());
        }

        [Test]
        public void RunScaleUp()
        {
            var s = Substitute.For<IScaler<object>>();
            s.IsFirstRun(1).Returns(false);
            s.ShouldScale().Returns(true);
            s.CurrentUnits.Returns(0);
            var config = new object();

            var scaler = new AutoScalerHelper(s, config);
            s.ScaleUp(scaler, config, Arg.Any<string>());

            scaler.Run();

            s.Received().IsFirstRun(1);
            s.Received().ShouldScale();
            var r = s.Received().CurrentUnits;
            s.Received().ScaleUp(scaler, config, Arg.Any<string>());
        }

        [Test]
        public void RunScaleDown()
        {
            var s = Substitute.For<IScaler<object>>();
            s.IsFirstRun(1).Returns(false);
            s.ShouldScale().Returns(false);
            s.CurrentUnits.Returns(100);
            s.ScaleDown(Arg.Any<string>());

            var scaler = new AutoScalerHelper(s, null);
            scaler.Run();

            s.Received().IsFirstRun(1);
            s.Received().ShouldScale();
            var r = s.Received().CurrentUnits;
            s.Received().ScaleDown(Arg.Any<string>());
        }

        [Test]
        public void RunOptimalShouldntScale()
        {
            var config = new object();
            var s = Substitute.For<IScaler<object>>();
            s.IsFirstRun(1).Returns(false);
            s.ShouldScale().Returns((bool?)null);

            var scaler = new AutoScalerHelper(s, config);
            scaler.Run();

            s.Received().IsFirstRun(1);
            s.Received(0).ScaleDown(Arg.Any<string>());
            s.Received(0).ScaleUp(scaler, config, Arg.Any<string>());
        }

        [Test]
        public void RunOptimalMax()
        {
            var config = new object();
            var s = Substitute.For<IScaler<object>>();
            s.IsFirstRun(1).Returns(false);
            s.ShouldScale().Returns(true);
            s.CurrentUnits.Returns(2);

            var scaler = new AutoScalerHelper(s, config);
            scaler.Run();

            s.Received().IsFirstRun(1);
            var r = s.Received().CurrentUnits;
            s.Received(0).ScaleDown(Arg.Any<string>());
            s.Received(0).ScaleUp(scaler, config, Arg.Any<string>());
        }

        [Test]
        public void RunOptimalMin()
        {
            var config = new object();
            var s = Substitute.For<IScaler<object>>();
            s.IsFirstRun(1).Returns(false);
            s.ShouldScale().Returns(false);
            s.CurrentUnits.Returns(1);

            var scaler = new AutoScalerHelper(s, config);
            scaler.Run();

            s.Received().IsFirstRun(1);
            var r = s.Received().CurrentUnits;
            s.Received(0).ScaleDown(Arg.Any<string>());
            s.Received(0).ScaleUp(scaler, config, Arg.Any<string>());
        }
    }
}