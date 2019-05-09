﻿namespace King.Service
{
    using King.Service.Timing;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Role Task Manager
    /// </summary>
    public class RoleTaskManager<T> : IRoleTaskManager<T>
    {
        #region Members
        /// <summary>
        /// Tasks
        /// </summary>
        protected IReadOnlyCollection<IRunnable> tasks = null;

        /// <summary>
        /// Task Starter
        /// </summary>
        protected Timer starter = null;

        /// <summary>
        /// Factories
        /// </summary>
        protected readonly IEnumerable<ITaskFactory<T>> factories = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>
        /// Default Task Finder Factory searches for all Attribute based services
        /// </remarks>
        public RoleTaskManager()
            : this(new TaskFinderFactory<T>())
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory">Task Factory</param>
        public RoleTaskManager(ITaskFactory<T> factory)
            : this(new[] { factory })
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory">Task Factories</param>
        public RoleTaskManager(params ITaskFactory<T>[] factories)
            : this(factories as IEnumerable<ITaskFactory<T>>)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory">Task Factories</param>
        public RoleTaskManager(IEnumerable<ITaskFactory<T>> factories)
        {
            if (null == factories)
            {
                throw new ArgumentNullException("factories");
            }

            this.factories = factories.Where(f => f != null);

            if (this.factories.Count() == 0)
            {
                throw new ArgumentException("No valid factories available.");
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~RoleTaskManager()
        {
            Dispose(false);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Tasks
        /// </summary>
        public virtual IReadOnlyCollection<IRunnable> Tasks
        {
            get
            {
                return this.tasks;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Run
        /// </summary>
        public virtual void Run()
        {
            Trace.TraceInformation("Run called");

            if (null != tasks && tasks.Any())
            {
                var taskCount = tasks.Count();

                Trace.TraceInformation("Starting {0} tasks", taskCount);

                var toStart = new Stack<IRunnable>(tasks);
                this.starter = new Timer(StartTaskTimer, toStart, BaseTimes.ThreadingOffset, BaseTimes.ThreadingOffset);
            }
            else
            {
                Trace.TraceWarning("No tasks to start");
            }

            Trace.TraceInformation("Run finished");
        }

        protected void StartTaskTimer(object state)
        {
            var stack = state as Stack<IRunnable>;
            if (null != stack)
            {
                var task = stack.Pop();
                if (task != null)
                {
                    try
                    {
                        var success = task.Start();

                        Trace.TraceInformation("{0} started: {1}.", task.GetType().ToString(), success);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Failed to start {0}: {1}", task.GetType().ToString(), ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// On Start
        /// </summary>
        /// <param name="passthrough">Pass through</param>
        /// <returns>Started</returns>
        public virtual bool OnStart(T passthrough = default(T))
        {
            Trace.TraceInformation("On start called");

            if (null == this.tasks)
            {
                var ts = (from f in this.factories select f.Tasks(passthrough)).Where(f => f != null).SelectMany(t => t).Where(t => t != null);

                if (null != ts && ts.Any())
                {
                    Trace.TraceInformation("Tasks loading");

                    this.tasks = new ReadOnlyCollection<IRunnable>(ts.ToList());

                    Trace.TraceInformation("Tasks loaded");
                }
                else
                {
                    Trace.TraceWarning("No tasks loaded from factories?");
                }
            }
            else
            {
                Trace.TraceInformation("Tasks previously loaded");
            }

            Trace.TraceInformation("On start finished");

            return true;
        }

        /// <summary>
        /// On Stop
        /// </summary>
        public virtual void OnStop()
        {
            Trace.TraceInformation("On stop called");

            if (null != tasks && tasks.Any())
            {
                Trace.TraceInformation("Stopping {0} tasks", tasks.Count());

                Parallel.ForEach(tasks, task =>
                {
                    try
                    {
                        var success = task.Stop();

                        Trace.TraceInformation("{0} Stopped; Success: {1}.", task.GetType().ToString(), success);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("{0}: {1}.", task.GetType().ToString(), ex.ToString());
                    }
                }
                );

                Trace.TraceInformation("Stopped {0} tasks", tasks.Count());

                tasks = null;
            }
            else
            {
                Trace.TraceInformation("No tasks to be stopped");
            }

            Trace.TraceInformation("On stop finished");
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public virtual void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing">Disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (null != this.starter)
                {
                    this.starter.Dispose();
                    this.starter = null;
                }
                if (null != this.tasks)
                {
                    Parallel.ForEach(tasks, task =>
                    {
                        try
                        {
                            task.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error while disposing of task ({0}): {1}", task.GetType(), ex.ToString());
                        }
                    });

                    this.tasks = null;
                }
            }
        }
        #endregion
    }
}