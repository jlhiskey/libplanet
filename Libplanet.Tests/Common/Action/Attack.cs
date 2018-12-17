using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Libplanet.Action;

namespace Libplanet.Tests.Common.Action
{
    [ActionType("attack")]
    public class Attack : BaseAction
    {
        public override IImmutableDictionary<string, object> PlainValue =>
            new Dictionary<string, object>()
            {
                { "weapon", Weapon },
                { "target", Target },
            }.ToImmutableDictionary();

        public string Weapon { get; set; }

        public string Target { get; set; }

        public override void LoadPlainValue(
            IImmutableDictionary<string, object> plainValue)
        {
            Weapon = Encoding.UTF8.GetString((byte[])plainValue["weapon"]);
            Target = Encoding.UTF8.GetString((byte[])plainValue["target"]);
        }

        public override AddressStateMap Execute(Address from, Address to, AddressStateMap states)
        {
            var result = (BattleResult)states.GetValueOrDefault(to);

            if (result == null)
            {
                result = new BattleResult();
            }

            result.UsedWeapons.Add(Weapon);
            result.Targets.Add(Target);

            return (AddressStateMap)states.SetItem(to, result);
        }
    }
}