# TPAccept

A plugin for TShock for Terraria, inheriting the concept of Minecrafts' multiplayer TP feature. 
This plugin replaces the base TP command and has permissions for ignoring TPAllow & the TPAllow flag itself parsed.

Permission: tpa.use

Command: 
/tpa (player)
- This sends a request to a player
/tpa
- This accepts a pending request, or gives you a warning if you do not have any requests pending.

A request will be denied once 10 seconds have passed. Both target and executing user are notified of this.
