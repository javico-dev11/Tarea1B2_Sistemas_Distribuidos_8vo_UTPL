using System;

namespace Logic_GC
{
    public class LogicalVectorClock
    {
        private int[] vectorClock;

        public LogicalVectorClock(int size)
        {
            vectorClock = new int[size];
        }

        public void Tick(int index)
        {
            vectorClock[index]++;
        }

        public void Update(LogicalVectorClock otherClock)
        {
            for (int i = 0; i < vectorClock.Length; i++)
            {
                vectorClock[i] = Math.Max(vectorClock[i], otherClock.vectorClock[i]);
            }
        }

        public int[] GetClock()
        {
            return vectorClock;
        }
    }

}
