using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.DB;

namespace History.Commands
{
	public class InfoCommand : HCommand
	{
		private string account;
		private int radius;
		private int time;

		public InfoCommand(string account, int time, int radius, TSPlayer sender)
			: base(sender)
		{
			this.account = account;
			this.time = time;
			this.radius = radius;
		}

		public override void Execute()
		{
			var actions = new List<Action>();
			int lookupTime = (int)(DateTime.UtcNow - History.Date).TotalSeconds - time;

			int plrX = sender.TileX;
			int plrY = sender.TileY;
			int lowX = plrX - radius;
			int highX = plrX + radius;
			int lowY = plrY - radius;
			int highY = plrY + radius;
			string XYReq = string.Format("XY / 65536 BETWEEN {0} AND {1} AND XY & 65535 BETWEEN {2} AND {3}", lowX, highX, lowY, highY);

			using (QueryResult reader =
				History.Database.QueryReader("SELECT Action, XY FROM History WHERE Account = @0 AND Time >= @1 AND " + XYReq + " AND WorldID = @2",
				account, lookupTime, Main.worldID))
			{
				while (reader.Read())
				{
					actions.Add(new Action
					{
						action = (byte)reader.Get<int>("Action"),
						x = reader.Get<int>("XY") >> 16,
						y = reader.Get<int>("XY") & 0xffff,
					});
				}
			}

			for (int i = 0; i >= 0 && i < History.Actions.Count; i++)
			{
				Action action = History.Actions[i];
				if (action.account == account && action.time >= lookupTime &&
					lowX <= action.x && lowY <= action.y && action.x <= highX && action.y <= highY)
				{
					actions.Add(action);
				}
			}
			// 0 actions escape
			if (actions.Count == 0)
			{
				sender.SendInfoMessage("[History] {0} não teve ações nesta área.", account);
				return;
			}

			// Done
			List<Action> TilePlaced = new List<Action>(), TileDestroyed = new List<Action>(), TileModified = new List<Action>(),
						WallPlaced = new List<Action>(), WallDestroyed = new List<Action>(), WallModified = new List<Action>(),
						WirePlaced = new List<Action>(), WireDestroyed = new List<Action>(), WireModified = new List<Action>(),
						ActuatorPlaced = new List<Action>(), ActuatorDestroyed = new List<Action>(), ActuatorModified = new List<Action>(),
						Painted = new List<Action>(), SignModified = new List<Action>(), Other = new List<Action>();
			foreach (Action action in actions)
			{
				switch (action.action)
				{
					case 0:
					case 4:
						if (TileDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (TilePlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							TileModified.Add(action);
							TilePlaced.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						TileDestroyed.Add(action);
						break;
					case 1:
						if (TilePlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (TileDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							TileModified.Add(action);
							TileDestroyed.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						TilePlaced.Add(action);
						break;
					case 2:
						if (WallDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (WallPlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							WallModified.Add(action);
							WallPlaced.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						WallDestroyed.Add(action);
						break;
					case 3:
						if (WallPlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (WallDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							WallModified.Add(action);
							WallDestroyed.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						WallPlaced.Add(action);
						break;
					case 5:
					case 10:
					case 12:
					case 16:
						WirePlaced.Add(action);
						break;
					case 6:
					case 11:
					case 13:
					case 17:
						WireDestroyed.Add(action);
						break;
					case 7:
					case 14:
						TileModified.Add(action); //slope/pound
						break;
					case 8:
						if (ActuatorPlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (ActuatorDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							ActuatorModified.Add(action);
							ActuatorDestroyed.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						ActuatorPlaced.Add(action);
						break;
					case 9:
						if (ActuatorDestroyed.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						if (ActuatorPlaced.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							ActuatorModified.Add(action);
							ActuatorPlaced.RemoveAll(act => act.x.Equals(action.x) && act.y.Equals(action.y));
							break;
						}
						ActuatorDestroyed.Add(action);
						break;
					case 25:
					case 26:
						if (Painted.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						Painted.Add(action);
						break;
					case 27:
						if (SignModified.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						SignModified.Add(action);
						break;
					default:
						if (Other.Exists(act => act.x.Equals(action.x) && act.y.Equals(action.y)))
						{
							break;
						}
						Other.Add(action);
						break;
				}
			}
			actions.Clear();
			StringBuilder InfoPrep = new StringBuilder();
			InfoPrep.Append(account);

			if (TileModified.Count > 0) InfoPrep.Append(" modificou " + TileModified.Count + " bloco" + (TileModified.Count == 1 ? "" : "s") + ",");
			if (TileDestroyed.Count > 0) InfoPrep.Append(" removeu " + TileDestroyed.Count + " bloco" + (TileDestroyed.Count == 1 ? "" : "s") + ",");
			if (TilePlaced.Count > 0) InfoPrep.Append(" colocou " + TilePlaced.Count + " bloco" + (TilePlaced.Count == 1 ? "" : "s") + ",");

			if (WallModified.Count > 0) InfoPrep.Append(" modificou " + WallModified.Count + " parede" + (WallModified.Count == 1 ? "" : "s") + ",");
			if (WallDestroyed.Count > 0) InfoPrep.Append(" removeu " + WallDestroyed.Count + " parede" + (WallDestroyed.Count == 1 ? "" : "s") + ",");
			if (WallPlaced.Count > 0) InfoPrep.Append(" colocou " + WallPlaced.Count + " parede" + (WallPlaced.Count == 1 ? "" : "s") + ",");

			if (WireModified.Count > 0) InfoPrep.Append(" modificou " + WireModified.Count + " fio" + (WireModified.Count == 1 ? "" : "s") + ",");
			if (WireDestroyed.Count > 0) InfoPrep.Append(" removeu " + WireDestroyed.Count + " fio" + (WireDestroyed.Count == 1 ? "" : "s") + ",");
			if (WirePlaced.Count > 0) InfoPrep.Append(" colocou " + WirePlaced.Count + " fio" + (WirePlaced.Count == 1 ? "" : "s") + ",");

			if (ActuatorModified.Count > 0) InfoPrep.Append(" modificou " + ActuatorModified.Count + " atuador" + (ActuatorModified.Count == 1 ? "" : "es") + ",");
			if (ActuatorDestroyed.Count > 0) InfoPrep.Append(" removeu " + ActuatorDestroyed.Count + " atuador" + (ActuatorDestroyed.Count == 1 ? "" : "es") + ",");
			if (ActuatorPlaced.Count > 0) InfoPrep.Append(" colocou " + ActuatorPlaced.Count + " atuador" + (ActuatorPlaced.Count == 1 ? "" : "s") + ",");

			if (Painted.Count > 0) InfoPrep.Append(" pintou " + Painted.Count + " tile" + (Painted.Count == 1 ? "" : "s") + "/wall" + (Painted.Count == 1 ? "" : "s") + ",");
			if (SignModified.Count > 0) InfoPrep.Append(" modificou " + SignModified.Count + " sign" + (SignModified.Count == 1 ? "" : "s") + ",");
			if (Other.Count > 0) InfoPrep.Append(" " + Other.Count + " outra " + (Other.Count == 1 ? "edição" : "edições") + ",");

			InfoPrep.Length--;
			InfoPrep.Append(".");
			sender.SendInfoMessage(InfoPrep.ToString());
		}
	}
}