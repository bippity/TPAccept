using System;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace TPAccept
{
    [ApiVersion(2, 1)]
    public class Plugin : TerrariaPlugin
    {
        public RequestManager Requests = new RequestManager();

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

            ServerApi.Hooks.GameUpdate.Register(this, Requests.Update);
            ServerApi.Hooks.NetGreetPlayer.Register(this, Requests.OnGreet);  // doing from memory so i dont know if this is actually the name of the hook
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
                        Requests.AddRequest(args.Player, target);

						args.Player.SendSuccessMessage($"Sent teleport request to: {target.Name}. They have 10 seconds to accept or deny.");
					}
				}
			}
			// syntax valid for accepting request
			if (args.Parameters.Count == 0)
            {
                var request = Requests.Check(args.Player.Index);

				if (request != null)
				{
                    Requests.AcceptRequest(args.Player.Index);
                }
				else 
		        {
					args.Player.SendErrorMessage("Nobody currently requests to teleport to you. '/tpa (player)' to send a request to another player.");
                }
            }
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

    public class RequestingPlayer
    {
        public int Target { get; private set; }
        public int Requester { get; private set; }
        public string TargetName { get; private set; }
        public string RequesterName { get; private set; }

        public int Duration { get; private set; } = 10 * 60;  // 10 seconds * 60 fps for 6000 ticks

        public bool DecrementDuration()
        {
            Duration--;

            return Duration <= 0;
        }

        public RequestingPlayer(int target, int requester)
        {
            Target = target;
            Requester = requester;

            var t = TShock.Players[target];
            var p = TShock.Players[requester];

            TargetName = t?.Name ?? "unknown";
            RequesterName = p?.Name ?? "unknown";
        }

        public TSPlayer GetTarget => TShock.Players[Target];

        public TSPlayer GetRequester => TShock.Players[Requester];
    }

    public class RequestManager
    {
        private RequestingPlayer[] RequestsByPlayers = new RequestingPlayer[256];

        public void AddRequest(TSPlayer target, TSPlayer requester)
        {
            if (target >= 256 || target < 0)
            {
                return;
            }

            RequestsByPlayers[target] = new RequestingPlayer(target, requester);

            Requested(target, requester);
        }

        public void OnGreet(GreetPlayerEventArgs args)
        {
            // just to make sure the slot is nulled so a new player doesnt
            // take an old request that could still be active
            RequestsByPlayers[args.whoAmI] = null; 
        }

        public void Update()
        {
            for (int i = 0; i < 256; i++)
            {
                var rq = RequestsByPlayers[i];

                if (rq != null)
                {
                    if (rq.DecrementDuration())
                    {
                        DurationPassed(rq);
                        RequestsByPlayers[i] = null;
                    }
                }
            }
        }

        public RequestingPlayer Check(int index)
        {
            if (index < 0 || index >= 256)
            {
                return null;
            }

            return RequestsByPlayers[index];
        }

        public void AcceptRequest(int index)
        {
            if (index < 0 || index >= 256)
            {
                return;
            }

            var rq = RequestsByPlayers[index];

            var t = rq.GetTarget;
            var p = rq.GetRequester;

            if (t != null)
            {
                if (p == null)
                {
                    rq.GetTarget.SendErrorMessage($"Could not find player \"{rq.RequesterName}\" to teleport.");
                    return;
                }

                Teleport(p, t);

                rq.GetTarget.SendSuccessMessage($"Successfully teleported {rq.RequesterName} to you.");

                RequestsByPlayers[index] = null;
            }
        }

        // i realize this is currently not used by anything but you could implement a deny subcmd and use this method the same way as AcceptRequest()
        public void Deny(int index)
        {
            if (index < 0 || index >= 256)
            {
                return;
            }

            var rq = RequestsByPlayers[index];

            var t = rq.GetTarget;
            var p = rq.GetRequester;

            rq.GetRequester.SendErrorMessage($"{rq.TargetName} has denied your teleport request.");

            RequestsByPlayers[index] = null;
        }

        private void Teleport(TSPlayer player, TSPlayer target)
        {
            player.Teleport(target.TPlayer.position.X, target.TPlayer.position.Y);
        }

        private void Requested(TSPlayer target, TSPlayer requester)
        { 
            target.SendInfoMessage($"{requester.Name} has requested to teleport to you. Type '/tpa' to accept. Ignore to deny.");
        }

        private void DurationPassed(RequestingPlayer request)
        {
            var player = request.GetRequester;

            if (player != null)
            {
                player.SendErrorMessage($"{request.TargetName} has not accepted your request in time.");
            }
        }
    }
}
