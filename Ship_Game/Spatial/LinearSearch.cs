﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Shared utility for performing linear searches
    /// This is used for validation and error detection of Spatial algorithms
    /// </summary>
    public unsafe class LinearSearch
    {
        public static SpatialObjectBase[] FindNearby(in SearchOptions opt, SpatialObjectBase[] objects, int count)
        {
            int maxResults = opt.MaxResults > 0 ? opt.MaxResults : 1;
            int* objectIds = stackalloc int[maxResults];
            int resultCount = 0;

            AABoundingBox2D searchRect = opt.SearchRect;
            bool filterByLoyalty = (opt.ExcludeLoyalty != null)
                                || (opt.OnlyLoyalty != null);

            float searchFX = opt.FilterOrigin.X;
            float searchFY = opt.FilterOrigin.Y;
            float searchFR = opt.FilterRadius;
            bool useSearchRadius = searchFR > 0f;

            for (int i = 0; i < count; ++i)
            {
                SpatialObjectBase obj = objects[i];
                if (obj == null
                    || (opt.Exclude != null && obj == opt.Exclude)
                    || (opt.Type != GameObjectType.Any && obj.Type != opt.Type))
                    continue;
                
                if (filterByLoyalty)
                {
                    Empire loyalty = obj.GetLoyalty();
                    if ((opt.ExcludeLoyalty != null && loyalty == opt.ExcludeLoyalty) ||
                        (opt.OnlyLoyalty != null && loyalty != opt.OnlyLoyalty))
                        continue;
                }

                var objectRect = new AABoundingBox2D(obj);
                if (!objectRect.Overlaps(searchRect))
                    continue;

                if (useSearchRadius)
                {
                    if (!objectRect.Overlaps(searchFX, searchFY, searchFR))
                        continue; // AABB not in SearchRadius
                }

                objectIds[resultCount++] = i;
                if (resultCount == maxResults)
                    break; // we are done !
            }

            SpatialObjectBase[] results = Copy(objectIds, resultCount, objects);
            if (opt.SortByDistance)
                SortByDistance(opt, results);
            return results;
        }

        public static SpatialObjectBase[] Copy(int* objectIds, int count, SpatialObjectBase[] objects)
        {
            if (count == 0)
                return Empty<SpatialObjectBase>.Array;

            var found = new SpatialObjectBase[count];
            for (int i = 0; i < found.Length; ++i)
            {
                int spatialIndex = objectIds[i];
                SpatialObjectBase go = objects[spatialIndex];
                found[i] = go ?? throw new NullReferenceException(
                    $"LinearSearch objectIds[{i}]={spatialIndex} objects[{spatialIndex}] was null. Results count={count}");
            }
            return found;
        }

        public static void SortByDistance<T>(in SearchOptions opt, T[] objects) where T : SpatialObjectBase
        {
            objects.SortByDistance(opt.SearchRect.Center);
        }
    }
}
