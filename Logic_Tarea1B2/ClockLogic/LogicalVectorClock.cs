using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic_Tarea1B2.ClockLogic
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
