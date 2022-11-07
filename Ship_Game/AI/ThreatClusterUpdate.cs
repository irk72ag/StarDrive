﻿using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;

namespace Ship_Game.AI;

// Utility for atomically updating a ThreatCluster
public sealed class ClusterUpdate
{
    public readonly ThreatCluster Cluster; // cluster to be updated
    public readonly Array<Ship> Ships = new(); // ships to be added
    public AABoundingBox2D Bounds;

    // whether this cluster was fully observed by us
    // if a cluster is fully observed and has no ships, it will be removed from threats
    public bool FullyObserved;

    // if true, this cluster will be dumped at the end of the update
    // otherwise it is kept in ThreatMatrix 'memory'
    bool ForceRemove;

    public ClusterUpdate(ThreatCluster cluster, Ship s)
    {
        Cluster = cluster;
        Bounds = new(s.Position, ThreatMatrix.InitialClusterRadius);
        Ships.Add(s);
        ApplyBoundsOnly();
    }

    public ClusterUpdate(ThreatCluster cluster, Vector2 pos, float radius)
    {
        Cluster = cluster;
        Bounds = new(pos, radius);
    }

    // Reset for the start of a new observation update
    public void ResetForObservation(bool isOwnerCluster)
    {
        Ships.Clear();
        ForceRemove = false;
        // owner's own clusters are always fully observed
        FullyObserved = isOwnerCluster;
    }

    public void AddShip(Ship s)
    {
        Bounds = Bounds.Merge(new(s.Position, s.Radius));
        Ships.Add(s);
        ApplyBoundsOnly();
    }

    // merge this cluster with `u`, and mark `u` for deletion
    public void Merge(ClusterUpdate u)
    {
        if (u.Ships.NotEmpty)
        {
            foreach (Ship s in u.Ships)
            {
                Bounds = Bounds.Merge(new(s.Position, s.Radius));
                #if DEBUG
                    if (Ships.Contains(s))
                        throw new InvalidOperationException("ThreatCluster Merge Double Insert bug");
                #endif
                Ships.Add(s);
            }
            u.Ships.Clear();
            u.ForceRemove = true;
            ApplyBoundsOnly();
        }
    }

    public void ApplyBoundsOnly()
    {
        Cluster.Position = Bounds.Center;
        // this is a bit bigger than the actual radius, but only way to ensure
        // that all ships are within the radius, without having to loop over all ships
        Cluster.Radius = Bounds.Diagonal*0.5f;
    }

    // TRUE if this cluster should be removed from Threats
    public bool ShouldBeRemoved
    {
        // fully observed but empty clusters MUST be removed
        get => ForceRemove || (FullyObserved && Ships.IsEmpty);
        set => ForceRemove = value;
    }

    /// <summary>
    /// Updates the ThreatCluster with its new state.
    /// </summary>
    /// <param name="owner">The empire which owns the ThreatMatrix</param>
    /// <param name="isOwnerCluster">if true, this cluster belongs to Owner (observation of self)</param>
    public void Update(Empire owner, bool isOwnerCluster)
    {
        if (Ships.IsEmpty)
        {
            // remove inactive ships to avoid ships remaining
            // in stale clusters and causing OOM
            Cluster.Ships = Cluster.Ships.Filter(s => s.Active);
            return;
        }

        // In the case when we have observed some ships
        // we will always take the observed amount at face value.
        // This makes everything simpler and Proper sensor ranges
        // will most edge cases automatically
        //
        // The AI difficulty settings should ensure a little bit more
        // ships are always sent.

        Ship[] ships = Ships.ToArr();

        bool inBorders = false;
        bool hasStarBases = false;
        float strength = 0f;
        SolarSystem system = null;

        for (int i = 0; i < ships.Length; ++i)
        {
            Ship s = ships[i];
            strength += s.GetStrength();
            if (s.IsStation)
                hasStarBases = true;

            if (!inBorders && s.IsInBordersOf(owner))
                inBorders = true;

            system ??= s.System;

            if (isOwnerCluster)
                s.CurrentCluster = Cluster;
        }
        
        // Unfortunately we also need to recalculate the Bounds
        // since the cluster can be a traveling fleet, in which case
        // the regular AddShip() bounds would forever expand the cluster
        Bounds = new(ships[0].Position, ThreatMatrix.InitialClusterRadius);
        for (int i = 1; i < ships.Length; ++i)
        {
            Ship s = ships[i];
            Bounds = Bounds.Merge(new(s.Position, s.Radius));
        }

        //// TODO: add a fast way to test with Radius in InfluenceTree
        //bool inBorders = owner.Universe.Influence.IsInInfluenceOf(owner, AveragePos);

        ApplyBoundsOnly();
        Cluster.Strength = strength;
        Cluster.Ships = ships;
        Cluster.HasStarBases = hasStarBases;
        Cluster.InBorders = inBorders;
        Cluster.System = system ?? (SolarSystem)owner.Universe.SystemsTree.FindOne(Cluster.Position, Cluster.Radius);
    }
}
