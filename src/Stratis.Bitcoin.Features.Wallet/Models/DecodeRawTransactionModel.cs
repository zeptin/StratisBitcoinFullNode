using System.Collections.Generic;
using NBitcoin;
using Newtonsoft.Json;

namespace Stratis.Bitcoin.Features.Wallet.Models
{
    /// <summary>
    /// </summary>
    public class DecodeRawTransactionModel
    {
        /// <summary>
        /// The transaction in hex format.
        /// </summary>
        [JsonProperty(PropertyName = "rawHex")]
        public string RawHex { get; set; }
    }
}
