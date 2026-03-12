using System.Linq;

namespace TheJazMaster.EnemyPack.Actions;

public class ARemoveAnnoyances : CardAction
{
    public bool accepted;

    public override void Begin(G g, State s, Combat c)
	{
		foreach (int uuid in s.deck.OfType<TrashAnnoyance>().Select(c => c.uuid).ToList())
			s.RemoveCardFromWhereverItIs(uuid);
	}        
}