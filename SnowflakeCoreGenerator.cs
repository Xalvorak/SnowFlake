namespace SnowFlake
{
    public sealed class SnowflakeCoreGenerator
    {
        private const int DatacenterIdBits = 8;
        private const int WorkerIdBits = 8;
        private const int SequenceBits = 14;

        private const long MaxDatacenterId = (1L << DatacenterIdBits) - 1;
        private const long MaxWorkerId = (1L << WorkerIdBits) - 1;
        private const long MaxSequence = (1L << SequenceBits) - 1;

        private const int WorkerIdShift = SequenceBits;
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        private readonly long epoch;
        private readonly long datacenterId;
        private readonly long workerId;

        private long lastTimestamp = -1L;
        private long sequence = 0L;

        private readonly int batchSize = 4096;
        private ulong batchBaseId = 0;
        private int batchIndex = 0;

        private SpinWait spin = new SpinWait();

        private const int MaxClockDriftTolerance = 5;

        public SnowflakeCoreGenerator(long datacenterId, long workerId, DateTime? customEpoch = null)
        {
            if (datacenterId < 0 || datacenterId > MaxDatacenterId)
                throw new ArgumentException($"Datacenter ID must be between 0 and {MaxDatacenterId}");
            if (workerId < 0 || workerId > MaxWorkerId)
                throw new ArgumentException($"Worker ID must be between 0 and {MaxWorkerId}");

            this.datacenterId = datacenterId;
            this.workerId = workerId;
            this.epoch = (customEpoch ?? new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10;
        }

        public ulong NextId()
        {
            while (true)
            {
                int localIndex = Interlocked.Increment(ref batchIndex) - 1;
                if (localIndex < batchSize)
                {
                    return batchBaseId + (ulong)localIndex;
                }
                else
                {
                    if (Interlocked.CompareExchange(ref batchIndex, 0, batchSize) >= batchSize)
                    {
                        RefillBatch();
                    }
                }
                spin.SpinOnce();
            }
        }

        private void RefillBatch()
        {
            long timestamp = GetCurrentTimestampMicros();
            long drift = lastTimestamp - timestamp;

            if (drift > MaxClockDriftTolerance * 1000)
                throw new InvalidOperationException($"System clock moved backwards by {drift}μs");

            if (timestamp < lastTimestamp)
                timestamp = lastTimestamp;

            if (timestamp == lastTimestamp)
            {
                sequence = (sequence + batchSize) & MaxSequence;
                if (sequence == 0)
                {
                    timestamp = WaitNextMicros(lastTimestamp);
                }
            }
            else
            {
                sequence = 0;
            }

            lastTimestamp = timestamp;

            batchBaseId = ((ulong)(timestamp - epoch) << TimestampLeftShift)
                        | ((ulong)datacenterId << DatacenterIdShift)
                        | ((ulong)workerId << WorkerIdShift)
                        | (ulong)sequence;
        }

        private long GetCurrentTimestampMicros()
        {
            return DateTime.UtcNow.Ticks / 10;
        }

        private long WaitNextMicros(long lastTimestamp)
        {
            long ts;
            int spins = 0;
            SpinWait localSpin = new SpinWait();
            do
            {
                localSpin.SpinOnce();
                if (++spins > 10000) Thread.Sleep(1);
                ts = GetCurrentTimestampMicros();
            } while (ts <= lastTimestamp);
            return ts;
        }
    }
}
