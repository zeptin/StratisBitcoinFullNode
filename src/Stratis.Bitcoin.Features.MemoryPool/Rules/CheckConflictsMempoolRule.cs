﻿using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NBitcoin;
using Stratis.Bitcoin.Features.MemoryPool.Interfaces;

namespace Stratis.Bitcoin.Features.MemoryPool.Rules
{
    /// <summary>
    /// Check for conflicts with in-memory transactions.
    /// If a conflict is found it is added to the validation context.
    /// </summary>
    public class CheckConflictsMempoolRule : IMempoolRule
    {
        public void CheckTransaction(MempoolRuleContext ruleContext, MempoolValidationContext context)
        {
            context.SetConflicts = new List<uint256>();
            foreach (TxIn txin in context.Transaction.Inputs)
            {
                TxMempool.NextTxPair itConflicting = ruleContext.Mempool.MapNextTx.Find(f => f.OutPoint == txin.PrevOut);
                if (itConflicting != null)
                {
                    Transaction ptxConflicting = itConflicting.Transaction;
                    if (!context.SetConflicts.Contains(ptxConflicting.GetHash()))
                    {
                        // Allow opt-out of transaction replacement by setting
                        // nSequence >= maxint-1 on all inputs.
                        //
                        // maxint-1 is picked to still allow use of nLockTime by
                        // non-replaceable transactions. All inputs rather than just one
                        // is for the sake of multi-party protocols, where we don't
                        // want a single party to be able to disable replacement.
                        //
                        // The opt-out ignores descendants as anyone relying on
                        // first-seen mempool behavior should be checking all
                        // unconfirmed ancestors anyway; doing otherwise is hopelessly
                        // insecure.
                        bool replacementOptOut = true;
                        if (ruleContext.Settings.EnableReplacement)
                        {
                            foreach (TxIn txiner in ptxConflicting.Inputs)
                            {
                                if (txiner.Sequence < Sequence.Final - 1)
                                {
                                    replacementOptOut = false;
                                    break;
                                }
                            }
                        }

                        if (replacementOptOut)
                        {
                            ruleContext.Logger.LogTrace("(-)[INVALID_CONFLICT]");
                            context.State.Invalid(MempoolErrors.Conflict).Throw();
                        }

                        context.SetConflicts.Add(ptxConflicting.GetHash());
                    }
                }
            }
        }
    }
}