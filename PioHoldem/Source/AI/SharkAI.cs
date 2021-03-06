﻿using System.Linq;
using System.Threading;

namespace PioHoldem
{
    class SharkAI : DecisionEngine
    {
        private readonly double openMult = 2.5;
        private readonly double oopRaiseMult = 3.5;
        private readonly double ipRaiseMult = 3.0;
        private readonly double betPct = 0.6;
        private FishAI fishAI = new FishAI();
        private PreflopLookups pf = new PreflopLookups();

        public override int GetAction(Game game)
        {
            Thread.Sleep(game.sleepTime);

            Player me = game.players[game.actingIndex];
            Player opp = game.players[game.GetNextPosition(game.actingIndex)];

            //Console.WriteLine();
            //Console.WriteLine(me.name + "'s hole cards: |" + me.holeCards[0] + "|" + me.holeCards[1] + "|");

            string holeCards = eval.ClassifyHoleCards(me.holeCards);

            // Preflop
            if (game.street == 0)
            {
                // Use push/fold strategy for effective stack of 20BB or shorter
                if (game.effectiveStack <= 20.0)
                {
                    if (game.actionCount == 0)
                    {
                        // BU first to act, BB all in after posting blinds
                        if (opp.stack == 0)
                        {
                            return game.effectiveStack <= pf.pushFold_call[holeCards] ? game.sbAmt : -1;
                        }

                        // BU first to act
                        return game.effectiveStack <= pf.pushFold_shove[holeCards] ? me.stack : -1;
                    }
                    else if (game.actionCount == 1 && opp.stack == 0)
                    {
                        // BB facing all in bet from BU
                        return game.effectiveStack <= pf.pushFold_call[holeCards] ? ValidateBetSize(game.betAmt - me.inFor, game) : -1;
                    }
                    else if (game.actionCount == 1 && game.betAmt == game.bbAmt)
                    {
                        // BB facing limp from BU
                        return game.effectiveStack <= pf.pushFold_shove[holeCards] ? me.stack : 0;
                    }
                    else if (game.actionCount == 1)
                    {
                        // BB facing open raise from BU
                        return game.effectiveStack <= pf.pushFold_shove[holeCards] ? me.stack : -1;
                    }
                    else
                    {
                        return me.stack;
                    }
                }
                else
                {
                    // BU first to act
                    if (game.actionCount == 0)
                    {
                        if (pf.BUopen_100.Contains(holeCards))
                        {
                            // Open
                            return ValidateBetSize((int)(openMult * game.bbAmt) - me.inFor, game);
                        }
                        else
                        {
                            // Fold
                            return -1;
                        }
                    }
                    // BB facing BU limp (option)
                    else if (game.actionCount == 1 && me.inFor == game.betAmt)
                    {
                        if (pf.BB3bet_100.Contains(holeCards))
                        {
                            // Raise
                            return ValidateBetSize((int)(oopRaiseMult * game.bbAmt) - me.inFor, game);
                        }
                        else
                        {
                            // Check
                            return 0;
                        }
                    }
                    // BB facing BU open raise
                    else if (game.actionCount == 1 && me.inFor < game.betAmt)
                    {
                        if (pf.BB3bet_100.Contains(holeCards))
                        {
                            // 3bet
                            return ValidateBetSize((int)(oopRaiseMult * game.betAmt) - me.inFor, game);
                        }
                        else if (pf.BBcallOpen_100.Contains(holeCards))
                        {
                            // Call
                            return ValidateBetSize(game.betAmt - me.inFor, game);
                        }
                        else
                        {
                            // Fold
                            return -1;
                        }
                    }
                    // BU facing BB 3bet
                    else if (game.actionCount == 2 && me.inFor < game.betAmt && me.inFor > game.bbAmt)
                    {
                        if (pf.BU4bet_100.Contains(holeCards))
                        {
                            // 4bet
                            return ValidateBetSize((int)(ipRaiseMult * game.betAmt) - me.inFor, game);
                        }
                        else if (pf.BUcall3bet_100.Contains(holeCards))
                        {
                            // Call
                            return ValidateBetSize(game.betAmt - me.inFor, game);
                        }
                        else
                        {
                            // Fold
                            return -1;
                        }
                    }
                    // BB facing BU 4bet
                    else if (game.actionCount == 3)
                    {
                        if (pf.BB5betShove_100.Contains(holeCards))
                        {
                            // 5bet shove
                            return me.stack - me.inFor;
                        }
                        else
                        {
                            // Fold
                            return -1;
                        }
                    }
                    // BU facing BU 5bet (and so on...)
                    else if (game.actionCount >= 4)
                    {
                        if (pf.BUcallShove_100.Contains(holeCards))
                        {
                            // Call/Shove
                            return me.stack - me.inFor;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            // Flop
            else if (game.street == 1)
            {
                // OOP first to act
                if (game.actionCount == 0)
                {
                    // We are the aggressor: continue on 90% of flops
                    if (me.isAggressor)
                    {
                        return rng.Next(10) < 9 ? ValidateBetSize((int)(game.pot * betPct), game) : 0;
                    }
                    // Opponent is the aggressor: check the flop
                    else if (opp.isAggressor)
                    {
                        return 0;
                    }
                    // Neither player is the aggressor: bet 33% of flops
                    else
                    {
                        return rng.Next(3) < 1 ? ValidateBetSize((int)(game.pot * betPct), game) : 0;
                    }
                }
                // IP second to act
                else if (game.actionCount == 1)
                {
                    // OOP player checked flop
                    if (game.betAmt == 0)
                    {
                        // We are the aggressor: continue on 90% of flops
                        if (me.isAggressor)
                        {
                            return rng.Next(10) < 9 ? ValidateBetSize((int)(game.pot * betPct), game) : 0;
                        }
                        // Opponent is the aggressor: check back the flop
                        else if (opp.isAggressor)
                        {
                            return 0;
                        }
                        // Neither player is the aggressor: bet 50% of flops
                        else
                        {
                            return rng.Next(2) < 1 ? ValidateBetSize((int)(game.pot * betPct), game) : 0;
                        }
                    }
                    // OOP player bet flop
                    else
                    {
                        return fishAI.GetAction(game);
                    }
                }
                else
                {
                    return fishAI.GetAction(game);
                }
            }
            // Use FishAI strategy
            else
            {
                return fishAI.GetAction(game);
            }
        }

        private int ValidateBetSize(int bet, Game game)
        {
            Player me = game.players[game.actingIndex];
            if (bet > me.stack)
            {
                return me.stack;
            }
            return bet;
        }
    }
}
