using System.Collections.Generic;
using CNC.PathFinding.Proximity;
using UnityEngine;
using UnityEngine.Events;

namespace CNC.PathFinding
{
    public abstract class PathDriverBase : MonoBehaviour, IPathDriver
    {
        private const float DETECTION_RANGE = 25f;
        
        protected DriverState _currentDriverState;
        
        protected int _unitProximityIndex = -1;
        protected bool _isFunctional = true; // TODO: 标志位，单位是否死亡
        protected List<IPathDriver> _proximityUnits = new List<IPathDriver>();
        protected readonly List<IPathDriver> _overlappingUnits = new List<IPathDriver>();
        protected float _lastTargetApproachRange;
        
        private readonly IProximityManager _proximityManager = ProximityManager.Singleton;
        
        protected int _unitGridIndex = -1;
        
        protected bool _isRequestingPath;
        
        protected bool _isIncludeAlignmentAtLast;
        protected Vector3 _lastTargetPoint;
        
        protected Vector3 _inputDestination;
        protected Vector3 _inputAlignment;
        protected float _inputApproachRange;
        protected DriverProxy _followCursor;
        
        protected bool _isStaying;
        
        
        [SerializeField] protected UnitGridSO _unitGridSO = default;
        [SerializeField] protected BlockMapSO _blockMapSO = default;
        
        [SerializeField] protected float _maxSpeed = default;
        public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = value; }
        
        [SerializeField] protected float _turnRadius = default;
        public float TurnRadius => _turnRadius;
        
        [SerializeField] protected LocomotionType _restrictType = LocomotionType.Wheels;
        public LocomotionType RestrictType => _restrictType;
        
        [SerializeField] protected LocomotionType _locomotionType = default;
        public LocomotionType LocomotionType => _locomotionType;
        
        public int UnitSize => Controllable.UnitSizeInWorld;
        
        public bool IsMoving { get; protected set; }
        public bool IsArrived { get; protected set; }

        public Damageable Damageable { get; private set; }
        public Controllable Controllable { get; private set; }
        public Transform Transform { get; private set; }
        public IPathGenerator PathGenerator { get; private set; }

        public virtual event UnityAction<ArrivalResult> OnArrivedEvent;
        public virtual float CurrentSpeed { get; }
        
        public DriverState CurrentDriverState { get; }

        public abstract void SetDestination(Vector3 destination, Vector3 alignment, bool isForcedAlign,
            float approachRange = 0);

        public abstract void SetInPlaceTurn(Vector3 alignment);

        public abstract void Stop(bool isInstant);

        public abstract bool IsNewDestination(Vector3 destination);

        public abstract bool IsNewAlignment(Vector3 alignment);

        public abstract bool IsNewApproachRange(Vector3 destination, float approachRange);

        public abstract IPathDriver GetEvasionTarget(int frameID);

        public abstract DriverProxy GetPredictionInfo(int futureFrameID);

        protected void Awake()
        {
            Transform = transform;
            PathGenerator = new AStarPathGenerator(this, _blockMapSO);
            Damageable = GetComponent<Damageable>();
            Controllable = GetComponent<Controllable>();
        }

        private void OnEnable()
        {
            Damageable.OnDie += RemoveFromGridOnDie;
        }

        private void OnDisable()
        {
            Damageable.OnDie -= RemoveFromGridOnDie;
        }
        
        protected virtual void Update()
        {
            UpdateUnitGrid();
            UpdateUnitProximity();
            // PreUpdateMovement();
            UpdateMovement();
            PostUpdateMovement();
        }

        private void UpdateNonMovement()
        {
            if (_overlappingUnits.Count > 0)
                _overlappingUnits.Clear();

            _isStaying = true;
        }
        
        protected abstract void PreUpdateMovement();

        private void UpdateMovement()
        {
            if (!_isRequestingPath)
            {
                UpdateNonMovement();
                return;
            }
            
            // UpdateUnitGrid();
            PreUpdateMovement();
            IsArrived = false;
            _isStaying = false;

            if (!_isFunctional)
                StopWithInertia();

            if (_followCursor != null && _followCursor.ArrivalState != ArrivalState.Stop)
            {
                MoveTowards(_inputDestination, _inputAlignment, _inputApproachRange);
            }

            if (HasArrived(out ArrivalResult arrivalResult) ||
                Vector3.Distance(Transform.position, _lastTargetPoint) < _lastTargetApproachRange &&
                !_isIncludeAlignmentAtLast)
            {
                OnArrived();

                // if (arrivalResult == ArrivalResult.Failed)
                // {
                //     _lastTargetPoint = _transform.position;
                //     _lastTargetAlignment = _transform.forward;
                //     _lastTargetApproachRange = 0f;
                // }
                OnArrivedEvent?.Invoke(arrivalResult);

                // _lastTargetPoint = _transform.position;
                // _lastTargetAlignment = _transform.forward;
                // _lastTargetApproachRange = 0f;

                _isRequestingPath = false;
                IsArrived = true;
                _isIncludeAlignmentAtLast = false;
            }
        }
        
        protected abstract void PostUpdateMovement();
        
        private void UpdateUnitProximity()
        {
            if (!_proximityManager.TryGetGridIndexFromTransformPosition(Transform.position, out int currentGridIndex))
                return;

            if (currentGridIndex != _unitProximityIndex)
            {
                if (_unitProximityIndex >= 0)
                    _proximityManager.RemoveUnitByGridIndex(_unitProximityIndex, this);

                _unitProximityIndex = currentGridIndex;

                if (_isFunctional && _unitProximityIndex >= 0)
                {
                    _proximityManager.AddUnitToIndex(_unitProximityIndex, this);
                }
            }

            _proximityUnits = _proximityManager.GetProximityUnits(this, Transform.position, DETECTION_RANGE);
        }

        protected abstract bool HasArrived(out ArrivalResult arrivalResult);
        
        protected void UpdateUnitGrid()
        {
            int currentUnitGridIndex =
                _isFunctional
                    ? _unitGridSO.WorldToCellIndex(PathDriverUtils.TransformToWorldPoint(Transform.position))
                    : -1;
            if (_unitGridIndex != currentUnitGridIndex)
            {
                if (_unitGridIndex >= 0)
                    _unitGridSO.RemoveFromIndex(_unitGridIndex, this);

                _unitGridIndex = currentUnitGridIndex;

                if (_unitGridIndex >= 0)
                    _unitGridSO.AddToIndex(_unitGridIndex, this);
            }
        }

        protected abstract void MoveTowards(Vector3 targetPoint, Vector3 alignment, float approachRange);

        protected abstract void OnArrived();
        
        protected abstract void StopWithInertia();
        
        private void RemoveFromGridOnDie(Damageable damageable)
        {
            _unitGridSO.RemoveFromIndex(_unitGridIndex, this);
            _proximityManager.RemoveUnitByGridIndex(_unitProximityIndex, this);
        }
    }
}