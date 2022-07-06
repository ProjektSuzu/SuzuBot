using RinBot.Core.Component.Message.Model;
using System.Collections;

namespace RinBot.Core.Component.Message
{
    internal class RinMessageChain : IEnumerable<BaseChain>
    {
        internal List<BaseChain> Chains { get; }

        internal RinMessageChain()
        {
            Chains = new();
        }

        internal void Add(BaseChain chain)
            => Chains.Add(chain);

        internal void Add(IEnumerable<BaseChain> chains)
            => Chains.AddRange(chains);


        internal RinMessageChain(params BaseChain[] chain)
        {
            Chains = new List<BaseChain>(chain.Where(i => i != null));
        }

        public IEnumerator<BaseChain> GetEnumerator() => Chains.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
