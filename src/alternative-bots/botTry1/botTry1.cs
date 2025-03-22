using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class botTry1 : Bot
{
    int gunDirection = 1;

    static void Main(string[] args)
    {
        new botTry1().Start();
    }

    private readonly Random random = new Random();
    int turnCounter;

    botTry1() : base(BotInfo.FromFile("botTry1.json")) { }

    public override void Run()
    {
        // Atur warna bot
        BodyColor = Color.FromArgb(0xFA, 0x80, 0x72);
        TurretColor = Color.FromArgb(0xFF, 0xA0, 0x7A);
        RadarColor = Color.FromArgb(0xCD, 0x5C, 0x5C);
        BulletColor = Color.FromArgb(0xFF, 0x11, 0x11);
        ScanColor = Color.FromArgb(0xFD, 0xED, 0xEC);
        TracksColor = Color.FromArgb(0x64, 0x1E, 0x16);
        GunColor = Color.FromArgb(0xD3, 0x54, 0x00);

        // GunTurnRate = 15; // Putar turret untuk mencari musuh
        turnCounter = 0;

        // GunTurnRate = 10;

			TurnRate = 0;
        while (IsRunning)
        {
            TurnGunLeft(30);
            // int angkaAcak = random.Next(30, 250); // Angka acak antara 50-150 untuk menghindari gerakan terlalu kecil
            // Forward(angkaAcak);
            // TurnGunRight(20);
            if (turnCounter % 70 == 0) {
				// Straighten out, if we were hit by a bullet (ends turning)

                // Go forward with a target speed of 4
				TargetSpeed = 5;
                // Forward(80);
			}
			if (turnCounter % 70 == 34) {
				// Go backwards, faster
                // TargetSpeed = -5;
                TurnRate = -7;
                // Back(60);
			}
             if (turnCounter % 70 == 35) {
				// Straighten out, if we were hit by a bullet (ends turning)

                // Go forward with a target speed of 4
				TargetSpeed = -7;
                // Forward(80);
			}
            if (turnCounter % 7 == 0) TurnRate = 1;
            TurnRate += 0.2;
			turnCounter++;
			Go(); // execute turn


            // TurnRight(random.Next(30, 91)); // Putar antara 30-90 derajat agar lebih dinamis
            // Back(angkaAcak);
            // Go();
            // MoveInPattern();
            // Go();
            // Rescan();
        }
    }

    // private void AdjustDistance(double enemyDistance)
    // {
    //     Console.WriteLine("Adjust Distance");

    //     if (enemyDistance < 300 - 50)
    //     {
    //         TargetSpeed = -4; // Mundur
    //     }
    //     else if (enemyDistance > 300 + 50)
    //     {
    //         TargetSpeed = 4; // Maju
    //     }
    //     else
    //     {
    //         TargetSpeed = 0; // Tetap diam jika dalam jarak aman
    //     }
    //     Go();
    // }
    // private void MoveInPattern()
    // {

    //     int pattern = random.Next(2); // 0 = persegi, 1 = angka 8
    //     // TurnGunRight(30);

    //     if (pattern == 0)
    //     {
    //     Console.WriteLine("Move in Persegi");
    //         // Pola Persegi
    //         TargetSpeed = 5;
    //         // TargetSpeed = 6;
    //         // TurnRate = 15;
    //         // TargetSpeed = -6;
    //         // TurnRate = -15;
        
    //         // Forward(70);
    //         TurnLeft(45);
    //         // Forward(70);
    //         // TurnLeft(45);
    //         // Forward(70);
    //         // TurnLeft(45);
    //     }
    //     else
    //     {
    //         Console.WriteLine("Move in melingkar");

    //         // Pola Angka 8
    //         // TargetSpeed = 6;
    //         // TurnRate = 15;
    //         TargetSpeed = 6;
    //         // TurnRate = -15;
    //         // Forward(70);
    //         TurnLeft(45);
    //         // Forward(70);
    //         // TurnLeft(45);
    //         // Forward(70);
    //         // TurnLeft(45);
    //     }
    //     // Go();
    // }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        // TurnToFaceTarget(e.X, e.Y);
        Console.WriteLine($"Enemy spotted at ({e.X}, {e.Y})");
        var distance = DistanceTo(e.X, e.Y);
        
        Stop(); // Hentikan sementara

        // AdjustDistance(distance);
        // var bearingFromGun = GunBearingTo(e.X, e.Y);
        // // Turn the gun toward the scanned bot
        TurnGunRight(GunBearingTo(e.X, e.Y));
        // GunTurnRate *= -1;

        // TurnGunLeft(10);
        SmartFire(distance);

        // TurnGunRight(30 * gunDirection);
        Resume(); // Lanjutkan pergerakan
        // Rescan();
    }

    private void SmartFire(double distance)
    {
        Console.WriteLine("Smart Fire");

        if (distance > 250 || Energy < 15)
            Fire(1);
        else if (distance > 70)
            Fire(2);
        else
            Fire(3);
    }

    // public override void OnHitBot(HitBotEvent e)
    // {
    //     Console.WriteLine($"Collided with bot at ({e.X}, {e.Y})");
    //     Back(70);
    //     TurnRight(20);
    //     Rescan();
    // }

    public override void OnHitWall(HitWallEvent e)
    {
        Console.WriteLine("Hit a wall! Reversing...");
        Back(150);
    }
    
    public override void OnHitByBullet(HitByBulletEvent evt)
    {
        // Calculate the bearing to the direction of the bullet
        var bearing = CalcBearing(evt.Bullet.Direction);

        // // Turn 90 degrees to the bullet direction based on the bearing
        TurnLeft(bearing);
        TurnGunRight(bearing);
        SmartFire(100);
        // Back(120);
        // Rescan();
        // MoveInPattern();

        // TargetSpeed = 0;
        // TurnRight(60);
        Back(100);
        Rescan();

    }

    // private void TurnToFaceTarget(double x, double y)
    // {
    //     Console.WriteLine("Face Turned");

    //     var bearing = BearingTo(x, y);
    //     if (bearing >= 0)
    //         gunDirection = 1;
    //     else
    //         gunDirection = -1;

    //     TurnGunLeft(bearing);
    // }
}
