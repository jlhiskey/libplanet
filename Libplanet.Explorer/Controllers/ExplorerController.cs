using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Blocks;
using Libplanet.Explorer.Interfaces;
using Libplanet.Explorer.ViewModels;
using Libplanet.Tx;
using Microsoft.AspNetCore.Mvc;

namespace Libplanet.Explorer.Controllers
{
    [GenericControllerNameConvention]
    public class ExplorerController<T> : Controller where T : IAction
    {
        private readonly IBlockchainStore Store;

        public string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        public ExplorerController(IBlockchainStore store)
        {
            Store = store;
        }

        public BlockChain<T> GetBlockChain()
        {
            // FIXME: policy should be configurable
            var chain = new BlockChain<T>(new BlockPolicy<T>(), Store.Store);

            return chain;
        }

        [HttpGet("/blocks/")]
        public List<Dictionary<string, string>> Index()
        {
            BlockChain<T> chain = GetBlockChain();

            return chain.Select(block => new Dictionary<string, string>
                {
                    { "hash", block.Hash.ToString() },
                    { "timestamp", block.Timestamp.ToString(TimestampFormat) }
                })
                .ToList();
        }

        [HttpGet("/blocks/{hash}/")]
        public IActionResult getBlock(string hash)
        {
            Block<T> block;
            HashDigest<SHA256> blockHash;
            BlockChain<T> chain = GetBlockChain();

            try
            {
                blockHash = HashDigest<SHA256>.FromString(hash);
            }
            catch (ArgumentException)
            {
                return BadRequest(new Dictionary<string, string>
                    {
                        { "message", $"\"{hash}\" is not a proper hash." }
                    });
            }

            try
            {
                block = chain.Blocks[blockHash];
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new Dictionary<string, string>
                {
                    { "message", $"block(\"{hash}\") is not found" }
                });
            }

            var model = new BlockViewModel
            {
                Index = block.Index,
                Difficulty = block.Difficulty,
                Nonce = block.Nonce.ToString(),
                PreviousHash = block.PreviousHash.ToString(),
                RewardBeneficiary = block.RewardBeneficiary?.ToHex(),
                Timestamp = block.Timestamp.ToString(TimestampFormat),
                TxIds = (block.Transactions
                    .OrderByDescending(tx => tx.Timestamp)
                    .Select(tx => new Dictionary<string, string>
                    {
                        { "id", tx.Id.ToString() },
                        { "timestamp", tx.Timestamp.ToString(TimestampFormat) }
                    })).ToList()
            };
            return Ok(model);
        }

        [HttpGet("/tx/{txIdString}/")]
        public IActionResult getTransaction(string txIdString)
        {
            Transaction<T> tx;
            TxId txId;
            BlockChain<T> chain = GetBlockChain();

            try
            {
                txId = new TxId(ByteUtil.ParseHex(txIdString));
            }
            catch (ArgumentException)
            {
                return BadRequest(new Dictionary<string, string>
                    {
                        {
                            "message",
                            $"\"{txIdString}\" is not a proper transaction id."
                        }
                    });
            }

            try
            {
                tx = chain.Transactions[txId];
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new Dictionary<string, string>
                {
                    { "message", $"Transaction(\"{txIdString}\") is not found" }
                });
            }

            var model = new TransactionViewModel
            {
                Id = tx.Id.ToString(),
                Signature = tx.Signature,
                Timestamp = tx.Timestamp,
                Sender = tx.Sender.ToHex(),
                Recipient = tx.Recipient.ToHex(),
                Actions = tx.Actions
                    .Select(act => new Dictionary<string, object>
                    {
                        {
                            "type_id",
                            ActionTypeAttribute.ValueOf(act.GetType())
                        },

                    }).ToList()
            };

            return Ok(model);
        }

        [HttpGet("/address/{addressId}/")]
        public IActionResult getAddress(string addressId)
        {
            Address address;
            IEnumerable<Transaction<T>> txs;
            BlockChain<T> chain = GetBlockChain();

            try
            {
                address = new Address(addressId);
            }
            catch (ArgumentException)
            {
                return BadRequest(new Dictionary<string, string>
                {
                    {
                        "message",
                        $"\"{addressId}\" is not a proper address."
                    }
                });
            }

            AddressStateMap state = chain.GetStates(
                new HashSet<Address> {address});

            try
            {
                txs = chain.Addresses[address];
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new Dictionary<string, string>
                {
                    { "message", $"Address(\"{addressId}\") is not found" }
                });
            }
            var model = new AddressViewModel
            {
                Tx = (
                    txs.OrderByDescending(tx => tx.Timestamp)
                    .Select(
                        tx => new Dictionary<string, string>
                        {
                            { "id", tx.Id.ToString() },
                            {
                                "timestamp",
                                tx.Timestamp.ToString(TimestampFormat)
                            }
                        })).ToList(),
                State = state
            };

            return Ok(model);
        }
    }
}
