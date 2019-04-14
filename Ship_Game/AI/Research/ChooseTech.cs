﻿using Ship_Game.Ships;
using System;
using System.Linq;

namespace Ship_Game.AI.Research
{
    public class ChooseTech
    {
        readonly Empire OwnerEmpire;
        int ScriptIndex;
        public EmpireAI.ResearchStrategy ScriptType { get; private set; }
        public readonly ShipTechLineFocusing LineFocus;
        ResearchPriorities ResearchPriorities;
        readonly EconomicResearchStrategy Strategy;

        // this reduces the cost of techs so that techs that are near the same cost
        // get compared as if they are the same cost.
        float CostNormalizer;
        int NormalizeTechCost(float techCost) => (int)Math.Ceiling(techCost * CostNormalizer);

        public ChooseTech(Empire empire)
        {
            OwnerEmpire = empire;
            Strategy    = OwnerEmpire.ResearchStrategy;
            LineFocus   = new ShipTechLineFocusing(empire);
            ScriptType  = Strategy?.TechPath?.Count > 0 ? EmpireAI.ResearchStrategy.Scripted : EmpireAI.ResearchStrategy.Random;
        }

        private void DebugLog(string text) => Empire.Universe?.DebugWin?.ResearchLog(text, OwnerEmpire);

        public void InitializeNewResearchRun(ResearchPriorities researchPriority)
        {
            ResearchPriorities = researchPriority;
            CostNormalizer = OwnerEmpire.MaxResearchPotential / 1000f;
        }

