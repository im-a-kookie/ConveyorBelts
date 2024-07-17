using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    /// <summary>
    /// Creates a simple asynchronous task operator
    /// </summary>
    public class TaskChannel
    {
        /// <summary>
        /// The action channel
        /// </summary>
        private Channel<Action> channel;
        /// <summary>
        /// The reader for the channel
        /// </summary>
        private ChannelReader<Action> _reader;
        /// <summary>
        /// The writer for the channel
        /// </summary>
        private ChannelWriter<Action> _writer;

        /// <summary>
        /// A collection of running flag things
        /// </summary>
        public List<bool> IsRunning = new();
        public int ActiveTasks = 0;
        public TaskChannel(int tasks = 1)
        {
            channel = Channel.CreateUnbounded<Action>(new UnboundedChannelOptions() { SingleReader = false });
            _reader = channel.Reader;
            _writer = channel.Writer;

            for(int i = 0; i < tasks; i++)
            {
                IsRunning.Add(true);
                StartTask();
            }
            
        }

        /// <summary>
        /// Starts a new task. 
        /// </summary>
        public void StartTask()
        {
            int k = Interlocked.Increment(ref ActiveTasks) - 1;
            while (IsRunning.Count < k) IsRunning.Add(true);
            Task.Run(async () =>
            {
                await Runner(k); 
            });
        }

        /// <summary>
        /// The runner function
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public async Task Runner(int n)
        {
            while (IsRunning[n] && await _reader.WaitToReadAsync())
            {
                while (IsRunning[n] && _reader.TryRead(out var job))
                {
                    job.Invoke();
                }
            }
            IsRunning[n] = false;
            Interlocked.Decrement(ref ActiveTasks);
        }

        /// <summary>
        /// Commands the channel to increase or decrease to the given number of workers
        /// </summary>
        /// <param name="count"></param>
        public void AffixTaskCount(int count)
        {
            if (count < ActiveTasks)
            {
                for(int i = ActiveTasks; i < count; i++)
                {
                    if (i >= IsRunning.Count) IsRunning.Add(true);
                    else IsRunning[i] = true;
                    Task.Run(async () => await Runner(i));
                    ++ActiveTasks;
                }
            }
            else
            {
                for (int i = ActiveTasks - 1; i >= count; i--)
                {
                    IsRunning[i] = false;
                }
            }
        }

        /// <summary>
        /// Queues the given job to this thingimy
        /// </summary>
        /// <param name="job"></param>
        public void Enqueue(Action job)
        {

            while (!_writer.TryWrite(job)) ;
        }

        /// <summary>
        /// Stops the runner
        /// </summary>
        public void Stop()
        {
            _writer.Complete();
            for (int i = 0; i < IsRunning.Count; i++) IsRunning[i] = false;
        }
    }
}
