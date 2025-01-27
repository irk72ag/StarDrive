﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public class ConstructionQueueScrollListItem : ScrollListItem<ConstructionQueueScrollListItem>
    {
        readonly Planet Planet;
        UniverseScreen Universe => Planet.Universe.Screen;
        public readonly QueueItem Item;
        readonly bool LowRes;

        public ConstructionQueueScrollListItem(QueueItem item, bool lowRes)
        {
            Planet = item.Planet;
            Item   = item;
            LowRes = lowRes;
            if (Planet.OwnerIsPlayer || Planet.Universe.Debug)
            {
                AddUp(new Vector2(-120, 0), /*Queue up*/GameText.ClickToMoveUpIn, OnUpClicked);
                AddDown(new Vector2(-90, 0), /*Queue down*/GameText.ClickToMoveDownIn, OnDownClicked);
                AddApply(new Vector2(-60, 0), /*Cancel production*/GameText.ClickToRushProductionFrom, OnApplyClicked);
                AddCancel(new Vector2(-30, 0), /*Cancel production*/GameText.CancelProductionAndRemoveThis, OnCancelClicked);
            }
        }

        void OnUpClicked()
        {
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown)
            {
                Universe.RunOnSimThread(() =>
                {
                    var index = Planet.ConstructionQueue.IndexOf(Item);
                    if (index > 0)
                    {
                        MoveToConstructionQueuePosition(0, index);
                    }
                    else
                    {
                        Log.Warning($"Deferred Action: Move Queue to top: Failed {index}");
                    }
                }); // move to top
            }
            else
            {
                Universe.RunOnSimThread(() =>
                {
                    int index = Planet.ConstructionQueue.IndexOf(Item);
                    if (index > 0)
                    {
                        SwapConstructionQueueItems(index - 1, index);
                    }
                    else
                    {
                        Log.Warning($"Deferred Action: Move Queue UP: Failed {index}");
                    }
                }); // move up by one
            }
        }

        void OnDownClicked()
        {
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown)
            {
                Universe.RunOnSimThread(() =>
                {
                    var listBottom = Planet.ConstructionQueue.Count - 1;
                    var index = Planet.ConstructionQueue.IndexOf(Item);
                    if (index >= 0 && index < listBottom)
                    {
                        MoveToConstructionQueuePosition(listBottom, index);
                    }
                    else
                    {
                        Log.Warning($"Deferred Action: Move Queue to bottom: Failed {index}");
                    }
                }); // move to bottom
            }
            else
            {
                Universe.RunOnSimThread(() =>
                {
                    var listBottom = Planet.ConstructionQueue.Count - 1;
                    var index = Planet.ConstructionQueue.IndexOf(Item);
                    if (index >=0 && index < listBottom)
                    {
                        SwapConstructionQueueItems(index + 1, index);
                    }
                    else
                    {
                        Log.Warning($"Deferred Action: Move Queue down: Failed {index}");
                    }
                }); // move down by one
            }
        }

        void OnApplyClicked()
        {
            InputState input = GameBase.ScreenManager.input;
            if (input.IsShiftKeyDown)
            {
                Universe.RunOnSimThread(() => Item.Rush = !Item.Rush);
                return;
            }

            float maxAmount = (input.IsCtrlKeyDown ? Planet.ProdHere : 10f).UpperBound(Item.ProductionNeeded);
            Universe.RunOnSimThread(() => RushProduction(Item, maxAmount.UpperBound(Planet.ProdHere)));
        }

        void RushProduction(QueueItem item, float amount)
        {
            int index = Planet.ConstructionQueue.IndexOf(item);

            if (index >=0 && !item.IsComplete && Planet.Construction.RushProduction(index, amount, rushButton: true))
            {
                GameAudio.AcceptClick();
            }
            else
            {
                GameAudio.NegativeClick();
                Log.Warning($"Deferred Action: Rush Queue: Failed {index}");
            }
        }
        void OnCancelClicked()
        {
            Universe.RunOnSimThread(() =>
            {
                int index = Planet.ConstructionQueue.IndexOf(Item);
                if (index >= 0 && !Item.IsComplete)
                {
                    Planet.Construction.Cancel(Item);
                    GameAudio.AcceptClick();
                }
                else
                {
                    GameAudio.NegativeClick();
                    Log.Warning($"Deferred Action: Cancel Queue Item: Failed {index}");
                }
                GameAudio.AcceptClick();
            });
        }

        void SwapConstructionQueueItems(int swapTo, int currentIndex)
        {
            Planet.Construction.Swap(swapTo, currentIndex);
            GameAudio.AcceptClick();
        }

        void MoveToConstructionQueuePosition(int moveTo, int currentIndex)
        {
            Planet.Construction.MoveTo(moveTo, currentIndex);
            GameAudio.AcceptClick();
        }
        
        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            Item.DrawAt(Planet.Universe, batch, Pos, LowRes);
            base.Draw(batch, elapsed);
        }
    }
}
