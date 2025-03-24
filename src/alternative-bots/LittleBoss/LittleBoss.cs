using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

// Aggressive revenge ram bot
public class LittleBoss : Bot
{
    static void Main(string[] args)
    {
        new LittleBoss().Start();
    }

    LittleBoss() : base(BotInfo.FromFile("LittleBoss.json")) { }

    private double targetX = -1;
    private double targetY = -1;

    public override void Run()
    {
        BodyColor = Color.Yellow;
        TurretColor = Color.Red;
        RadarColor = Color.Black;
        BulletColor = Color.Black;
        ScanColor = Color.Gray;
        TracksColor = Color.Green;
        GunColor = Color.White;

        SetFireAssist(true);

        while (IsRunning)
        {
            AdjustRadarForGunTurn = false;
            if (targetX == -1 || targetY == -1) // Kalo gaada target
            {
                //scan
                SetTurnRight(10);
                SetRescan();
                Go();
            }
            else // Kalo ada target
            {
                // scan lagi
                // SetBack(200);
                // Console.Write("Mundur!");
                SetRescan();
                Go();

                //Kalo di-scan kosong, set target kosong
                targetX = -1;
                targetY = -1;
            }
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {

        double distance = DistanceTo(e.X, e.Y);
        Console.WriteLine($"Scanned X: {e.X}, Y: {e.Y}, Distance: {distance}");

        if (e.Energy < 70)
        {
            targetX = e.X;
            targetY = e.Y;
            Console.WriteLine("Target energy is low, charging!");
            RamTarget(targetX, targetY, distance);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        Console.WriteLine("REVENGE MODE ACTIVATED!");
        


        if (e.Energy < 70)
        {
            targetX = e.Bullet.X;
            targetY = e.Bullet.Y;
            Console.WriteLine("Target energy is low, charging!");
            // Charge at the shooter for revenge
            RamTarget(targetX, targetY, DistanceTo(e.Bullet.X, e.Bullet.Y));
        } else {
            Forward(100);
        }


    }

    public override void OnHitBot(HitBotEvent e)
    {
        Console.WriteLine("Hit bot successfully!");
        SetRescan();
        Go();
    }

    public override void OnHitWall(HitWallEvent e)
    {
        SetBack(50);
        SetTurnRight(90);
        Go();
    }

    private void RamTarget(double x, double y, double distance)
    {
        TurnRate = MaxTurnRate; 
        TurnTo(BearingTo(x,y));
        Go();

        TargetSpeed = MaxSpeed;
        SetForward(distance);
        Go();
    }

    private void TurnTo(double angle)
    {
        double turnAngle = angle - Direction;
        if (turnAngle > 180)
            turnAngle -= 360;
        if (turnAngle < -180)
            turnAngle += 360;

        SetTurnLeft(turnAngle);
    }
}