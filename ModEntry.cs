using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace FightingWithNPCs
{
    /// <summary>Hit Lewis, Pierre or Pam with a scythe and they end up in the hospital.
    /// Reconstructed from the 1.2.0 DLL (2020) and ported to Stardew Valley 1.6 / SMAPI 4.</summary>
    public class ModEntry : Mod
    {
        private const string SpriteAsset = "LooseSprites/FightingWithNPCs";
        private const string HospitalName = "Hospital";

        // Fight victims, in the same order as the three lists below.
        private static readonly string[] Victims = { "Lewis", "Pierre", "Pam" };

        // Where each victim is placed inside the hospital (pixel coordinates).
        private static readonly Vector2[] HospitalPositions =
        {
            new Vector2(928, 280),
            new Vector2(1308, 288),
            new Vector2(1184, 792)
        };

        // Source rectangles of the "knocked out" sprite in assets/images.png (x, y, width, height).
        private static readonly Rectangle[] SpriteRects =
        {
            new Rectangle(0, 0, 16, 31),
            new Rectangle(16, 0, 16, 31),
            new Rectangle(32, 0, 16, 31)
        };

        // Item dropped by each victim: Lucky Purple Shorts, Broken Glasses, Beer.
        // 1.6 uses string item IDs; (O) = object. The numbers are the same as before.
        private static readonly string[] DropItemIds = { "(O)789", "(O)170", "(O)346" };

        private readonly Random random = new Random();

        public override void Entry(IModHelper helper)
        {
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        }

        /// <summary>Replaces the old IAssetLoader: serves assets/images.png as LooseSprites/FightingWithNPCs.</summary>
        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo(SpriteAsset))
                e.LoadFromModFile<Texture2D>("assets/images.png", AssetLoadPriority.Medium);
        }

        private static bool IsScythe(Tool tool)
        {
            if (tool == null)
                return false;
            return tool.Name == "Scythe" || tool.Name == "Golden Scythe" || tool.Name == "Iridium Scythe";
        }

        private string RandomLine(string keyPrefix)
        {
            // keys are provocation_text_1..11 / repentance_text_1..11
            int n = this.random.Next(1, 12);
            return this.Helper.Translation.Get($"{keyPrefix}_{n}");
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button != SButton.MouseLeft && e.Button != SButton.C)
                return;

            Farmer player = Game1.player;
            if (!IsScythe(player.CurrentTool))
                return;

            GameLocation here = Game1.currentLocation;
            Vector2 playerPos = new Vector2((int)player.Position.X, (int)player.Position.Y);

            for (int i = 0; i < Victims.Length; i++)
            {
                NPC npc = Game1.getCharacterFromName(Victims[i], true);
                if (npc == null || npc.currentLocation == null)
                    continue;

                Vector2 npcPos = new Vector2((int)npc.Position.X, (int)npc.Position.Y);
                bool sameLocation = here.Name == npc.currentLocation.Name;

                // Taunt (or moan, if they're already in the hospital) whenever you swing near them.
                if (sameLocation)
                {
                    string line = here.Name != HospitalName
                        ? this.RandomLine("provocation_text")
                        : this.RandomLine("repentance_text");
                    npc.showTextAboveHead(line);
                }

                bool inRange =
                    here.Name != HospitalName
                    && sameLocation
                    && playerPos.X >= npcPos.X - 100 && playerPos.X <= npcPos.X + 100
                    && playerPos.Y >= npcPos.Y - 100 && playerPos.Y <= npcPos.Y + 100;

                if (!inRange)
                    continue;

                this.SendToHospital(npc, i, here, npcPos);
            }
        }

        private void SendToHospital(NPC npc, int index, GameLocation here, Vector2 npcPos)
        {
            Farmer player = Game1.player;

            // Stop whatever the NPC was doing and pull them off their schedule for today.
            npc.controller = null;
            npc.temporaryController = null;
            npc.ignoreScheduleToday = true;
            npc.Schedule = null; // 1.6: clearSchedule() no longer exists; Schedule is a settable property
            npc.followSchedule = false;
            npc.Halt();

            GameLocation hospital = Game1.getLocationFromName(HospitalName);
            Game1.warpCharacter(npc, hospital, new Vector2(1f, 1f));
            npc.Position = HospitalPositions[index];
            npc.playSleepingAnimation();

            // Swing animation + quack.
            player.CurrentTool.beginUsing(here, 0, 0, player);
            Game1.playSound("Duck");

            // The "knocked out" sprite flies off where the NPC was standing.
            Rectangle rect = SpriteRects[index];
            var sprite = new TemporaryAnimatedSprite(
                SpriteAsset,
                rect,
                9999f, 1, 99999,
                new Vector2(rect.Height, rect.Width) * 64f,
                false, false,
                1f, 0f,
                Color.White,
                4f, 0f, 0f, 0f,
                false)
            {
                position = new Vector2(npcPos.X, npcPos.Y - 27f),
                motion = new Vector2(4f, -8f),
                rotationChange = 0.19634955f,
                shakeIntensity = 1f
            };
            here.TemporarySprites.Add(sprite);

            Game1.showGlobalMessage(this.Helper.Translation.Get("hospital_hud_message", new { character_name = npc.Name }));
            player.doEmote(12);
            player.Money += 1000;

            // Victim drops their item toward the player.
            Item drop = ItemRegistry.Create(DropItemIds[index]);
            here.debris.Add(new Debris(drop, npcPos, player.getStandingPosition()));

            player.changeFriendship(-250, npc);
        }
    }
}
