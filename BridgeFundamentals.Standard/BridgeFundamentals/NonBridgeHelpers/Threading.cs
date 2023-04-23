using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge
{
    public static class Threading
	{
		/// <summary>
		/// Portable version of Thread.Sleep
		/// </summary>
		/// <param name="milliSeconds">Time to pause</param>
		public static void Sleep(int milliSeconds)
		{
			using (var mre = new ManualResetEvent(false))
			{
				mre.WaitOne(milliSeconds);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="minMilliSeconds">Minimum time to sleep</param>
		/// <param name="maxMilliseconds">Maximum time to sleep</param>
		public static void SleepRandom(int minMilliSeconds, int maxMilliseconds)
		{
			Sleep(minMilliSeconds + RandomGenerator.Instance.Next(maxMilliseconds - minMilliSeconds));
		}
	}

    public class AsyncLock : IDisposable
    {
        private string semaphoreLockKey;
        private static Dictionary<string, SemaphoreSlim> internalSemaphoreSlimDict = new Dictionary<string, SemaphoreSlim>();

        /// <summary>
        /// <para>Creates a <see cref="AsyncLock"/> for the given <paramref name="key"/> and aquires the lock this <see cref="AsyncLock"/> represents.</para>
        /// <para>The task this method returns will await the lock for this <see cref="AsyncLock"/> if the semaphore with the key is already in use.
        /// Once the task aquired the lock, an instance of <see cref="AsyncLock"/> is returned, which will release the lock once <see cref="Dispose"/> is called (preferably via a using() statement)</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns a <see cref="AsyncLock"/> that holds the lock of the given <paramref name="key"/>. Dispose the returned instance to release the lock (preferably via a using() statement)</returns>
        /// <remarks>Wrap this into a using() to release the semaphore upon finishing your locked code</remarks>
        public static async Task<AsyncLock> WaitForLockAsync(string key)
        {
            var mySemaphore = new AsyncLock(key);

            await internalSemaphoreSlimDict[key].WaitAsync();
            return mySemaphore;
        }

        /// <summary>
        /// <para>Creates a <see cref="AsyncLock"/> for the given <paramref name="key"/> and aquires the lock this <see cref="AsyncLock"/> represents.</para>
        /// <para>The task this method returns will await the lock for this <see cref="AsyncLock"/> if the semaphore with the key is already in use.
        /// Once the task aquired the lock, an instance of <see cref="AsyncLock"/> is returned, which will release the lock once <see cref="Dispose"/> is called (preferably via a using() statement)</para>
        /// </summary>
        /// <param name="key"></param>
        /// <returns>Returns a <see cref="AsyncLock"/> that holds the lock of the given <paramref name="key"/>. Dispose the returned instance to release the lock (preferably via a using() statement)</returns>
        /// <remarks>Wrap this into a using() to release the semaphore upon finishing your locked code</remarks>
        public static AsyncLock WaitForLock(string key)
        {
            var mySemaphore = new AsyncLock(key);

            internalSemaphoreSlimDict[key].Wait();
            return mySemaphore;
        }

        /// <summary>
        /// Constructor using a key. If a key already exists and is currently used, it will lock the calling thread until the other thread has disposed his MySemaphore
        /// </summary>
        /// <param name="key"></param>
        private AsyncLock(string key)
        {
            this.semaphoreLockKey = key;
            if (!internalSemaphoreSlimDict.ContainsKey(key))
                internalSemaphoreSlimDict[key] = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Releases the Lock that is held by this instance
        /// </summary>
        public void Dispose()
        {
            internalSemaphoreSlimDict[semaphoreLockKey].Release();
        }
    }

}
