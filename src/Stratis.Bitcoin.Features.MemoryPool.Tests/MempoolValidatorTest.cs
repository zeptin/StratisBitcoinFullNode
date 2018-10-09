﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Networks;
using Stratis.Bitcoin.Tests.Common;
using Xunit;

namespace Stratis.Bitcoin.Features.MemoryPool.Tests
{
    /// <summary>
    /// Unit tests for the memory pool validator.
    /// </summary>
    /// <remarks>TODO: Currently only stubs - need to complete</remarks>
    public class MempoolValidatorTest : TestBase
    {
        public MempoolValidatorTest() : base(KnownNetworks.RegTest)
        {
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckFinalTransaction_WithStandardLockTimeAndValidTxTime_ReturnsTrue()
        {
            // TODO: Implement test
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckFinalTransaction_WithNoLockTimeAndValidTxTime_ReturnsTrue()
        {
            // TODO: Implement test
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckFinalTransaction_WithStandardLockTimeAndExpiredTxTime_Fails()
        {
            // TODO: Implement test
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckFinalTransaction_WithNoLockTimeLockTimeAndExpiredTxTime_Fails()
        {
            // TODO: Implement test
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckSequenceLocks_WithExistingLockPointAndValidChain_ReturnsTrue()
        {
            // TODO: Test case- Check two cases, chain with/without previous block
            // No Previous - time of lock == 0
            // Previous - time of lock < tip chain median time
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckSequenceLocks_WithExistingLockPointAndChainWithPrevAndExpiredBlockTime_Fails()
        {
            // TODO: Test case - The lock point MinTime exceeds chain previous block median time
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckSequenceLocks_WithExistingLockPointAndChainWihtNoPrevAndExpiredBlockTime_Fails()
        {
            // TODO: Test case - the lock point MinTime exceeds 0
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CheckSequenceLocks_WithExistingLockPointAndBadHeight_Fails()
        {
            // TODO: Test case - lock point height exceeds chain height
        }

        [Fact(Skip = "Not implemented yet.")]
        public void GetTransactionWeight_WitnessTx_ReturnsWeight()
        {
            // TODO: Test getting tx weight on transaction with witness
        }

        [Fact(Skip = "Not implemented yet.")]
        public void GetTransactionWeight_StandardTx_ReturnsWeight()
        {
            // TODO: Test getting tx weight on transaction without witness
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CalculateModifiedSize_CalcWeightWithTxIns_ReturnsSize()
        {
            // TODO: Test GetTransactionWeight - InputSizes = CalculateModifiedSize
            // Test weight calculation by input nTxSize = 0
        }

        [Fact(Skip = "Not implemented yet.")]
        public void CalculateModifiedSize_PreCalcWeightWithTxIns_ReturnsSize()
        {
            // TODO: Test GetTransactionWeight - InputSizes = CalculateModifiedSize
            // Test weight calculation by passing in weight as nTxSize, nTxSize can be computed with GetTransactionWeight()
        }

        [Fact]
        public async Task AcceptToMemoryPool_WithValidP2PKHTxn_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();
            tx.AddInput(new TxIn(new OutPoint(context.SrcTxs[0].GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.CENT * 11), destSecret.PubKeyHash));
            tx.Sign(KnownNetworks.RegTest, minerSecret, false);

            var state = new MempoolValidationState(false);
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.True(isSuccess, "P2PKH tx not valid.");
        }

        /// <summary>
        /// Validate multi input/output P2PK, P2PKH transactions in memory pool.
        /// Transaction scenario adapted from code project article referenced below.
        /// </summary>
        /// <seealso cref="https://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All"/>
        [Fact]
        public async Task AcceptToMemoryPool_WithMultiInOutValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var alice = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var satoshi = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // Fund Alice, Bob, Satoshi
            // 50 Coins come from first tx on chain - send satoshi 1, bob 2, Alice 1.5 and change back to miner
            var coin = new Coin(context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.ScriptPubKey);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction multiOutputTx = txBuilder
                .AddCoins(new List<Coin> { coin })
                .AddKeys(miner)
                .Send(satoshi.GetAddress(), "1.00")
                .Send(bob.GetAddress(), "2.00")
                .Send(alice.GetAddress(), "1.50")
                .SendFees("0.001")
                .SetChange(miner.GetAddress())
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(multiOutputTx)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, multiOutputTx), $"Transaction: {nameof(multiOutputTx)} failed mempool validation.");

            // Alice then Bob sends to Satoshi
            Coin[] aliceCoins = multiOutputTx.Outputs
                        .Where(o => o.ScriptPubKey == alice.ScriptPubKey)
                        .Select(o => new Coin(new OutPoint(multiOutputTx.GetHash(), multiOutputTx.Outputs.IndexOf(o)), o))
                        .ToArray();
            Coin[] bobCoins = multiOutputTx.Outputs
                        .Where(o => o.ScriptPubKey == bob.ScriptPubKey)
                        .Select(o => new Coin(new OutPoint(multiOutputTx.GetHash(), multiOutputTx.Outputs.IndexOf(o)), o))
                        .ToArray();

            txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction multiInputTx = txBuilder
                .AddCoins(aliceCoins)
                .AddKeys(alice)
                .Send(satoshi.GetAddress(), "0.8")
                .SetChange(alice.GetAddress())
                .SendFees("0.0005")
                .Then()
                .AddCoins(bobCoins)
                .AddKeys(bob)
                .Send(satoshi.GetAddress(), "0.2")
                .SetChange(bob.GetAddress())
                .SendFees("0.0005")
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(multiInputTx)); //check fully signed
            Assert.True(await validator.AcceptToMemoryPool(state, multiInputTx), $"Transaction: {nameof(multiInputTx)} failed mempool validation.");
        }

        /// <summary>
        /// Validate multi sig transactions in memory pool.
        /// Transaction scenario adapted from code project article referenced below.
        /// </summary>
        /// <seealso cref="https://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All"/>
        [Fact]
        public async Task AcceptToMemoryPool_WithMultiSigValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var alice = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var satoshi = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var nico = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // corp needs two out of three of alice, bob, nico
            Script corpMultiSig = PayToMultiSigTemplate
                        .Instance
                        .GenerateScriptPubKey(2, new[] { alice.PubKey, bob.PubKey, nico.PubKey });

            // Fund corp
            // 50 Coins come from first tx on chain - send corp 42 and change back to miner
            var coin = new Coin(context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.ScriptPubKey);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction sendToMultiSigTx = txBuilder
                .AddCoins(new List<Coin> { coin })
                .AddKeys(miner)
                .Send(corpMultiSig, "42.00")
                .SendFees("0.001")
                .SetChange(miner.GetAddress())
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(sendToMultiSigTx)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, sendToMultiSigTx), $"Transaction: {nameof(sendToMultiSigTx)} failed mempool validation.");

            // AliceBobNico corp. send to Satoshi
            Coin[] corpCoins = sendToMultiSigTx.Outputs
                        .Where(o => o.ScriptPubKey == corpMultiSig)
                        .Select(o => new Coin(new OutPoint(sendToMultiSigTx.GetHash(), sendToMultiSigTx.Outputs.IndexOf(o)), o))
                        .ToArray();

            // Alice initiates the transaction
            txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction multiSigTx = txBuilder
                    .AddCoins(corpCoins)
                    .AddKeys(alice)
                    .Send(satoshi.GetAddress(), "4.5")
                    .SendFees("0.001")
                    .SetChange(corpMultiSig)
                    .BuildTransaction(true);
            Assert.True(!txBuilder.Verify(multiSigTx)); //Well, only one signature on the two required...

            // Nico completes the transaction
            txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            multiSigTx = txBuilder
                    .AddCoins(corpCoins)
                    .AddKeys(nico)
                    .SignTransaction(multiSigTx);
            Assert.True(txBuilder.Verify(multiSigTx));

            Assert.True(await validator.AcceptToMemoryPool(state, multiSigTx), $"Transaction: {nameof(multiSigTx)} failed mempool validation.");
        }

        /// <summary>
        /// Validate P2SH transaction in memory pool.
        /// Transaction scenario adapted from code project article referenced below.
        /// </summary>
        /// <seealso cref="https://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All"/>
        [Fact]
        public async Task AcceptToMemoryPool_WithP2SHValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var alice = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var satoshi = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var nico = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // corp needs two out of three of alice, bob, nico
            Script corpMultiSig = PayToMultiSigTemplate
                        .Instance
                        .GenerateScriptPubKey(2, new[] { alice.PubKey, bob.PubKey, nico.PubKey });

            // P2SH address for corp multi-sig
            BitcoinScriptAddress corpRedeemAddress = corpMultiSig.GetScriptAddress(KnownNetworks.RegTest);

            // Fund corp
            // 50 Coins come from first tx on chain - send corp 42 and change back to miner
            var coin = new Coin(context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.ScriptPubKey);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction fundP2shTx = txBuilder
                .AddCoins(new List<Coin> { coin })
                .AddKeys(miner)
                .Send(corpRedeemAddress, "42.00")
                .SendFees("0.001")
                .SetChange(miner.GetAddress())
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(fundP2shTx)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, fundP2shTx), $"Transaction: {nameof(fundP2shTx)} failed mempool validation.");