        public bool ProcessScript()
        {
            bool atWar         = ResearchPriorities.Wars > 0.5f;
            bool highTaxes     = ResearchPriorities.Economics > 0.5f;
            bool lowResearch   = ResearchPriorities.ResearchDebt > 0.5f;
            bool lowIncome     = OwnerEmpire.Money < OwnerEmpire.NetPlanetIncomes;
            ResearchPriorities = ResearchPriorities;
            int loopCount      = 0;
            var strat          = Strategy;

            Start:
            if (strat != null && ScriptIndex < strat.TechPath.Count && loopCount < strat.TechPath.Count)
            {
                string scriptEntry = strat.TechPath[ScriptIndex].id;
                string scriptCommand;

                if (OwnerEmpire.HasTechEntry(scriptEntry))
                    scriptCommand = scriptEntry;
                else
                    scriptCommand = scriptEntry.Split(':')[0];

                DebugLog($"index : {ScriptIndex}");
                DebugLog($"Script Command : {scriptCommand}");
                switch (scriptCommand)
                {
                    case "SCRIPT":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

                            if (script.Count() > 2)
                            {
                                modifier = script[2];
                            }
                            ScriptIndex++;
                            if (ScriptedResearch("CHEAPEST", script[1], modifier))
                                return true;
                            loopCount++;
                            goto Start;
                        }
                    case "LOOP":
                        {
                            ScriptIndex =
                                int.Parse(OwnerEmpire.ResearchStrategy.TechPath[ScriptIndex].id
                                    .Split(':')[1]);
                            loopCount++;
                            goto Start;
                        }
                    case "CHEAPEST":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

                            if (script.Count() == 1)
                            {
                                GoRandomOnce();
                                ScriptIndex++;
                                return true;
                            }
                            string[] modifiers = new string[script.Count() - 1];
                            for (int i = 1; i < script.Count(); i++)
                            {
                                modifiers[i - 1] = script[i];
                            }
                            modifier = String.Join(":", modifiers);
                            ScriptIndex++;
                            if (ScriptedResearch(scriptCommand, script[1], modifier))
                                return true;
                            loopCount++;
                            goto Start;
                        }
                    case "EXPENSIVE":
                        {
                            string modifier = "";
                            string[] script = scriptEntry.Split(':');

                            if (script.Count() == 1)
                            {
                                GoRandomOnce("EXPENSIVE");
                                ScriptIndex++;
                                return true;
                            }
                            string[] modifiers = new string[script.Count() - 1];
                            for (int i = 1; i < script.Count(); i++)
                            {
                                modifiers[i - 1] = script[i];
                            }
                            modifier = String.Join(":", modifiers);
                            ScriptIndex++;
                            if (ScriptedResearch(scriptCommand, script[1], modifier))
                                return true;
                            loopCount++;
                            goto Start;
                        }
                    case "IFWAR":
                        {
                            loopCount += ScriptBump(atWar);
                            goto Start;
                        }
                    case "IFHIGHTAX":
                        {
                            loopCount += ScriptBump(highTaxes);
                            goto Start;
                        }
                    case "IFPEACE":
                        {
                            loopCount += ScriptBump(!atWar);
                            goto Start;
                        }
                    case "IFCYBERNETIC":
                        {
                            loopCount += ScriptBump(OwnerEmpire.IsCybernetic);
                            goto Start;
                        }
                    case "IFLOWRESEARCH":
                        {
                            loopCount += ScriptBump(lowResearch);
                            goto Start;
                        }
                    case "IFNOTLOWRESEARCH":
                        {
                            loopCount += ScriptBump(!lowResearch);
                            goto Start;
                        }
                    case "IFLOWINCOME":
                        {
                            loopCount += ScriptBump(lowIncome);
                            goto Start;
                        }
                    case "IFNOTLOWINCOME":
                        {
                            loopCount += ScriptBump(!lowIncome);
                            goto Start;
                        }
                    case "RANDOM":
                        {
                            GoRandomOnce();
                            ScriptIndex++;
                            return true;
                        }
                    case "IFRESEARCHHIGHERTHAN":
                        bool researchPreReqMet = false;
                        string[] researchScript = scriptEntry.Split(':');
                        if (float.TryParse(researchScript[2], out float researchAmount))
                            if (OwnerEmpire.GetProjectedResearchNextTurn() >= researchAmount)
                                researchPreReqMet = true;

                        loopCount += ScriptBump(researchPreReqMet);
                        goto Start;
                    case "IFTECHRESEARCHED":
                        bool techResearched = false;
                        string[] techResearchedScript = scriptEntry.Split(':');
                        if (OwnerEmpire.TryGetTechEntry(techResearchedScript[2], out TechEntry checkedTech))
                        {
                            if (checkedTech.Unlocked)
                                techResearched = true;
                        }
                        loopCount += ScriptBump(techResearched);
                        goto Start;
                    default:
                        {
                            DebugLog($"Hard Script : {scriptEntry}");
                            if (OwnerEmpire.TryGetTechEntry(scriptEntry, out TechEntry defaulttech))
                            {
                                if (defaulttech.Unlocked)

                                {
                                    DebugLog("Already Unlocked");
                                    ScriptIndex++;
                                    goto Start;
                                }
                                if (!defaulttech.Unlocked && OwnerEmpire.HavePreReq(defaulttech.UID))
                                {
                                    DebugLog("Researching");
                                    OwnerEmpire.ResearchTopic = defaulttech.UID;
                                    ScriptIndex++;
                                    if (!string.IsNullOrEmpty(scriptEntry))
                                        return true;
                                }
                                else
                                {
                                    ScriptIndex++;
                                    goto Start;
                                }
                            }
                            else
                            {
                                Log.Info($"TechNotFound : {scriptEntry}");
                                ScriptIndex++;
                                goto Start;
                            }

                            foreach (EconomicResearchStrategy.Tech tech in OwnerEmpire.ResearchStrategy.TechPath)
                            {
                                if (OwnerEmpire.HasTechEntry(tech.id) && !OwnerEmpire.HasUnlocked(tech.id) &&
                                    OwnerEmpire.HavePreReq(tech.id))
                                {
                                    OwnerEmpire.ResearchTopic = tech.id;
                                    ScriptIndex++;
                                    if (tech.id.NotEmpty())
                                        return true;
                                }
                            }
                            GoRandomOnce();
                            ScriptIndex++;
                            return true;
                        }
                }
            }
            if (OwnerEmpire.ResearchTopic.IsEmpty())
            {
                GoRandomOnce();
                if (loopCount >= OwnerEmpire.ResearchStrategy.TechPath.Count)
                    ScriptType = EmpireAI.ResearchStrategy.Random;

            }
            return false;
        }

        private bool GoRandomOnce(string command = "CHEAPEST")
        {
            DebugLog($"Go Random Once");
            ScriptType = EmpireAI.ResearchStrategy.Random;
            ScriptedResearch(command, "RANDOM", ResearchPriorities.TechCategoryPrioritized);
            ScriptType = EmpireAI.ResearchStrategy.Scripted;
            return OwnerEmpire.ResearchTopic.NotEmpty();
        }

        private int ScriptBump(bool check, int index = 1)
        {
            if (check)
            {
                ScriptIndex =
                    int.Parse(
                        OwnerEmpire.ResearchStrategy.TechPath[ScriptIndex].id.Split(':')[1]);
                return 1;
            }
            ScriptIndex++;
            return 0;
        }

        public bool ScriptedResearch(string command1, string command2, string modifier, bool doLookAhead = false)
        {
            Array<TechEntry> availableTechs = OwnerEmpire.CurrentTechsResearchable();

            if (availableTechs.Count <= 0) return false;

            DebugLog($"Possible Techs : {availableTechs.Count}");

            string researchTopic = "";

            availableTechs = LineFocus.LineFocusShipTechs(modifier, availableTechs, command2);

            int previousCost = command1 == "CHEAPEST" ? int.MaxValue : int.MinValue;
            switch (command2)
            {
                case "RANDOM":
                case "TECH":
                    {

                        string[] script = modifier.Split(':');
                        for (int i = 1; i < script.Count(); i++)
                        {
                            var techType = ConvertTechStringTechType(script[i]);

                            TechEntry researchTech = GetScriptedTech(command1, techType, availableTechs, doLookAhead);
                            if (researchTech == null) continue;

                            string testResearchTopic = researchTech.UID;

                            int currentCost = 0;
                            if (researchTech.TechnologyTypes.Contains(techType))
                                currentCost = NormalizeTechCost(researchTech.TechCost);
                            else
                                currentCost = NormalizeTechCost(researchTech.CostOfNextTechWithType(techType));

                            if (command1 == "CHEAPEST" && currentCost < previousCost)
                            {
                                researchTopic = testResearchTopic;
                                previousCost = currentCost;
                                CostNormalizer += .005f;
                            }
                            else if (command1 == "EXPENSIVE" && currentCost > previousCost)
                            {
                                researchTopic = testResearchTopic;
                                previousCost = currentCost;
                                CostNormalizer *= .25f;
                            }
                        }
                        if (OwnerEmpire.data.TechDelayTime % 6 == 0 && ResearchPriorities.Wars > 2
                                                                    && LineFocus.WasBestShipHullNotChosen(researchTopic, availableTechs))
                            DebugLog($"Hull Was Too Expensive");
                        break;
                    }

                default:
                    {
                        var techType = ConvertTechStringTechType(command2);
                        TechEntry researchTech = GetScriptedTech(command1, techType, availableTechs, doLookAhead);
                        if (researchTech != null)
                        {
                            researchTopic = researchTech.UID;
                            break;
                        }
                        researchTopic = null;
                        break;
                    }
            }

            OwnerEmpire.ResearchTopic = researchTopic;
            //DebugLog($"Tech Chosen : {researchTopic}");

            if (!doLookAhead && OwnerEmpire.ResearchTopic.IsEmpty())
                ScriptedResearch(command1, command2, modifier, true);


            if (string.IsNullOrEmpty(OwnerEmpire.ResearchTopic))
                return false;
            return true;
        }

        private TechnologyType ConvertTechStringTechType(string typeName)
        {
            TechnologyType techType = TechnologyType.General;
            try
            {
                techType = (TechnologyType)Enum.Parse(typeof(TechnologyType), typeName);
            }
            catch
            {
                Log.Error($"techType not found : ");
            }
            return techType;
        }

        private TechEntry GetScriptedTech(string command1, TechnologyType techType, Array<TechEntry> availableTechs, bool doLookAhead)
        {

            DebugLog($"\nFind : {techType.ToString()}");

            TechEntry researchTech = null;

            TechEntry[] techsTypeFiltered = availableTechs.Filter(tech => tech.TechnologyTypes.Contains(techType));
            if (techType.ToString().Contains("Ship"))
            {
                Array<TechEntry> filteredTech = new Array<TechEntry>();
                foreach(var tech in techsTypeFiltered)
                {
                    if (LineFocus.BestCombatShip.shipData.TechsNeeded.Contains(tech.UID))
                        filteredTech.Add(tech);
                }
                techsTypeFiltered = filteredTech.ToArray();
            }
            if (techsTypeFiltered.Length == 0 && doLookAhead)
            {
                //this get lookahead is tricky.
                //Its trying here to see if the current tech with the wrong techType has a future tech with the right one.
                //otherwise it would be a simple tech matches techType formula.
                //its also checking economy tech types for their hulls.
                //It doesnt want to build freighters to make more money.
                //but it does want to build stations that make more money.
                techsTypeFiltered = availableTechs.Filter(tech =>
                {
                    //if (availableTechs.Count == 1) return true;
                    if (IncludeFreighters(tech) && tech.CostOfNextTechWithType(techType) > 0)
                        return true;
                    return false;
                });
            }
            LogPossibleTechs(techsTypeFiltered);
            researchTech = TechWithWantedCost(command1, techsTypeFiltered, techType);
            
            return researchTech;
        }

        bool IncludeFreighters(TechEntry tech)
        {
            if (tech.TechnologyType != TechnologyType.Economic)
                return true;

            if (tech.Tech.HullsUnlocked.Count == 0 || ResearchPriorities.Economics > 0.9f)
                return true;

            foreach (var hull in tech.Tech.HullsUnlocked)
            {
                if (!ResourceManager.Hull(hull.Name, out ShipData hullData) || hullData == null) continue;
                switch (hullData.HullRole)
                {
                    case ShipData.RoleName.station:
                    case ShipData.RoleName.platform:
                        return true;
                }
            }
            return false;

        }

        private TechEntry TechWithWantedCost(string command1, TechEntry[] filteredTechs, TechnologyType techType)
        {
            TechEntry researchTech = null;
            if (command1 == "CHEAPEST")
                researchTech = filteredTechs.FindMin(cost =>
                {
                    if (cost.TechnologyTypes.Contains(techType))
                        return cost.TechCost;
                    return cost.CostOfNextTechWithType(techType);
                });
            else if (command1 == "EXPENSIVE")
                researchTech = filteredTechs.FindMax(cost => cost.TechCost);
            DebugLog($"{command1} : {researchTech?.UID ?? "Nothing Found"}");
            return researchTech;
        }

        private void LogPossibleTechs(TechEntry[] filteredTechs)
        {
            foreach (var tech in filteredTechs)
                DebugLog($" {tech.UID} : {tech.TechCost} : ({NormalizeTechCost(tech.TechCost)})");
        }
    }
}
