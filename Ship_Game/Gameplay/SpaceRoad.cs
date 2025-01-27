using System.Collections.Generic;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class RoadNode
    {
        [StarData] public readonly Vector2 Position;
        [StarData] public Ship Projector { get; private set; }
        [StarData] public bool Overlapping { get; private set; }
        [StarData] byte DelayByTries;
        public bool ProjectorExists => Projector is { Active: true };
        public bool OkToBuild(Empire owner)
        {
            if (owner.KnownEnemyStrengthNoResearchStationsIn(Position, 50_000) > 0.1f)
                DelayByTries = 50;
            else if (DelayByTries > 0) // Even if no hostile forces found, take some more time before approving build
                DelayByTries--;

            return DelayByTries == 0;
        }

        [StarDataConstructor]
        public RoadNode() {}

        public RoadNode(Vector2 pos)
        {
            Position = pos;
        }
        public void SetProjector(Ship projector)
        {
            Projector = projector;
        }

        public void SetOverlappingAs(bool value)
        {
            Overlapping = value;
        }
    }

    [StarDataType]
    public sealed class SpaceRoad
    {
        [StarData] public readonly Empire Owner;
        [StarData] public Array<RoadNode> RoadNodesList = new();
        [StarData] public SolarSystem System1;
        [StarData] public SolarSystem System2;
        [StarData] public readonly string Name;
        [StarData] public readonly int NumProjectors;
        [StarData] public SpaceRoadStatus Status { get; private set; }
        [StarData] public float Heat { get; private set; }

        // The expected maintenance if the road is online or in progress
        [StarData] public float OperationalMaintenance { get; private set; }

        const float ProjectorDensity = 1.7f;
        const float SpacingOffset = 0.5f;
        const float OverlapRadius = 0.9f;

        public bool IsHot => Heat >= NumProjectors*2;
        public bool IsCold => Heat <= -(NumProjectors+2);
        public float Maintenance => Status == SpaceRoadStatus.Down ? 0 : OperationalMaintenance;
        public bool HasSystem(SolarSystem s) => System1 == s || System2== s;

        [StarDataConstructor]
        public SpaceRoad()
        {
        }

        public SpaceRoad(SolarSystem sys1, SolarSystem sys2, Empire owner, int numProjectors, 
            string name, IReadOnlyCollection<RoadNode> allEmpireActiveNodes)
        {
            System1 = sys1;
            System2 = sys2;
            NumProjectors = numProjectors;
            Status = SpaceRoadStatus.Down;
            Owner = owner;
            Name = name;

            InitNodes(allEmpireActiveNodes);
            AddHeat();
        }

        // Need to check that all nodes are needed, since roads are dynamic and there
        // is a possibility that other active roads cover some parts fo this road.
        // if there is an over lap, the note will be set as overlap and it is a signal
        // a projector is not needed at this node currently
        void InitNodes(IReadOnlyCollection<RoadNode> allEmpireActiveNodes)
        {
            float distance = System1.Position.Distance(System2.Position);
            float projectorSpacing = distance / NumProjectors;
            float baseOffset = projectorSpacing * SpacingOffset;

            for (int i = 0; i < NumProjectors; i++)
            {
                float nodeOffset = baseOffset + projectorSpacing * i;
                Vector2 roadDirection = System1.Position.DirectionToTarget(System2.Position);
                Vector2 desiredNodePos = System1.Position + roadDirection * nodeOffset;
                var node = new RoadNode(desiredNodePos);
                RoadNodesList.Add(node);
                node.SetOverlappingAs(NodePosOverlappingAnotherNode(node, allEmpireActiveNodes, out _));
            }

            UpdateMaintenance();
        }

        public void AddHeat(float extraHeat = 0)
        {
            Heat = (Heat + 1 + extraHeat + NumProjectors / 5f ).UpperBound(NumProjectors * 3);
        }

        public void CoolDown()
        {
            Heat--;
        }

        public void UpdateMaintenance()
        {
            float maint = ResourceManager.GetShipTemplate("Subspace Projector").GetMaintCost(Owner);
            OperationalMaintenance = maint * RoadNodesList.Count(r => !r.Overlapping);
        }

        public static int GetNeededNumProjectors(SolarSystem origin, SolarSystem destination, Empire owner)
        {
            float projectorRadius = owner.GetProjectorRadius() * ProjectorDensity;
            float distance = origin.Position.Distance(destination.Position);
            return (int)(distance / projectorRadius);
        }

        // This ensures a road will be the same object, regardless of the order of sys1 and sys2
        public static string GetSpaceRoadName(SolarSystem sys1, SolarSystem sys2)
        {
            return sys1.Id < sys2.Id ? $"{sys1.Id}-{sys2.Id}" : $"{sys2.Id}-{sys1.Id}";
        }

        public void DeployAllProjectors(IReadOnlyCollection<RoadNode> allEmpireActiveNodes)
        {
            CalculateNodeOverlapping(allEmpireActiveNodes);
            foreach (RoadNode node in RoadNodesList)
            {
                if (node.Overlapping)
                {
                    Log.Info($"DeployAllProjectors - {Owner.Name} - node position {node.Position} overlaps with another road" +
                             " skipping node deployment.");
                }
                else
                {
                    Log.Info($"BuildProjector - {Owner.Name} - at {node.Position}");
                    Owner.AI.AddGoal(new BuildConstructionShip(node.Position, "Subspace Projector", Owner));
                    Status = SpaceRoadStatus.InProgress;
                }
            }
        }

        public bool FillProjectorGaps()
        {
            bool requestedFill = false;
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (!node.ProjectorExists 
                    && !node.Overlapping 
                    && !Owner.AI.SpaceRoadsManager.NodeGoalAlreadyExistsFor(node.Position)
                    )
                {
                    if (node.OkToBuild(Owner))
                    {
                        Log.Info($"BuildProjector - {Owner.Name} - fill gap at {node.Position}");
                        Owner.AI.AddGoal(new BuildConstructionShip(node.Position, "Subspace Projector", Owner));
                        requestedFill = true;
                    }
                }
            }

            return requestedFill;
        }

        // Need to check that all nodes are needed, since roads are dynamic and there
        // is a possibility that other active roads cover some parts fo this road.
        // if there is an over lap, the note will be set as overlap and it is a signal
        // a projector is not needed at this node currently
        public void CalculateNodeOverlapping(IReadOnlyCollection<RoadNode> nodesList)
        {
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (NodePosOverlappingAnotherNode(node, nodesList, out _))
                {
                    node.SetOverlappingAs(true);
                    ScuttleAndRemoveProjectorRefFrom(node);
                    Log.Info($"CalculateNodeOverlapping - {Owner.Name} {System1.Name}-{System2.Name} - node not needed since it " +
                             $"overlaps with another at pos {node.Position}");
                }
                else if (node.Overlapping)
                {
                    node.SetOverlappingAs(false);
                    Log.Info($"CalculateNodeOverlapping - {Owner.Name} - filling node gap at {node.Position}");
                }
            }

            UpdateMaintenance();
            RecalculateStatus();
        }

        public void FillNodeGaps(RoadNode[] allNodes, ref RoadNode[] checkedNodes)
        {
            bool nodeGapFilled = false;
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (node.Overlapping 
                    && NodePosOverlappingAnotherNode(node, checkedNodes, out RoadNode overlappingNode) 
                    && !NodePosOverlappingAnotherNode(node, allNodes, out _))
                {
                    nodeGapFilled = true;
                    node.SetOverlappingAs(false);
                    Log.Info($"FillNodeGaps - {Owner.Name} - Road: {System1.Name}-{System2.Name} - filling node {i} gap at {node.Position}");
                    // no need to check that node again for other roads. The current road will fill it
                    checkedNodes.Remove(overlappingNode, out checkedNodes);
                }
            }
                                                                                                                                                                                                                                      
            if (nodeGapFilled)
                UpdateMaintenance();

            RecalculateStatus();
        }

        public void Scrap()
        {
            foreach (RoadNode node in RoadNodesList)
            {
                ScuttleAndRemoveProjectorRefFrom(node);
            }
        }

        public bool AddProjector(Ship projector, Vector2 buildPos)
        {
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (node.Position.InRadius(buildPos, 100))
                {
                    SetProjectorAtNode(node, projector);
                    return true;
                }
            }

            return false;
        }
        
        // Note - this does not remove the projector itself
        public bool RemoveProjectorRef(Ship projector)
        {
            for (int i = 0; i < RoadNodesList.Count; i++)
            {
                RoadNode node = RoadNodesList[i];
                if (node.Projector == projector)
                {
                    RemoveProjectorRefAtNode(node);
                    return true;
                }
            }

            return false;
        }

        void RemoveProjectorGoal(IReadOnlyList<Goal> goalsList, Vector2 nodePos)
        {
            for (int i = goalsList.Count - 1; i >= 0; i--)
            {
                Goal g = goalsList[i];
                if (g.Type == GoalType.DeepSpaceConstruction && g.BuildPosition.AlmostEqual(nodePos))
                {
                    g.PlanetBuildingAt?.Construction.Cancel(g);
                    g.FinishedShip?.AI.OrderScrapShip();
                    Owner.AI.RemoveGoal(g);
                    break;
                }
            }
        }

        bool NodePosOverlappingAnotherNode(RoadNode node, IReadOnlyCollection<RoadNode> nodesList, out RoadNode overlappingNode)
        {
            overlappingNode = null;
            float projectorRadius = Owner.GetProjectorRadius();
            foreach (RoadNode n in nodesList)
            {
                if (node != n && !n.Overlapping && node.Position.InRadius(n.Position, projectorRadius * OverlapRadius))
                {
                    overlappingNode = n;
                    return true;
                }
            }

            return false;
        }

        void RecalculateStatus()
        {
            Status = RoadNodesList.Any(n => !n.ProjectorExists && !n.Overlapping) 
                ? SpaceRoadStatus.InProgress : SpaceRoadStatus.Online;
        }

        void SetProjectorAtNode(RoadNode node, Ship projector)
        {
            node.SetProjector(projector);
            RecalculateStatus();
        }

        void RemoveProjectorRefAtNode(RoadNode node)
        {
            node.SetProjector(null);
            RecalculateStatus();
        }

        void ScuttleAndRemoveProjectorRefFrom(RoadNode node)
        {
            if (node.ProjectorExists)
                node.Projector.AI.OrderScuttleShip();
            else if (Status == SpaceRoadStatus.InProgress)
                RemoveProjectorGoal(Owner.AI.Goals, node.Position);

            RemoveProjectorRefAtNode(node);
        }


        public enum SpaceRoadStatus
        {
            Down, // Road is set up, but is too cold to be created or no budget, this status is the default
            InProgress, // Road in in progress of being created or a node is missing
            Online, // full operational with all SSPs active
        }
    }
}