using System.Collections.Generic;
using UnityEngine;

namespace CNC.PathFinding
{
    internal class PredictionPool : Singleton<PredictionPool>
    {
        private DriverProxy _predictionPool;
        private int _count = 0;

        /// <summary>
        /// 取一个空 DriverProxy。
        /// </summary>
        /// <returns></returns>
        private DriverProxy GetNextBlankPrediction()
        {
            DriverProxy next = _predictionPool;

            if (next == null)
            {
                next = new DriverProxy();
                _count++;
            }

            _predictionPool = next.Next;
            next.Previous = null;
            next.Next = null;
            _count--;

            return next;
        }

        /// <summary>
        /// 生成一个 DriverProxy 作为 <paramref name="current"/> 的 Next。
        /// </summary>
        /// <param name="current"></param>
        /// <returns></returns>
        internal DriverProxy GetNextBlankPrediction(DriverProxy current)
        {
            if (current == null)
            {
                return GetNextBlankPrediction();
            }

            if (current.Next == null)
            {
                DriverProxy nextBlankPrediction = GetNextBlankPrediction();
                current.Next = nextBlankPrediction;
                nextBlankPrediction.Previous = current;
            }

            return current.Next;
        }

        internal void ReturnPredictionToPool(DriverProxy current)
        {
            if (_predictionPool == null)
                current.Next = null;
            else
                current.Next = _predictionPool;

            _predictionPool = current;
            _count++;
        }

        internal void ReturnPredictionsBefore(DriverProxy before)
        {
            DriverProxy previous = before.Previous;

            while (previous != null)
            {
                ReturnPredictionToPool(previous);
                previous = previous.Previous;
            }

            before.Previous = null;
        }
    }
}
