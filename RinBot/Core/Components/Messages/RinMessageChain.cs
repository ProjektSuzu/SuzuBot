using RinBot.Core.Components.Messages.Models;
using System.Collections;

namespace RinBot.Core.Components.Messages
{
    internal class RinMessageChain : IEnumerable<BaseChain>
    {
        internal List<BaseChain> Chains { get; }

        internal List<BaseChain> Text(string text)
        {
            Chains.Add(TextChain.Create(text));
            return Chains;
        }

        internal List<BaseChain> Add(BaseChain chain)
        {
            Chains.Add(chain);
            return Chains;
        }

        internal List<BaseChain> Add(IEnumerable<BaseChain> chains)
        {
            Chains.AddRange(chains);
            return Chains;
        }

        internal RinMessageChain(params BaseChain[] chain)
        {
            Chains = new List<BaseChain>(chain.Where(i => i != null));
        }

        public IEnumerator<BaseChain> GetEnumerator()
            => Chains.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