            // AliceBobNico corp. send 20 to Satoshi
            Coin[] corpCoins = fundP2shTx.Outputs
                        .Where(o => o.ScriptPubKey == corpRedeemAddress.ScriptPubKey)
                        .Select(o => ScriptCoin.Create(KnownNetworks.RegTest, new OutPoint(fundP2shTx.GetHash(), fundP2shTx.Outputs.IndexOf(o)), o, corpMultiSig))
                        .ToArray();

            txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction p2shSpendTx = txBuilder
                    .AddCoins(corpCoins)
                    .AddKeys(alice, bob)
                    .Send(satoshi.GetAddress(), "20")
                    .SendFees("0.001")
                    .SetChange(corpRedeemAddress)
                    .BuildTransaction(true);
            Assert.True(txBuilder.Verify(p2shSpendTx));

            Assert.True(await validator.AcceptToMemoryPool(state, p2shSpendTx), $"Transaction: {nameof(p2shSpendTx)} failed mempool validation.");
        }

        /// <summary>
        /// Validate P2WPKH transaction in memory pool.
        /// </summary>
        [Fact]
        public async Task AcceptToMemoryPool_WithP2WPKHValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.WitHash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // Fund Bob
            // 50 Coins come from first tx on chain - send bob 42 and change back to miner
            var witnessCoin = new Coin(context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.PubKey.WitHash.ScriptPubKey);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction p2wpkhTx = txBuilder
                .AddCoins(witnessCoin)
                .AddKeys(miner)
                .Send(bob, "42.00")
                .SendFees("0.001")
                .SetChange(miner)
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(p2wpkhTx)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, p2wpkhTx), $"Transaction: {nameof(p2wpkhTx)} failed mempool validation.");
        }

        /// <summary>
        /// Validate P2WSH transaction in memory pool.
        /// </summary>
        [Fact]
        public async Task AcceptToMemoryPool_WithP2WSHValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.ScriptPubKey.WitHash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // Fund Bob
            // 50 Coins come from first tx on chain - send bob 42 and change back to miner
            ScriptCoin witnessCoin = ScriptCoin.Create(KnownNetworks.RegTest, context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.PubKey.ScriptPubKey.WitHash.ScriptPubKey, miner.PubKey.ScriptPubKey).AssertCoherent(KnownNetworks.RegTest);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction p2wshTx = txBuilder
                .AddCoins(witnessCoin)
                .AddKeys(miner)
                .Send(bob, "42.00")
                .SendFees("0.001")
                .SetChange(miner)
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(p2wshTx)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, p2wshTx), $"Transaction: {nameof(p2wshTx)} failed mempool validation.");
        }

        /// <summary>
        /// Validate SegWit transaction in memory pool.
        /// </summary>
        [Fact]
        public async Task AcceptToMemoryPool_WithSegWitValidTxns_IsSuccessfullAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.ScriptPubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);

            // Fund Bob
            // 50 Coins come from first tx on chain - send bob 42 and change back to miner
            ScriptCoin witnessCoin =  ScriptCoin.Create(KnownNetworks.RegTest, context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, miner.PubKey.ScriptPubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey, miner.PubKey.ScriptPubKey);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);
            Transaction p2shOverp2wpkh = txBuilder
                .AddCoins(witnessCoin)
                .AddKeys(miner)
                .Send(bob, "42.00")
                .SendFees("0.001")
                .SetChange(miner)
                .BuildTransaction(true);
            Assert.True(txBuilder.Verify(p2shOverp2wpkh)); //check fully signed

            // remove witness data from tx
            Transaction noWitTx = p2shOverp2wpkh.WithOptions(TransactionOptions.None, KnownNetworks.RegTest.Consensus.ConsensusFactory);

            Assert.Equal(p2shOverp2wpkh.GetHash(), noWitTx.GetHash());
            Assert.True(noWitTx.GetSerializedSize() < p2shOverp2wpkh.GetSerializedSize());

            Assert.True(txBuilder.Verify(p2shOverp2wpkh)); //check fully signed
            var state = new MempoolValidationState(false);
            Assert.True(await validator.AcceptToMemoryPool(state, p2shOverp2wpkh), $"Transaction: {nameof(p2shOverp2wpkh)} failed mempool validation.");
        }

        [Fact]
        public async void AcceptToMemoryPool_TxIsCoinbase_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();

            // Create a transaction that looks like a coinbase.
            tx.AddInput(new TxIn(new OutPoint(new uint256(0), uint.MaxValue), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.CENT * 11), destSecret.PubKeyHash));
            Assert.True(tx.IsCoinBase);

            // It is not necessary to sign the transaction as we want the scriptSig left untouched and (2 < size < 100) bytes to pass the coinbase size check.
            var state = new MempoolValidationState(false);
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);

            // Tests PreMempoolChecks context.Transaction.IsCoinBase
            Assert.False(isSuccess, "Coinbase should not be accepted to mempool.");
            Assert.Equal(MempoolErrors.Coinbase, state.Error);
        }

        [Fact]
        public async void AcceptToMemoryPool_TxIsCoinbaseWithInvalidSize_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();

            // Create a transaction that looks like a coinbase. But the consensus rules that apply to coinbases will reject it anyway.
            // We need two forms of the test because submitting an incorrectly constructed coinbase triggers a consensus rule instead
            // of the coinbase being rejected by the mempool for being a coinbase.
            // TODO: Instead of creating multiple versions of all such tests we should perhaps find a way of testing the applicable rules in the mempool simultaneously.
            tx.AddInput(new TxIn(new OutPoint(new uint256(0), uint.MaxValue), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.CENT * 11), destSecret.PubKeyHash));
            tx.Sign(KnownNetworks.RegTest, minerSecret, false);
            Assert.True(tx.IsCoinBase);

            var state = new MempoolValidationState(false);
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);

            // Tests PreMempoolChecks invokes CheckPowTransactionRule and is enforced for mempool transactions.
            Assert.False(isSuccess, "Coinbase with incorrect size should not be accepted to mempool.");
        }

        [Fact]
        public async void AcceptToMemoryPool_TxIsCoinstake_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();

            // Create a transaction that looks like a coinstake.
            tx.AddInput(new TxIn()
            {
                PrevOut = new OutPoint(new uint256(15), 1),
                ScriptSig = new Script()
            });
            tx.AddOutput(new TxOut(Money.Zero, (IDestination)null));
            tx.AddOutput(new TxOut(Money.Zero, (IDestination)null));
            Assert.True(tx.IsCoinStake);

            var state = new MempoolValidationState(false);
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);

            // Tests PreMempoolChecks context.Transaction.IsCoinStake
            Assert.False(isSuccess, "Coinstake should not be accepted to mempool.");
            Assert.Equal(MempoolErrors.Coinstake, state.Error);
        }

        [Fact]
        public async void AcceptToMemoryPool_TxIsNonStandardVersionUnsupported_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            // Have to be on mainnet for this or the RequireStandard flag is not set in the mempool settings.
            Network network = KnownNetworks.Main;
            var minerSecret = new BitcoinSecret(new Key(), network);
            ITestChainContext context = await TestChainFactory.CreateAsync(network, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), network);
            var tx = new Transaction();
            tx.AddInput(new TxIn(new OutPoint(context.SrcTxs[0].GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.Satoshis(1)), destSecret.PubKeyHash));
            tx.Version = 0;
            tx.Sign(network, minerSecret, false);

            var state = new MempoolValidationState(false);

            // Tests the version number case in PreMempoolChecks CheckStandardTransaction (version too low)
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.False(isSuccess, "Transaction with version too low should not have been accepted.");
            Assert.Equal(MempoolErrors.Version, state.Error);

            // Can't reuse previous transaction with bumped version due to signing process having side effects.
            tx = new Transaction();
            tx.AddInput(new TxIn(new OutPoint(context.SrcTxs[0].GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.Satoshis(1)), destSecret.PubKeyHash));
            tx.Version = (uint)validator.ConsensusOptions.MaxStandardVersion + 1;
            tx.Sign(network, minerSecret, false);

            state = new MempoolValidationState(false);

            // Tests the version number case in PreMempoolChecks CheckStandardTransaction (version too high)
            isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.False(isSuccess, "Transaction with version too high should not have been accepted.");
            Assert.Equal(MempoolErrors.Version, state.Error);
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxIsNonStandardTransactionSizeInvalid_ReturnsFalse()
        {
            // TODO:Test the cases in PreMempoolChecks CheckStandardTransaction
            // - Check transaction size
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxIsNonStandardInputScriptSigsLengthInvalid_ReturnsFalse()
        {
            // TODO:Test the cases in PreMempoolChecks CheckStandardTransaction
            // - Check input scriptsig lengths
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxIsNonStandardScriptSigIsPushOnly_ReturnsFalse()
        {
            // TODO:Test the cases in PreMempoolChecks CheckStandardTransaction
            // - Check input scriptsig push only
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxIsNonStandardScriptTemplateIsNull_ReturnsFalse()
        {
            // TODO:Test the casses in PreMempoolChecks CheckStandardTransaction
            // - Check output scripttemplate == null
        }

        [Fact]
        public async void AcceptToMemoryPool_TxIsNonStandardOutputIsDust_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            // Have to be on mainnet for this or the RequireStandard flag is not set in the mempool settings.
            Network network = KnownNetworks.Main;
            var minerSecret = new BitcoinSecret(new Key(), network);
            ITestChainContext context = await TestChainFactory.CreateAsync(network, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), network);
            var tx = new Transaction();
            tx.AddInput(new TxIn(new OutPoint(context.SrcTxs[0].GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.Satoshis(1)), destSecret.PubKeyHash));
            tx.Sign(network, minerSecret, false);

            var state = new MempoolValidationState(false);

            // Tests the dust output case in PreMempoolChecks CheckStandardTransaction
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.False(isSuccess, "Transaction with dust output should not have been accepted.");
            Assert.Equal(MempoolErrors.Dust, state.Error);
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxIsNonStandardOutputNotSingleReturn_ReturnsFalse()
        {
            // TODO:Test the cases in PreMempoolChecks CheckStandardTransaction
            // - Checkout output single return
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxFinalCannotMine_ReturnsFalse()
        {
            // TODO: Execute cases in PreMempoolChecks CheckFinalTransaction
        }

        [Fact]
        public async void AcceptToMemoryPool_TxAlreadyExists_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();
            tx.AddInput(new TxIn(new OutPoint(context.SrcTxs[0].GetHash(), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.CENT * 11), destSecret.PubKeyHash));
            tx.Sign(KnownNetworks.RegTest, minerSecret, false);

            var state = new MempoolValidationState(false);
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.True(isSuccess, "Transaction should have been accepted but was not.");

            // This tests the method this.memPool.Exists(context.TransactionHash)
            isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.False(isSuccess, "Transaction should not have been accepted a second time to the mempool.");
            Assert.Equal(MempoolErrors.InPool, state.Error);
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxAlreadyHaveCoins_ReturnsFalse()
        {
            // TODO: Execute this case CheckMempoolCoinView context.View.HaveCoins(context.TransactionHash)
        }

        [Fact]
        public async void AcceptToMemoryPool_TxMissingInputs_ReturnsFalseAsync()
        {
            string dataDir = GetTestDirectoryPath(this);

            var minerSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, minerSecret.PubKey.Hash.ScriptPubKey, dataDir);
            IMempoolValidator validator = context.MempoolValidator;
            Assert.NotNull(validator);

            var destSecret = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var tx = new Transaction();

            // Nonexistent input.
            tx.AddInput(new TxIn(new OutPoint(new uint256(57), 0), PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(minerSecret.PubKey)));
            tx.AddOutput(new TxOut(new Money(Money.CENT * 11), destSecret.PubKeyHash));
            tx.Sign(KnownNetworks.RegTest, minerSecret, false);

            var state = new MempoolValidationState(false);

            // CheckMempoolCoinView !context.View.HaveCoins(txin.PrevOut.Hash)
            bool isSuccess = await validator.AcceptToMemoryPool(state, tx);
            Assert.False(isSuccess, "Transaction with invalid input should not have been accepted.");
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxInputsAreBad_ReturnsFalse()
        {
            // TODO: Execute this case CheckMempoolCoinView !context.View.HaveInputs(context.Transaction)
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_NonBIP68CanMine_ReturnsFalse()
        {
            // TODO: Execute this case CreateMempoolEntry !CheckSequenceLocks(this.chain.Tip, context, PowCoinViewRule.StandardLocktimeVerifyFlags, context.LockPoints)
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_NonStandardP2SH_ReturnsFalse()
        {
            // TODO: Execute failure cases for CreateMempoolEntry AreInputsStandard
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_NonStandardP2WSH_ReturnsFalse()
        {
            // TODO: Execute failure cases for P2WSH Transactions CreateMempoolEntry
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxExcessiveSigOps_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckSigOps
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxFeeInvalidLessThanMin_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckFee
            // - less than minimum fee
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxFeeInvalidInsufficentPriorityForFree_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckFee
            // - Insufficient priority for free transaction
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxFeeInvalidAbsurdlyHighFee_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckFee
            // - Absurdly high fee
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxTooManyAncestors_ReturnsFalse()
        {
            // TODO: Execute failure cases for CheckAncestors
            // - Too many ancestors
        }

        [Fact]
        public async void AcceptToMemoryPool_TxAncestorsConflictSpend_ReturnsFalseAsync()
        {
            // TODO: Execute failure cases for CheckAncestors
            // - conflicting spend transaction

            string dataDir = GetTestDirectoryPath(this);

            var miner = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            ITestChainContext context = await TestChainFactory.CreateAsync(KnownNetworks.RegTest, miner.PubKey.Hash.ScriptPubKey, dataDir).ConfigureAwait(false);
            IMempoolValidator validator = context.MempoolValidator;
            var bob = new BitcoinSecret(new Key(), KnownNetworks.RegTest);
            var txBuilder = new TransactionBuilder(KnownNetworks.RegTest);

            //Create Coin from first tx on chain
            var coin = new Coin(context.SrcTxs[0].GetHash(), 0, context.SrcTxs[0].TotalOut, PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(miner.PubKey));

            //Send 10 to Bob and return the rest as change to miner
            Transaction originalTx = txBuilder
               .AddCoins(coin)
               .AddKeys(miner)
               .Send(bob, "10.00")
               .SendFees("0.001")
               .SetChange(miner)
               .BuildTransaction(true);
            var state = new MempoolValidationState(false);

            //Mempool should accept it, there's nothing wrong
            Assert.True(await validator.AcceptToMemoryPool(state, originalTx).ConfigureAwait(false), $"Transaction: {nameof(originalTx)} failed mempool validation.");

            //Create second transaction spending the same coin
            Transaction conflictingTx = txBuilder
               .AddCoins(coin)
               .AddKeys(miner)
               .Send(bob, "10.00")
               .SendFees("0.001")
               .SetChange(miner)
               .BuildTransaction(true);

            //Mempool should reject the second transaction
            Assert.False(await validator.AcceptToMemoryPool(state, conflictingTx).ConfigureAwait(false), $"Transaction: {nameof(conflictingTx)} should have failed mempool validation.");

            Directory.Delete(dataDir, true);
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxReplacementInsufficientFees_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckReplacement InsufficientFees
            // - three separate logic checks inside CheckReplacement
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxReplacementTooManyReplacements_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckReplacement TooManyPotentialReplacements
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxReplacementAddsUnconfirmed_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckReplacement ReplacementAddsUnconfirmed
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxPowConsensusCheckInputNegativeOrOverflow_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs PowCoinViewRule.CheckInputs BadTransactionInputValueOutOfRange
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxPowConsensusCheckInputBadTransactionInBelowOut_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs PowCoinViewRule.CheckInputs BadTransactionInBelowOut
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxPowConsensusCheckInputNegativeFee_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs PowCoinViewRule.CheckInputs NegativeFee
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxPowConsensusCheckInputFeeOutOfRange_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs PowCoinViewRule.CheckInputs FeeOutOfRange
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxVerifyStandardScriptConsensusFailure_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs VerifyScriptConsensus for ScriptVerify.Standard
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxContextVerifyStandardScriptFailure_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs ctx.VerifyScript for ScriptVerify.Standard
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxVerifyP2SHScriptConsensusFailure_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs VerifyScriptConsensus for ScriptVerify.P2SH
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_TxContextVerifyP2SHScriptFailure_ReturnsFalse()
        {
            // TODO: Execute failure case for CheckAllInputs CheckInputs ctx.VerifyScript for ScriptVerify.P2SH
        }

        [Fact(Skip = "Not implemented yet.")]
        public void AcceptToMemoryPool_MemPoolFull_ReturnsFalse()
        {
            // TODO: Execute failure case for this check after trimming mempool !this.memPool.Exists(context.TransactionHash)
        }
    }
}
