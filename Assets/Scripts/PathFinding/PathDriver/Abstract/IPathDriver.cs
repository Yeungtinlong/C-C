using UnityEngine;
using UnityEngine.Events;

namespace CNC.PathFinding
{
    public interface IPathDriver
    {
        public LocomotionType RestrictType { get; }
        public LocomotionType LocomotionType { get; }
        
        public int UnitSize { get; }
        public float TurnRadius { get; }
        public bool IsMoving { get; }
        public bool IsArrived { get; }
        public Controllable Controllable { get; }
        public Damageable Damageable { get; }
        public Transform Transform { get; }

        public event UnityAction<ArrivalResult> OnArrivedEvent;

        public float CurrentSpeed { get; }

        public float MaxSpeed { get; set; }

        public DriverState CurrentDriverState { get; }

        /// <summary>
        /// 设置导航目的地。
        /// </summary>
        /// <param name="destination">目的地</param>
        /// <param name="alignment">目标朝向</param>
        /// <param name="approachRange">到达范围</param>
        public void SetDestination(Vector3 destination, Vector3 alignment, bool isForcedAlign,
            float approachRange = 0f);

        public void SetInPlaceTurn(Vector3 alignment);

        public void Stop(bool isInstant);

        public bool IsNewDestination(Vector3 destination);

        public bool IsNewAlignment(Vector3 alignment);

        public bool IsNewApproachRange(Vector3 destination, float approachRange);

        public IPathDriver GetEvasionTarget(int frameID);

        public DriverProxy GetPredictionInfo(int futureFrameID);
    }
}