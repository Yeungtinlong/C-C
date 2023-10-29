using CNC.Utility;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CNC.PathFinding
{
    [RequireComponent(typeof(Damageable), typeof(Controllable))]
    public partial class PathDriver : PathDriverBase
    {
        private Vector3 _lastTargetAlignment;
        
        private BlockMapSO.BlockFlag _lastTargetBlockOverride;

        private Vector2[] _globalRoute;
        private bool _isGlobalRouteValid;
        
        private DriverProxy _buildCursor;
        private List<Maneuver> _maneuvers = new List<Maneuver>();
        private int _nextManeuverID;
        private bool _isLocalRouteCompleted;
        private bool _isLocalRouteCulledAtEnd;
        private CollisionInfo _lastCollision;
        private IPathDriver _evasionTarget;
        private int _evasionFrameID;
        private List<Vector2> _localRoute = new List<Vector2>();
        private int _localRouteID;
        private Vector2 _lastLocalFrom;
        private Vector2 _lastLocalFromHeading;
        private Vector2 _localTarget;
        private float _nextRetryTime;
        private int _retryCounter;
        private bool _isManeuversRollbacked;
        private bool _isUsingRemainingManeuvers;
        private int _currentGlobalRouteIndex;
        private int _processedGlobalRouteIndex;
        private List<IPathDriver> _markedUnits = new List<IPathDriver>();
        
        private bool _isPathLost;
        // private ArrivalResult _lastArrivalResult = ArrivalResult.Enroute;

        private const float TIME_TO_FULL_SPEED = 2f;
        private const float MAXIMUM_UNIT_SIZE = 7f;
        private const float LIMIT_BRAKE_DISTANCE = 1.5f;
        private const int MAXIMUM_PREDICTION_COUNT = 45;
        private const int EVASION_DURATION = 80;
        private const int PREDICTION_PAUSE_FRAMES = 3;
        private const int PREDICTION_PAUSE_FRAME_DESYNC = 12;
        private const int COLLISION_ROLLBACK_FRAMES = 15;
        private const float LOCAL_PATH_RANGE = 30f;
        private const float TURN_THRESHOLD = 0.2f;
        private const float MAXIMUM_TURN_ANGLE = 80f;
        private const float REVERSE_THRESHOLD_SQR = 4f;
        private const float LOCAL_PATH_VALID_THRESHOLD_SQR = 1E-05f;
        private const int CASCADE_EVASION_MAX_IGNORE = 3;
        private const int PREDICTION_PAUSE_FRAMES_WAIT_EVASION = 7;
        private const int PREDICTION_PAUSE_FRAMES_WAIT = 2;
        private const float ARRIVE_DISTANCE_THRESHOLD = 1f;
        private const float ALIGNMENT_ANGLE_THRESHOLD = 5f;
        private const float BREAK_STOP_THRESHOLD = 0.05f;
        private const float MAXIMUN_GROUND_UNIT_DISTANCE = 17.5f;
        private const int MANEUVERS_RETRY_COUNT = 5;
        private const float MANEUVERS_RETRY_SECONDS = 1f;
        private const float MAXIMUM_LOCAL_PATH_RANGE = 45f;

        public override event UnityAction<ArrivalResult> OnArrivedEvent;

        public override float CurrentSpeed
        {
            get
            {
                if (_followCursor != null)
                {
                    return _followCursor.ProxySpeed;
                }

                return 0f;
            }
        }

        private float Acceleration
        {
            get
            {
                if (_locomotionType == LocomotionType.Infantry)
                    return 2f * _maxSpeed / TIME_TO_FULL_SPEED;

                return _maxSpeed / TIME_TO_FULL_SPEED;
            }
        }

#if UNITY_EDITOR
        [SerializeField] private bool _isShowGlobalRoute = default;
        [SerializeField] private bool _isShowLocalRoute = default;

        private void OnDrawGizmos()
        {
            if (_isShowGlobalRoute && _globalRoute != null && _globalRoute.Length > 0)
            {
                for (int i = 0; i < _globalRoute.Length; i++)
                {
                    Vector3 point = new Vector3(_globalRoute[i].x, 10f, _globalRoute[i].y);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(point,
                        new Vector3(_blockMapSO.ToWorldScale * 0.95f, 0.5f, _blockMapSO.ToWorldScale * 0.95f));
                }
            }

            if (_isShowLocalRoute && _localRoute.Count > 0)
            {
                for (int i = 0; i < _localRoute.Count; i++)
                {
                    Vector3 point = new Vector3(_localRoute[i].x, 10f, _localRoute[i].y);
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(point,
                        new Vector3(_blockMapSO.ToWorldScale * 0.9f, 0.75f, _blockMapSO.ToWorldScale * 0.9f));
                }
            }
        }
#endif
        /// <summary>
        /// 供外部调用修改导航目的地。
        /// </summary>
        /// <param name="destination">目的地</param>
        /// <param name="alignment">目标朝向</param>
        /// <param name="approachRange">到达范围</param>
        public override void SetDestination(Vector3 destination, Vector3 alignment, bool isForcedAlign,
            float approachRange = 0f)
        {
            _inputDestination = destination;
            _inputAlignment = alignment;
            _inputApproachRange = approachRange;
            _isRequestingPath = true;
            _isIncludeAlignmentAtLast = isForcedAlign;
        }

        public override void SetInPlaceTurn(Vector3 alignment)
        {
            SetDestination(Transform.position, alignment, true, 0f);
        }

        public override void Stop(bool isInstant)
        {
            SetProxyBrake(true);
            SetProxyArriving(isInstant ? ArrivalState.Stop : ArrivalState.StopWithInertia);

            _lastTargetPoint = Transform.position;
            _lastTargetAlignment = Transform.forward;
            _lastTargetApproachRange = 0f;
        }

        public override bool IsNewDestination(Vector3 destination)
        {
            return Vector3.Distance(destination, _lastTargetPoint) > ARRIVE_DISTANCE_THRESHOLD;
        }

        public override bool IsNewAlignment(Vector3 alignment)
        {
            return Vector3.Angle(alignment, _lastTargetAlignment) > ALIGNMENT_ANGLE_THRESHOLD;
        }

        public override bool IsNewApproachRange(Vector3 destination, float approachRange)
        {
            return Vector3.Distance(destination, _lastTargetPoint) >
                   _lastTargetApproachRange + ARRIVE_DISTANCE_THRESHOLD;
        }
        
        private void Start()
        {
            // 赋值_followCursor初始的位置与旋转。
            ResetProxy();
        }

        protected override void OnArrived()
        {
            SetProxyBrake(false);
            SetProxyArriving(ArrivalState.FreeMoving);
            _currentDriverState.IsGiveUp = false;
            ResetProxy();
        }

        private void GiveUp()
        {
            _currentDriverState.IsGiveUp = true;
        }

        private bool CheckBreakStop()
        {
            return _followCursor.ProxySpeed < BREAK_STOP_THRESHOLD;
            // return _currentDriverState.ProxySpeed < BREAK_STOP_THRESHOLD;
        }

        private bool CheckRouteEnd()
        {
            return IsLocalRouteAtEnd() && _followCursor.ManeuverIndex >= _maneuvers.Count;
        }

        protected override void StopWithInertia()
        {
            SetProxyBrake(false);
            SetProxyArriving(ArrivalState.StopWithInertia);
        }

        private void SetProxyBrake(bool isBraking)
        {
            _currentDriverState.IsBraking = isBraking;
            _followCursor.IsBraking = isBraking;
            RePredictFromCursor(_followCursor);
        }
        
        private void ResetProxy()
        {
            IsMoving = false;
            _currentDriverState.DriverInfo.Reset(PathDriverUtils.TransformToWorldPoint(Transform.position),
                PathDriverUtils.TransformToWorldPoint(Transform.forward));
            RePredictFromCursor(_followCursor);
            ResetFollowCursor();
            ResetManeuver();
            _maneuvers.Clear();
            _lastCollision.Reset();
        }

        private void ResetManeuver()
        {
            if (_buildCursor != null)
            {
                _buildCursor.ResetManeuver();
            }
        }

        protected override void PreUpdateMovement()
        {
            Vector2 worldPosition = _blockMapSO.SnapToBlock(PathDriverUtils.TransformToWorldPoint(Transform.position),
                Controllable.UnitSizeInWorld);
            float collisionRadius = SizeToCollisionRadius(Controllable.UnitSizeInWorld);
            float maxCollisionRadius = collisionRadius + SizeToCollisionRadius(MAXIMUM_UNIT_SIZE);

            if (_isStaying)
            {
                List<IPathDriver> unitsInAround = _unitGridSO.GetUnitsInAround(worldPosition, maxCollisionRadius);

                foreach (IPathDriver driver in unitsInAround)
                {
                    if (!PreFilterCollision(worldPosition, collisionRadius, driver))
                        _overlappingUnits.Add(driver);
                }
            }
            else
            {
                for (int i = _overlappingUnits.Count - 1; i >= 0; i--)
                {
                    IPathDriver driver = _overlappingUnits[i];

                    if (PreFilterCollision(worldPosition, collisionRadius, driver))
                        _overlappingUnits.Remove(driver);
                }
            }
        }

        protected override void PostUpdateMovement() { }
        
        private float SizeToCollisionRadius(float unitSizeInWorld)
        {
            return (unitSizeInWorld > 1f)
                ? (unitSizeInWorld * 0.5f - (0.5f * _blockMapSO.ToWorldScale * 1.4142f))
                : 0.5f;
        }

        private bool PreFilterCollision(Vector2 worldPoint, float collisionRadius, IPathDriver otherDriver)
        {
            if (otherDriver == this || otherDriver == null)
            {
                return true;
            }

            Vector2 otherWorldBlockPoint =
                _blockMapSO.SnapToBlock(PathDriverUtils.TransformToWorldPoint(otherDriver.Transform.position),
                    otherDriver.UnitSize);
            float sqrDistance = Utils.SqrDistance(otherWorldBlockPoint, worldPoint);
            float otherCollisionRadius = SizeToCollisionRadius(otherDriver.UnitSize);

            return sqrDistance >= Utils.Sqr(collisionRadius + otherCollisionRadius);
        }

        private void UpdateProxySpeed(float deltaTime, DriverProxy currentProxy, out float displacement)
        {
            if (deltaTime <= 0f)
            {
                displacement = 0;
                return;
            }

            // 测试
            // displacement = deltaTime * _maxSpeed;
            // return;

            float acceleration = Acceleration;
            float maxSpeed = currentProxy.ArrivalState == ArrivalState.FreeMoving ? _maxSpeed : 0f;

            // 设定默认值。
            displacement = currentProxy.ProxySpeed * deltaTime;
            currentProxy.ProxyAcceleration = 0f;

            if (currentProxy.IsBraking)
            {
                float a1 = currentProxy.ProxySpeed / 0.5f;
                float a2 = 0.5f * currentProxy.ProxySpeed * currentProxy.ProxySpeed / LIMIT_BRAKE_DISTANCE;
                acceleration = Mathf.Max(acceleration, a1);
                acceleration = Mathf.Max(acceleration, a2);
            }

            // 如果设定了减速帧数，要在减速帧数内减到0。
            if (_lastCollision.TargetDriver != null && _lastCollision.DecelerationFrames > 0)
            {
                maxSpeed = 0f;
                acceleration = _lastCollision.CollisionSpeed / (_lastCollision.DecelerationFrames * Time.deltaTime);
            }

            //if (IsReplacableArrivalState(currentProxy.ArrivalState) && !isRestrictedMovement)
            //    currentProxy.ArrivalState = ArrivalState.FreeMoving;

            if (currentProxy.ProxySpeed > maxSpeed)
            {
                displacement = currentProxy.ProxySpeed * deltaTime + 0.5f * acceleration * Utils.Sqr(deltaTime);
                currentProxy.ProxySpeed -= acceleration * deltaTime;
                currentProxy.ProxyAcceleration = -acceleration;

                if (currentProxy.ProxySpeed < maxSpeed)
                    currentProxy.ProxySpeed = maxSpeed;
            }
            else if (currentProxy.ProxySpeed < maxSpeed)
            {
                displacement = currentProxy.ProxySpeed * deltaTime + 0.5f * acceleration * Utils.Sqr(deltaTime);
                currentProxy.ProxySpeed += acceleration * deltaTime;
                currentProxy.ProxyAcceleration = acceleration;

                if (currentProxy.ProxySpeed > _maxSpeed)
                    currentProxy.ProxySpeed = _maxSpeed;
            }
        }

        protected override void MoveTowards(Vector3 targetPoint, Vector3 alignment, float approachRange)
        {
            // 如果没有新的寻路指示或者新的。
            if (!UpdateGlobalPath(targetPoint, alignment, approachRange))
                return;

            // 预测，生成代理链。
            if (RefreshPrediction())
                RefreshPrediction();

            ExecuteMovement();
        }

        private void ExecuteMovement()
        {
            Vector2 position = PathDriverUtils.TransformToWorldPoint(Transform.position);
            Vector2 rotation = PathDriverUtils.TransformToWorldPoint(Transform.forward);

            if (FollowPrediction(ref position, ref rotation))
            {
                Transform.position = PathDriverUtils.WorldToTransformPoint(position);
                Transform.forward = PathDriverUtils.WorldToTransformPoint(rotation);
            }

            IsMoving = true;
        }

        /// <summary>
        /// 检测预测代理是否会产生碰撞，然后跟随预测代理。
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="currentRotation"></param>
        /// <returns>当 _followCursor 与 _buildCursor 相等时，返回 false，否则返回 true。 </returns>
        private bool FollowPrediction(ref Vector2 currentPoint, ref Vector2 currentRotation)
        {
            if (_followCursor == _buildCursor)
                return false;

            DriverProxy next = _followCursor.Next;
            CollisionInfo newCollisionInfo = default;

            if (DetectCollisions(next, PlayerInfo.Instance.FrameCount, -1, false, ref newCollisionInfo))
            {
                CollisionInfo.Update(newCollisionInfo, ref _lastCollision);
                _lastCollision.ManeuverIndex = -1;
                _lastCollision.ManeuverInfo = null;
                RollBackPredictionToCursor(_followCursor);
                RewindRouteEnd();
            }
            else
            {
                currentPoint = next.Position;
                currentRotation = next.Orientation;

                _followCursor = next;
                ReturnPredictionsBefore(_followCursor);

                if (_followCursor.ArrivalState != ArrivalState.FreeMoving)
                    _currentDriverState.ArrivalState = _followCursor.ArrivalState;
            }

            return true;
        }

        private bool IsLocalRouteAtEnd()
        {
            return _currentGlobalRouteIndex < 0 || _isLocalRouteCulledAtEnd;
        }

        private void ReturnPredictionsBefore(DriverProxy before)
        {
            PredictionPool.Instance.ReturnPredictionsBefore(before);
        }

        private DriverProxy GetNextBlankPrediction(DriverProxy current)
        {
            return PredictionPool.Instance.GetNextBlankPrediction(current);
        }

        /// <summary>
        /// 逐帧步预测，生成 DriverProxy 链表。
        /// </summary>
        /// <returns>当回滚的 DriverProxy 处于优先的 Maneuver 时，返回 true。</returns>
        private bool RefreshPrediction()
        {
            int frameCount = PlayerInfo.Instance.FrameCount;
            DriverProxy currentCursor = _buildCursor;
            int predictionCount = GetPredictionCount();
            int predictionBudget = Mathf.Max(MAXIMUM_PREDICTION_COUNT - predictionCount, 0);

            if (_buildCursor == _followCursor && _buildCursor.FrameID < frameCount)
            {
                _buildCursor.FrameID = frameCount;
            }

            CollisionInfo newCollision = default;
            int i = predictionBudget;

            while (i > 0)
            {
                DriverProxy nextBlankPrediction = GetNextBlankPrediction(currentCursor);
                nextBlankPrediction.SetNextFrom(currentCursor);

                // 一、解决碰撞问题，依序判断是否到达停车帧-->赋值帧-->回避帧，最终进行回避寻路。

                // 1、有可能会碰撞的目标；
                // 2、回滚到同一个maneuver，但已经把车刹停；
                // 3、回滚到上一个maneuver，已经把这个最后的maneuver预测完；
                if (_lastCollision.IsAvailable() && (nextBlankPrediction.FrameID >= _lastCollision.StopFrameID ||
                                                     nextBlankPrediction.ManeuverIndex >= _maneuvers.Count))
                {
                    // 已经把车刹停，但等到赋值帧后再向下执行。
                    if (nextBlankPrediction.FrameID < _lastCollision.EvaluationFrameID)
                    {
                        currentCursor = nextBlankPrediction;
                        i--;
                        continue;
                    }

                    // 需要赋值碰撞信息。
                    if (_lastCollision.EvaluationFrameID >= 0)
                    {
                        EvaluateCollision(ref _lastCollision);
                        _lastCollision.EvaluationFrameID = -1;
                    }

                    // 已经赋值，但等待回避帧再向下执行。
                    if (nextBlankPrediction.FrameID < _lastCollision.EvasionFrameID)
                    {
                        currentCursor = nextBlankPrediction;
                        i--;
                        continue;
                    }

                    RequestEvasionPath(nextBlankPrediction, _lastCollision.EvasionFrameID, _lastCollision.ManeuverInfo);
                }

                // 二、若该步maneuver索引大于最大索引，而且无法更新局部路径，那么判断是否已经到达终点。
                if (nextBlankPrediction.ManeuverIndex >= _maneuvers.Count &&
                    !UpdateLocalPath(nextBlankPrediction.Position, nextBlankPrediction.Orientation,
                        nextBlankPrediction))
                {
                    UpdateProxySpeed(Time.deltaTime, nextBlankPrediction, out float arriveStep);
                    ValidateArrive(arriveStep, nextBlankPrediction);
                    _buildCursor = currentCursor;
                    return false;
                }

                // 三、更新currentCursor的速度与加速度。
                UpdateProxySpeed(Time.deltaTime, nextBlankPrediction, out float displacement);

                // 四、更新step到currentCursor中，此时要判断剩余路程是否已经进入刹车距离，从而改变currentCursor的加速度。
                ProcessStep(displacement, currentCursor, nextBlankPrediction);

                currentCursor = nextBlankPrediction;
                i--;

                // 如果最后一个预测预算的cursor位于Turn，继续预测。
                if (i == 0 && currentCursor.IsMoving() && currentCursor.ManeuverIndex < _maneuvers.Count &&
                    _maneuvers[currentCursor.ManeuverIndex].Priority > 0)
                {
                    i = 1;
                }

                newCollision.Reset();

                // 五、检测碰撞，记录碰撞信息。
                if (DetectCollisions(nextBlankPrediction, nextBlankPrediction.FrameID,
                        nextBlankPrediction.ManeuverIndex, true, ref newCollision))
                {
                    if (_lastCollision.CollisionFrameID <= 0 ||
                        newCollision.CollisionFrameID < _lastCollision.CollisionFrameID)
                    {
                        CollisionInfo.Update(newCollision, ref _lastCollision);
                        _lastCollision.CollisionSpeed = nextBlankPrediction.ProxySpeed;
                    }

                    break;
                }
            }

            bool isPriorityRollback = false;

            // 六、如果产生碰撞，进行回滚。
            if (_lastCollision.NeedProcessing())
            {
                // 回滚方案有两种：
                // 1、在 Turn 时碰撞，会记录发生的 ManeuverIndex，回滚到上一个 Maneuver 的最后一个 DriverProxy；
                // 2、非 Turn 时碰撞，回滚到15帧前的 proxy，将所处 ManeuverID 之后的 Maneuver 删除。
                if (_lastCollision.ManeuverIndex >= 0)
                {
                    _lastCollision.ManeuverInfo = _maneuvers[_lastCollision.ManeuverIndex];

                    // 回滚成功将回避帧提前，再执行一次代理预测，重新生成路径和 Maneuver。
                    if (RollBackManeuver(_lastCollision.ManeuverInfo, currentCursor))
                    {
                        _lastCollision.EvasionFrameID = _buildCursor.FrameID;
                        isPriorityRollback = true;
                    }
                    else
                    {
                        Debug.LogWarning(gameObject.name +
                                         "is unablle to rollback to suit maneuver, reverting to new route.");
                        _lastCollision.ManeuverIndex = -1;
                        _lastCollision.MaxManeuverPriority = _lastCollision.ManeuverInfo.Priority - 1;
                        _lastCollision.ManeuverInfo = null;
                    }
                }
                else
                {
                    RollBackPredictionToFrame(_lastCollision.CollisionPreventionFrameID, currentCursor);
                }

                RewindRouteEnd();

                _lastCollision.IsProcessed = true;

                if (!isPriorityRollback)
                {
                    int buildframeID = _buildCursor.FrameID;
                    _lastCollision.StopFrameID = _lastCollision.CollisionFrameID - 3;
                    int decelerationFrames = _lastCollision.StopFrameID - buildframeID;

                    if (decelerationFrames > 0)
                        _lastCollision.DecelerationFrames = decelerationFrames;
                    else
                        _lastCollision.StopFrameID -= decelerationFrames;
                }
                else
                {
                    _lastCollision.DecelerationFrames = 0;
                    _lastCollision.StopFrameID = -1;
                }
            }
            else
            {
                _buildCursor = currentCursor;
            }

            return isPriorityRollback;
        }

        private void RewindRouteEnd()
        {
            if (_currentGlobalRouteIndex < 0)
                _currentGlobalRouteIndex = _globalRoute != null ? _globalRoute.Length - 1 : -1;

            _isLocalRouteCulledAtEnd = false;
        }

        /// <summary>
        /// 从 <paramref name="from"/> 开始回溯到指定帧ID <paramref name="frameID"/>。
        /// </summary>
        /// <param name="frameID"></param>
        /// <param name="from"></param>
        private void RollBackPredictionToFrame(int frameID, DriverProxy from)
        {
            DriverProxy driverProxy = from;

            while (driverProxy != _followCursor && driverProxy != null)
            {
                if (driverProxy.FrameID == frameID)
                {
                    CommitRollBack(driverProxy);
                    return;
                }

                driverProxy = driverProxy.Previous;
            }

            RollBackPredictionToCursor(_followCursor);
        }

        /// <summary>
        /// 从 <paramref name="from"/> 开始回溯，直到找到某个 DriverProxy.ManeuverID 小于 <paramref name="startRollbackManeuver"/>.ManeuverID 的 driverProxy，并回滚到此。
        /// </summary>
        /// <param name="startRollbackManeuver"></param>
        /// <param name="from"></param>
        /// <returns>
        /// 若回溯到合法的 DriverProxy，则回滚并返回 true；
        /// 若回溯至 _followCursor，则回滚到 _followCursor，并返回 true；
        /// 若 _followCursor 也有 linkedManeuvers，则也回滚到 _followCursor，并返回 false。
        /// </returns>
        private bool RollBackManeuver(Maneuver startRollbackManeuver, DriverProxy from)
        {
            DriverProxy driverProxy = from;

            while (driverProxy != _followCursor && driverProxy != null)
            {
                // 检查：
                // 1、driverProxy所处的ManeuverID是否小于startRollbackManeuver；
                // 2、这个Maneuver是否startRollbackManeuver的linkManeuver；
                // 若1是2非，则可以回滚到此driverProxy，并将之后的Maneuver删除。

                if (CheckManeuverCleared(driverProxy, startRollbackManeuver))
                {
                    CommitRollBack(driverProxy);
                    return true;
                }

                driverProxy = driverProxy.Previous;
            }

            bool result = CheckManeuverCleared(_followCursor, startRollbackManeuver);
            RollBackPredictionToCursor(_followCursor);

            return result;
        }

        /// <summary>
        /// 检查 driverProxy 所处的 maneuver 是否 startRollbackManeuver 的 linkManeuver，即前 Maneuver 和后 Maneuver 是否双扇形转弯的连接关系。
        /// </summary>
        /// <param name="driverProxy"></param>
        /// <param name="startRollbackManeuver"></param>
        /// <returns>若不是，返回true，若是，返回false。</returns>
        private bool CheckManeuverCleared(DriverProxy driverProxy, Maneuver startRollbackManeuver)
        {
            int maneuverIndex = driverProxy.ManeuverIndex;
            // 检查maneuverIndex的合法性。
            Maneuver maneuver =
                (maneuverIndex < 0 || maneuverIndex >= _maneuvers.Count ||
                 _maneuvers[maneuverIndex].ManeuverID != driverProxy.ManeuverID)
                    ? null
                    : _maneuvers[maneuverIndex];
            return driverProxy.ManeuverID < startRollbackManeuver.ManeuverID &&
                   startRollbackManeuver.CheckNonLinkedManeuver(maneuver);
        }

        private void ProcessStep(float step, DriverProxy previousProxy, DriverProxy currentProxy)
        {
            Vector2 position = currentProxy.Position;
            Vector2 orientation = currentProxy.Orientation;
            if (currentProxy.ArrivalState == ArrivalState.FreeMoving && !_isUsingRemainingManeuvers &&
                IsLocalRouteAtEnd() && _isLocalRouteCompleted)
            {
                float maneuversLength = GetManeuversLength(currentProxy.ManeuverIndex, currentProxy.ManeuverPercentage,
                    _maneuvers.Count - 1, 1f);
                float brakingDistance = Utils.AccelerationRange(currentProxy.ProxySpeed, 0f, Acceleration);

                // 一步就进入了减速距离以内。
                if (step + brakingDistance >= maneuversLength)
                {
                    float replaceStep = Mathf.Max(maneuversLength - brakingDistance, 0f);
                    if (replaceStep >= 0f)
                        Step(currentProxy, ref replaceStep, ref position, ref orientation);

                    // 根据路程、加速度得出时间。
                    float remainingTime = GetRemainingTime(currentProxy,
                        step - Mathf.Max(maneuversLength - brakingDistance, 0f));
                    // Debug.Log("Set Arrive.");
                    currentProxy.ArrivalState = ArrivalState.Arrive;
                    // 根据时间更新proxy的速度。
                    UpdateProxySpeed(remainingTime, currentProxy, out step);
                }
            }

            // 若此step把剩余maneuvers走完，但step仍有余，那么更新局部路径和maneuvers，在新maneuver上接着移动。
            if (Step(currentProxy, ref step, ref position, ref orientation))
            {
                if (UpdateLocalPath(position, orientation, currentProxy))
                {
                    Step(currentProxy, ref step, ref position, ref orientation);
                }
            }

            currentProxy.Position = position;
            currentProxy.Orientation = orientation;
        }

        private float GetRemainingTime(DriverProxy driverProxy, float step)
        {
            if (step <= 0f)
                return 0f;

            float acceleration = driverProxy.ProxyAcceleration;
            float speed = driverProxy.ProxySpeed;

            int solutionCount =
                Utils.SolveQuadratic(0.5f * acceleration, speed, -step, out float time1, out float time2);

            if (solutionCount == 0)
                return 0f;

            if (solutionCount == 1)
                return Mathf.Max(time1, 0f);

            if (time1 < 0f)
                return Mathf.Max(time2, 0f);
            if (time2 < 0f)
                return Mathf.Max(time1, 0f);

            return Mathf.Min(time1, time2);
        }

        /// <summary>
        /// 计算一次step后，DriverProxy在Maneuver的落点。
        /// </summary>
        /// <param name="driverProxy"></param>
        /// <param name="step"></param>
        /// <param name="currentPoint"></param>
        /// <param name="currentDirection"></param>
        /// <returns>若此step把剩余maneuvers走完，则true，否则false。</returns>
        private bool Step(DriverProxy driverProxy, ref float step, ref Vector2 currentPoint,
            ref Vector2 currentDirection)
        {
            int maneuverIndex = driverProxy.ManeuverIndex;
            if (maneuverIndex >= _maneuvers.Count)
                return true;

            while (step > 0f)
            {
                Maneuver currentManeuver = _maneuvers[driverProxy.ManeuverIndex];
                float remainingLength = currentManeuver.GetRemainingLength(driverProxy.ManeuverPercentage);
                float currentPercentage = driverProxy.ManeuverPercentage;

                // step未超过当前maneuver的剩余长度，仍在当前maneuvers内。
                if (step <= remainingLength)
                {
                    currentManeuver.Step(step, ref currentPercentage, out currentPoint, out currentDirection);
                    step -= remainingLength;

                    driverProxy.ManeuverPercentage = currentPercentage;
                    driverProxy.ManeuverID = currentManeuver.ManeuverID;
                }
                // step超过当前maneuver的剩余长度，跳到下一个maneuver来判断。
                else
                {
                    if (driverProxy.ManeuverIndex == _maneuvers.Count - 1)
                        _maneuvers[driverProxy.ManeuverIndex].Step(remainingLength, ref currentPercentage,
                            out currentPoint, out currentDirection);

                    driverProxy.ManeuverIndex++;
                    driverProxy.ManeuverID = -1;
                    driverProxy.ManeuverPercentage = 0f;
                    step -= remainingLength;

                    if (driverProxy.ManeuverIndex >= _maneuvers.Count)
                        return true;

                    driverProxy.ManeuverID = _maneuvers[driverProxy.ManeuverIndex].ManeuverID;
                }
            }

            return false;
        }

        private void ValidateArrive(float step, DriverProxy driverProxy)
        {
            if (driverProxy.ArrivalState == ArrivalState.FreeMoving && !_isUsingRemainingManeuvers &&
                IsLocalRouteAtEnd() && _isLocalRouteCompleted)
            {
                // 取得剩余的maneuvers长度。
                float maneuversLength = GetManeuversLength(driverProxy.ManeuverIndex, driverProxy.ManeuverPercentage,
                    _maneuvers.Count - 1, 1f);
                float remainingDistance =
                    maneuversLength - Utils.AccelerationRange(driverProxy.ProxySpeed, 0f, Acceleration);

                if (remainingDistance <= step)
                {
                    driverProxy.ArrivalState = ArrivalState.Arrive;
                    _currentDriverState.ArrivalState = driverProxy.ArrivalState;
                }
            }
        }

        private float GetManeuversLength(int startManeuverIndex, float startPercentage, int endManeuverIndex,
            float endPercentage)
        {
            if (_maneuvers.Count == 0 || startManeuverIndex >= _maneuvers.Count)
                return 0f;

            if (startManeuverIndex == endManeuverIndex)
                return _maneuvers[startManeuverIndex].Length * (endPercentage - startPercentage);

            float sumOfLength = _maneuvers[startManeuverIndex].Length * (1f - startPercentage);
            for (int i = startManeuverIndex + 1; i < endManeuverIndex; i++)
            {
                sumOfLength += _maneuvers[i].Length;
            }

            return sumOfLength + _maneuvers[endManeuverIndex].Length * endPercentage;
        }

        private void RequestEvasionPath(DriverProxy at, int evasionFrameID, Maneuver maneuver)
        {
            int entryIndex = -1;
            int maxPriority = int.MaxValue;
            bool isSameLocalRoute = false;

            // 这个maneuver是由本次的局部寻路所得，所以无需更新局部路径点，直接生成maneuver接驳上去。
            if (maneuver != null && maneuver.LocalRouteID == _localRouteID && maneuver.BypassNode < _localRoute.Count &&
                _localRoute.Count > 0)
            {
                // 在发生碰撞的maneuver的前一个localRoute节点开始寻路。
                entryIndex = maneuver.BypassNode - 1;
                maxPriority = maneuver.Priority - 1;
                isSameLocalRoute = true;
            }

            // 将followCursor所在maneuver之前的maneuver全部清除。
            CleanupManeuvers();
            int connectionIndex = _maneuvers.Count;

            if (isSameLocalRoute)
                GenerateManeuvers(at.Position, at.Orientation, entryIndex, maxPriority, false);
            else
                RequestLocalPath(at.Position, at.Orientation, at, true);

            _lastCollision.Reset();
            at.FrameID = evasionFrameID;

            SetupManeuverConnection(at, connectionIndex);
        }

        private void EvaluateCollision(ref CollisionInfo collisionInfo)
        {
            if (collisionInfo.TargetDriver == null)
                return;

            IPathDriver otherEvasionTarget = EvaluateCollider(collisionInfo.TargetDriver,
                collisionInfo.CollisionFrameID,
                true, out Vector2 otherPosition, out Vector2 otherVelocity, out int otherFrameID);
            if (otherVelocity.sqrMagnitude > 0f)
            {
                Vector2 vectorToOther = (otherPosition - collisionInfo.EntryPosition).normalized;
                Vector2 relativeVelocity = collisionInfo.EntryVelocity - otherVelocity;

                // 相对速度并不是走向对方。
                if (Vector2.Dot(vectorToOther, relativeVelocity) <= 0f)
                {
                    collisionInfo.EvasionFrameID = collisionInfo.EvaluationFrameID;
                    collisionInfo.TargetDriver = null;
                    return;
                }

                if (_locomotionType != LocomotionType.Infantry)
                {
                    // 在追尾。
                    if (Vector2.Dot(otherVelocity, vectorToOther) > 0f)
                    {
                        collisionInfo.EvasionFrameID = collisionInfo.EvaluationFrameID;
                        return;
                    }
                }
            }

            // 上边没有return，此时满足相对速度走向对方。
            collisionInfo.EvasionFrameID = !IsCascadeEvasion(otherEvasionTarget)
                ? (collisionInfo.EvaluationFrameID + PREDICTION_PAUSE_FRAMES_WAIT_EVASION)
                : (collisionInfo.EvaluationFrameID + PREDICTION_PAUSE_FRAMES_WAIT);
        }

        private int GetPredictionCount()
        {
            DriverProxy next = _followCursor;
            int count = 0;

            while (next != _buildCursor && next != null)
            {
                next = next.Next;
                count++;
            }

            return count;
        }

        private bool UpdateLocalPath(Vector2 from, Vector2 fromHeading, DriverProxy attachProxy)
        {
            // 清除followCursor所对应maneuverID之前的所有maneuver。
            CleanupManeuvers();
            int connectionIndex = _maneuvers.Count;

            // 1、已有maneuvers；
            // 2、进行过回滚或GenerateManeuvers()中添加了Turn，导致localRoute未完全生成Maneuver；
            // 3、最后一个maneuver是在本次局部寻路产生的；
            if (connectionIndex > 0 && !_isLocalRouteCompleted &&
                _maneuvers[connectionIndex - 1].LocalRouteID == _localRouteID)
            {
                int bypassNode = _maneuvers[connectionIndex - 1].BypassNode;
                bool isForcedAlign = _isManeuversRollbacked;

                if (GenerateManeuvers(from, fromHeading, bypassNode, int.MaxValue, isForcedAlign))
                {
                    SetupManeuverConnection(attachProxy, connectionIndex);
                    _isManeuversRollbacked = false;
                    return true;
                }
            }

            // 若未有maneuvers，则开始局部寻路。
            bool result = RequestLocalPath(from, fromHeading, attachProxy, false);
            SetupManeuverConnection(attachProxy, connectionIndex);
            return result;
        }

        private void SetupManeuverConnection(DriverProxy proxy, int connectionIndex)
        {
            proxy.ManeuverIndex = connectionIndex;
            proxy.ManeuverID = (connectionIndex < 0 || connectionIndex > _maneuvers.Count - 1)
                ? -1
                : _maneuvers[connectionIndex].ManeuverID;
            proxy.ManeuverPercentage = 0f;
        }

        private bool RequestLocalPath(Vector2 from, Vector2 fromHeading, DriverProxy attachProxy, bool isEvade)
        {
            if (IsLocalRouteAtEnd())
                return false;

            if (_nextRetryTime > 0f)
            {
                _nextRetryTime -= Time.deltaTime;
                return _isUsingRemainingManeuvers;
            }

            _processedGlobalRouteIndex = _currentGlobalRouteIndex;
            _localTarget = GetNextRouteTarget(from, LOCAL_PATH_RANGE, _processedGlobalRouteIndex,
                out int mappingGlobalRouteIndex);
            _processedGlobalRouteIndex = mappingGlobalRouteIndex;
            _markedUnits.Clear();

            for (int i = 0; i < _proximityUnits.Count; i++)
            {
                IPathDriver other = _proximityUnits[i];
                // 若为预计会发生碰撞的目标，在下面特别处理。
                if (other != _lastCollision.TargetDriver)
                {
                    if (!_overlappingUnits.Contains(other))
                    {
                        // TODO: IsMoving在ExecuteMovement()中置true，在ResetProxy()中置false。
                        if (IsIgnoreEvasionType(other) || other.IsMoving)
                            continue;
                        // 排除超过某个距离的单位。
                        //if ((PathDriverUtils.TransformToWorldPoint(other._transform.position) - from).sqrMagnitude > Utils.Sqr(LOCAL_PATH_MAX_RANGE + _unitSizeInWorld + other.UnitSize))
                        //    continue;
                    }
                }

                _markedUnits.Add(other);
                MarkUnitToBlockMap(other);
            }

            IPathDriver otherEvasionTarget = _lastCollision.TargetDriver != null
                ? _lastCollision.TargetDriver.GetEvasionTarget(attachProxy.FrameID)
                : null;

            bool isRequiredMarkLastCollisionUnit = _lastCollision.TargetDriver != null &&
                                                   IsCascadeEvasion(otherEvasionTarget, _lastCollision.IgnoreCounter);
            Vector2 markPoint = Vector2.zero;
            int unitSizeInWorld = 0;

            if (isRequiredMarkLastCollisionUnit)
            {
                //Debug.Log(this + " mark " + _lastCollision.TargetDriver);
                markPoint = _lastCollision.TargetPosition;
                unitSizeInWorld = _lastCollision.TargetDriver.UnitSize;
                _blockMapSO.MarkBlockMap(markPoint, unitSizeInWorld, BlockMapSO.BlockFlag.Dynamic);
            }

            _isUsingRemainingManeuvers = _isUsingRemainingManeuvers || _maneuvers.Count > 0;
            PathGenerator.Reset();
            _localRoute.Clear();
            _localRouteID++;
            _lastLocalFrom = from;
            _lastLocalFromHeading = fromHeading;

            PathGenerator.RequestPath(PathDriverUtils.WorldToTransformPoint(from),
                PathDriverUtils.WorldToTransformPoint(_localTarget),
                MAXIMUM_LOCAL_PATH_RANGE);
            OnLocalPathComplete(isEvade);

            for (int i = 0; i < _markedUnits.Count; i++)
                UnmarkUnitFromBlockMap(_markedUnits[i]);

            if (isRequiredMarkLastCollisionUnit)
                _blockMapSO.UnmarkBlockMap(markPoint, unitSizeInWorld, BlockMapSO.BlockFlag.Dynamic);

            return true;
        }

        private void OnLocalPathComplete(bool isEvade)
        {
            if (PathGenerator.PathResult == Path.PathResult.Success)
            {
                int count = PathGenerator.CurrentPath.Count;
                float arrivalTolerance = Mathf.Max(ARRIVE_DISTANCE_THRESHOLD, _lastTargetApproachRange);

                // 检查局部寻路终点与拣选出的局部终点是否一致。
                if (count > 1 && (_localTarget - PathGenerator.CurrentPath[count - 1]).magnitude < arrivalTolerance)
                {
                    _currentGlobalRouteIndex = _processedGlobalRouteIndex;
                    _isPathLost = false;
                }
                else
                {
                    _isPathLost = true;
                }

                GenerateLocalRoute(PathGenerator.CurrentPath, _lastTargetApproachRange);
            }
            else
                _localRoute.Clear();

            _isLocalRouteCompleted = false;
            _isManeuversRollbacked = false;
            _isUsingRemainingManeuvers = false;

            int maxPriority = isEvade ? _lastCollision.MaxManeuverPriority : int.MaxValue;
            GenerateManeuvers(_lastLocalFrom, _lastLocalFromHeading, 0, maxPriority, _isIncludeAlignmentAtLast);
        }

        /// <summary>
        /// 根据给出的局部路径点，生成局部路径，如果点距离过近，或有停车距离要求，则予以精简。
        /// </summary>
        /// <param name="localPath">待精简的局部路径。</param>
        /// <param name="stopRange">与战斗距离有关，寻路时考虑战斗距离，可传入stopRange参数。</param>
        private void GenerateLocalRoute(List<Vector2> localPath, float stopRange)
        {
            _isLocalRouteCulledAtEnd = false;
            if (localPath.Count >= 2)
            {
                Vector2 startPoint = localPath[0];
                Vector2 endPoint = localPath[localPath.Count - 1];
                // 删减在终点stopRange范围内的路径点，返回调整后中间路径点数量。
                int transitionRouteCount = CullRoute(localPath, stopRange, !_isIncludeAlignmentAtLast, startPoint,
                    PathDriverUtils.TransformToWorldPoint(_lastTargetPoint), ref endPoint,
                    ref _isLocalRouteCulledAtEnd);
                if (transitionRouteCount >= 0)
                {
                    _localRoute.Capacity = transitionRouteCount + 2;
                    for (int i = 0; i <= transitionRouteCount; i++)
                    {
                        _localRoute.Add(localPath[i]);
                    }

                    _localRoute.Add(endPoint);
                }
                else
                {
                    _localRoute.AddRange(new Vector2[] { startPoint, startPoint });
                }
            }
        }

        /// <summary>
        /// 指定了到达范围时，算出终点的替代点 <paramref name="arrivalPoint"/> 的位置。
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stopRange"></param>
        /// <param name="isMoveOnly"></param>
        /// <param name="startPoint"></param>
        /// <param name="targetPoint"></param>
        /// <param name="arrivalPoint"></param>
        /// <param name="isCulledAtEnd"></param>
        /// <returns>返回除起点、终点以外，中间路径点的数量。</returns>
        private int CullRoute(List<Vector2> path, float stopRange, bool isMoveOnly, Vector2 startPoint,
            Vector2 targetPoint, ref Vector2 arrivalPoint, ref bool isCulledAtEnd)
        {
            // 过渡节点数，即除起、终点外的节点数量。
            int transitionRouteCount = path.Count - 2;
            if (stopRange == 0f)
                return transitionRouteCount;

            // 起点本身就处于停车范围内，中间路径点数精简为0。
            if ((targetPoint - startPoint).magnitude <= stopRange)
            {
                if (isMoveOnly)
                {
                    Stop(true);
                }

                arrivalPoint = startPoint;
                isCulledAtEnd = true;
                return 0;
            }

            Vector2 currentPoint = startPoint;

            for (int i = 0; i <= transitionRouteCount; i++)
            {
                Vector2 nextPoint = path[i + 1];
                if ((targetPoint - nextPoint).magnitude <= stopRange)
                {
                    transitionRouteCount = i;
                    Circle stopCircle = new Circle(targetPoint, stopRange);
                    Segment currentToNext = new Segment(currentPoint, nextPoint);
                    int intersectionCount = stopCircle.IntersectWith(currentToNext, out Vector2 intersection1,
                        out Vector2 intersection2);
                    if (intersectionCount == 1)
                    {
                        arrivalPoint = intersection1;
                        isCulledAtEnd = true;
                        break;
                    }
                }
                else
                {
                    currentPoint = nextPoint;
                }
            }

            return transitionRouteCount;
        }

        /// <summary>
        /// 若对方不是在回避我，且我是步兵，且超过3次要回避同一单位，则定义为“一连串的碰撞”。
        /// </summary>
        /// <param name="otherEvasionTarget">他者的回避目标</param>
        /// <param name="ignoreCounter">忽略次数</param>
        /// <returns></returns>
        private bool IsCascadeEvasion(IPathDriver otherEvasionTarget, int ignoreCounter = 0)
        {
            return otherEvasionTarget != this || _locomotionType == LocomotionType.Infantry ||
                   ignoreCounter > CASCADE_EVASION_MAX_IGNORE;
        }

        private void MarkUnitToBlockMap(IPathDriver unit)
        {
            _blockMapSO.MarkBlockMap(PathDriverUtils.TransformToWorldPoint(unit.Transform.position), unit.UnitSize,
                BlockMapSO.BlockFlag.Dynamic);
        }

        private void UnmarkUnitFromBlockMap(IPathDriver unit)
        {
            _blockMapSO.UnmarkBlockMap(PathDriverUtils.TransformToWorldPoint(unit.Transform.position), unit.UnitSize,
                BlockMapSO.BlockFlag.Dynamic);
        }

        private bool IsIgnoreEvasionType(IPathDriver other)
        {
            return this == other || other.RestrictType == LocomotionType.Infantry ||
                   _locomotionType == LocomotionType.Infantry && other.LocomotionType == LocomotionType.Infantry;
        }

        private Vector2 GetNextRouteTarget(Vector2 startPoint, float range, int currentIndex,
            out int mappingGlobalRouteIndex)
        {
            int globalRouteLength = _globalRoute.Length;
            Vector2 from = startPoint;
            float sumOfDistance = 0f;

            // 该局部路径点无法抵达。
            if (_isPathLost)
            {
                _isPathLost = false;
                currentIndex = globalRouteLength - 1;
            }

            currentIndex = currentIndex < 0 ? globalRouteLength - 1 : currentIndex;

            for (int i = currentIndex; i < globalRouteLength; i++)
            {
                Vector2 to = _globalRoute[i];
                Vector2 vectorToTarget = to - from;
                float distanceToTarget = vectorToTarget.magnitude;

                if (sumOfDistance + distanceToTarget >= range)
                {
                    float remainingDistance = range - sumOfDistance;
                    mappingGlobalRouteIndex = i;
                    return from + remainingDistance * vectorToTarget / distanceToTarget;
                }

                sumOfDistance += distanceToTarget;
                from = to;
            }

            mappingGlobalRouteIndex = -1;
            return _globalRoute[globalRouteLength - 1];
        }

        private bool GenerateManeuvers(Vector2 from, Vector2 fromHeading, int entryIndex,
            int maxPriority = int.MaxValue, bool isForceAlign = false)
        {
            Vector2 currentPoint = from;
            Vector2 currentDirection = fromHeading;
            bool isManeuversValid = false;
            maxPriority = (_locomotionType != LocomotionType.Infantry) ? maxPriority : 0;

            if (_localRoute.Count >= 2)
            {
                _nextRetryTime = 0f;
                int localRouteCount = _localRoute.Count;
                Vector2 localRouteEndPoint = _localRoute[localRouteCount - 1];
                bool isNewLocalRoute = (entryIndex <= 0);
                entryIndex = isNewLocalRoute ? 0 : entryIndex;

                // 索引关系：entryIndex + 2 == nextIndex + 1 == followIndex
                // 行动顺序：entryPoint --> nextPoint --> followPoint
                int currentIndex = entryIndex;
                int nextIndex = entryIndex + 1;
                Vector2 nextPoint = localRouteEndPoint;

                // 从这里开始情况分两种来处理：
                // 1、待处理节点数大于等于3个；
                // 2、待处理节点数只有2个；

                // 一、情况1成功添加了InPlaceTurn或Drift而没有添加到Turn的时候；
                // 二、情况2没能添加Turn的时候；
                // 满足上述其一，则需要添加Straight用作前往最后一个节点。
                bool isRequiredLastStraight = false;

                // 入口索引需要小于终点索引。
                if (currentIndex < localRouteCount - 1)
                {
                    nextPoint = _localRoute[nextIndex];
                    // 1、有3个或以上的localRoute节点；
                    // 2、localRoute节点数大于0；
                    if (localRouteCount - 1 >= nextIndex + 1 && localRouteCount > 0)
                    {
                        int followIndex = nextIndex + 1;
                        Vector2 followPoint;
                        // 如果followIndex未为终点索引，
                        if (followIndex < localRouteCount - 1)
                            followPoint = _localRoute[followIndex];
                        else
                            followPoint = localRouteEndPoint;

                        bool isGenerateCompleted = false;

                        // 如果设定了入口索引是-1，即无索引，或需要强制对齐，就添加混合Maneuver操作（漂移等）。
                        if (isNewLocalRoute || isForceAlign)
                        {
                            ManeuverInfo info = new ManeuverInfo
                            {
                                EntryPoint = currentPoint,
                                EntryDirection = currentDirection,
                                NextPoint = nextPoint,
                                FollowPoint = followPoint,
                                TargetDirection = (nextPoint - currentPoint).normalized
                            };
                            InsertCompositeTurnToManeuver(0, maxPriority, info, ref currentPoint, out isManeuversValid,
                                out bool isDoubleTurned, out bool isUseInverseDrift);

                            // 1、若添加了双扇形转弯，那么生成Maneuver的任务已经结束；
                            // question: 为何双扇形转弯就return，因为双扇形使用的空间已经太多，超出了localRoute的范围？
                            if (isDoubleTurned)
                            {
                                _retryCounter = 0;
                                return true;
                            }

                            // 2、若使用了反向漂移，则可能需要继续处理；
                            if (isUseInverseDrift)
                            {
                                nextIndex = followIndex;
                                nextPoint = followPoint;
                                followIndex++;
                                if (followIndex < localRouteCount - 1)
                                    followPoint = _localRoute[followIndex];
                                else if (followIndex == localRouteCount - 1)
                                    followPoint = localRouteEndPoint;
                                else
                                    isGenerateCompleted = true;
                            }
                        }

                        if (isManeuversValid && _maneuvers.Count > 0)
                            currentDirection = _maneuvers[_maneuvers.Count - 1].ExitDirection;

                        while (!isGenerateCompleted)
                        {
                            // 先确定currentPoint之后两点nextPoint和followPoint的位置关系，确定真实nextPoint的准确位置，
                            // 再生成由currentPoint前往nextPoint准确位置的Straight。
                            Maneuver turnOrDrift = BuildTurnManeuver(nextIndex, maxPriority, currentPoint, nextPoint,
                                followPoint);
                            Maneuver straight = new Straight(currentPoint, turnOrDrift.EntryPoint, nextIndex - 1);

                            if (AddManeuver(straight))
                                isManeuversValid = true;
                            if (AddManeuver(turnOrDrift))
                            {
                                isManeuversValid = true;
                                // 只有Turn的priority > 0，当turnOfDrift为Turn时，生成结束。
                                if (turnOrDrift.Priority > 0)
                                {
                                    _retryCounter = 0;
                                    return true;
                                }
                            }

                            // 由于currentPoint是被处理过的localRoute节点，所以不能用index来获得currentPoint。
                            currentPoint = turnOrDrift.ExitPoint;
                            currentDirection = turnOrDrift.ExitDirection;
                            nextIndex = followIndex;
                            nextPoint = followPoint;
                            followIndex++;

                            if (followIndex < localRouteCount - 1)
                                followPoint = _localRoute[followIndex];
                            else if (followIndex == localRouteCount - 1)
                                followPoint = localRouteEndPoint;
                            // 满足followIndex > localRouteCount - 1则break，
                            // 这里跳过了前往最后一点，即nextPoint的Maneuver，由下面补充。
                            else
                                break;
                        }

                        isRequiredLastStraight = (_maneuvers.Count > 0);
                    }
                    // 满足(localRouteCount - 1 < nextIndex + 1) || (localRouteCount <= 0)，
                    // 即不足3个localRoute节点，且终点位置合理。
                    else if ((localRouteEndPoint - currentPoint).sqrMagnitude > LOCAL_PATH_VALID_THRESHOLD_SQR)
                    {
                        nextPoint = localRouteEndPoint;
                        if (isNewLocalRoute || isForceAlign)
                        {
                            ManeuverInfo maneuverInfo = new ManeuverInfo
                            {
                                EntryPoint = currentPoint,
                                EntryDirection = currentDirection,
                                NextPoint = nextPoint,
                                TargetDirection = (nextPoint - currentPoint).normalized
                            };
                            InsertTurnToManeuver(0, maxPriority, maneuverInfo, out currentPoint, out isManeuversValid,
                                out bool isDoubleTurned);

                            if (isDoubleTurned)
                            {
                                _retryCounter = 0;
                                return true;
                            }
                        }

                        if (isManeuversValid && _maneuvers.Count > 0)
                            currentDirection = _maneuvers[_maneuvers.Count - 1].ExitDirection;

                        isRequiredLastStraight = true;
                    }
                } // endif (currentIndex < localRouteCount - 1)

                _isLocalRouteCompleted = true;

                if (isRequiredLastStraight)
                {
                    Maneuver lastStraight = new Straight(currentPoint, nextPoint, entryIndex + 1);
                    if (AddManeuver(lastStraight))
                        isManeuversValid = true;

                    currentPoint = lastStraight.ExitPoint;
                    currentDirection = lastStraight.ExitDirection;
                }

                if (_isIncludeAlignmentAtLast && IsLocalRouteAtEnd())
                {
                    Vector2 targetDirection = new Vector2(_lastTargetAlignment.x, _lastTargetAlignment.z);
                    InPlaceTurn inPlaceTurn = new InPlaceTurn(currentPoint, currentDirection, targetDirection,
                        _turnRadius, currentIndex);
                    currentPoint = inPlaceTurn.ExitPoint;
                    if (AddManeuver(inPlaceTurn))
                        isManeuversValid = true;
                }
            } // endif (localRoute.Count >= 2)

            if (isManeuversValid || _isLocalRouteCompleted)
                _retryCounter = 0;
            else
            {
                if (_retryCounter >= MANEUVERS_RETRY_COUNT)
                {
                    _nextRetryTime = 0f;
                    _retryCounter = 0;
                    Debug.Log(this + " give up.");
                    GiveUp();
                }
                else if (_retryCounter >= 2)
                {
                    ResetGlobalPath();
                    _nextRetryTime = MANEUVERS_RETRY_SECONDS;
                    _retryCounter++;
                }
                else
                {
                    _nextRetryTime = MANEUVERS_RETRY_SECONDS;
                    _retryCounter++;
                }
            }

            return isManeuversValid;
        }

        private Maneuver BuildTurnManeuver(int entryIndex, int maxPriority, Vector2 previousPoint, Vector2 currentPoint,
            Vector2 nextPoint)
        {
            Vector2 previousDirection = (currentPoint - previousPoint).normalized;
            Vector2 currentDirection = (nextPoint - currentPoint).normalized;
            float angle = Vector2.SignedAngle(previousDirection, currentDirection);
            float currentToCenter = _turnRadius / Mathf.Cos(0.5f * angle * Mathf.Deg2Rad);
            float fitDistance = Mathf.Sqrt(Utils.Sqr(currentToCenter) - Utils.Sqr(_turnRadius));
            Vector2 center = currentPoint + (currentDirection - previousDirection).normalized * currentToCenter;
            Vector2 fitPrevious = currentPoint - previousDirection * fitDistance;
            Vector2 fitNext = currentPoint + currentDirection * fitDistance;

            // 若合理入口位置在真实位置之前，则位置不够，采取Drift到达currentPoint。
            if (Vector2.Dot(previousDirection, fitPrevious - previousPoint) < 0f)
                return new Drift(previousPoint, previousDirection, currentPoint, currentDirection, _turnRadius,
                    entryIndex);
            // 若真实出口位置在合理位置之前，则位置不够，采取Drift到达currentPoint。
            if (Vector2.Dot(currentDirection, nextPoint - fitNext) < 0f || maxPriority == 0)
                return new Drift(fitPrevious, previousDirection, currentPoint, currentDirection, _turnRadius,
                    entryIndex);

            return new Turn(fitPrevious, fitNext, center, angle * Mathf.Deg2Rad, _turnRadius, entryIndex);
        }

        private void InsertCompositeTurnToManeuver(int entryIndex, int maxPriority, ManeuverInfo maneuverInfo,
            ref Vector2 exitPoint, out bool isManeuversValid, out bool isDoubleTurned, out bool isUseInverseDrift)
        {
            isUseInverseDrift = false;
            isManeuversValid = false;
            isDoubleTurned = false;

            float angle = Vector2.Angle(maneuverInfo.EntryDirection, maneuverInfo.TargetDirection);
            if (angle < TURN_THRESHOLD)
                return;

            float sqrDistance = (maneuverInfo.NextPoint - maneuverInfo.EntryPoint).sqrMagnitude;
            // 如果小于倒车阈值，用倒车解决。
            if (_locomotionType != LocomotionType.Infantry && angle > 90f && sqrDistance < REVERSE_THRESHOLD_SQR)
            {
                Vector2 targetDirection = (maneuverInfo.FollowPoint - maneuverInfo.NextPoint).normalized;
                Drift inverseDrift = new Drift(maneuverInfo.EntryPoint, maneuverInfo.EntryDirection,
                    maneuverInfo.NextPoint, targetDirection, _turnRadius, entryIndex);
                exitPoint = inverseDrift.ExitPoint;
                isManeuversValid = AddManeuver(inverseDrift);
                isUseInverseDrift = true;
                return;
            }

            InsertTurnToManeuver(entryIndex, maxPriority, maneuverInfo, out exitPoint, out isManeuversValid,
                out isDoubleTurned);
        }

        private void InsertTurnToManeuver(int entryIndex, int maxPriority, ManeuverInfo maneuverInfo,
            out Vector2 exitPoint, out bool isManeuversValid, out bool isDoubleTurned)
        {
            Vector2 entryPoint = maneuverInfo.EntryPoint;
            Vector2 entryDirection = maneuverInfo.EntryDirection;
            Vector2 nextPoint = maneuverInfo.NextPoint;
            Vector2 targetDirection = maneuverInfo.TargetDirection;

            exitPoint = maneuverInfo.EntryPoint; // out
            isManeuversValid = false; // out
            isDoubleTurned = false; // out

            // 以逆时针为正方向计算角度
            float signedAngle = Vector2.SignedAngle(entryDirection, targetDirection);
            float signOfAngle = Mathf.Sign(signedAngle);
            float space = (nextPoint - entryPoint).magnitude;

            if (Mathf.Abs(signedAngle) < TURN_THRESHOLD)
                return;

            InPlaceTurn inPlaceTurn = null;

            Vector2 currentPoint = entryPoint;
            Vector2 currentDirection = entryDirection;

            // 如果priority为0，就添加InPlaceTurn，否则添加Turn。
            if (maxPriority == 0)
            {
                inPlaceTurn = new InPlaceTurn(currentPoint, currentDirection, targetDirection, _turnRadius, entryIndex);
                exitPoint = inPlaceTurn.ExitPoint;
                isManeuversValid = AddManeuver(inPlaceTurn);
                return;
            }

            // 若需旋转的角度大于80度，那么先原地旋转到目标方向减80度的位置，
            // 即实际要旋转的角度区间：(0, 100)。
            if (Mathf.Abs(signedAngle) > MAXIMUM_TURN_ANGLE)
            {
                Vector2 turnedDirection = Utils.Rotate(targetDirection, -signOfAngle * MAXIMUM_TURN_ANGLE);
                inPlaceTurn = new InPlaceTurn(currentPoint, currentDirection, turnedDirection, _turnRadius, entryIndex);
                currentDirection = turnedDirection;
                currentPoint = exitPoint;
                //targetDirection = (maneuverInfo.NextPoint - maneuverInfo.EntryPoint).normalized;
                signedAngle = Vector2.SignedAngle(currentDirection, targetDirection);
            }

            // 旋转80度后，先得出圆心位置，再判断余下角度如何处理。
            Vector2 centerOfSector1 = currentPoint + Utils.Perpendicular(currentDirection, signOfAngle) * _turnRadius;
            float sinAngle = Mathf.Sin(Mathf.Abs(signedAngle) * Mathf.Deg2Rad);
            float cosAngle = Mathf.Cos(Mathf.Abs(signedAngle) * Mathf.Deg2Rad);
            float requiredLength = _turnRadius * (sinAngle + Mathf.Sqrt(Utils.Sqr(sinAngle) - 2f * (cosAngle - 1f)));

            // 如果不够空间完成两个扇形的旋转动作，那么就InPlaceTurn即可。
            if (space < requiredLength)
            {
                inPlaceTurn = new InPlaceTurn(currentPoint, entryDirection, targetDirection, _turnRadius, entryIndex);
                exitPoint = inPlaceTurn.ExitPoint; // out
                isManeuversValid = AddManeuver(inPlaceTurn);
                return;
            }

            if (inPlaceTurn != null)
                isManeuversValid = AddManeuver(inPlaceTurn);

            // 第二个扇形的出口位置
            Vector2 exitPointOfSector2 = currentPoint + targetDirection * requiredLength;
            // 第二个扇形的圆心
            Vector2 centerOfSector2 =
                exitPointOfSector2 + Utils.Perpendicular(targetDirection, -signOfAngle) * _turnRadius;
            // 两个扇形的交汇点
            Vector2 crossOfSectors = (centerOfSector1 + centerOfSector2) * 0.5f;
            // 第一个扇形的圆心角
            float angleOfSector1 =
                Vector2.SignedAngle(currentPoint - centerOfSector1, centerOfSector2 - centerOfSector1);
            float angleOfSector1InRad;
            float angleOfSector2InRad;

            // 第一个扇形圆心角与转向角异向，即转向角为钝角。
            if (angleOfSector1 * signOfAngle < 0f)
            {
                angleOfSector1InRad = angleOfSector1 <= 0f
                    ? (angleOfSector1 + 360f) * Mathf.Deg2Rad
                    : (angleOfSector1 - 360f) * Mathf.Deg2Rad;
                angleOfSector2InRad = angleOfSector1 <= 0f
                    ? (signedAngle - (angleOfSector1 + 360f)) * Mathf.Deg2Rad
                    : -(signedAngle + angleOfSector1) * Mathf.Deg2Rad;
            }
            else
            {
                angleOfSector1InRad = angleOfSector1 * Mathf.Deg2Rad;
                angleOfSector2InRad = (signedAngle - angleOfSector1) * Mathf.Deg2Rad;
            }

            Maneuver sector1 = new Turn(entryPoint, crossOfSectors, centerOfSector1, angleOfSector1InRad, _turnRadius,
                entryIndex);
            Maneuver sector2 = new Turn(crossOfSectors, exitPointOfSector2, centerOfSector2, angleOfSector2InRad,
                _turnRadius, entryIndex);

            if (AddManeuver(sector1))
            {
                isManeuversValid = true;
                sector2.LinkManeuver(sector1);
            }

            if (AddManeuver(sector2))
                isManeuversValid = true;

            isDoubleTurned = true;
            exitPoint = exitPointOfSector2;
        }

        private bool AddManeuver(Maneuver maneuver)
        {
            // 如果这个maneuver没有长度或者弧长，则添加失败。
            if (maneuver.Length > 0f)
            {
                maneuver.LocalRouteID = _localRouteID;
                RegisterManeuver(maneuver);
                return true;
            }

            return false;
        }

        private void RegisterManeuver(Maneuver maneuver)
        {
            maneuver.ManeuverID = _nextManeuverID;
            _nextManeuverID++;
            _maneuvers.Add(maneuver);
        }

        private void CleanupManeuvers()
        {
            int maneuverID = _followCursor.ManeuverID;
            int count = 0;
            for (int i = _maneuvers.Count - 1; i >= 0; i--)
            {
                if (_maneuvers[i].ManeuverID < maneuverID)
                {
                    count = i + 1;
                    _maneuvers.RemoveRange(0, count);

                    break;
                }
            }

            if (count > 0)
            {
                DriverProxy next = _followCursor;
                while (next != null)
                {
                    next.ManeuverIndex -= count;
                    next = next.Next;
                }
            }
        }

        private bool IsPositionRestricted(Vector2 worldPoint, BlockMapSO.BlockFlag blockFlag)
        {
            return _blockMapSO.IsBlockRestricted(worldPoint, Controllable.UnitSizeInWorld, blockFlag);
        }

        private bool DetectCollisions(DriverProxy proxy, int frameID, int maneuverIndex, bool isPredictive,
            ref CollisionInfo collisionInfo)
        {
            collisionInfo.StaticDriver = false;

            if (proxy.ProxyVelocity.sqrMagnitude == 0f)
                return false;

            if (isPredictive && maneuverIndex >= 0 && maneuverIndex < _maneuvers.Count &&
                _maneuvers[maneuverIndex].Priority > 0 &&
                IsPositionRestricted(proxy.Position, _lastTargetBlockOverride))
            {
                collisionInfo.TargetDriver = null;
                collisionInfo.StaticDriver = true;
                collisionInfo.CollisionFrameID = frameID;
                collisionInfo.CollisionPreventionFrameID = frameID;
                collisionInfo.ManeuverIndex = maneuverIndex;
                collisionInfo.TargetPosition = proxy.Position;
                collisionInfo.EvaluationFrameID = -1;
                collisionInfo.EvasionFrameID = collisionInfo.CollisionPreventionFrameID;
                return true;
            }

            Vector2 gridPosition = _blockMapSO.SnapToBlock(proxy.Position, Controllable.UnitSizeInWorld);
            float collisionRadius = SizeToCollisionRadius(Controllable.UnitSizeInWorld);
            float detectionRadius = collisionRadius + SizeToCollisionRadius(MAXIMUM_UNIT_SIZE);

            if (isPredictive)
                detectionRadius += MAXIMUN_GROUND_UNIT_DISTANCE;

            List<IPathDriver> unitsInAround = _unitGridSO.GetUnitsInAround(gridPosition, detectionRadius);

            for (int i = 0; i < unitsInAround.Count; i++)
            {
                IPathDriver other = unitsInAround[i];

                if (!IsIgnoreEvasionType(other) && !_overlappingUnits.Contains(other))
                {
                    IPathDriver otherEvasionTarget = EvaluateCollider(other, frameID, isPredictive,
                        out Vector2 otherPosition, out Vector2 otherVeloctiy, out int otherFrameID);

                    if (PreFilterCollision(gridPosition, collisionRadius, otherPosition, other))
                        continue;

                    Vector2 orientToOther = (otherPosition - gridPosition).normalized;

                    // 向对方行驶，有可能碰撞
                    if (Vector2.Dot(proxy.ProxyVelocity, orientToOther) >= 0f)
                    {
                        collisionInfo.EntryPosition = proxy.Position;
                        collisionInfo.EntryVelocity = proxy.ProxyVelocity;
                        collisionInfo.TargetDriver = other;
                        collisionInfo.TargetPosition = otherPosition;

                        // 产生碰撞所处的帧ID。
                        collisionInfo.CollisionFrameID = frameID;
                        // 产生碰撞后，到达赋值帧后，进行回避信息赋值的帧ID。
                        collisionInfo.EvaluationFrameID = frameID + PREDICTION_PAUSE_FRAMES;
                        // 产生碰撞后，进行回避寻路的帧ID。
                        collisionInfo.EvasionFrameID = frameID + PREDICTION_PAUSE_FRAMES;
                        // 产生碰撞后，回滚到碰撞前的帧ID，以预防这次的碰撞。
                        collisionInfo.CollisionPreventionFrameID = frameID - COLLISION_ROLLBACK_FRAMES;

                        // 1、在预测状态；
                        // 2、且在Turn时发生碰撞；
                        // 则回滚后直接回避寻路，不需要停车。
                        if (isPredictive && maneuverIndex >= 0 && maneuverIndex < _maneuvers.Count &&
                            _maneuvers[maneuverIndex].Priority > 0)
                        {
                            collisionInfo.ManeuverIndex = maneuverIndex;
                            collisionInfo.EvaluationFrameID = -1;
                            collisionInfo.CollisionPreventionFrameID = frameID;
                        }

                        // 如果对方要回避我，那我停车后等待久一点再回避。
                        if (otherEvasionTarget == this)
                        {
                            collisionInfo.EvaluationFrameID = -1;
                            collisionInfo.EvasionFrameID = frameID + PREDICTION_PAUSE_FRAME_DESYNC;
                        }
                        else
                        {
                            _evasionTarget = other;
                            _evasionFrameID = frameID;
                        }

                        break;
                    }
                }
            }

            return collisionInfo.IsAvailable();
        }

        private bool PreFilterCollision(Vector2 gridPosition, float collisionRadius, Vector2 otherPosition,
            IPathDriver other)
        {
            Vector2 otherGridPosition = _blockMapSO.SnapToBlock(otherPosition, other.UnitSize);
            float otherCollisionRadius = SizeToCollisionRadius(other.UnitSize);

            return Utils.SqrDistance(gridPosition, otherGridPosition) >=
                   Utils.Sqr(collisionRadius + otherCollisionRadius);
        }

        private IPathDriver EvaluateCollider(IPathDriver other, int frameID, bool isPredictive,
            out Vector2 otherPosition,
            out Vector2 otherVelocity, out int otherFrameID)
        {
            otherPosition = PathDriverUtils.TransformToWorldPoint(other.Transform.position);
            otherVelocity = Vector2.zero;
            otherFrameID = PlayerInfo.Instance.FrameCount;

            if (isPredictive && other.IsMoving)
            {
                otherVelocity = other.CurrentDriverState.DriverInfo.Rotation *
                                other.CurrentDriverState.DriverInfo.Speed;
                DriverProxy otherPrediction = other.GetPredictionInfo(frameID);

                if (otherPrediction != null)
                {
                    otherPosition = otherPrediction.Position;
                    otherVelocity = otherPrediction.ProxyVelocity;
                    otherFrameID = otherPrediction.FrameID;
                }
            }

            return other.GetEvasionTarget(frameID);
        }

        /// <summary>
        /// 取得当下要回避的目标，在检测到碰撞的80帧后目标会重置。
        /// </summary>
        /// <param name="frameID"></param>
        /// <returns>返回检测到碰撞需要回避的目标，在80帧后该目标会重置为 null。</returns>
        public override IPathDriver GetEvasionTarget(int frameID)
        {
            return frameID >= _evasionFrameID + EVASION_DURATION ? null : _evasionTarget;
        }

        public override DriverProxy GetPredictionInfo(int futureFrameID)
        {
            if (_followCursor != null)
            {
                DriverProxy next = _followCursor;
                while (next != null && next != _buildCursor)
                {
                    if (next.FrameID == futureFrameID)
                        return next;

                    next = next.Next;
                }

                if (_buildCursor.FrameID >= 0)
                    return _buildCursor;
            }

            return null;
        }

        /// <summary>
        /// 本类帧更新的首要方法，检查是否制定了新目标地点，决定是否重新全局寻路。
        /// </summary>
        /// <param name="targetPoint"></param>
        /// <param name="targetAlignment"></param>
        /// <param name="approachRange"></param>
        /// <returns></returns>
        private bool UpdateGlobalPath(Vector3 targetPoint, Vector3 targetAlignment, float approachRange)
        {
            if (_currentDriverState.ArrivalState == ArrivalState.StopWithInertia)
                return true;

            if (CheckNewPathRequirement(targetPoint, targetAlignment, approachRange))
            {
                ResetGlobalPath();
                SetProxyArriving(ArrivalState.FreeMoving);
                _isUsingRemainingManeuvers = _maneuvers.Count > 0;
            }

            if (!_isGlobalRouteValid)
            {
                _lastTargetPoint = targetPoint;
                _lastTargetAlignment = targetAlignment;
                _lastTargetApproachRange = approachRange;
                _lastTargetBlockOverride = BlockMapSO.BlockFlag.Dynamic | BlockMapSO.BlockFlag.RestrictVehicle;

                PathGenerator.RequestPath(Transform.position, new Vector3(targetPoint.x, 0f, targetPoint.z), -1);
                GenerateGlobalRoute(PathGenerator.CurrentPath, _lastTargetApproachRange);

                if (!PathGenerator.IsPathValid)
                    return _isUsingRemainingManeuvers;

                _isGlobalRouteValid = true;
            }

            return true;
        }

        private void GenerateGlobalRoute(List<Vector2> globalPath, float stopRange)
        {
            PrepareForNewGlobalRoute();

            if (globalPath.Count >= 2)
            {
                _globalRoute = globalPath.ToArray();
                _currentGlobalRouteIndex = 1;
                _processedGlobalRouteIndex = 0;
            }
        }

        private void PrepareForNewGlobalRoute()
        {
            if (_followCursor != _buildCursor)
            {
                ResetFollowCursor();
                // _buildCursor = _followCursor
                RePredictFromCursor(_followCursor);
            }

            _buildCursor.ResetManeuver();
            _maneuvers.Clear();
            _lastCollision.Reset();
        }

        private void ResetFollowCursor()
        {
            if (_followCursor == null)
                _followCursor = GetNextBlankPrediction(null);

            _followCursor.Reset(PlayerInfo.Instance.FrameCount,
                PathDriverUtils.TransformToWorldPoint(Transform.position),
                PathDriverUtils.TransformToWorldPoint(Transform.forward));
        }

        private void SetProxyArriving(ArrivalState arrivalState)
        {
            _currentDriverState.ArrivalState = arrivalState;
            _followCursor.ArrivalState = arrivalState;
            RePredictFromCursor(_followCursor);
        }

        private void RePredictFromCursor(DriverProxy cursor)
        {
            RollBackPredictionToCursor(cursor);
        }

        private void RollBackPredictionToCursor(DriverProxy cursor)
        {
            CommitRollBack(cursor);
        }

        private void CommitRollBack(DriverProxy cursor)
        {
            _buildCursor = cursor;
            if (cursor != null)
            {
                for (int i = _maneuvers.Count - 1; i >= 0; i--)
                {
                    if (_maneuvers[i].ManeuverID > cursor.ManeuverID)
                    {
                        _maneuvers.RemoveAt(i);
                        _isLocalRouteCompleted = false;
                    }
                }
            }
        }

        private void ResetGlobalPath()
        {
            _globalRoute = null;
            _isGlobalRouteValid = false;
            _isLocalRouteCulledAtEnd = false;
            _currentGlobalRouteIndex = -1;
            _processedGlobalRouteIndex = -1;
            PathGenerator.Reset();
        }

        private bool CheckNewPathRequirement(Vector3 targetPoint, Vector3 alignment, float approachRange)
        {
            if (_lastTargetAlignment == Vector3.zero)
                _lastTargetAlignment = Transform.forward;

            return (Vector3.Distance(targetPoint, _lastTargetPoint) > 0.001f) ||
                   (Vector3.Angle(alignment, _lastTargetAlignment) > ALIGNMENT_ANGLE_THRESHOLD) ||
                   (Mathf.Abs(approachRange - _lastTargetApproachRange) > 0.1f);
        }
        
        protected override bool HasArrived(out ArrivalResult arrivalResult)
        {
            arrivalResult = ArrivalResult.Enroute;

            if (_currentDriverState.IsGiveUp)
            {
                arrivalResult = ArrivalResult.Failed;
                return true;
            }

            // 进入刹车状态后判断到达。
            if (_followCursor != null && _followCursor.IsBraking &&
                (_maneuvers.Count == 0 || _followCursor.ProxySpeed < BREAK_STOP_THRESHOLD))
            {
                arrivalResult = ArrivalResult.Arrived;
                return true;
            }

            if (_currentDriverState.ArrivalState == ArrivalState.Arrive)
            {
                if (CheckRouteEnd() || CheckBreakStop())
                {
                    arrivalResult = ArrivalResult.Arrived;
                    return true;
                }
            }
            else if (_currentDriverState.ArrivalState == ArrivalState.StopWithInertia)
            {
                if (CheckBreakStop())
                {
                    arrivalResult = ArrivalResult.Arrived;
                    return true;
                }
            }
            else if (_currentDriverState.ArrivalState == ArrivalState.Stop)
            {
                arrivalResult = ArrivalResult.Arrived;
                return true;

                // if (CheckRouteEnd() || CheckBreakStop())
                // {
                //     Debug.Log("Instant Stop.");
                //     arrivalResult = ArrivalResult.Arrived;
                //     return true;
                // }
            }
            else if (IsLocalRouteAtEnd() && _maneuvers.Count == 0)
            {
                arrivalResult = ArrivalResult.Arrived;
                return true;
            }

            return false;
        }
    }
}