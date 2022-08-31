using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using CNC.Utility;
using CNC.PathFinding;

[Serializable]
public class Platoon : IGroupAbility
{
    public enum FormationType
    {
        VehicleFirst,
        SoldierFirst,
        Circle
    };

    private List<Controllable> _platoonUnits = new List<Controllable>();
    private List<PlatoonMember> _members = new List<PlatoonMember>();
    private List<Vector3> _formationPositions = new List<Vector3>();
    private Vector3 _platoonAlignment;
    private Vector3 _platoonCenter;
    private float _radiusSqr;
    private float _sumOfUnitSize;
    private BlockMapSO _blockMap;

    private float _maxAccelerate;
    private float _maxSpeed;

    public bool IsAttackable { get; set; } = false;
    public bool IsMoveable { get; set; } = false;
    public bool IsRotatable { get; set; } = false;
    public bool IsLayable { get; set; } = false;
    public bool IsEmptyPlatoon => _platoonUnits.Count < 1;

    private const float _PLATOON_COLUNM_GAP_SCALE = 1.5f;
    private const float _PLATOON_ROW_GAP_SCALE = 2f;

    // Dismiss this platoon when finished the command.
    // public UnityAction OnCommandFinished;
    // public UnityAction OnDismiss;

    public Platoon(List<Controllable> platoonUnits, BlockMapSO blockMap)
    {
        InitializePlatoon(platoonUnits);
        _blockMap = blockMap;
    }

    public Platoon(Controllable platoonUnit, BlockMapSO blockMap)
    {
        _platoonUnits.Add(platoonUnit);
        RecalculateAbility();
        _blockMap = blockMap;
    }

    public void ResetPlatoon()
    {
        _platoonUnits.Clear();
        _formationPositions.Clear();
    }

    public void InitializePlatoon(List<Controllable> platoonUnits)
    {
        _platoonUnits = platoonUnits;
        RecalculateAbility();
    }

    public void ResetAbility()
    {
        IsAttackable = false;
        IsMoveable = false;
    }

    public void RecalculateAbility()
    {
        ResetAbility();
        foreach (Controllable platoonUnit in _platoonUnits)
        {
            IsAttackable = IsAttackable || platoonUnit.IsAttackable;
            IsMoveable = IsMoveable || platoonUnit.IsMoveable;
        }
    }

    public void AddUnitIntoPlatoon(Controllable unit)
    {
        _platoonUnits.Add(unit);
        unit.Damageable.OnDie += OnPlatoonUnitDie;
        RecalculateAbility();
    }

    public void AddUnitIntoPlatoon(List<Controllable> units)
    {
        foreach (Controllable unit in units)
        {
            _platoonUnits.Add(unit);
            unit.Damageable.OnDie += OnPlatoonUnitDie;
        }

        RecalculateAbility();
    }

    public void RemoveUnitFromPlatoon(Controllable unit)
    {
        if (!_platoonUnits.Contains(unit))
            return;

        unit.Damageable.OnDie -= OnPlatoonUnitDie;
        _platoonUnits.Remove(unit);
        RecalculateAbility();
    }

    private void OnPlatoonUnitDie(Damageable damageable)
    {
        damageable.OnDie -= OnPlatoonUnitDie;
        RemoveUnitFromPlatoon(damageable.Controllable);
    }

    public void AssignCommand(Command command)
    {
        switch (command.CommandType)
        {
            case CommandType.Chase:
                for (int i = 0; i < _platoonUnits.Count; i++)
                {
                    _platoonUnits[i].AddCommand(command);
                }

                break;

            case CommandType.Move:
                if (_platoonUnits.Count == 1)
                {
                    if (_formationPositions.Count > 0)
                    {
                        command.Destination = _formationPositions[0];
                        command.TargetAlignment = _platoonAlignment;
                        command.IsForcedAlign = true;
                    }

                    _platoonUnits[0].AddCommand(command);
                    break;
                }

                SetupPlatoonMembers();
                CreateMoveCommand(command);
                ApplyPrecalculatedCommand();

                // if (_formationPositions.Count < 1)
                // {
                //     Vector3[] unitPositions = _platoonUnits.Select(t => t.transform.position).ToArray();
                //     _platoonCenter = Utils.CalculateCenterOfGravity(unitPositions);
                //     _formationPositions = GetFormationPositions(command.Destination, _platoonCenter,
                //         out _platoonAlignment, FormationType.VehicleFirst).ToList();
                // }
                //
                // for (int i = 0; i < _formationPositions.Count; i++)
                // {
                //     Command moveCommand = new Command
                //     {
                //         CommandType = CommandType.Move,
                //         Destination = _formationPositions[i],
                //         TargetAlignment = _platoonAlignment,
                //         IsForcedAlign = true
                //     };
                //     _platoonUnits[i].AddCommand(moveCommand);
                // }

                break;

            case CommandType.Stop:
                for (int i = 0; i < _platoonUnits.Count; i++)
                {
                    _platoonUnits[i].AddCommand(command);
                }

                break;

            case CommandType.LayOrStand:
                if (_platoonUnits.Count == 1)
                {
                    _platoonUnits[0].AddCommand(command, false, true);
                    break;
                }

                List<Controllable> soldiers = _platoonUnits.FindAll(u => u.Damageable.UnitType == UnitType.Infantry);

                if (soldiers.Count > 0)
                {
                    List<Controllable> layingSoldiers = soldiers.FindAll(u => u.GetComponent<Human>().IsLaying);
                    // 仅让Laying的单位Stand。
                    if (layingSoldiers.Count > 0)
                    {
                        foreach (Controllable layingSoldier in layingSoldiers)
                        {
                            layingSoldier.AddCommand(command, false, true);
                        }
                    }
                    else
                    {
                        foreach (Controllable soldier in soldiers)
                        {
                            soldier.AddCommand(command, false, true);
                        }
                    }
                }

                break;
        }
    }

