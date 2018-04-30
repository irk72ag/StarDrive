﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Particle3DSample;

namespace Ship_Game.Ships
{
    public class ShipModuleDamageVisualization
    {
        private readonly ParticleEmitter Lightning;
        private readonly ParticleEmitter Trail;
        private readonly ParticleEmitter Smoke;
        private readonly ParticleEmitter Flame;
        private readonly float LightningVelZ;
        private readonly float TrailVelZ;
        private readonly float SmokeVelZ;
        private readonly float Area;


        public static bool CanVisualize(ShipModule module)
        {
            int area = module.XSIZE * module.YSIZE;
            if (module.ModuleType == ShipModuleType.Armor && area <= 1)
                return false; // Small armor modules don't have damage visualization
            return true;
        }

        public ShipModuleDamageVisualization(ShipModule module)
        {
            Area = module.XSIZE * module.YSIZE;
            Vector3 center = module.GetCenter3D;
            ShipModuleType type = module.ModuleType;

            switch (type) // other special effects based on some module types.
            {
                case ShipModuleType.FuelCell:
                    Lightning = Empire.Universe.photonExplosionParticles.NewEmitter(Area * 6f, center);
                    LightningVelZ = -6f;
                    return;
                case ShipModuleType.Shield:
                    Lightning = Empire.Universe.lightning.NewEmitter(8f, center);
                    LightningVelZ = -8f;
                    break;
                case ShipModuleType.PowerPlant:
                    Lightning = Empire.Universe.lightning.NewEmitter(10f, center);
                    LightningVelZ = -10f;
                    break;
                case ShipModuleType.PowerConduit:  // power conduit get only sparks
                    Lightning = Empire.Universe.sparks.NewEmitter(25, center.ToVec2(), -10f);
                    LightningVelZ = -2f;
                    return;
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Trail = Empire.Universe.smokePlumeParticles.NewEmitter(Area, center);
            Smoke = Empire.Universe.explosionSmokeParticles.NewEmitter(Area * 3f, center);
            TrailVelZ = -0.1f;
            SmokeVelZ = -2.0f;

            // armor and small modules don't produce flames
            if (type != ShipModuleType.Armor && Area > 2)
            {
                Flame = Empire.Universe.flameParticles.NewEmitter(Area, center);
            }
        }


        public void Update(float elapsedTime, Vector3 center)
        {
            Lightning?.Update(elapsedTime, center, LightningVelZ);
            Trail?.Update(elapsedTime, center, TrailVelZ);
            Smoke?.Update(elapsedTime, center, SmokeVelZ);
            Flame?.Update(elapsedTime, center, zVelocity: Area / -RandomMath.RandomBetween(2f, 6f));
        }
    }
}
