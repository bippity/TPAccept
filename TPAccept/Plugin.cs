using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TPAccept
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
		// time before the request is ignored (indexed) (this should be 10 seconds)
		public Timer[] _AcceptCounter;

		// player who sent the request (indexed)
		public TSPlayer _RequestPlayer;

		// player who recieves request (indexed)
		public TSPlayer _TargetPlayer;

		// start of plugin
		public Plugin(Main game) : base(game) { }
		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}
		public override string Author
		{
			get { return "Rozen4334"; }
		}
		public override string Name
		{
			get { return "TPAccept"; }
		}
		public override string Description
		{
			get { return "A accepting and denying feature for teleports in TShock for Terraria, replacing TPAllow."; }
		}

		public override void Initialize()
        {
			Commands.ChatCommands.Add(new Command("tpa.use", TPA, "tpa"));
        }

		// start of command
		private void TPA(CommandArgs args)
        {
			// syntax entirely invalid
			if (args.Parameters.Count >= 2)
            {
				args.Player.SendErrorMessage("Invalid usage. Correct usage: '/tpa (player)'"); return;
            }
			// syntax valid for sending request
			if (args.Parameters.Count == 1)
			{
				// find the player
				var players = TSPlayer.FindByNameOrID(args.Parameters[0]);
				if (players.Count == 0)
					args.Player.SendErrorMessage("Invalid player!");
				else if (players.Count > 1)
					args.Player.SendMultipleMatchError(players.Select(p => p.Name));
				else
				{
					// making use of tpallow so people dont get requests.
					// making use of tpoverride so that it can still be applied.
					var target = players[0];
					if (!target.TPAllow && !args.Player.HasPermission(Permissions.tpoverride))
					{
						args.Player.SendErrorMessage("{0} has disabled players from teleporting.", target.Name); return;
					}
					else
					{
						args.Player.SendSuccessMessage($"Sent teleport request to: {target.Name}. They have 10 seconds to accept or deny.");
					}
				}
			}
			// syntax valid for accepting request
			if (args.Parameters.Count == 0)
            {
				if (_RequestPlayer is sending request to _TargetPlayer)
				{
					TeleportToTarget(_RequestPlayer, _TargetPlayer);
					args.Player.SendSuccessMessage($"Successfully teleported {_RequestPlayer.Name} to you.");
                }
				if (_RequestPlayer is null)
                {
					args.Player.SendErrorMessage("Nobody currently requests to teleport to you. '/tpa (player)' to send a request to another player."); return;
                }
            }
		}
		
		// seperate void to teleport, makes calling this easier
		private void TeleportToTarget(TSPlayer player, TSPlayer target)
        {
			player.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y);
        }

		// seperate info bubble to handle requested and (probably) invoke the timer?
		private void Requested(TSPlayer player, TSPlayer target)
        {
			target.SendInfoMessage($"{player.Name} has requested to teleport to you. Type '/tpa' to accept. Ignore to deny.");
        }

		// timer has passed, probably using timer.elapsed
		private void TimerPassed(TSPlayer player, TSPlayer target)
        {
			player.SendErrorMessage($"{target.Name} has not accepted your request in time.");
        }
	}
}