    public List<Controllable> GetPlatoonUnits()
    {
        return _platoonUnits;
    }

    private void SetupPlatoonMembers()
    {
        _members.Clear();
        foreach (Controllable platoonUnit in _platoonUnits)
        {
            _members.Add(new PlatoonMember(platoonUnit));
        }
    }

    public void ApplyPrecalculatedCommand()
    {
        foreach (PlatoonMember member in _members)
        {
            member.Unit.AddCommand(member.PrecalculatedCommand);
        }
    }

    public void AssignDestPointAndOrientation(List<Vector3> positions, Vector3 orientation)
    {
        _formationPositions = positions;
        _platoonAlignment = orientation;
    }

    public void GetFormationPositions(Vector3 centerOfFirstLine, Vector3 alignment,
        FormationType formationType = FormationType.SoldierFirst)
    {
        // 用这个向量确定圆形布阵的0点方向位置
        // orientation = (targetPoint - centerOfFirstLine).normalized;
        float offsetDegree = Vector3.Cross(alignment, Vector3.forward).y > 0f
            ? Vector3.Angle(alignment, Vector3.forward)
            : -Vector3.Angle(alignment, Vector3.forward);

        // 保存计算出的所有坐标结果，用于返回
        // var resultDestinations = new List<Vector3>();
        // 容量表示共有多少种半径，元素值表示该种半径的大小
        var radiusList = new List<int>();
        // 容量表示共有多少种半径，元素值表示该种半径有多少个
        var radiusCountList = new List<int>();
        // 升序排列
        if (formationType == FormationType.SoldierFirst)
            _members = _members.OrderBy(t => t.Unit.UnitSizeInWorld).ToList();
        else
            _members = _members.OrderByDescending(t => t.Unit.UnitSizeInWorld).ToList();

        // float averageRadius = 0f;
        
        // 记录有多少种agent半径，有多少种就排多少个圆阵
        // i是单位总数索引，j是半径种类索引
        for (int i = 0, j = 0; i < _members.Count && j < _members.Count; i++)
        {
            int unitSizeInWorld = _members[i].Unit.UnitSizeInWorld;
            
            if (i == 0)
            {
                radiusList.Add(unitSizeInWorld);
                radiusCountList.Add(1); // 在第0种半径有一个
                // averageRadius += unitSizeInWorld;
            }
            else
            {
                if (radiusList[j] == unitSizeInWorld)
                {
                    radiusCountList[j]++;
                }
                else
                {
                    radiusList.Add(unitSizeInWorld);
                    radiusCountList.Add(1);
                    // averageRadius += unitSizeInWorld;
                    j++;
                }
            }
        }

        // averageRadius /= radiusList.Count;

        switch (formationType)
        {
            // The heavier units such as tank will be firstly assigned in front of platoon.
            case FormationType.VehicleFirst:
            // Soldiers will be firstly assigned in front of platoon.
            case FormationType.SoldierFirst:
                float rowGapSum = 0f;
                int index = 0;
                
                for (int row = 0; row < radiusCountList.Count; row++)
                {
                    // The column gap between units at the same row.
                    float colGap = radiusList[row] * _PLATOON_COLUNM_GAP_SCALE;
                    float rowWidth = colGap * (radiusCountList[row] - 1);

                    Vector3 rowCenter = new Vector3(
                        centerOfFirstLine.x + rowGapSum * Mathf.Sin(offsetDegree * Mathf.Deg2Rad),
                        0f, centerOfFirstLine.z - rowGapSum * Mathf.Cos(offsetDegree * Mathf.Deg2Rad));

                    Vector3 rowStartPoint = new Vector3(
                        rowCenter.x - (rowWidth / 2) * Mathf.Cos(offsetDegree * Mathf.Deg2Rad),
                        0f,
                        rowCenter.z - (rowWidth / 2) * Mathf.Sin(offsetDegree * Mathf.Deg2Rad));

                    for (int col = 0; col < radiusCountList[row]; col++)
                    {
                        Vector3 tempDest = rowStartPoint +
                                           new Vector3(Mathf.Cos(offsetDegree * Mathf.Deg2Rad) * colGap * col, 0f,
                                               Mathf.Sin(offsetDegree * Mathf.Deg2Rad) * colGap * col);
                        // resultDestinations.Add(tempDest);

                        Command precalculatedCommand = new Command
                        {
                            CommandType = CommandType.Move,
                            Destination = tempDest,
                            TargetAlignment = alignment,
                            IsForcedAlign = true
                        };
                        
                        _members[index].PrecalculatedCommand = precalculatedCommand;
                        index++;
                    }

                    if (row + 1 < radiusList.Count)
                    {
                        rowGapSum += Mathf.Max(radiusList[row], radiusList[row + 1]) * _PLATOON_ROW_GAP_SCALE;
                    }
                }

                break;
            default:
                break;

                // Formate platoon as a circle.
                // case FormationType.Circle:
                //
                //     float basicOffset = 4f;
                //     float circleRadiusOffset = 8f;
                //
                //     // 生成多个圆圈，确定每个单位位置
                //     for (int i = 0; i < radiusCountList.Count; i++)
                //     {
                //         // j是圈内单位索引，i是圈树索引
                //         for (int j = 0; j < radiusCountList[i]; j++)
                //         {
                //             float angle = j * (360f / radiusCountList[i]) + offsetDegree;
                //             Vector3 tempDestination = new Vector3(
                //                 targetPoint.x + Mathf.Cos(angle * Mathf.Deg2Rad) *
                //                 (radiusList[i] + i * circleRadiusOffset + basicOffset),
                //                 0f,
                //                 targetPoint.z + Mathf.Sin(angle * Mathf.Deg2Rad) *
                //                 (radiusList[i] + i * circleRadiusOffset + basicOffset));
                //
                //             resultDestinations.Add(tempDestination);
                //         }
                //     }

                // break;
        }

        // return resultDestinations.ToArray();
    }

