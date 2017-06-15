using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Gameplay;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        public void CallAllyToWar(Empire ally, Empire enemy)
        {
            var offer = new Offer()
            {
                AcceptDL = "HelpUS_War_Yes",
                RejectDL = "HelpUS_War_No"
            };
            const string dialogue = "HelpUS_War";
            var ourOffer = new Offer()
            {
                ValueToModify = new Ref<bool>(() => ally.GetRelations(enemy).AtWar, x =>
                {
                    if (x)
                    {
                        ally.GetGSAI().DeclareWarOnViaCall(enemy, WarType.ImperialistWar);
                        return;
                    }
                    float amount = 30f;
                    if (OwnerEmpire.data.DiplomaticPersonality != null &&
                        OwnerEmpire.data.DiplomaticPersonality.Name == "Honorable")
                    {
                        amount                                            = 60f;
                        offer.RejectDL                                    = "HelpUS_War_No_BreakAlliance";
                        OwnerEmpire.GetRelations(ally).Treaty_Alliance    = false;
                        ally.GetRelations(OwnerEmpire).Treaty_Alliance    = false;
                        OwnerEmpire.GetRelations(ally).Treaty_OpenBorders = false;
                        OwnerEmpire.GetRelations(ally).Treaty_NAPact      = false;
                    }
                    Relationship item                                = OwnerEmpire.GetRelations(ally);
                    item.Trust                                       = item.Trust - amount;
                    Relationship angerDiplomaticConflict             = OwnerEmpire.GetRelations(ally);
                    angerDiplomaticConflict.Anger_DiplomaticConflict =
                        angerDiplomaticConflict.Anger_DiplomaticConflict + amount;
                })
            };
            if (ally == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                    Empire.Universe.PlayerEmpire, dialogue, ourOffer, offer, enemy));
            }
        }

        public void DeclareWarFromEvent(Empire them, WarType wt)
        {
            OwnerEmpire.GetRelations(them).AtWar     = true;
            OwnerEmpire.GetRelations(them).Posture   = Posture.Hostile;
            OwnerEmpire.GetRelations(them).ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (OwnerEmpire.GetRelations(them).Trust > 0f)
            {
                OwnerEmpire.GetRelations(them).Trust = 0f;
            }
            OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            OwnerEmpire.GetRelations(them).Treaty_NAPact      = false;
            OwnerEmpire.GetRelations(them).Treaty_Trade       = false;
            OwnerEmpire.GetRelations(them).Treaty_Alliance    = false;
            OwnerEmpire.GetRelations(them).Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void DeclareWarOn(Empire them, WarType wt)
        {
            Relationship ourRelations = OwnerEmpire.GetRelations(them);
            Relationship theirRelations = them.GetRelations(OwnerEmpire );
            ourRelations.PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || (them.data.Defeated || them.isFaction))
                return;
            ourRelations.FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && ourRelations.Treaty_NAPact)
            {
                ourRelations.Treaty_NAPact = false;
                foreach (var kv in OwnerEmpire.AllRelations)
                {
                    if (kv.Key != them)
                    {
                        kv.Key.GetRelations(OwnerEmpire).Trust                    -= 50f;
                        kv.Key.GetRelations(OwnerEmpire).Anger_DiplomaticConflict += 20f;
                        kv.Key.GetRelations(OwnerEmpire).UpdateRelationship(kv.Key, OwnerEmpire);
                    }
                }
                theirRelations.Trust                    -= 50f;
                theirRelations.Anger_DiplomaticConflict += 50f;
                theirRelations.UpdateRelationship(them, OwnerEmpire);
            }
            if (them == Empire.Universe.PlayerEmpire && !ourRelations.AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                        if (ourRelations.contestedSystemGuid != Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC TarSys", ourRelations.GetContestedSystem()));
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC"));
                            break;
                        }
                    case WarType.ImperialistWar:
                        if (ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism Break NA"));
                            using (var enumerator = OwnerEmpire.AllRelations.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    var kv = enumerator.Current;
                                    if (kv.Key != them)
                                    {
                                        kv.Value.Trust                    -= 50f;
                                        kv.Value.Anger_DiplomaticConflict += 20f;
                                    }
                                }
                                break;
                            }
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                    case WarType.DefensiveWar:
                        if (!ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense"));
                            ourRelations.Anger_DiplomaticConflict += 25f;
                            ourRelations.Trust                    -= 25f;
                            break;
                        }
                        else if (ourRelations.Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense BrokenNA"));
                            ourRelations.Treaty_NAPact = false;
                            foreach (var kv in OwnerEmpire.AllRelations)
                            {
                                if (kv.Key != them)
                                {
                                    kv.Value.Trust                    -= 50f;
                                    kv.Value.Anger_DiplomaticConflict += 20f;
                                }
                            }
                            ourRelations.Trust                    -= 50f;
                            ourRelations.Anger_DiplomaticConflict += 50f;
                            break;
                        }
                        else
                            break;
                    case WarType.GenocidalWar:
                        break;
                    case WarType.SkirmishWar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(wt), wt, null);
                }
            }
            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            ourRelations.AtWar             = true;
            ourRelations.Posture           = Posture.Hostile;
            ourRelations.ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate) {WarType = wt};
            if (ourRelations.Trust > 0f)
                ourRelations.Trust          = 0.0f;
            ourRelations.Treaty_OpenBorders = false;
            ourRelations.Treaty_NAPact      = false;
            ourRelations.Treaty_Trade       = false;
            ourRelations.Treaty_Alliance    = false;
            ourRelations.Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void DeclareWarOnViaCall(Empire them, WarType wt)
        {
            OwnerEmpire.GetRelations(them).PreparingForWar = false;
            if (OwnerEmpire.isFaction || OwnerEmpire.data.Defeated || them.data.Defeated || them.isFaction)
            {
                return;
            }
            OwnerEmpire.GetRelations(them).FedQuest = null;
            if (OwnerEmpire == Empire.Universe.PlayerEmpire && OwnerEmpire.GetRelations(them).Treaty_NAPact)
            {
                OwnerEmpire.GetRelations(them).Treaty_NAPact     = false;
                Relationship item                                = them.GetRelations(OwnerEmpire);
                item.Trust                                       = item.Trust - 50f;
                Relationship angerDiplomaticConflict             = them.GetRelations(OwnerEmpire);
                angerDiplomaticConflict.Anger_DiplomaticConflict =
                    angerDiplomaticConflict.Anger_DiplomaticConflict + 50f;
                them.GetRelations(OwnerEmpire).UpdateRelationship(them, OwnerEmpire);
            }
            if (them == Empire.Universe.PlayerEmpire && !OwnerEmpire.GetRelations(them).AtWar)
            {
                switch (wt)
                {
                    case WarType.BorderConflict:
                    {
                        if (OwnerEmpire.GetRelations(them).contestedSystemGuid == Guid.Empty)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War BC"));
                            break;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Declare War BC Tarsys", OwnerEmpire.GetRelations(them).GetContestedSystem()));
                        break;
                    }
                    case WarType.ImperialistWar:
                    {
                        if (!OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Imperialism"));
                            break;
                        }
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                            "Declare War Imperialism Break NA"));
                        break;
                    }
                    case WarType.DefensiveWar:
                    {
                        if (OwnerEmpire.GetRelations(them).Treaty_NAPact)
                        {
                            if (!OwnerEmpire.GetRelations(them).Treaty_NAPact)
                            {
                                break;
                            }
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense BrokenNA"));
                            OwnerEmpire.GetRelations(them).Treaty_NAPact = false;
                            Relationship trust                           = OwnerEmpire.GetRelations(them);
                            trust.Trust                                  = trust.Trust - 50f;
                            Relationship relationship                    = OwnerEmpire.GetRelations(them);
                            relationship.Anger_DiplomaticConflict        = relationship.Anger_DiplomaticConflict + 50f;
                            break;
                        }
                        else
                        {
                            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire, them,
                                "Declare War Defense"));
                            Relationship item1             = OwnerEmpire.GetRelations(them);
                            item1.Anger_DiplomaticConflict = item1.Anger_DiplomaticConflict + 25f;
                            Relationship trust1            = OwnerEmpire.GetRelations(them);
                            trust1.Trust                   = trust1.Trust - 25f;
                            break;
                        }
                    }
                    case WarType.GenocidalWar:
                        break;
                    case WarType.SkirmishWar:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(wt), wt, null);
                }
            }
            if (them == Empire.Universe.PlayerEmpire || OwnerEmpire == Empire.Universe.PlayerEmpire)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            else if (Empire.Universe.PlayerEmpire.GetRelations(them).Known &&
                     Empire.Universe.PlayerEmpire.GetRelations(OwnerEmpire).Known)
            {
                Empire.Universe.NotificationManager.AddWarDeclaredNotification(OwnerEmpire, them);
            }
            OwnerEmpire.GetRelations(them).AtWar     = true;
            OwnerEmpire.GetRelations(them).Posture   = Posture.Hostile;
            OwnerEmpire.GetRelations(them).ActiveWar = new War(OwnerEmpire, them, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (OwnerEmpire.GetRelations(them).Trust > 0f)
            {
                OwnerEmpire.GetRelations(them).Trust = 0f;
            }
            OwnerEmpire.GetRelations(them).Treaty_OpenBorders = false;
            OwnerEmpire.GetRelations(them).Treaty_NAPact      = false;
            OwnerEmpire.GetRelations(them).Treaty_Trade       = false;
            OwnerEmpire.GetRelations(them).Treaty_Alliance    = false;
            OwnerEmpire.GetRelations(them).Treaty_Peace       = false;
            them.GetGSAI().GetWarDeclaredOnUs(OwnerEmpire, wt);
        }

        public void EndWarFromEvent(Empire them)
        {
            OwnerEmpire.GetRelations(them).AtWar = false;
            them.GetRelations(OwnerEmpire).AtWar = false;
            //lock (GlobalStats.TaskLocker)
            {
                TaskList.ForEach(task => //foreach (MilitaryTask task in TaskList)
                {
                    if (OwnerEmpire.GetFleetsDict().ContainsKey(task.WhichFleet) &&
                        OwnerEmpire.data.Traits.Name == "Corsairs")
                    {
                        bool foundhome = false;
                        foreach (Ship ship in OwnerEmpire.GetShips())
                        {
                            if (ship.shipData.Role != ShipData.RoleName.station &&
                                ship.shipData.Role != ShipData.RoleName.platform)
                            {
                                continue;
                            }
                            foundhome = true;
                            foreach (Ship fship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                fship.AI.OrderQueue.Clear();
                                fship.DoEscort(ship);
                            }
                            break;
                        }
                        if (!foundhome)
                        {
                            foreach (Ship ship in OwnerEmpire.GetFleetsDict()[task.WhichFleet].Ships)
                            {
                                ship.AI.OrderQueue.Clear();
                                ship.AI.State = AIState.AwaitingOrders;
                            }
                        }
                    }
                    task.EndTaskWithMove();
                }, false, false);
            }
        }

        // ReSharper disable once UnusedMember.Local 
        // Lets think about using this
        private void FightBrutalWar(KeyValuePair<Empire, Relationship> r)
        {
            var invasionTargets = new Array<Planet>();
            foreach (Planet p in OwnerEmpire.GetPlanets())
            {
                foreach (Planet toCheck in p.system.PlanetList)
                {
                    if (toCheck.Owner == null || toCheck.Owner == OwnerEmpire || !toCheck.Owner.isFaction &&
                        !OwnerEmpire.GetRelations(toCheck.Owner).AtWar)
                    {
                        continue;
                    }
                    invasionTargets.Add(toCheck);
                }
            }
            if (invasionTargets.Count > 0)
            {
                Planet target = invasionTargets[0];
                bool ok = true;

                using (TaskList.AcquireReadLock())
                {
                    foreach (Tasks.MilitaryTask task in TaskList)
                    {
                        if (task.GetTargetPlanet() != target)
                        {
                            continue;
                        }
                        ok = false;
                        break;
                    }
                }
                if (ok)
                {
                    var invadeTask = new Tasks.MilitaryTask(target, OwnerEmpire);
                    {
                        TaskList.Add(invadeTask);
                    }
                }
            }
            var planetsWeAreInvading = new Array<Planet>();
            {
                TaskList.ForEach(task =>
                {
                    if (task.type != Tasks.MilitaryTask.TaskType.AssaultPlanet || task.GetTargetPlanet().Owner == null ||
                        task.GetTargetPlanet().Owner != r.Key)
                    {
                        return;
                    }
                    planetsWeAreInvading.Add(task.GetTargetPlanet());
                }, false, false);
            }
            if (planetsWeAreInvading.Count < 3 && OwnerEmpire.GetPlanets().Count > 0)
            {
                Vector2 vector2 = FindAveragePosition(OwnerEmpire);
                FindAveragePosition(r.Key);
                IOrderedEnumerable<Planet> sortedList =
                    from planet in r.Key.GetPlanets()
                    orderby Vector2.Distance(vector2, planet.Center)
                    select planet;
                foreach (Planet p in sortedList)
                {
                    if (planetsWeAreInvading.Contains(p))
                    {
                        continue;
                    }
                    if (planetsWeAreInvading.Count >= 3)
                    {
                        break;
                    }
                    planetsWeAreInvading.Add(p);
                    var invade = new Tasks.MilitaryTask(p, OwnerEmpire);
                    {
                        TaskList.Add(invade);
                    }
                }
            }
        }

        private void FightDefaultWar(KeyValuePair<Empire, Relationship> r)
        {
            float warWeight = 1 + OwnerEmpire.getResStrat().ExpansionPriority +
                              OwnerEmpire.getResStrat().MilitaryPriority;
            foreach (Tasks.MilitaryTask militaryTask in TaskList)
            {
                if (militaryTask.type == Tasks.MilitaryTask.TaskType.AssaultPlanet)
                {
                    warWeight--;
                }
                if (warWeight < 0)
                    return;
            }
            Array<SolarSystem> s;
            SystemCommander scom;
            switch (r.Value.ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    var list1 = new Array<Planet>();
                    IOrderedEnumerable<Planet> orderedEnumerable1 = r.Key.GetPlanets()
                        .OrderBy(planet => GetDistanceFromOurAO(planet) / 150000 +
                                  (r.Key.GetGSAI()
                                      .DefensiveCoordinator.DefenseDict
                                      .TryGetValue(planet.ParentSystem, out scom)
                                      ? scom.RankImportance
                                      : 0));
                    s = new Array<SolarSystem>();

                    for (int x = 0; x < orderedEnumerable1.Count(); ++x)
                    {
                        Planet p = orderedEnumerable1.ElementAt(x);
                        if (s.Count > warWeight)
                            break;

                        if (!s.Contains(p.ParentSystem))
                        {
                            s.Add(p.ParentSystem);
                        }
                        list1.Add(p);
                    }
                    foreach (Planet planet in list1)
                    {
                        bool canAddTask = true;

                        using (TaskList.AcquireReadLock())
                        {
                            foreach (Tasks.MilitaryTask task in TaskList)
                            {
                                if (task.GetTargetPlanet() == planet &&
                                    task.type == Tasks.MilitaryTask.TaskType.AssaultPlanet)
                                {
                                    canAddTask = false;
                                    break;
                                }
                            }
                        }
                        if (canAddTask)
                        {
                            TaskList.Add(new Tasks.MilitaryTask(planet, OwnerEmpire));
                        }
                    }
                    break;
                case WarType.ImperialistWar:
                    var planets = new Array<Planet>();                    
                    IOrderedEnumerable<Planet> importantPlanets = r.Key.GetPlanets().OrderBy(planet => GetDistanceFromOurAO(planet) / 150000 +
                                  (r.Key.GetGSAI()
                                      .DefensiveCoordinator.DefenseDict
                                      .TryGetValue(planet.ParentSystem, out scom)
                                      ? scom.RankImportance
                                      : 0));
                    s = new Array<SolarSystem>();
                    for (int index = 0;
                        index < importantPlanets.Count();
                        ++index)
                    {
                        Planet p = importantPlanets.ElementAt(index);
                        if (s.Count > warWeight)
                            break;

                        if (!s.Contains(p.ParentSystem))
                        {
                            s.Add(p.ParentSystem);
                        }
                        planets.Add(p);
                    }
                    foreach (Planet planet in planets)
                    {
                        bool flag = true;
                        bool claim = false;
                        bool claimPressent = false;
                        if (!s.Contains(planet.ParentSystem))
                            continue;
                        using (TaskList.AcquireReadLock())
                        {
                            foreach (Tasks.MilitaryTask militaryTask in TaskList)
                            {
                                if (militaryTask.GetTargetPlanet() == planet)
                                {
                                    if (militaryTask.type == Tasks.MilitaryTask.TaskType.AssaultPlanet)
                                        flag = false;
                                    if (militaryTask.type == Tasks.MilitaryTask.TaskType.DefendClaim)
                                    {
                                        claim = true;
                                        if (militaryTask.Step == 2)
                                            claimPressent = true;
                                    }
                                }
                            }
                        }
                        if (flag && claimPressent)
                        {
                            TaskList.Add(new Tasks.MilitaryTask(planet, OwnerEmpire));
                        }
                        if (claim) continue;
                        var task = new Tasks.MilitaryTask()
                        {
                            AO = planet.Center
                        };
                        task.SetEmpire(OwnerEmpire);
                        task.AORadius = 75000f;
                        task.SetTargetPlanet(planet);
                        task.TargetPlanetGuid = planet.guid;
                        task.type             = Tasks.MilitaryTask.TaskType.DefendClaim;
                        TaskList.Add(task);
                    }
                    break;
                case WarType.GenocidalWar:
                    break;
                case WarType.DefensiveWar:
                    break;
                case WarType.SkirmishWar:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void GetWarDeclaredOnUs(Empire warDeclarant, WarType wt)
        {
            Relationship relations = OwnerEmpire.GetRelations(warDeclarant);
            relations.AtWar        = true;
            relations.FedQuest     = null;
            relations.Posture      = Posture.Hostile;
            relations.ActiveWar    = new War(OwnerEmpire, warDeclarant, Empire.Universe.StarDate)
            {
                WarType = wt
            };
            if (Empire.Universe.PlayerEmpire != OwnerEmpire)
            {
                if (OwnerEmpire.data.DiplomaticPersonality.Name == "Pacifist")
                {
                    relations.ActiveWar.WarType = relations.ActiveWar.StartingNumContestedSystems <= 0
                        ? WarType.DefensiveWar
                        : WarType.BorderConflict;
                }
            }
            if (relations.Trust > 0f)
                relations.Trust          = 0f;
            relations.Treaty_Alliance    = false;
            relations.Treaty_NAPact      = false;
            relations.Treaty_OpenBorders = false;
            relations.Treaty_Trade       = false;
            relations.Treaty_Peace       = false;
        }

        public void OfferPeace(KeyValuePair<Empire, Relationship> relationship, string whichPeace)
        {
            var offerPeace = new Offer()
            {
                PeaceTreaty = true,
                AcceptDL = "OFFERPEACE_ACCEPTED",
                RejectDL = "OFFERPEACE_REJECTED"
            };
            Relationship value = relationship.Value;
            offerPeace.ValueToModify = new Ref<bool>(() => false, x => value.SetImperialistWar());
            string dialogue = whichPeace;
            if (relationship.Key != Empire.Universe.PlayerEmpire)
            {
                var ourOffer = new Offer {PeaceTreaty = true};
                relationship.Key.GetGSAI().AnalyzeOffer(ourOffer, offerPeace, OwnerEmpire, Offer.Attitude.Respectful);
                return;
            }
            Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, OwnerEmpire,
                Empire.Universe.PlayerEmpire, dialogue, new Offer(), offerPeace));
        }

        private void RunWarPlanner()
        {
            float warWeight = 1 + OwnerEmpire.getResStrat().ExpansionPriority +
                              OwnerEmpire.getResStrat().MilitaryPriority;

            foreach (var kv in OwnerEmpire.AllRelations.OrderByDescending(anger =>
                {
                    float angerMod = Vector2.Distance(anger.Key.GetWeightedCenter(), OwnerEmpire.GetWeightedCenter());
                    angerMod = (Empire.Universe.UniverseSize - angerMod) / UniverseData.UniverseWidth;
                    if (anger.Value.AtWar)
                        angerMod *= 100;
                    return anger.Value.TotalAnger * angerMod;
                }
            ))
            {
                if (!(warWeight > 0)) continue;
                if (kv.Key.isFaction)
                {
                    kv.Value.AtWar = false;
                    continue;
                }
                warWeight--;
                SystemCommander scom;
                if (kv.Value.PreparingForWar)
                {
                    Array<SolarSystem> s;
                    switch (kv.Value.PreparingForWarType)
                    {
                        case WarType.BorderConflict:
                            Array<Planet> list1 = new Array<Planet>();
                            s = new Array<SolarSystem>();

                            IOrderedEnumerable<Planet> orderedEnumerable1 = kv.Key.GetPlanets()
                                .OrderBy(planet => GetDistanceFromOurAO(planet) / 150000 +
                                    (kv.Key.GetGSAI()
                                        .DefensiveCoordinator.DefenseDict
                                        .TryGetValue(planet.ParentSystem, out scom)
                                        ? scom.RankImportance
                                        : 0));
                            for (int index = 0;
                                index < orderedEnumerable1.Count();
                                ++index)
                            {
                                Planet p =
                                    orderedEnumerable1.ElementAt(index);
                                if (s.Count > warWeight)
                                    break;

                                if (!s.Contains(p.ParentSystem))
                                {
                                    s.Add(p.ParentSystem);
                                }

                                list1.Add(p);
                            }
                            foreach (Planet planet in list1)
                            {
                                bool assault      = true;
                                bool claim        = false;
                                bool claimPresent = false;
                                {
                                    TaskList.ForEach(task =>
                                    {
                                        if (task.GetTargetPlanet() == planet &&
                                            task.type == Tasks.MilitaryTask.TaskType.AssaultPlanet)
                                        {
                                            assault = false;
                                        }
                                        if (task.GetTargetPlanet() == planet &&
                                            task.type == Tasks.MilitaryTask.TaskType.DefendClaim)
                                        {
                                            if (task.Step == 2)
                                                claimPresent = true;
                                            claim = true;
                                        }
                                    }, false, false);
                                }
                                if (assault && claimPresent)
                                {
                                    TaskList.Add(new Tasks.MilitaryTask(planet, OwnerEmpire));
                                }
                                if (!claim)
                                {
                                    var task = new Tasks.MilitaryTask()
                                    {
                                        AO = planet.Center
                                    };
                                    task.SetEmpire(OwnerEmpire);
                                    task.AORadius = 75000f;
                                    task.SetTargetPlanet(planet);
                                    task.TargetPlanetGuid = planet.guid;
                                    task.type             = Tasks.MilitaryTask.TaskType.DefendClaim;
                                    TaskList.Add(task);
                                }
                            }
                            break;
                        case WarType.ImperialistWar:
                            Array<Planet> list2 = new Array<Planet>();
                            s = new Array<SolarSystem>();
                            IOrderedEnumerable<Planet> orderedEnumerable2 = kv.Key.GetPlanets()
                                .OrderBy(
                                    (planet => GetDistanceFromOurAO(planet) / 150000 +
                                               (kv.Key.GetGSAI()
                                                   .DefensiveCoordinator.DefenseDict
                                                   .TryGetValue(planet.ParentSystem, out scom)
                                                   ? scom.RankImportance
                                                   : 0)));
                            for (int index = 0; index < orderedEnumerable2.Count(); ++index)
                            {
                                Planet p = orderedEnumerable2.ElementAt(index);
                                if (s.Count > warWeight)
                                    break;

                                if (!s.Contains(p.ParentSystem))
                                {
                                    s.Add(p.ParentSystem);
                                }
                                list2.Add(p);
                            }
                            foreach (Planet planet in list2)
                            {
                                bool flag         = true;
                                bool claim        = false;
                                bool claimPresent = false;
                                {
                                    TaskList.ForEach(task =>
                                    {
                                        if (!flag && claim)
                                            return;
                                        if (task.GetTargetPlanet() == planet &&
                                            task.type == Tasks.MilitaryTask.TaskType.AssaultPlanet)
                                        {
                                            flag = false;
                                        }
                                        if (task.GetTargetPlanet() != planet ||
                                            task.type != Tasks.MilitaryTask.TaskType.DefendClaim) return;
                                        if (task.Step == 2)
                                            claimPresent = true;

                                        claim = true;
                                    }, false, false);
                                }
                                if (flag && claimPresent)
                                {
                                    TaskList.Add(new Tasks.MilitaryTask(planet, OwnerEmpire));
                                }
                                if (!claim)
                                {
                                    // @todo This is repeated everywhere. Might cut down a lot of code by creating a function

                                    Tasks.MilitaryTask task = new Tasks.MilitaryTask()
                                    {
                                        AO = planet.Center
                                    };
                                    task.SetEmpire(OwnerEmpire);
                                    task.AORadius = 75000f;
                                    task.SetTargetPlanet(planet);
                                    task.TargetPlanetGuid = planet.guid;
                                    task.type             = Tasks.MilitaryTask.TaskType.DefendClaim;
                                    task.EnemyStrength    = 0;
                                    {
                                        TaskList.Add(task);
                                    }
                                }
                            }
                            break;
                        case WarType.GenocidalWar:
                            break;
                        case WarType.DefensiveWar:
                            break;
                        case WarType.SkirmishWar:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                if (kv.Value.AtWar)
                {
                    FightDefaultWar(kv);
                }
            }
        }
    }
}