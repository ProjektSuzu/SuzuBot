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

        internal List<BaseChain> Add(string text)
        {
            Chains.Add(new TextChain(text));
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

        public IEnumerator<BaseChain> GetEnumerator() => Chains.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
