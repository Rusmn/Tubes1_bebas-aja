using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class FirstBot : Bot
{
    bool peek; // Don't turn if there's a bot there
    double moveAmount; // How much to move

    // The main method starts our bot
    static void Main()
    {
        new FirstBot().Start();
    }

    // Constructor, which loads the bot config file
    FirstBot() : base(BotInfo.FromFile("FirstBot.json")) { }

    // Called when a new round is started -> initialize and do some movement
    public override void Run()
    {
        //bot color
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0xFF);   // Putih
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); // Merah
        RadarColor = Color.FromArgb(0x64, 0xA7, 0xFF);  // Biru Muda
        BulletColor = Color.FromArgb(0xFF, 0xFF, 0x64); // Kuning
        ScanColor = Color.FromArgb(0x90, 0xEE, 0x90);   // Hijau Muda

        // Initialize moveAmount to the maximum possible for the arena
        moveAmount = Math.Max(ArenaWidth, ArenaHeight);
        // Initialize peek to false
        peek = false;

        // turn to face a wall.
        // `Direction % 90` means the remainder of Direction divided by 90.
        TurnRight(Direction % 90);
        Forward(moveAmount);

        // Turn the gun to turn right 90 degrees.
        peek = true;
        TurnGunRight(90);
        TurnRight(90);

        // Main loop
        while (IsRunning)
        {
            // Peek before we turn when forward() completes.
            peek = true;
            // Move up the wall
            Forward(moveAmount);
            // Don't peek now
            peek = false;
            // Turn to the next wall
            TurnRight(90);
        }
    }

    // We hit another bot -> move away a bit
    public override void OnHitBot(HitBotEvent e)
    {
        // If he's in front of us, set back up a bit.
        var bearing = BearingTo(e.X, e.Y);
        if (bearing > -90 && bearing < 90)
        {
            Back(100);
        }
        else
        { // else he's in back of us, so set ahead a bit.
            Forward(100);
        }
    }

    // We scanned another bot -> fire!
    public override void OnScannedBot(ScannedBotEvent e)
    {
        SetFire(2);
        // Note that scan is called automatically when the bot is turning.
        // By calling it manually here, we make sure we generate another scan event if there's a bot
        // on the next wall, so that we do not start moving up it until it's gone.
        if (peek)
            Rescan();
    }
}