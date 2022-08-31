using CNC.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CNC.PathFinding
{
    [RequireComponent(typeof(Damageable), typeof(Controllable))]
    public class PathDriver : MonoBehaviour
    {
        [SerializeField] private UnitGridSO _unitGridSO = default;
        [SerializeField] private UnitProximitySO _unitProximitySO = default;
        [SerializeField] private BlockMapSO _blockMapSO = default;
        [SerializeField] private float _maxSpeed = default;

        [SerializeField] private float _turnRadius = default;

        // [SerializeField] private int _unitSizeInWorld = default;
        [SerializeField] private LocomotionType _locomotionType = default;
        [SerializeField] private LocomotionType _restrictType = LocomotionType.Wheels;

        private Damageable _damageable;
        private Controllable _controllable;
        private Transform _transform;
        private AStarPathGenerator _pathGenerator;

        private List<PathDriver> _overlappingUnits = new List<PathDriver>();
        private List<PathDriver> _proximityUnits = new List<PathDriver>();
        private int _unitGridIndex = -1;
        private int _unitProximityIndex = -1;
        private bool _isValid = true; // TODO: ��־λ����λ�Ƿ�����
        private DriverState _currentDriverState;
        private Vector3 _inputDestination;
        private Vector3 _inputAlignment;
        private float _inputApproachRange;

        private bool _isIncludeAlignmentAtLast;
        private Vector3 _lastTargetPoint;
        private Vector3 _lastTargetAlignment;
        private float _lastTargetApproachRange;
        private BlockMapSO.BlockFlag _lastTargetBlockOverride;

        private Vector2[] _globalRoute;
        private bool _isGlobalRouteValid;
        private DriverProxy _followCursor;
        private DriverProxy _buildCursor;
        private List<Maneuver> _maneuvers = new List<Maneuver>();
        private int _nextManeuverID;
        private bool _isLocalRouteCompleted;
        private bool _isLocalRouteCulledAtEnd;
        private CollisionInfo _lastCollision;
        private PathDriver _evasionTarget;
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
        private List<PathDriver> _markedUnits = new List<PathDriver>();
        private bool _isStaying;
        private bool _isRequestingPath;
        private bool _isPathLost;
        // private ArrivalResult _lastArrivalResult = ArrivalResult.Enroute;

        [SerializeField] private bool _isMoving = false;
        [SerializeField] private bool _isArrived = false;

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
        private const float DETECTION_RANGE = 25f;
        private const int MANEUVERS_RETRY_COUNT = 5;
        private const float MANEUVERS_RETRY_SECONDS = 1f;
        private const float MAXIMUM_LOCAL_PATH_RANGE = 45f;

        public LocomotionType UnitLocomotionType => _locomotionType;
        public int UnitSize => _controllable.UnitSizeInWorld;
        public float TurnRadius => _turnRadius;
        public bool IsArrived => _isArrived;
        public Controllable Controllable => _controllable;
        public Damageable Damageable => _damageable;

        public event UnityAction<ArrivalResult> OnArrivedDelegate;

        public float CurrentSpeed
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

        public float MaxSpeed
        {
            get => _maxSpeed;
            set => _maxSpeed = value;
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
        /// ���ⲿ�����޸ĵ���Ŀ�ĵء�
        /// </summary>
        /// <param name="destination">Ŀ�ĵ�</param>
        /// <param name="alignment">Ŀ�곯��</param>
        /// <param name="approachRange">���ﷶΧ</param>
        public void SetDestination(Vector3 destination, Vector3 alignment, bool isForcedAlign, float approachRange = 0f)
        {
            _inputDestination = destination;
            _inputAlignment = alignment;
            _inputApproachRange = approachRange;
            _isRequestingPath = true;
            _isIncludeAlignmentAtLast = isForcedAlign;
        }

        public void SetInPlaceTurn(Vector3 alignment)
        {
            SetDestination(_transform.position, alignment, true, 0f);
        }

        public void Stop(bool isInstant)
        {
            SetProxyBrake(true);
            SetProxyArriving(isInstant ? ArrivalState.Stop : ArrivalState.StopWithInertia);

            _lastTargetPoint = _transform.position;
            _lastTargetAlignment = _transform.forward;
            _lastTargetApproachRange = 0f;
        }

        public bool IsNewDestination(Vector3 destination)
        {
            return Vector3.Distance(destination, _lastTargetPoint) > ARRIVE_DISTANCE_THRESHOLD;
        }

        public bool IsNewAlignment(Vector3 alignment)
        {
            return Vector3.Angle(alignment, _lastTargetAlignment) > ALIGNMENT_ANGLE_THRESHOLD;
        }

        public bool IsNewApproachRange(Vector3 destination, float approachRange)
        {
            return Vector3.Distance(destination, _lastTargetPoint) > _lastTargetApproachRange + ARRIVE_DISTANCE_THRESHOLD;
        }

        private void Awake()
        {
            _pathGenerator = new AStarPathGenerator(this, _blockMapSO);
            _damageable = GetComponent<Damageable>();
            _controllable = GetComponent<Controllable>();
            _transform = transform;
        }

        private void OnEnable()
        {
            _damageable.OnDie += RemoveFromGridOnDie;
        }

        private void OnDisable()
        {
            _damageable.OnDie -= RemoveFromGridOnDie;
        }

        private void RemoveFromGridOnDie(Damageable damageable)
        {
            _unitGridSO.RemoveFromIndex(_unitGridIndex, this);
        }

        private void Start()
        {
            // ��ֵ_followCursor��ʼ��λ������ת��
            ResetProxy();
        }

        private void Update()
        {
            UpdateUnitGrid();
            UpdateUnitProximity();
            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (!_isRequestingPath)
            {
                UpdateNonMovement();
                return;
            }

            UpdateUnitGrid();
            PreUpdateMovement();
            _isArrived = false;
            _isStaying = false;

            if (!_isValid)
                StopWithInertia();

            if (_followCursor != null && _followCursor.ArrivalState != ArrivalState.Stop)
            {
                MoveTowards(_inputDestination, _inputAlignment, _inputApproachRange);
            }

            if (HasArrived(out ArrivalResult arrivalResult) ||
                Vector3.Distance(_transform.position, _lastTargetPoint) < _lastTargetApproachRange &&
                !_isIncludeAlignmentAtLast)
            {
                OnArrived();
                
                // if (arrivalResult == ArrivalResult.Failed)
                // {
                //     _lastTargetPoint = _transform.position;
                //     _lastTargetAlignment = _transform.forward;
                //     _lastTargetApproachRange = 0f;
                // }
                if (OnArrivedDelegate != null)
                {
                    OnArrivedDelegate.Invoke(arrivalResult);
                }
                
                // _lastTargetPoint = _transform.position;
                // _lastTargetAlignment = _transform.forward;
                // _lastTargetApproachRange = 0f;

                _isRequestingPath = false;
                _isArrived = true;
                _isIncludeAlignmentAtLast = false;
            }
        }

        private void OnArrived()
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

        private bool HasArrived(out ArrivalResult arrivalResult)
        {
            arrivalResult = ArrivalResult.Enroute;

            if (_currentDriverState.IsGiveUp)
            {
                arrivalResult = ArrivalResult.Failed;
                return true;
            }

            // ����ɲ��״̬���жϵ��
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

        private bool CheckBreakStop()
        {
            return _followCursor.ProxySpeed < BREAK_STOP_THRESHOLD;
            // return _currentDriverState.ProxySpeed < BREAK_STOP_THRESHOLD;
        }

        private bool CheckRouteEnd()
        {
            return IsLocalRouteAtEnd() && _followCursor.ManeuverIndex >= _maneuvers.Count;
        }

        private void StopWithInertia()
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

        private void UpdateNonMovement()
        {
            if (_overlappingUnits.Count > 0)
                _overlappingUnits.Clear();

            _isStaying = true;
        }

        private void ResetProxy()
        {
            _isMoving = false;
            _currentDriverState.DriverInfo.Reset(TransformToWorldPoint(_transform.position),
                TransformToWorldPoint(_transform.forward));
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

        private void PreUpdateMovement()
        {
            Vector2 worldPosition = _blockMapSO.SnapToBlock(TransformToWorldPoint(_transform.position),
                _controllable.UnitSizeInWorld);
            float collisionRadius = SizeToCollisionRadius(_controllable.UnitSizeInWorld);
            float maxCollisionRadius = collisionRadius + SizeToCollisionRadius(MAXIMUM_UNIT_SIZE);

            if (_isStaying)
            {
                List<PathDriver> unitsInAround = _unitGridSO.GetUnitsInAround(worldPosition, maxCollisionRadius);

                foreach (PathDriver driver in unitsInAround)
                {
                    if (!PreFilterCollision(worldPosition, collisionRadius, driver))
                        _overlappingUnits.Add(driver);
                }
            }
            else
            {
                for (int i = _overlappingUnits.Count - 1; i >= 0; i--)
                {
                    PathDriver driver = _overlappingUnits[i];

                    if (PreFilterCollision(worldPosition, collisionRadius, driver))
                        _overlappingUnits.Remove(driver);
                }
            }
        }

        private void UpdateUnitProximity()
        {
            if (_unitProximitySO.TryGetGridIndexFromTransformPosition(_transform.position, out int currentGridIndex))
            {
                if (currentGridIndex != _unitProximityIndex)
                {
                    if (_unitProximityIndex >= 0)
                        _unitProximitySO.RemoveUnitByGridIndex(_unitProximityIndex, this);

                    _unitProximityIndex = currentGridIndex;

                    if (_isValid && _unitProximityIndex >= 0)
                    {
                        _unitProximitySO.AddUnitToIndex(_unitProximityIndex, this);
                    }
                }

                _proximityUnits = _unitProximitySO.GetProximityUnits(this, _transform.position, DETECTION_RANGE);
            }
        }

        private void UpdateUnitGrid()
        {
            int currentUnitGridIndex =
                _isValid ? _unitGridSO.WorldToCellIndex(TransformToWorldPoint(_transform.position)) : -1;
            if (_unitGridIndex != currentUnitGridIndex)
            {
                if (_unitGridIndex >= 0)
                    _unitGridSO.RemoveFromIndex(_unitGridIndex, this);

                _unitGridIndex = currentUnitGridIndex;

                if (_unitGridIndex >= 0)
                    _unitGridSO.AddToIndex(_unitGridIndex, this);
            }
        }

        public static Vector2 TransformToWorldPoint(Vector3 transformPoint)
        {
            return new Vector2(transformPoint.x, transformPoint.z);
        }

        public static Vector3 WorldTotransformPoint(Vector2 worldPoint)
        {
            return new Vector3(worldPoint.x, 0, worldPoint.y);
        }

        private float SizeToCollisionRadius(float unitSizeInWorld)
        {
            return (unitSizeInWorld > 1f)
                ? (unitSizeInWorld * 0.5f - (0.5f * _blockMapSO.ToWorldScale * 1.4142f))
                : 0.5f;
        }

        private bool PreFilterCollision(Vector2 worldPoint, float collisionRadius, PathDriver otherDriver)
        {
            if (otherDriver == this || otherDriver == null)
            {
                return true;
            }

            Vector2 otherWorldBlockPoint =
                _blockMapSO.SnapToBlock(TransformToWorldPoint(otherDriver._transform.position), otherDriver.UnitSize);
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

            float acceleration = Acceleration;
            float maxSpeed = currentProxy.ArrivalState == ArrivalState.FreeMoving ? _maxSpeed : 0f;

            // �趨Ĭ��ֵ��
            displacement = currentProxy.ProxySpeed * deltaTime;
            currentProxy.ProxyAcceleration = 0f;

            if (currentProxy.IsBraking)
            {
                float a1 = currentProxy.ProxySpeed / 0.5f;
                float a2 = 0.5f * currentProxy.ProxySpeed * currentProxy.ProxySpeed / LIMIT_BRAKE_DISTANCE;
                acceleration = Mathf.Max(acceleration, a1);
                acceleration = Mathf.Max(acceleration, a2);
            }

            // ����趨�˼���֡����Ҫ�ڼ���֡���ڼ���0��
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

        private void MoveTowards(Vector3 targetPoint, Vector3 alignment, float approachRange)
        {
            // ���û���µ�Ѱ·ָʾ�����µġ�
            if (!UpdateGlobalPath(targetPoint, alignment, approachRange))
                return;

            // Ԥ�⣬���ɴ�������
            if (RefreshPrediction())
                RefreshPrediction();

            ExecuteMovement();
        }

        private void ExecuteMovement()
        {
            Vector2 position = TransformToWorldPoint(_transform.position);
            Vector2 rotation = TransformToWorldPoint(_transform.forward);

            if (FollowPrediction(ref position, ref rotation))
            {
                _transform.position = WorldTotransformPoint(position);
                _transform.forward = WorldTotransformPoint(rotation);
            }

            _isMoving = true;
        }

        /// <summary>
        /// ���Ԥ������Ƿ�������ײ��Ȼ�����Ԥ�����
        /// </summary>
        /// <param name="currentPoint"></param>
        /// <param name="currentRotation"></param>
        /// <returns>�� _followCursor �� _buildCursor ���ʱ������ false�����򷵻� true�� </returns>
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
        /// ��֡��Ԥ�⣬���� DriverProxy ����
        /// </summary>
        /// <returns>���ع��� DriverProxy �������ȵ� Maneuver ʱ������ true��</returns>
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

                // һ�������ײ���⣬�����ж��Ƿ񵽴�ͣ��֡-->��ֵ֡-->�ر�֡�����ս��лر�Ѱ·��

                // 1���п��ܻ���ײ��Ŀ�ꣻ
                // 2���ع���ͬһ��maneuver�����Ѿ��ѳ�ɲͣ��
                // 3���ع�����һ��maneuver���Ѿ����������maneuverԤ���ꣻ
                if (_lastCollision.IsAvailable() && (nextBlankPrediction.FrameID >= _lastCollision.StopFrameID ||
                                                     nextBlankPrediction.ManeuverIndex >= _maneuvers.Count))
                {
                    // �Ѿ��ѳ�ɲͣ�����ȵ���ֵ֡��������ִ�С�
                    if (nextBlankPrediction.FrameID < _lastCollision.EvaluationFrameID)
                    {
                        currentCursor = nextBlankPrediction;
                        i--;
                        continue;
                    }

                    // ��Ҫ��ֵ��ײ��Ϣ��
                    if (_lastCollision.EvaluationFrameID >= 0)
                    {
                        EvaluateCollision(ref _lastCollision);
                        _lastCollision.EvaluationFrameID = -1;
                    }

                    // �Ѿ���ֵ�����ȴ��ر�֡������ִ�С�
                    if (nextBlankPrediction.FrameID < _lastCollision.EvasionFrameID)
                    {
                        currentCursor = nextBlankPrediction;
                        i--;
                        continue;
                    }

                    RequestEvasionPath(nextBlankPrediction, _lastCollision.EvasionFrameID, _lastCollision.ManeuverInfo);
                }

                // �������ò�maneuver����������������������޷����¾ֲ�·������ô�ж��Ƿ��Ѿ������յ㡣
                if (nextBlankPrediction.ManeuverIndex >= _maneuvers.Count &&
                    !UpdateLocalPath(nextBlankPrediction.Position, nextBlankPrediction.Orientation,
                        nextBlankPrediction))
                {
                    UpdateProxySpeed(Time.deltaTime, nextBlankPrediction, out float arriveStep);
                    ValidateArrive(arriveStep, nextBlankPrediction);
                    _buildCursor = currentCursor;
                    return false;
                }

                // ��������currentCursor���ٶ�����ٶȡ�
                UpdateProxySpeed(Time.deltaTime, nextBlankPrediction, out float displacement);

                // �ġ�����step��currentCursor�У���ʱҪ�ж�ʣ��·���Ƿ��Ѿ�����ɲ�����룬�Ӷ��ı�currentCursor�ļ��ٶȡ�
                ProcessStep(displacement, currentCursor, nextBlankPrediction);

                currentCursor = nextBlankPrediction;
                i--;

                // ������һ��Ԥ��Ԥ���cursorλ��Turn������Ԥ�⡣
                if (i == 0 && currentCursor.IsMoving() && currentCursor.ManeuverIndex < _maneuvers.Count &&
                    _maneuvers[currentCursor.ManeuverIndex].Priority > 0)
                {
                    i = 1;
                }

                newCollision.Reset();

                // �塢�����ײ����¼��ײ��Ϣ��
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

            // �������������ײ�����лع���
            if (_lastCollision.NeedProcessing())
            {
                // �ع����������֣�
                // 1���� Turn ʱ��ײ�����¼������ ManeuverIndex���ع�����һ�� Maneuver �����һ�� DriverProxy��
                // 2���� Turn ʱ��ײ���ع���15֡ǰ�� proxy�������� ManeuverID ֮��� Maneuver ɾ����
                if (_lastCollision.ManeuverIndex >= 0)
                {
                    _lastCollision.ManeuverInfo = _maneuvers[_lastCollision.ManeuverIndex];

                    // �ع��ɹ����ر�֡��ǰ����ִ��һ�δ���Ԥ�⣬��������·���� Maneuver��
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
        /// �� <paramref name="from"/> ��ʼ���ݵ�ָ��֡ID <paramref name="frameID"/>��
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
        /// �� <paramref name="from"/> ��ʼ���ݣ�ֱ���ҵ�ĳ�� DriverProxy.ManeuverID С�� <paramref name="startRollbackManeuver"/>.ManeuverID �� driverProxy�����ع����ˡ�
        /// </summary>
        /// <param name="startRollbackManeuver"></param>
        /// <param name="from"></param>
        /// <returns>
        /// �����ݵ��Ϸ��� DriverProxy����ع������� true��
        /// �������� _followCursor����ع��� _followCursor�������� true��
        /// �� _followCursor Ҳ�� linkedManeuvers����Ҳ�ع��� _followCursor�������� false��
        /// </returns>
        private bool RollBackManeuver(Maneuver startRollbackManeuver, DriverProxy from)
        {
            DriverProxy driverProxy = from;

            while (driverProxy != _followCursor && driverProxy != null)
            {
                // ��飺
                // 1��driverProxy������ManeuverID�Ƿ�С��startRollbackManeuver��
                // 2�����Maneuver�Ƿ�startRollbackManeuver��linkManeuver��
                // ��1��2�ǣ�����Իع�����driverProxy������֮���Maneuverɾ����

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
        /// ��� driverProxy ������ maneuver �Ƿ� startRollbackManeuver �� linkManeuver����ǰ Maneuver �ͺ� Maneuver �Ƿ�˫����ת������ӹ�ϵ��
        /// </summary>
        /// <param name="driverProxy"></param>
        /// <param name="startRollbackManeuver"></param>
        /// <returns>�����ǣ�����true�����ǣ�����false��</returns>
        private bool CheckManeuverCleared(DriverProxy driverProxy, Maneuver startRollbackManeuver)
        {
            int maneuverIndex = driverProxy.ManeuverIndex;
            // ���maneuverIndex�ĺϷ��ԡ�
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

                // һ���ͽ����˼��پ������ڡ�
                if (step + brakingDistance >= maneuversLength)
                {
                    float replaceStep = Mathf.Max(maneuversLength - brakingDistance, 0f);
                    if (replaceStep >= 0f)
                        Step(currentProxy, ref replaceStep, ref position, ref orientation);

                    // ����·�̡����ٶȵó�ʱ�䡣
                    float remainingTime = GetRemainingTime(currentProxy,
                        step - Mathf.Max(maneuversLength - brakingDistance, 0f));
                    // Debug.Log("Set Arrive.");
                    currentProxy.ArrivalState = ArrivalState.Arrive;
                    // ����ʱ�����proxy���ٶȡ�
                    UpdateProxySpeed(remainingTime, currentProxy, out step);
                }
            }

            // ����step��ʣ��maneuvers���꣬��step�����࣬��ô���¾ֲ�·����maneuvers������maneuver�Ͻ����ƶ���
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
        /// ����һ��step��DriverProxy��Maneuver����㡣
        /// </summary>
        /// <param name="driverProxy"></param>
        /// <param name="step"></param>
        /// <param name="currentPoint"></param>
        /// <param name="currentDirection"></param>
        /// <returns>����step��ʣ��maneuvers���꣬��true������false��</returns>
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

                // stepδ������ǰmaneuver��ʣ�೤�ȣ����ڵ�ǰmaneuvers�ڡ�
                if (step <= remainingLength)
                {
                    currentManeuver.Step(step, ref currentPercentage, out currentPoint, out currentDirection);
                    step -= remainingLength;

                    driverProxy.ManeuverPercentage = currentPercentage;
                    driverProxy.ManeuverID = currentManeuver.ManeuverID;
                }
                // step������ǰmaneuver��ʣ�೤�ȣ�������һ��maneuver���жϡ�
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
                // ȡ��ʣ���maneuvers���ȡ�
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

            // ���maneuver���ɱ��εľֲ�Ѱ·���ã�����������¾ֲ�·���㣬ֱ������maneuver�Ӳ���ȥ��
            if (maneuver != null && maneuver.LocalRouteID == _localRouteID && maneuver.BypassNode < _localRoute.Count &&
                _localRoute.Count > 0)
            {
                // �ڷ�����ײ��maneuver��ǰһ��localRoute�ڵ㿪ʼѰ·��
                entryIndex = maneuver.BypassNode - 1;
                maxPriority = maneuver.Priority - 1;
                isSameLocalRoute = true;
            }

            // ��followCursor����maneuver֮ǰ��maneuverȫ�������
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

            PathDriver otherEvasionTarget = EvaluateCollider(collisionInfo.TargetDriver, collisionInfo.CollisionFrameID,
                true, out Vector2 otherPosition, out Vector2 otherVelocity, out int otherFrameID);
            if (otherVelocity.sqrMagnitude > 0f)
            {
                Vector2 vectorToOther = (otherPosition - collisionInfo.EntryPosition).normalized;
                Vector2 relativeVelocity = collisionInfo.EntryVelocity - otherVelocity;

                // ����ٶȲ���������Է���
                if (Vector2.Dot(vectorToOther, relativeVelocity) <= 0f)
                {
                    collisionInfo.EvasionFrameID = collisionInfo.EvaluationFrameID;
                    collisionInfo.TargetDriver = null;
                    return;
                }

                if (_locomotionType != LocomotionType.Infantry)
                {
                    // ��׷β��
                    if (Vector2.Dot(otherVelocity, vectorToOther) > 0f)
                    {
                        collisionInfo.EvasionFrameID = collisionInfo.EvaluationFrameID;
                        return;
                    }
                }
            }

            // �ϱ�û��return����ʱ��������ٶ�����Է���
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
            // ���followCursor����ӦmaneuverID֮ǰ������maneuver��
            CleanupManeuvers();
            int connectionIndex = _maneuvers.Count;

            // 1������maneuvers��
            // 2�����й��ع���GenerateManeuvers()�������Turn������localRouteδ��ȫ����Maneuver��
            // 3�����һ��maneuver���ڱ��ξֲ�Ѱ·�����ģ�
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

            // ��δ��maneuvers����ʼ�ֲ�Ѱ·��
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
                PathDriver other = _proximityUnits[i];
                // ��ΪԤ�ƻᷢ����ײ��Ŀ�꣬�������ر���
                if (other != _lastCollision.TargetDriver)
                {
                    if (!_overlappingUnits.Contains(other))
                    {
                        // TODO: _isMoving��ExecuteMovement()����true����ResetProxy()����false��
                        if (IsIgnoreEvasionType(other) || other._isMoving)
                            continue;
                        // �ų�����ĳ������ĵ�λ��
                        //if ((TransformToWorldPoint(other._transform.position) - from).sqrMagnitude > Utils.Sqr(LOCAL_PATH_MAX_RANGE + _unitSizeInWorld + other.UnitSize))
                        //    continue;
                    }
                }

                _markedUnits.Add(other);
                MarkUnitToBlockMap(other);
            }

            PathDriver otherEvasionTarget = _lastCollision.TargetDriver != null
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
            _pathGenerator.Reset();
            _localRoute.Clear();
            _localRouteID++;
            _lastLocalFrom = from;
            _lastLocalFromHeading = fromHeading;

            _pathGenerator.RequestPath(WorldTotransformPoint(from), WorldTotransformPoint(_localTarget), MAXIMUM_LOCAL_PATH_RANGE);
            OnLocalPathComplete(isEvade);

            for (int i = 0; i < _markedUnits.Count; i++)
                UnmarkUnitFromBlockMap(_markedUnits[i]);

            if (isRequiredMarkLastCollisionUnit)
                _blockMapSO.UnmarkBlockMap(markPoint, unitSizeInWorld, BlockMapSO.BlockFlag.Dynamic);

            return true;
        }

        private void OnLocalPathComplete(bool isEvade)
        {
            if (_pathGenerator.PathResult == Path.PathResult.Success)
            {
                int count = _pathGenerator.CurrentPath.Count;
                float arrivalTolerance = Mathf.Max(ARRIVE_DISTANCE_THRESHOLD, _lastTargetApproachRange);

                // ���ֲ�Ѱ·�յ����ѡ���ľֲ��յ��Ƿ�һ�¡�
                if (count > 1 && (_localTarget - _pathGenerator.CurrentPath[count - 1]).magnitude < arrivalTolerance)
                {
                    _currentGlobalRouteIndex = _processedGlobalRouteIndex;
                    _isPathLost = false;
                }
                else
                {
                    _isPathLost = true;
                }

                GenerateLocalRoute(_pathGenerator.CurrentPath, _lastTargetApproachRange);
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
        /// ���ݸ����ľֲ�·���㣬���ɾֲ�·���������������������ͣ������Ҫ�������Ծ���
        /// </summary>
        /// <param name="localPath">������ľֲ�·����</param>
        /// <param name="stopRange">��ս�������йأ�Ѱ·ʱ����ս�����룬�ɴ���stopRange������</param>
        private void GenerateLocalRoute(List<Vector2> localPath, float stopRange)
        {
            _isLocalRouteCulledAtEnd = false;
            if (localPath.Count >= 2)
            {
                Vector2 startPoint = localPath[0];
                Vector2 endPoint = localPath[localPath.Count - 1];
                // ɾ�����յ�stopRange��Χ�ڵ�·���㣬���ص������м�·����������
                int transitionRouteCount = CullRoute(localPath, stopRange, !_isIncludeAlignmentAtLast, startPoint,
                    TransformToWorldPoint(_lastTargetPoint), ref endPoint, ref _isLocalRouteCulledAtEnd);
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
                    _localRoute.AddRange(new Vector2[] {startPoint, startPoint});
                }
            }
        }

        /// <summary>
        /// ָ���˵��ﷶΧʱ������յ������� <paramref name="arrivalPoint"/> ��λ�á�
        /// </summary>
        /// <param name="path"></param>
        /// <param name="stopRange"></param>
        /// <param name="isMoveOnly"></param>
        /// <param name="startPoint"></param>
        /// <param name="targetPoint"></param>
        /// <param name="arrivalPoint"></param>
        /// <param name="isCulledAtEnd"></param>
        /// <returns>���س���㡢�յ����⣬�м�·�����������</returns>
        private int CullRoute(List<Vector2> path, float stopRange, bool isMoveOnly, Vector2 startPoint,
            Vector2 targetPoint, ref Vector2 arrivalPoint, ref bool isCulledAtEnd)
        {
            // ���ɽڵ������������յ���Ľڵ�������
            int transitionRouteCount = path.Count - 2;
            if (stopRange == 0f)
                return transitionRouteCount;

            // ��㱾��ʹ���ͣ����Χ�ڣ��м�·����������Ϊ0��
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
        /// ���Է������ڻر��ң������ǲ������ҳ���3��Ҫ�ر�ͬһ��λ������Ϊ��һ��������ײ����
        /// </summary>
        /// <param name="otherEvasionTarget">���ߵĻر�Ŀ��</param>
        /// <param name="ignoreCounter">���Դ���</param>
        /// <returns></returns>
        private bool IsCascadeEvasion(PathDriver otherEvasionTarget, int ignoreCounter = 0)
        {
            return otherEvasionTarget != this || _locomotionType == LocomotionType.Infantry ||
                   ignoreCounter > CASCADE_EVASION_MAX_IGNORE;
        }

        private void MarkUnitToBlockMap(PathDriver unit)
        {
            _blockMapSO.MarkBlockMap(TransformToWorldPoint(unit._transform.position), unit.UnitSize,
                BlockMapSO.BlockFlag.Dynamic);
        }

        private void UnmarkUnitFromBlockMap(PathDriver unit)
        {
            _blockMapSO.UnmarkBlockMap(TransformToWorldPoint(unit._transform.position), unit.UnitSize,
                BlockMapSO.BlockFlag.Dynamic);
        }

        private bool IsIgnoreEvasionType(PathDriver other)
        {
            return this == other || other._restrictType == LocomotionType.Infantry || _locomotionType == LocomotionType.Infantry && other._locomotionType == LocomotionType.Infantry;
        }

        private Vector2 GetNextRouteTarget(Vector2 startPoint, float range, int currentIndex,
            out int mappingGlobalRouteIndex)
        {
            int globalRouteLength = _globalRoute.Length;
            Vector2 from = startPoint;
            float sumOfDistance = 0f;

            // �þֲ�·�����޷��ִ
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

                // ������ϵ��entryIndex + 2 == nextIndex + 1 == followIndex
                // �ж�˳��entryPoint --> nextPoint --> followPoint
                int currentIndex = entryIndex;
                int nextIndex = entryIndex + 1;
                Vector2 nextPoint = localRouteEndPoint;

                // �����￪ʼ���������������
                // 1��������ڵ������ڵ���3����
                // 2��������ڵ���ֻ��2����

                // һ�����1�ɹ������InPlaceTurn��Drift��û����ӵ�Turn��ʱ��
                // �������2û�����Turn��ʱ��
                // ����������һ������Ҫ���Straight����ǰ�����һ���ڵ㡣
                bool isRequiredLastStraight = false;

                // ���������ҪС���յ�������
                if (currentIndex < localRouteCount - 1)
                {
                    nextPoint = _localRoute[nextIndex];
                    // 1����3�������ϵ�localRoute�ڵ㣻
                    // 2��localRoute�ڵ�������0��
                    if (localRouteCount - 1 >= nextIndex + 1 && localRouteCount > 0)
                    {
                        int followIndex = nextIndex + 1;
                        Vector2 followPoint;
                        // ���followIndexδΪ�յ�������
                        if (followIndex < localRouteCount - 1)
                            followPoint = _localRoute[followIndex];
                        else
                            followPoint = localRouteEndPoint;

                        bool isGenerateCompleted = false;

                        // ����趨�����������-1����������������Ҫǿ�ƶ��룬����ӻ��Maneuver������Ư�Ƶȣ���
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

                            // 1���������˫����ת�䣬��ô����Maneuver�������Ѿ�������
                            // question: Ϊ��˫����ת���return����Ϊ˫����ʹ�õĿռ��Ѿ�̫�࣬������localRoute�ķ�Χ��
                            if (isDoubleTurned)
                            {
                                _retryCounter = 0;
                                return true;
                            }

                            // 2����ʹ���˷���Ư�ƣ��������Ҫ��������
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
                            // ��ȷ��currentPoint֮������nextPoint��followPoint��λ�ù�ϵ��ȷ����ʵnextPoint��׼ȷλ�ã�
                            // ��������currentPointǰ��nextPoint׼ȷλ�õ�Straight��
                            Maneuver turnOrDrift = BuildTurnManeuver(nextIndex, maxPriority, currentPoint, nextPoint,
                                followPoint);
                            Maneuver straight = new Straight(currentPoint, turnOrDrift.EntryPoint, nextIndex - 1);

                            if (AddManeuver(straight))
                                isManeuversValid = true;
                            if (AddManeuver(turnOrDrift))
                            {
                                isManeuversValid = true;
                                // ֻ��Turn��priority > 0����turnOfDriftΪTurnʱ�����ɽ�����
                                if (turnOrDrift.Priority > 0)
                                {
                                    _retryCounter = 0;
                                    return true;
                                }
                            }

                            // ����currentPoint�Ǳ��������localRoute�ڵ㣬���Բ�����index�����currentPoint��
                            currentPoint = turnOrDrift.ExitPoint;
                            currentDirection = turnOrDrift.ExitDirection;
                            nextIndex = followIndex;
                            nextPoint = followPoint;
                            followIndex++;

                            if (followIndex < localRouteCount - 1)
                                followPoint = _localRoute[followIndex];
                            else if (followIndex == localRouteCount - 1)
                                followPoint = localRouteEndPoint;
                            // ����followIndex > localRouteCount - 1��break��
                            // ����������ǰ�����һ�㣬��nextPoint��Maneuver�������油�䡣
                            else
                                break;
                        }

                        isRequiredLastStraight = (_maneuvers.Count > 0);
                    }
                    // ����(localRouteCount - 1 < nextIndex + 1) || (localRouteCount <= 0)��
                    // ������3��localRoute�ڵ㣬���յ�λ�ú���
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

            // ���������λ������ʵλ��֮ǰ����λ�ò�������ȡDrift����currentPoint��
            if (Vector2.Dot(previousDirection, fitPrevious - previousPoint) < 0f)
                return new Drift(previousPoint, previousDirection, currentPoint, currentDirection, _turnRadius,
                    entryIndex);
            // ����ʵ����λ���ں���λ��֮ǰ����λ�ò�������ȡDrift����currentPoint��
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
            // ���С�ڵ�����ֵ���õ��������
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

            // ����ʱ��Ϊ���������Ƕ�
            float signedAngle = Vector2.SignedAngle(entryDirection, targetDirection);
            float signOfAngle = Mathf.Sign(signedAngle);
            float space = (nextPoint - entryPoint).magnitude;

            if (Mathf.Abs(signedAngle) < TURN_THRESHOLD)
                return;

            InPlaceTurn inPlaceTurn = null;

            Vector2 currentPoint = entryPoint;
            Vector2 currentDirection = entryDirection;

            // ���priorityΪ0�������InPlaceTurn���������Turn��
            if (maxPriority == 0)
            {
                inPlaceTurn = new InPlaceTurn(currentPoint, currentDirection, targetDirection, _turnRadius, entryIndex);
                exitPoint = inPlaceTurn.ExitPoint;
                isManeuversValid = AddManeuver(inPlaceTurn);
                return;
            }

            // ������ת�ĽǶȴ���80�ȣ���ô��ԭ����ת��Ŀ�귽���80�ȵ�λ�ã�
            // ��ʵ��Ҫ��ת�ĽǶ����䣺(0, 100)��
            if (Mathf.Abs(signedAngle) > MAXIMUM_TURN_ANGLE)
            {
                Vector2 turnedDirection = Utils.Rotate(targetDirection, -signOfAngle * MAXIMUM_TURN_ANGLE);
                inPlaceTurn = new InPlaceTurn(currentPoint, currentDirection, turnedDirection, _turnRadius, entryIndex);
                currentDirection = turnedDirection;
                currentPoint = exitPoint;
                //targetDirection = (maneuverInfo.NextPoint - maneuverInfo.EntryPoint).normalized;
                signedAngle = Vector2.SignedAngle(currentDirection, targetDirection);
            }

            // ��ת80�Ⱥ��ȵó�Բ��λ�ã����ж����½Ƕ���δ���
            Vector2 centerOfSector1 = currentPoint + Utils.Perpendicular(currentDirection, signOfAngle) * _turnRadius;
            float sinAngle = Mathf.Sin(Mathf.Abs(signedAngle) * Mathf.Deg2Rad);
            float cosAngle = Mathf.Cos(Mathf.Abs(signedAngle) * Mathf.Deg2Rad);
            float requiredLength = _turnRadius * (sinAngle + Mathf.Sqrt(Utils.Sqr(sinAngle) - 2f * (cosAngle - 1f)));

            // ��������ռ�����������ε���ת��������ô��InPlaceTurn���ɡ�
            if (space < requiredLength)
            {
                inPlaceTurn = new InPlaceTurn(currentPoint, entryDirection, targetDirection, _turnRadius, entryIndex);
                exitPoint = inPlaceTurn.ExitPoint; // out
                isManeuversValid = AddManeuver(inPlaceTurn);
                return;
            }

            if (inPlaceTurn != null)
                isManeuversValid = AddManeuver(inPlaceTurn);

            // �ڶ������εĳ���λ��
            Vector2 exitPointOfSector2 = currentPoint + targetDirection * requiredLength;
            // �ڶ������ε�Բ��
            Vector2 centerOfSector2 =
                exitPointOfSector2 + Utils.Perpendicular(targetDirection, -signOfAngle) * _turnRadius;
            // �������εĽ����
            Vector2 crossOfSectors = (centerOfSector1 + centerOfSector2) * 0.5f;
            // ��һ�����ε�Բ�Ľ�
            float angleOfSector1 =
                Vector2.SignedAngle(currentPoint - centerOfSector1, centerOfSector2 - centerOfSector1);
            float angleOfSector1InRad;
            float angleOfSector2InRad;

            // ��һ������Բ�Ľ���ת������򣬼�ת���Ϊ�۽ǡ�
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
            // ������maneuverû�г��Ȼ��߻����������ʧ�ܡ�
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
            return _blockMapSO.IsBlockRestricted(worldPoint, _controllable.UnitSizeInWorld, blockFlag);
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

            Vector2 gridPosition = _blockMapSO.SnapToBlock(proxy.Position, _controllable.UnitSizeInWorld);
            float collisionRadius = SizeToCollisionRadius(_controllable.UnitSizeInWorld);
            float detectionRadius = collisionRadius + SizeToCollisionRadius(MAXIMUM_UNIT_SIZE);

            if (isPredictive)
                detectionRadius += MAXIMUN_GROUND_UNIT_DISTANCE;

            List<PathDriver> unitsInAround = _unitGridSO.GetUnitsInAround(gridPosition, detectionRadius);

            for (int i = 0; i < unitsInAround.Count; i++)
            {
                PathDriver other = unitsInAround[i];

                if (!IsIgnoreEvasionType(other) && !_overlappingUnits.Contains(other))
                {
                    PathDriver otherEvasionTarget = EvaluateCollider(other, frameID, isPredictive,
                        out Vector2 otherPosition, out Vector2 otherVeloctiy, out int otherFrameID);

                    if (PreFilterCollision(gridPosition, collisionRadius, otherPosition, other))
                        continue;

                    Vector2 orientToOther = (otherPosition - gridPosition).normalized;

                    // ��Է���ʻ���п�����ײ
                    if (Vector2.Dot(proxy.ProxyVelocity, orientToOther) >= 0f)
                    {
                        collisionInfo.EntryPosition = proxy.Position;
                        collisionInfo.EntryVelocity = proxy.ProxyVelocity;
                        collisionInfo.TargetDriver = other;
                        collisionInfo.TargetPosition = otherPosition;

                        // ������ײ������֡ID��
                        collisionInfo.CollisionFrameID = frameID;
                        // ������ײ�󣬵��︳ֵ֡�󣬽��лر���Ϣ��ֵ��֡ID��
                        collisionInfo.EvaluationFrameID = frameID + PREDICTION_PAUSE_FRAMES;
                        // ������ײ�󣬽��лر�Ѱ·��֡ID��
                        collisionInfo.EvasionFrameID = frameID + PREDICTION_PAUSE_FRAMES;
                        // ������ײ�󣬻ع�����ײǰ��֡ID����Ԥ����ε���ײ��
                        collisionInfo.CollisionPreventionFrameID = frameID - COLLISION_ROLLBACK_FRAMES;

                        // 1����Ԥ��״̬��
                        // 2������Turnʱ������ײ��
                        // ��ع���ֱ�ӻر�Ѱ·������Ҫͣ����
                        if (isPredictive && maneuverIndex >= 0 && maneuverIndex < _maneuvers.Count &&
                            _maneuvers[maneuverIndex].Priority > 0)
                        {
                            collisionInfo.ManeuverIndex = maneuverIndex;
                            collisionInfo.EvaluationFrameID = -1;
                            collisionInfo.CollisionPreventionFrameID = frameID;
                        }

                        // ����Է�Ҫ�ر��ң�����ͣ����ȴ���һ���ٻرܡ�
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
            PathDriver other)
        {
            Vector2 otherGridPosition = _blockMapSO.SnapToBlock(otherPosition, other.UnitSize);
            float otherCollisionRadius = SizeToCollisionRadius(other.UnitSize);

            return Utils.SqrDistance(gridPosition, otherGridPosition) >=
                   Utils.Sqr(collisionRadius + otherCollisionRadius);
        }

        private PathDriver EvaluateCollider(PathDriver other, int frameID, bool isPredictive, out Vector2 otherPosition,
            out Vector2 otherVelocity, out int otherFrameID)
        {
            otherPosition = TransformToWorldPoint(other._transform.position);
            otherVelocity = Vector2.zero;
            otherFrameID = PlayerInfo.Instance.FrameCount;

            if (isPredictive && other._isMoving)
            {
                otherVelocity = other._currentDriverState.DriverInfo.Rotation *
                                other._currentDriverState.DriverInfo.Speed;
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
        /// ȡ�õ���Ҫ�رܵ�Ŀ�꣬�ڼ�⵽��ײ��80֡��Ŀ������á�
        /// </summary>
        /// <param name="frameID"></param>
        /// <returns>���ؼ�⵽��ײ��Ҫ�رܵ�Ŀ�꣬��80֡���Ŀ�������Ϊ null��</returns>
        private PathDriver GetEvasionTarget(int frameID)
        {
            return frameID >= _evasionFrameID + EVASION_DURATION ? null : _evasionTarget;
        }

        private DriverProxy GetPredictionInfo(int futureFrameID)
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
        /// ����֡���µ���Ҫ����������Ƿ��ƶ�����Ŀ��ص㣬�����Ƿ�����ȫ��Ѱ·��
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

                _pathGenerator.RequestPath(_transform.position, new Vector3(targetPoint.x, 0f, targetPoint.z), -1);
                GenerateGlobalRoute(_pathGenerator.CurrentPath, _lastTargetApproachRange);

                if (!_pathGenerator.IsPathValid)
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

            _followCursor.Reset(PlayerInfo.Instance.FrameCount, TransformToWorldPoint(_transform.position),
                TransformToWorldPoint(_transform.forward));
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
            _pathGenerator.Reset();
        }

        private bool CheckNewPathRequirement(Vector3 targetPoint, Vector3 alignment, float approachRange)
        {
            if (_lastTargetAlignment == Vector3.zero)
                _lastTargetAlignment = _transform.forward;

            return (Vector3.Distance(targetPoint, _lastTargetPoint) > 0.001f) ||
                   (Vector3.Angle(alignment, _lastTargetAlignment) > ALIGNMENT_ANGLE_THRESHOLD) ||
                   (Mathf.Abs(approachRange - _lastTargetApproachRange) > 0.1f);
        }

        internal enum ArrivalState
        {
            FreeMoving,
            Arrive,
            Stop,
            StopWithInertia,
            GiveUp,
            Complete
        }

        public enum ArrivalResult
        {
            Enroute, // ��;��
            Arrived, // ����
            Paused, // ��ͣ
            Failed // ʧ��
        }

        internal struct DriverState
        {
            internal DriverInfo DriverInfo { get; set; }
            internal float ProxySpeed { get; set; }
            internal float ProxyAcceleration { get; set; }
            internal bool IsBraking { get; set; }
            internal bool IsGiveUp { get; set; }
            internal ArrivalState ArrivalState { get; set; }
        }

        internal struct DriverInfo
        {
            internal Vector2 Position { get; set; }
            internal Vector2 Rotation { get; set; }
            internal float Speed { get; set; }

            internal void Reset(Vector2 position, Vector2 rotation)
            {
                Position = position;
                Rotation = rotation;
                Speed = 0f;
            }
        }

        internal struct CollisionInfo
        {
            internal Vector2 EntryPosition { get; set; }
            internal Vector2 EntryVelocity { get; set; }
            internal float CollisionSpeed { get; set; }

            internal Maneuver ManeuverInfo { get; set; }
            internal int ManeuverIndex { get; set; }
            internal int CollisionFrameID { get; set; }
            internal int EvaluationFrameID { get; set; }
            internal int EvasionFrameID { get; set; }

            internal PathDriver TargetDriver { get; set; }
            internal PathDriver LastDriver { get; set; }
            internal Vector2 TargetPosition { get; set; }
            internal int MaxManeuverPriority { get; set; }
            internal bool StaticDriver { get; set; }

            internal int CollisionPreventionFrameID { get; set; }
            internal int StopFrameID { get; set; }
            internal int DecelerationFrames { get; set; }
            internal int IgnoreCounter { get; set; }

            internal bool IsProcessed { get; set; }

            internal void Reset()
            {
                ManeuverInfo = null;
                ManeuverIndex = -1;
                CollisionSpeed = 0f;
                CollisionFrameID = -1;
                EvaluationFrameID = -1;
                EvasionFrameID = -1;

                TargetDriver = null;
                StopFrameID = -1;
                DecelerationFrames = 0;
                MaxManeuverPriority = 0;
                StaticDriver = false;

                IsProcessed = false;
            }

            internal static void Update(CollisionInfo newInfo, ref CollisionInfo oldInfo)
            {
                PathDriver driver = newInfo.TargetDriver;
                int ignoreCounter = newInfo.IgnoreCounter;

                if (driver == oldInfo.LastDriver)
                    ignoreCounter++;
                else
                    ignoreCounter = 0;

                oldInfo = newInfo;
                oldInfo.LastDriver = driver;
                oldInfo.IgnoreCounter = ignoreCounter;
            }

            internal bool NeedProcessing()
            {
                return IsAvailable() && !IsProcessed;
            }

            internal bool IsAvailable()
            {
                return TargetDriver != null || StaticDriver;
            }
        }

        public enum LocomotionType
        {
            Infantry,
            Wheels,
        }
    }
}