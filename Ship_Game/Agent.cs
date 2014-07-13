using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Ship_Game
{
	public class Agent
	{
		public string Name;

		public int Level = 1;

        public int Experience = 0;

		public AgentMission Mission;

		public int TurnsRemaining;

		public string TargetEmpire = "";

		public Guid TargetGUID;

		public int MissionNameIndex = 2183;
        public bool spyMute = false;

		public Agent()
		{
		}
        //added by gremlin custom damage spy routine.
        public bool DamageSpy(Agent agent, Empire owner, int damage, string deathMessage, Empire target)
        {
            agent.Level = agent.Level - damage;
            if (agent.Level <= 0)
            {
                agent.Level = 0;
                if (deathMessage == "") deathMessage = string.Concat(this.Name, "was horribly wounded and is in hospital.\nDr. Gremlin thinks amputation is the only recourse");
                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, deathMessage, owner);


                return true;
            }
            List<string> messageList = new List<string>();
            messageList.Add(string.Concat(this.Name, " saw unspeakable things... \nThier mind has suffered...\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Took the blue pill...\nand the red one\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " feels their pay is too little...\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " had an epic battle\nthat is too expensive to show here\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " demands more assitance next time\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Had parts of their memory erased\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Spent a day thier buddy in the null prison\n-", damage, "level"));
            if (deathMessage == "") deathMessage = messageList[HelperFunctions.GetRandomIndex(messageList.Count)];

            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, deathMessage, owner);
            return false;
        }
		public void AssignMission(AgentMission mission, Empire Owner, string empname)
		{
			this.Initialize(mission, Owner);
			if (this.Mission == AgentMission.Undercover)
			{
				foreach (Mole m in Owner.data.MoleList)
				{
					if (m.PlanetGuid != this.TargetGUID)
					{
						continue;
					}
					Owner.data.MoleList.QueuePendingRemoval(m);
					break;
				}
			}
			Owner.data.MoleList.ApplyPendingRemovals();
			this.Mission = mission;
			this.TargetEmpire = empname;
		}

		public void DoMissionorig(Empire Owner)
		{
			Planet target;
			Empire Target = EmpireManager.GetEmpireByName(this.TargetEmpire);
			if (Target != null && Target.data.Defeated)
			{
				this.Mission = AgentMission.Defending;
				this.MissionNameIndex = 2183;
				return;
			}
			float DiceRoll = RandomMath.RandomBetween(0f, 100f);
			if (DiceRoll > 97.5f)
			{
				DiceRoll = DiceRoll + 100f;
			}
			DiceRoll = DiceRoll + Owner.data.SpyModifier;
			DiceRoll = DiceRoll + (float)this.Level * RandomMath.RandomBetween(1f, 5f);
			DiceRoll = DiceRoll + Owner.data.OffensiveSpyBonus;
			float DefensiveRoll = 0f;
			if (Target != null)
			{
				for (int i = 0; i < Target.data.AgentList.Count; i++)
				{
					if (Target.data.AgentList[i].Mission == AgentMission.Defending)
					{
						float Roll = 1f + (float)Target.data.AgentList[i].Level * RandomMath.RandomBetween(0f, 3f);
						if (Roll > DefensiveRoll)
						{
							DefensiveRoll = Roll;
						}
					}
				}
				DefensiveRoll = DefensiveRoll + Target.data.SpyModifier;
				DefensiveRoll = DefensiveRoll + Target.data.DefensiveSpyBonus;
			}
			switch (this.Mission)
			{
				case AgentMission.Training:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (DiceRoll >= 95f)
					{
						Agent level = this;
						level.Level = level.Level + 2;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully complete training\nThe Agent's brilliance results in a gain of +2 levels."), Owner);
						break;
					}
					else if (DiceRoll > 25f)
					{
						Agent agent = this;
						agent.Level = agent.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully completed training\nand has gained +1 level."), Owner);
						break;
					}
					else if (DiceRoll < 10f)
					{
						if (DiceRoll >= 10f)
						{
							break;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed in a training accident."), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " has completed training, but failed to learn anything useful."), Owner);
						break;
					}
				}
				case AgentMission.Infiltrate:
				{
					if (Target.GetPlanets().Count == 0)
					{
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						return;
					}
					if (DiceRoll >= 50f)
					{
						this.Mission = AgentMission.Undercover;
						this.MissionNameIndex = 2201;
						Agent level1 = this;
						level1.Level = level1.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Mole m = Mole.PlantMole(Owner, Target);
						this.TargetGUID = m.PlanetGuid;
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " successfully infiltrated a colony: ", Ship.universeScreen.PlanetsDict[m.PlanetGuid].Name, "\nThe Agent was not detected and gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll < 25f)
					{
						if (DiceRoll >= 25f)
						{
							break;
						}
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to infiltrate a colony"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							break;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed trying to infiltrate one of our colonies\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						break;
					}
					else
					{
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was unable to infiltrate a colony\nand had to abort the mission to avoid capture"), Owner);
						if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							break;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was foiled trying to infiltrate a colony\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						break;
					}
				}
				case AgentMission.Assassinate:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.data.AgentList.Count == 0)
					{
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " could not assassinate an enemy Agent \nbecause target empire has no Agents"), Owner);
						return;
					}
					if (DiceRoll >= 85f)
					{
						Agent agent1 = this;
						agent1.Level = agent1.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Agent m = Target.data.AgentList[HelperFunctions.GetRandomIndex(Target.data.AgentList.Count)];
						Target.data.AgentList.Remove(m);
						if (m.Mission == AgentMission.Undercover)
						{
							foreach (Mole mole in Owner.data.MoleList)
							{
								if (mole.PlanetGuid != m.TargetGUID)
								{
									continue;
								}
								Owner.data.MoleList.QueuePendingRemoval(mole);
								break;
							}
						}
						Owner.data.MoleList.ApplyPendingRemovals();
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was mysteriously assassinated: ", m.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent escaped unharmed and undetected, gaining + 1 level"), Owner);
						break;
					}
					else if (DiceRoll >= 70f)
					{
						Agent m = Target.data.AgentList[HelperFunctions.GetRandomIndex(Target.data.AgentList.Count)];
						Target.data.AgentList.Remove(m);
						if (m.Mission == AgentMission.Undercover)
						{
							foreach (Mole mole in Owner.data.MoleList)
							{
								if (mole.PlanetGuid != m.TargetGUID)
								{
									continue;
								}
								Owner.data.MoleList.QueuePendingRemoval(mole);
								break;
							}
						}
						Owner.data.MoleList.ApplyPendingRemovals();
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was assassinated: ", m.Name, "\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent was detected but escaped, gaining + 1 level"), Owner);
						break;
					}
					else if (DiceRoll < 25f)
					{
						if (DiceRoll >= 25f)
						{
							break;
						}
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed during an assassination attempt\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to assassinate an enemy agent"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We managed to detect an enemy Assassin before it could strike\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was foiled trying to assassinate an enemy agent, but managed to escape"), Owner);
						break;
					}
				}
				case AgentMission.Sabotage:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.GetPlanets().Count == 0)
					{
						return;
					}
					target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[HelperFunctions.GetRandomIndex(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
					this.TargetGUID = target.guid;
					if (DiceRoll >= 80f)
					{
						Planet crippledTurns = target;
						crippledTurns.Crippled_Turns = crippledTurns.Crippled_Turns + 5 + this.Level * 5;
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name), Target);
						}
						NotificationManager notificationManager = Ship.universeScreen.NotificationManager;
						string[] name = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
						int num = 5 + this.Level * 5;
						name[2] = num.ToString();
						name[3] = " turns: ";
						name[4] = target.Name;
						name[5] = "\nThe Agent was not detected and gains +1 level";
						notificationManager.AddAgentResultNotification(true, string.Concat(name), Owner);
						Agent level2 = this;
						level2.Level = level2.Level + 1;
						if (this.Level <= 10)
						{
							break;
						}
						this.Level = 10;
						break;
					}
					else if (DiceRoll > 50f)
					{
						Planet planet = target;
						planet.Crippled_Turns = planet.Crippled_Turns + 5 + this.Level * 3;
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						NotificationManager notificationManager1 = Ship.universeScreen.NotificationManager;
						string[] str = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
						int num1 = 5 + this.Level * 3;
						str[2] = num1.ToString();
						str[3] = " turns: ";
						str[4] = target.Name;
						str[5] = "\nThe Agent was not detected and gains +1 level";
						notificationManager1.AddAgentResultNotification(true, string.Concat(str), Owner);
						Agent agent2 = this;
						agent2.Level = agent2.Level + 1;
						if (this.Level <= 10)
						{
							break;
						}
						this.Level = 10;
						break;
					}
					else if (DiceRoll < 15f)
					{
						if (DiceRoll >= 15f)
						{
							break;
						}
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("We killed an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to sabotage an enemy colony"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to sabotage ", target.Name), Owner);
						break;
					}
				}
				case AgentMission.StealTech:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					List<string> PotentialUIDs = new List<string>();
					foreach (KeyValuePair<string, TechEntry> entry in Target.GetTDict())
					{
                        //Added by McShooterz: Racial tech cannot be stolen
                        if (!entry.Value.Unlocked || !Owner.HavePreReq(entry.Value.UID) || Owner.GetTDict()[entry.Value.UID].Unlocked || entry.Value.GetTech().RaceRestrictions.Count != 0)
						{
							continue;
						}
						PotentialUIDs.Add(entry.Value.UID);
					}
					string theUID = "";
					if (PotentialUIDs.Count != 0)
					{
						theUID = PotentialUIDs[HelperFunctions.GetRandomIndex(PotentialUIDs.Count)];
						if (DiceRoll >= 85f)
						{
							Agent level3 = this;
							level3.Level = level3.Level + 1;
							if (this.Level > 10)
							{
								this.Level = 10;
							}
							if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
							{
								Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, "An enemy spy stole some technology from us \nbut we don't know who they were working for", Target);
							}
							Owner.UnlockTech(theUID);
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was not detected and gains +1 level"), Owner);
							break;
						}
						else if (DiceRoll > 75f)
						{
							Agent agent3 = this;
							agent3.Level = agent3.Level + 1;
							if (this.Level > 10)
							{
								this.Level = 10;
							}
							if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
							{
								Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent stole a technology from us: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Owner.UnlockTech(theUID);
							Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nHowever, the Agent was detected but escaped. + 1 level"), Owner);
							break;
						}
						else if (DiceRoll < 20f)
						{
							if (DiceRoll >= 20f)
							{
								break;
							}
							if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
							{
								Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent was killed trying to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal technology"), Owner);
							Owner.data.AgentList.QueuePendingRemoval(this);
							break;
						}
						else
						{
							if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
							{
								Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was detected while attempting to steal a technology\nbut managed to escape unharmed"), Owner);
							break;
						}
					}
					else
					{
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " aborted the Steal Technology mission because\nthere is nothing to steal; 125 Credits are therefore refunded"), Owner);
						Empire owner = Owner;
						owner.Money = owner.Money + 125f;
						break;
					}
				}
				case AgentMission.Robbery:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					int amount = (int)(RandomMath.RandomBetween(1f, 50f) * (float)this.Level);
					if ((float)amount > Target.Money && Target.Money > 0f)
					{
						amount = (int)Target.Money;
					}
					else if (Target.Money <= 0f)
					{
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " could not rob ", this.TargetEmpire, "\nbecause they have no money"), Owner);
						return;
					}
					if (DiceRoll >= 85f)
					{
						Agent level4 = this;
						level4.Level = level4.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire money = Target;
						money.Money = money.Money - (float)amount;
						Empire empire = Owner;
						empire.Money = empire.Money + (float)amount;
						NotificationManager notificationManager2 = Ship.universeScreen.NotificationManager;
						object[] objArray = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nThe Agent was not detected and gains +1 level" };
						notificationManager2.AddAgentResultNotification(true, string.Concat(objArray), Owner);
						if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							break;
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were mysteriously stolen from our treasury.\nWe have no suspects in the theft"), Target);
						break;
					}
					else if (DiceRoll > 60f)
					{
						Agent agent4 = this;
						agent4.Level = agent4.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire money1 = Target;
						money1.Money = money1.Money - (float)amount;
						Empire owner1 = Owner;
						owner1.Money = owner1.Money + (float)amount;
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were stolen from our treasury by an enemy Agent.\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						NotificationManager notificationManager3 = Ship.universeScreen.NotificationManager;
						object[] name1 = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nHowever, the Agent was detected but escaped. + 1 level" };
						notificationManager3.AddAgentResultNotification(true, string.Concat(name1), Owner);
						break;
					}
					else if (DiceRoll < 20f)
					{
						if (DiceRoll >= 20f)
						{
							break;
						}
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy Agent during an attempted robbery\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal credits from ", this.TargetEmpire), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to rob our treasury\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was unable to steal any credits\nbut managed to escape unharmed"), Owner);
						break;
					}
				}
				case AgentMission.InciteRebellion:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.GetPlanets().Count == 0)
					{
						return;
					}
					target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[HelperFunctions.GetRandomIndex(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
					if (DiceRoll >= 85f)
					{
						Agent level5 = this;
						level5.Level = level5.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						if (!EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched)
						{
							Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(EmpireManager.GetEmpireByName(this.TargetEmpire).data, EmpireManager.GetEmpireByName(this.TargetEmpire));
							rebels.data.IsRebelFaction = true;
							rebels.data.Traits.Name = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName;
							rebels.data.Traits.Singular = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelSing;
							rebels.data.Traits.Plural = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelPlur;
							rebels.isFaction = true;
							foreach (Empire e in EmpireManager.EmpireList)
							{
								e.GetRelations().Add(rebels, new Relationship(rebels.data.Traits.Name));
								rebels.GetRelations().Add(e, new Relationship(e.data.Traits.Name));
							}
							EmpireManager.EmpireList.Add(rebels);
							EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched = true;
						}
						Empire darebels = EmpireManager.GetEmpireByName(EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName);
						for (int i = 0; i < 4; i++)
						{
							foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
							{
								if (!EmpireManager.GetEmpireByName(this.TargetEmpire).WeCanBuildTroop(troop.Key))
								{
									continue;
								}
								Troop t = ResourceManager.CreateTroop(troop.Value, darebels);
								t.Name = Localizer.Token(darebels.data.TroopNameIndex);
								t.Description = Localizer.Token(darebels.data.TroopDescriptionIndex);
								target.AssignTroopToTile(t);
								break;
							}
						}
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nThe Agent was not detected and gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll > 70f)
					{
						Agent agent5 = this;
						agent5.Level = agent5.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nHowever, they know we are behind it. Agent gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll < 40f)
					{
						if (DiceRoll >= 40f)
						{
							break;
						}
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy agent trying to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to incite rebellion on ", target.Name), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
						{
							Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to incite rebellion on ", target.Name), Owner);
						break;
					}
				}
			}
			this.TargetEmpire = "";
		}

        //added by gremlin Domission from devek mod.
        public void DoMission(Empire Owner)
        {
            spyMute = AgentComponent.SpyMute;
            Planet target;
            Empire Target = EmpireManager.GetEmpireByName(this.TargetEmpire);
            AgentMission startingmission = this.Mission;
            #region EmpireDefeated
            if (Target != null && Target.data.Defeated)
            {
                this.Mission = AgentMission.Defending;
                this.MissionNameIndex = 2183;
                return;
            }
            #endregion
            #region DefensiveRoll
            float DefensiveRoll = 0f;
            int spysOnDefense = 0;
            int spysOnDefenseLevel = 0;
            int targetPlanets = 0;
            int effectiveLevel = this.Level;
            int defensiveCoverage = 0;
            List<Agent> defendingAgents = new List<Agent>();

            if (Target != null)
            {
                targetPlanets = Target.GetPlanets().Count;
                IEnumerable<Agent> defendingAgentList = Target.data.AgentList.OrderByDescending(level => level.Level).Where(defending => defending.Mission == AgentMission.Defending);
                foreach (Agent defendingAgent in defendingAgentList)
                {
                    if (defendingAgent.Mission == AgentMission.Defending && spysOnDefense < targetPlanets)
                    {
                        spysOnDefense++;
                        spysOnDefenseLevel = spysOnDefenseLevel + defendingAgent.Level;
                        defendingAgents.Add(defendingAgent);
                    }
                }




                if (spysOnDefenseLevel > 0)
                {
                    for (int i = 0; i < spysOnDefenseLevel; i++)
                    {
                        DefensiveRoll = DefensiveRoll + RandomMath.RandomBetween(0f, .75f) + RandomMath.RandomBetween(0f, .75f) + RandomMath.RandomBetween(0f, .75f);
                    }

                    if (Target.data.SpyModifier > 0) DefensiveRoll = DefensiveRoll + DefensiveRoll / (100 / Target.data.SpyModifier);
                    if (Target.data.DefensiveSpyBonus > 0) DefensiveRoll = DefensiveRoll + DefensiveRoll / (100 / Target.data.DefensiveSpyBonus);
                    if (spysOnDefense > 0) defensiveCoverage = (int)DefensiveRoll / targetPlanets;

                    //if (effectiveLevel < 1) effectiveLevel = 1;
                    if (defensiveCoverage > 0)
                    {
                        effectiveLevel -= defensiveCoverage;
                        int spiesNeededfordefense = effectiveLevel;
                        if (spiesNeededfordefense < 1) spiesNeededfordefense = 1;
                        if (spiesNeededfordefense > spysOnDefense) spiesNeededfordefense = spysOnDefense;
                        for (int i = 0; i < spiesNeededfordefense; i++)//((this.Level + Target.GetPlanets().Count) / spysOnDefense) + 1; i++)
                        {
                            Agent defender = defendingAgents.Where(notTraining => notTraining.Mission == AgentMission.Defending).FirstOrDefault();

                            //Agent recovering = defendingAgents.Where(notTraining => notTraining.Mission == AgentMission.Defending).OrderByDescending(agentLevel => agentLevel.Level).FirstOrDefault();
                            //if (recovering != null) recovering.AssignMission(AgentMission.Training,Target,"");
                            if (defender == null)
                            {
                                break;
                            }
                            if (defender != null)
                            {
                                int spyBattle = (int)RandomMath.RandomBetween((float)-1 * defender.Level, (float)effectiveLevel);
                                if (spyBattle <= -5)
                                {
                                    effectiveLevel--;
                                    //this.Level--;
                                    if (defender.Level < 8) defender.Level++;
                                    //defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                else
                                {
                                    if (spyBattle <= -1)
                                    {
                                        if (this.Level > defender.Level && defender.Level < 8) defender.Level++;
                                        effectiveLevel--;
                                        //this.Level--;
                                        //defender.AssignMission(AgentMission.Training, Target, "");
                                    }
                                }
                                if (spyBattle == 0)
                                {
                                    effectiveLevel--;
                                    //this.Level--;
                                    //if (defender.Level > 1) defender.Level--;
                                    defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                if (spyBattle >= 5)
                                {
                                    if (defender.Level > 1) defender.Level--;
                                    if (this.Level < 5) this.Level++;
                                    defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                else
                                {
                                    if (spyBattle >= 1)
                                    {
                                        //if (defender.Level > 1) defender.Level--;
                                        defender.AssignMission(AgentMission.Training, Target, "");
                                    }
                                }
                            }


                        }
                    }


                }



            }


            #endregion
            #region DiceRoll
            IEnumerable<Agent> moleList = Owner.data.AgentList.Where(moles => moles.Mission == AgentMission.Undercover);


            //effectiveLevel = this.Level - defensiveCoverage;
            if (effectiveLevel <= -5) effectiveLevel = -5;
            string moleEmpire = null;
            string moleSpies = null;
            string missionReport = null;


            float DiceRoll = (RandomMath.RandomBetween(0f, 10f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f));
            if (DiceRoll > 77.5f)
            {
                DiceRoll = DiceRoll + 10 * this.Level;
            }
            DiceRoll = DiceRoll + Owner.data.SpyModifier;
            for (float i = 0; i < effectiveLevel; i++)
            {
                DiceRoll = DiceRoll + RandomMath.RandomBetween(1f, 5f);
            }
            if (effectiveLevel < 0)
            {
                for (float i = effectiveLevel; i < 0; i++)
                {
                    DiceRoll = DiceRoll - RandomMath.RandomBetween(1f, 5f);
                }
            }
            DiceRoll = DiceRoll + Owner.data.OffensiveSpyBonus;
            #endregion
            switch (this.Mission)
            {
                #region Training
                case AgentMission.Training:
                {
                    this.Mission = AgentMission.Defending;
                    this.MissionNameIndex = 2183;
                    if (DiceRoll >= 95f)
                    {
                        //Agent level = this;
                        //level.Level = level.Level + 2;
                        //if (this.Level > 10)
                        //{
                            //this.Level = 10;
                        //}
                        //Added by McShooterz
                        this.AddExperience(2, Owner);
                        Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully complete training\nThe Agent's performance exceeded expectation."), Owner);
                        break;
                    }
                    else if (DiceRoll > 25f)
                    {
                        //Agent agent = this;
                        //agent.Level = agent.Level + 1;
                        //if (this.Level > 10)
                        //{
                            //this.Level = 10;
                        //}
                        //Added by McShooterz
                        this.AddExperience(1, Owner);
                        Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully completed training\nand has gained valuable knowledge."), Owner);
                        break;
                    }
                    else if (DiceRoll < 10f)
                    {
                        if (DiceRoll >= 10f)
                        {
                            break;
                        }
                        Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed in a training accident."), Owner);
                        Owner.data.AgentList.QueuePendingRemoval(this);
                        break;
                    }
                    else
                    {
                        Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " has completed training, but failed to learn anything useful."), Owner);
                        break;
                    }
                }
                        /*
                        if (DiceRoll >= 95f)
                        {
                            Agent level = this;
                            level.Level = level.Level + 2;
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " completed training course Gremlins Paradox\n Also fixed the captains sink.\n +2 Levels. "), Owner);
                            startingmission = AgentMission.Defending;
                            break;
                        }
                        if (DiceRoll > 25f)
                        {
                            Agent agent = this;
                            agent.Level = agent.Level + 1;
                            if (this.Level < 2)
                            {
                                this.AssignMission(AgentMission.Training, Owner, TargetEmpire);
                            }
                            else
                            {
                                this.Mission = AgentMission.Defending;
                            }

                            break;
                        }
                        else if (DiceRoll < 10f)
                        {
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was eaten by a gremlin... and died."), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            startingmission = AgentMission.Defending;
                            break;
                        }
                        */ 
                        //else
                        //{


                        //    if (DiceRoll > 10f && Mole != null)
                        //    {
                        //        Mole = moleList.Skip(HelperFunctions.GetRandomIndex(Owner.data.MoleList.Count) - 1).FirstOrDefault();
                        //        Target = EmpireManager.GetEmpireByName(Mole.TargetEmpire);
                        //        moleEmpire = String.Concat("Empire: ", Target.PortraitName, "/n");
                        //        moleSpies = String.Concat("Spy Count: ", Target.data.AgentList.Count, "/n");

                        //        if (DiceRoll > 50 && Mole != null)
                        //        {
                        //            missionReport = String.Concat(missionReport, " /nMole Data Retrived: /n", moleEmpire, moleSpies);
                        //        }
                        //        else
                        //        {

                        //        }
                        //    }



                        //    if (DiceRoll < 10f)
                        //    {


                        //        DamageSpy(agent, Owner, 1, string.Concat(agent.Name, " was attacked by gremlins!\n"));
                        //        break;
                        //    }

                        //    if (missionReport != null) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, missionReport), Owner);
                        //    if (this.Mission == AgentMission.Training) this.AssignMission(startingmission, Owner, this.TargetEmpire);
                        //    this.Mission = AgentMission.Defending;
                        //    this.MissionNameIndex = 2183;
                        //    break;
                        //}
                #endregion
                #region Infiltrate easy
                case AgentMission.Infiltrate:
                    {
                        if (Target.GetPlanets().Count == 0)
                        {
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            return;
                        }
                        if (DiceRoll >= 50f)
                        {
                            this.Mission = AgentMission.Undercover;
                            this.MissionNameIndex = 2201;
                            //Agent level1 = this;
                            //level1.Level = level1.Level + 1;
                            //if (this.Level > 10)
                            //{
                                //this.Level = 10;
                            //}
                            //Added by McShooterz
                            this.AddExperience(3, Owner);
                            Mole m = Mole.PlantMole(Owner, Target);
                            this.TargetGUID = m.PlanetGuid;
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " successfully infiltrated a colony: ", Ship.universeScreen.PlanetsDict[m.PlanetGuid].Name, "\nThe Agent was not detected"), Owner);
                            break;
                        }
                        else if (DiceRoll < 25f)
                        {
                            if (DiceRoll >= 15f)
                            {

                                DamageSpy(this, Owner, 1, "", Target);

                                break;
                            }
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to infiltrate a colony"), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            //if (!DamageSpy(this, Owner, 5, " was killed trying to infiltrate a colony"))
                            //{
                            //    Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " says airlocks are no match for me"), Owner);
                            //    if (this.Level < 2)
                            //    {
                            //        this.Mission = AgentMission.Training;
                            //    }
                            //}
                            if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                break;
                            }

                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed trying to infiltrate one of our colonies\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            break;
                        }
                        else
                        {
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was unable to infiltrate a colony\nand had to abort the mission to avoid capture"), Owner);
                            if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                break;
                            }
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was foiled trying to infiltrate a colony\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            break;
                        }
                    }
                #endregion
                #region Assassinate hard
                case AgentMission.Assassinate:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target.data.AgentList.Count == 0)
                        {
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " could not assassinate an enemy Agent \nbecause target empire has no Agents"), Owner);
                            return;
                        }
                        if (DiceRoll >= 85f)
                        {
                            //Agent agent1 = this;
                            //agent1.Level = agent1.Level + 1;
                            //if (this.Level > 10)
                            //{
                                //this.Level = 10;
                            //}
                            //Added by McShooterz
                            this.AddExperience(6, Owner);
                            Agent m = Target.data.AgentList[HelperFunctions.GetRandomIndex(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in Owner.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    Owner.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            Owner.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                //if (!GremlinAgentComponent.AutoTrain) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was mysteriously assassinated: ", m.Name), Target);
                            }
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent escaped unharmed and undetected."), Owner);
                            break;
                        }
                        else if (DiceRoll >= 70f)
                        {
                            Agent m = Target.data.AgentList[HelperFunctions.GetRandomIndex(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in Owner.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    Owner.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            Owner.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was assassinated: ", m.Name, "\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent was detected but escaped."), Owner);
                            break;
                        }
                        else if (DiceRoll < 25f)
                        {
                            if (DiceRoll >= 15f)
                            {
                                DamageSpy(this, Owner, 1, "", Target);
                                break;
                            }
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed during an assassination attempt\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to assassinate an enemy agent"), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //if (this.Level < 10) this.Level++;
                            //Added by McShooterz
                            this.AddExperience(3, Owner);
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We managed to detect an enemy Assassin before it could strike\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
                            }
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was foiled trying to assassinate an enemy agent, but managed to escape"), Owner);

                            break;
                        }
                    }
                #endregion
                #region Sabotage easy
                case AgentMission.Sabotage:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target.GetPlanets().Count == 0)
                        {
                            return;
                        }
                        target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[HelperFunctions.GetRandomIndex(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
                        this.TargetGUID = target.guid;
                        if (DiceRoll >= 80f)
                        {
                            Planet crippledTurns = target;
                            crippledTurns.Crippled_Turns = crippledTurns.Crippled_Turns + 5 + this.Level * 5;
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name), Target);
                            }
                            NotificationManager notificationManager = Ship.universeScreen.NotificationManager;
                            string[] name = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
                            int num = 5 + this.Level * 5;
                            name[2] = num.ToString();
                            name[3] = " turns: ";
                            name[4] = target.Name;
                            name[5] = "\nThe Agent was not detected";
                            if (!spyMute) notificationManager.AddAgentResultNotification(true, string.Concat(name), Owner);
                            //Agent level2 = this;
                            //level2.Level = level2.Level + 1;
                            //if (this.Level <= 10)
                            //{
                                //break;
                            //}
                            //this.Level = 10;
                            //Added by McShooterz
                            this.AddExperience(4, Owner);
                            break;
                        }
                        else if (DiceRoll > 50f)
                        {
                            Planet planet = target;
                            planet.Crippled_Turns = planet.Crippled_Turns + 5 + this.Level * 3;
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            NotificationManager notificationManager1 = Ship.universeScreen.NotificationManager;
                            string[] str = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
                            int num1 = 5 + this.Level * 3;
                            str[2] = num1.ToString();
                            str[3] = " turns: ";
                            str[4] = target.Name;
                            str[5] = "\nThe Agent was not detected";
                            if (!spyMute) notificationManager1.AddAgentResultNotification(true, string.Concat(str), Owner);
                            //Agent agent2 = this;
                            //agent2.Level = agent2.Level + 1;
                            //if (this.Level <= 10)
                            //{
                                //break;
                            //}
                            //this.Level = 10;
                            //Added by McShooterz
                            this.AddExperience(3, Owner);
                            break;
                        }
                        else if (DiceRoll < 25f)
                        {
                            if (DiceRoll >= 15f)
                            {
                                DamageSpy(this, Owner, 1, "", Target);
                                break;
                            }
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!AgentComponent.AutoTrain) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("We killed an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to sabotage an enemy colony"), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //if (this.Level < 10) this.Level++;
                            //Added by McShooterz
                            this.AddExperience(3, Owner);
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to sabotage ", target.Name), Owner);
                            break;
                        }
                    }
                #endregion
                #region StealTech hard
                case AgentMission.StealTech:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        List<string> PotentialUIDs = new List<string>();
                        foreach (KeyValuePair<string, TechEntry> entry in Target.GetTDict())
                        {
                            if (!entry.Value.Unlocked || !Owner.HavePreReq(entry.Value.UID) || Owner.GetTDict()[entry.Value.UID].Unlocked)
                            {
                                continue;
                            }
                            PotentialUIDs.Add(entry.Value.UID);
                        }
                        string theUID = "";
                        if (PotentialUIDs.Count != 0)
                        {
                            theUID = PotentialUIDs[HelperFunctions.GetRandomIndex(PotentialUIDs.Count)];
                            if (DiceRoll >= 85f)
                            {
                                //Agent level3 = this;
                                //level3.Level = level3.Level + 1;
                                //if (this.Level > 10)
                                //{
                                    //this.Level = 10;
                                //}
                                //Added by McShooterz
                                this.AddExperience(6, Owner);
                                if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                                {
                                    // Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, "An enemy spy stole some technology from us \nbut we don't know who they were working for", Target);
                                }
                                Owner.UnlockTech(theUID);
                                Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was not detected"), Owner);
                                break;
                            }
                            else if (DiceRoll > 75f)
                            {
                                //Agent agent3 = this;
                                //agent3.Level = agent3.Level + 1;
                                //if (this.Level > 10)
                                //{
                                    //this.Level = 10;
                                //}
                                //Added by McShooterz
                                this.AddExperience(5, Owner);
                                if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                                {
                                    if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent stole a technology from us: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                                }
                                Owner.UnlockTech(theUID);
                                Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nHowever, the Agent was detected but escaped."), Owner);
                                break;
                            }
                            else if (DiceRoll < 20f)
                            {
                                if (DiceRoll >= 10f)
                                {
                                    DamageSpy(this, Owner, 1, "", Target);
                                    break;
                                }
                                if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                                {
                                    if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent was killed trying to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal technology"), Owner);
                                Owner.data.AgentList.QueuePendingRemoval(this);
                                break;
                            }
                            else
                            {
                                if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                                {
                                    if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was detected while attempting to steal a technology\nbut managed to escape unharmed"), Owner);
                                break;
                            }
                        }
                        else
                        {
                            //if (this.Level < 10) this.Level++;
                            this.AddExperience(4, Owner);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " aborted the Steal Technology mission because\nthere is nothing to steal; 125 Credits are therefore refunded"), Owner);
                            Empire owner = Owner;
                            owner.Money = owner.Money + 125f;
                            break;
                        }
                    }
                #endregion
                #region Robbery
                case AgentMission.Robbery:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        int amount = (int)(RandomMath.RandomBetween(1f, (float)Target.GetPlanets().Count * 10f) * (float)this.Level);
                        if ((float)amount > Target.Money && Target.Money > 0f)
                        {
                            amount = (int)Target.Money;
                        }
                        else if (Target.Money <= 0f)
                        {
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " could not rob ", this.TargetEmpire, "\nbecause they have no money"), Owner);
                            return;
                        }
                        if (DiceRoll >= 85f)
                        {
                            //Agent level4 = this;
                            //level4.Level = level4.Level + 1;
                            //if (this.Level > 10)
                            //{
                                //this.Level = 10;
                            //}
                            //Added by McShooterz
                            this.AddExperience(4, Owner);
                            Empire money = Target;
                            money.Money = money.Money - (float)amount;
                            Empire empire = Owner;
                            empire.Money = empire.Money + (float)amount;
                            NotificationManager notificationManager2 = Ship.universeScreen.NotificationManager;
                            object[] objArray = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nThe Agent was not detected" };
                            if (!spyMute) notificationManager2.AddAgentResultNotification(true, string.Concat(objArray), Owner);
                            if (Target != EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                break;
                            }
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were mysteriously stolen from our treasury.\nWe have no suspects in the theft"), Target);
                            break;
                        }
                        else if (DiceRoll > 60f)
                        {
                            //Agent agent4 = this;
                            //agent4.Level = agent4.Level + 1;
                            //if (this.Level > 10)
                            //{
                                //this.Level = 10;
                            //}
                            //Added by McShooterz
                            this.AddExperience(3, Owner);
                            Empire money1 = Target;
                            money1.Money = money1.Money - (float)amount;
                            Empire owner1 = Owner;
                            owner1.Money = owner1.Money + (float)amount;
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were stolen from our treasury by an enemy Agent.\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            NotificationManager notificationManager3 = Ship.universeScreen.NotificationManager;
                            object[] name1 = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nHowever, the Agent was detected but escaped." };
                            if (!spyMute) notificationManager3.AddAgentResultNotification(true, string.Concat(name1), Owner);
                            break;
                        }
                        else if (DiceRoll < 20f)
                        {
                            if (DiceRoll >= 10f)
                            {
                                DamageSpy(this, Owner, 1, "", Target);
                                break;
                            }
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy Agent during an attempted robbery\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal credits from ", this.TargetEmpire), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //if (this.Level < 10) this.Level++;
                            this.AddExperience(2, Owner);
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!AgentComponent.AutoTrain && !spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to rob our treasury\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) if (!AgentComponent.AutoTrain) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was unable to steal any credits\nbut managed to escape unharmed"), Owner);
                            break;
                        }
                    }
                #endregion
                #region InciteRebellion hard and deadly
                case AgentMission.InciteRebellion:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target.GetPlanets().Count == 0)
                        {
                            return;
                        }
                        target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[HelperFunctions.GetRandomIndex(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
                        if (DiceRoll >= 70f)
                        {
                            //Agent level5 = this;
                            //level5.Level = level5.Level + 1;
                            //if (this.Level > 10)
                            //{
                                //this.Level = 10;
                            //}
                            //Added by McShooterz
                            this.AddExperience(8, Owner);
                            if (!EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched)
                            {
                                Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(EmpireManager.GetEmpireByName(this.TargetEmpire).data, EmpireManager.GetEmpireByName(this.TargetEmpire));
                                rebels.data.IsRebelFaction = true;
                                rebels.data.Traits.Name = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName;
                                rebels.data.Traits.Singular = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelSing;
                                rebels.data.Traits.Plural = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelPlur;
                                rebels.isFaction = true;
                                foreach (Empire e in EmpireManager.EmpireList)
                                {
                                    e.GetRelations().Add(rebels, new Relationship(rebels.data.Traits.Name));
                                    rebels.GetRelations().Add(e, new Relationship(e.data.Traits.Name));
                                }
                                EmpireManager.EmpireList.Add(rebels);
                                EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched = true;
                            }
                            Empire darebels = EmpireManager.GetEmpireByName(EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName);
                            for (int i = 0; i < effectiveLevel * ((DiceRoll - 65) * .01); i++)
                            {
                                foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
                                {
                                    if (!EmpireManager.GetEmpireByName(this.TargetEmpire).WeCanBuildTroop(troop.Key))
                                    {
                                        continue;
                                    }
                                    Troop t = ResourceManager.CreateTroop(troop.Value, darebels);
                                    t.Name = Localizer.Token(darebels.data.TroopNameIndex);
                                    t.Description = Localizer.Token(darebels.data.TroopDescriptionIndex);
                                    target.AssignTroopToTile(t);
                                    break;
                                }
                            }
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (effectiveLevel * ((DiceRoll - 65) * .01) > 4)
                                {
                                    Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has has released the Kraken on ", target.Name), Target);
                                    ///Wyvern
                                    target.AssignTroopToTile(ResourceManager.TroopsDict["Wyvern"]);
                                }
                                else
                                {
                                    Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name), Target);
                                }
                            }
                            if (effectiveLevel * ((DiceRoll - 65) * .01) > 4)
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " Released the Kraken on ", target.Name), Target);
                                ///Wyvern
                                target.AssignTroopToTile(ResourceManager.TroopsDict["Wyvern"]);
                            }
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nThe Agent was not detected"), Owner);
                            break;
                        }
                        //else if (DiceRoll > 70f)
                        //{
                        //    Agent agent5 = this;
                        //    agent5.Level = agent5.Level + 1;
                        //    if (this.Level > 10)
                        //    {
                        //        this.Level = 10;
                        //    }

                        //    if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                        //    {
                        //        if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                        //    }
                        //    if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nHowever, they know we are behind it. Agent gains +1 level"), Owner);
                        //    break;
                        //}
                        else if (DiceRoll < 40f)
                        {
                            if (DiceRoll >= 25f)
                            {
                                DamageSpy(this, Owner, 2, "", Target);
                                break;
                            }
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy agent trying to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to incite rebellion on ", target.Name), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //if (this.Level < 10) this.Level++;
                            this.AddExperience(4, Owner);
                            if (Target == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty))
                            {
                                if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations()[Owner].DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) Ship.universeScreen.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to incite rebellion on ", target.Name), Owner);
                            break;
                        }
                    }
                #endregion
            }
            #region AutoTrain
            /*
            if (Owner == EmpireManager.GetEmpireByName(Ship.universeScreen.PlayerLoyalty) && Mission == AgentMission.Defending && Owner.Money > 500 && AgentComponent.AutoTrain == true)
            {
                //if (startingmission == AgentMission.Training && this.Level >= 10)
                //{
                //    Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " is a master spy."), Owner);
                //    this.TargetEmpire = "";
                //    return;
                //}
                //agent.AssignMission(AgentMission.Training, Owner, universeScreen.PlayerLoyalty);
                if (this.Level > 1)
                {
                    this.AssignMission(startingmission, Owner, this.TargetEmpire);
                }
                else
                {
                    this.AssignMission(AgentMission.Training, Owner, this.TargetEmpire);
                }
                return;
            }
             */
            #endregion
            this.TargetEmpire = "";
        }

		public void Initialize(AgentMission TheMission, Empire Owner)
		{
			switch (TheMission)
			{
				case AgentMission.Training:
				{
					this.TurnsRemaining = 25;
					Empire owner = Owner;
					owner.Money = owner.Money - 50f;
					this.MissionNameIndex = 2196;
					return;
				}
				case AgentMission.Infiltrate:
				{
					this.TurnsRemaining = 30;
					Empire money = Owner;
					money.Money = money.Money - 75f;
					this.MissionNameIndex = 2188;
					return;
				}
				case AgentMission.Assassinate:
				{
					this.TurnsRemaining = 50;
					Empire empire = Owner;
					empire.Money = empire.Money - 75f;
					this.MissionNameIndex = 2184;
					return;
				}
				case AgentMission.Sabotage:
				{
					this.TurnsRemaining = 30;
					Empire owner1 = Owner;
					owner1.Money = owner1.Money - 75f;
					this.MissionNameIndex = 2190;
					return;
				}
				case AgentMission.StealTech:
				{
					this.TurnsRemaining = 50;
					Empire money1 = Owner;
					money1.Money = money1.Money - 250f;
					this.MissionNameIndex = 2194;
					return;
				}
				case AgentMission.Robbery:
				{
					this.TurnsRemaining = 30;
					Empire empire1 = Owner;
					empire1.Money = empire1.Money - 50f;
					this.MissionNameIndex = 2192;
					return;
				}
				case AgentMission.InciteRebellion:
				{
					this.TurnsRemaining = 100;
					Empire owner2 = Owner;
					owner2.Money = owner2.Money - 250f;
					this.MissionNameIndex = 2186;
					return;
				}
				default:
				{
					return;
				}
			}
		}

        //Added by McShooterz: add experience to the agent and determine if level up.
        private void AddExperience(int exp, Empire Owner)
        {
            this.Experience += exp;
            while(this.Experience >=  3 * this.Level)
            {
                this.Experience -=  3 * this.Level;
                this.Level++;
                Ship.universeScreen.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has been promoted +1 Level"), Owner);
            }
            if (this.Level > 10)
                this.Level = 10;
        }
	}
}