    /// <summary>
    /// 直接下达移动指令，计算各单位位置。
    /// </summary>
    public void CreateMoveCommand(Command command)
    {
        UpdatePlatoonCenter();
        UpdateMemberOffsetInfo();
        UpdateDestinationCenter(command.Destination);

        _members = _members.OrderBy(x => x.Unit.UnitSizeInWorld).ToList();

        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                BlockMapSO.BlockFlag movementMask = Path.GetMovementFlags(_members[i].Unit.Damageable.UnitType);
                int unitSizeInWorld = _members[i].Unit.UnitSizeInWorld;
                unitSizeInWorld = Mathf.Min(5, unitSizeInWorld + 1);

                Vector3 destination = _members[i].PrecalculatedCommand.Destination;
                destination = FindNearestValidFormationPoint(command.Destination, destination, unitSizeInWorld,
                    movementMask);

                _blockMap.MarkBlockMap(new Vector2(destination.x, destination.z), unitSizeInWorld,
                    BlockMapSO.BlockFlag.Dynamic);

                _members[i].PrecalculatedCommand.Destination = destination;
            }
        }

        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                Vector3 dest = _members[i].PrecalculatedCommand.Destination;
                int unitSizeInWorld = _members[i].Unit.UnitSizeInWorld;
                unitSizeInWorld = Mathf.Min(5, unitSizeInWorld + 1);

                _blockMap.UnmarkBlockMap(new Vector2(dest.x, dest.z), unitSizeInWorld, BlockMapSO.BlockFlag.Dynamic);
            }
        }
    }

    public void ShowMoveCommandGhost(Vector3 center, Vector3 alignment)
    {
        SetupPlatoonMembers();
        GetFormationPositions(center, alignment);
        
        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                BlockMapSO.BlockFlag movementMask = Path.GetMovementFlags(_members[i].Unit.Damageable.UnitType);
                int unitSizeInWorld = _members[i].Unit.UnitSizeInWorld;
                unitSizeInWorld = Mathf.Min(5, unitSizeInWorld + 1);

                Vector3 destination = _members[i].PrecalculatedCommand.Destination;
                destination = FindNearestValidFormationPoint(destination, unitSizeInWorld, movementMask);

                _blockMap.MarkBlockMap(new Vector2(destination.x, destination.z), unitSizeInWorld,
                    BlockMapSO.BlockFlag.Dynamic);

                _members[i].PrecalculatedCommand.Destination = destination;
            }
        }

        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                Vector3 dest = _members[i].PrecalculatedCommand.Destination;
                int unitSizeInWorld = _members[i].Unit.UnitSizeInWorld;
                unitSizeInWorld = Mathf.Min(5, unitSizeInWorld + 1);

                _blockMap.UnmarkBlockMap(new Vector2(dest.x, dest.z), unitSizeInWorld, BlockMapSO.BlockFlag.Dynamic);
            }
        }
        
        CreatePlatoonGhost();
    }

    public void HideGhost()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                _members[i].Unit.HideModelGhostForPlatoon();
            }
        }
    }
    
    private Vector3 FindNearestValidFormationPoint(Vector3 from, Vector3 to, int unitSizeInWorld,
        BlockMapSO.BlockFlag movementMask)
    {
        Path path = new Path(_blockMap);
        Vector3 targetPoint = path.ProjectToNearestValidPoint(from, to, unitSizeInWorld, movementMask);
        targetPoint = path.FindNearestValidPoint(targetPoint, unitSizeInWorld, movementMask);

        return targetPoint;
    }

    private Vector3 FindNearestValidFormationPoint(Vector3 position, int unitSizeInWorld,
        BlockMapSO.BlockFlag movementMask)
    {
        Path path = new Path(_blockMap);
        return path.FindNearestValidPoint(position, unitSizeInWorld, movementMask);
    }

    private void UpdateDestinationCenter(Vector3 destination)
    {
        float suitRadius = Mathf.Max(10f, _sumOfUnitSize * 0.3f);
        float suitRadiusSqr = Utils.Sqr(suitRadius);
        int suitMemberCount = 0;
        _radiusSqr = 0f;
        Vector3 destCenter = Vector3.zero;

        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i].DistSqr < suitRadiusSqr)
            {
                if (_members[i].DistSqr > _radiusSqr)
                {
                    // 得到在合理范围内的最大逐单位偏移半径。
                    _radiusSqr = _members[i].DistSqr;
                }

                destCenter += _members[i].Unit.transform.position;
                suitMemberCount++;
            }
        }

        if (suitMemberCount > 0)
        {
            _platoonCenter = destCenter / suitMemberCount;
        }

        foreach (PlatoonMember member in _members)
        {
            member.Offset = member.Position - _platoonCenter;
            member.DistSqr = member.Offset.sqrMagnitude;
            if (member.DistSqr > suitRadiusSqr)
            {
                member.Offset = member.Offset.normalized * suitRadius;
                member.DistSqr = member.Offset.sqrMagnitude;
            }

            Command precalculatedCommand = new Command
                {CommandType = CommandType.Move, Destination = destination + member.Offset};

            member.PrecalculatedCommand = precalculatedCommand;
        }
    }

    private void UpdatePlatoonCenter()
    {
        int validCount = 0;
        _platoonCenter = Vector3.zero;
        for (int i = _members.Count - 1; i >= 0; i--)
        {
            if (!CheckMemberValid(_members[i]))
            {
                _members.RemoveAt(i);
            }
            else
            {
                _members[i].Position = _members[i].Unit.transform.position;
                _platoonCenter += _members[i].Position;
                _sumOfUnitSize += _members[i].Unit.UnitSizeInWorld;
                validCount++;
            }
        }

        if (validCount > 0)
        {
            _platoonCenter /= validCount;
        }
    }

    private void UpdateMemberOffsetInfo()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            _members[i].Offset = _members[i].Position - _platoonCenter;
            _members[i].DistSqr = _members[i].Offset.sqrMagnitude;
        }
    }

    private bool CheckMemberValid(PlatoonMember member)
    {
        return member != null && !member.Unit.Damageable.IsDead;
    }

    private void CreatePlatoonGhost()
    {
        for (int i = 0; i < _members.Count; i++)
        {
            if (CheckMemberValid(_members[i]))
            {
                Command precalculatedCommand = _members[i].PrecalculatedCommand;
                Controllable unit = _members[i].Unit;
                
                if (precalculatedCommand.TargetAlignment.sqrMagnitude < 0.1f)
                {
                    precalculatedCommand.TargetAlignment = unit.transform.forward;
                }
                unit.ShowModelGhostForPlatoon(precalculatedCommand.Destination, precalculatedCommand.TargetAlignment);
            }
        }
    }

    public class PlatoonMember
    {
        public Controllable Unit { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Offset { get; set; }
        public float DistSqr { get; set; }
        public Command PrecalculatedCommand { get; set; }

        public PlatoonMember(Controllable unit)
        {
            Unit = unit;
        }
    }
}