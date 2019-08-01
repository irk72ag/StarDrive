﻿using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System.Threading;

namespace Ship_Game.Ships
{
    public partial class Ship
    {
        public class ProjectileCollection<T> : Array<T> where T : Projectile
        {
            readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

            public ScopedReadLock AcquireReadLock() => Locker.AcquireReadLock();

            public new void Add(T projectile)
            {
                Locker.EnterWriteLock();
                base.Add(projectile);
                Locker.ExitWriteLock();
            }

            public new T[] ToArray()
            {
                Locker.EnterReadLock();
                var copy = base.ToArray();
                Locker.ExitReadLock();
                return copy;
            }

            void RemoveInActive()
            {
                using (Locker.AcquireWriteLock())
                {
                    this.RemoveInActiveObjects();
                }
            }

            public void Update(float elapsedTime)
            {
                using (Locker.AcquireReadLock())
                {
                    for (int i = 0; i < Count; ++i)
                    {
                        Projectile p = this[i];
                        if (p.Active) p.Update(elapsedTime);
                    }
                }
                RemoveInActive();
            }

            public void KillActive(bool force, bool cleanup)
            {
                using (Locker.AcquireReadLock())
                {
                    for (int i = 0; i < Count; ++i)
                    {
                        Projectile p = this[i];
                        if (p.Active && (p.DieNextFrame || force))
                            p.Die(p, cleanup);
                    }
                }
                RemoveInActive();
            }
        }


        readonly ProjectileCollection<Projectile> Projectiles = new ProjectileCollection<Projectile>();
        readonly ProjectileCollection<Beam> Beams             = new ProjectileCollection<Beam>();

        public Projectile[] CopyProjectiles => Projectiles.ToArray();

        public void AddProjectile(Projectile projectile)
        {
            Projectiles.Add(projectile);
        }

        public void AddBeam(Beam beam)
        {
            Beams.Add(beam);
        }

        public void DrawProjectiles(SpriteBatch batch, GameScreen screen)
        {
            using (Projectiles.AcquireReadLock())
            {
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile p = Projectiles[i];
                    if (p.Active) p.Draw(batch, screen);
                }
            }
        }

        public void RemoveDyingProjectiles()
        {
            Projectiles.KillActive(force:false, cleanup:false);
        }

        void RemoveProjectiles()
        {
            Projectiles.KillActive(force:true, cleanup:false);
        }

        void RemoveBeams()
        {
            Beams.KillActive(force:true, cleanup:true);
        }

        public void DrawDroneBeams(UniverseScreen screen)
        {
            using (Projectiles.AcquireReadLock())
            {
                for (int i = 0; i < Projectiles.Count; ++i)
                {
                    Projectile p = Projectiles[i];
                    if (p.Active && p.Weapon?.IsRepairDrone != false)
                        p.DroneAI?.DrawBeams(screen);
                }
            }
        }

        public void DrawBeams(UniverseScreen screen)
        {
            using (Beams.AcquireReadLock())
            {
                for (int i = 0; i < Beams.Count; ++i)
                {
                    Beam beam = Beams[i];
                    if (beam.Active) beam.Draw(screen);
                }
            }
        }

    }
}
