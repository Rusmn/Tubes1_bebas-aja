using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

//Greedy by come closer then fire
public class BigBoss : Bot
{
    static void Main(string[] args)
    {
        new BigBoss().Start();
    }

    BigBoss() : base(BotInfo.FromFile("BigBoss.json")) { }

    private double targetX = -1;
    private double targetY = -1;

    public override void Run()
    {
        BodyColor = Color.Black;
        TurretColor = Color.Red;
        RadarColor = Color.Yellow;
        BulletColor = Color.Black;
        ScanColor = Color.Gray;
        TracksColor = Color.Green;
        GunColor = Color.White;

        //Bantu aim
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
                // Tembak, scan lagi
                Fire(3);
                Rescan();

                //Kalo di-scan kosong, set target kosong
                targetX = -1;
                targetY = -1;
            }
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        targetX = e.X;
        targetY = e.Y;
        double distance = DistanceTo(e.X, e.Y);
        Console.WriteLine("Scanned X: ",e.X);
        

        // if (distance <= 300) {

            TurnRate = MaxTurnRate; 
            TurnTo(BearingTo(targetX,targetY));
            Go();

            TargetSpeed = MaxSpeed;
            SetForward(distance - 50);
            Go();
            // Forward(distance - 50);
            // AdjustRadarForGunTurn = false;

        // }
        // else {
        //     targetX = -1;
        //     targetY = -1;
        //     Resume();
        // }
    }

    public override void OnHitBot(HitBotEvent e)
    {
        // TurnGunRight(10);
        Rescan();
    }

    // Kalo ditembak, bales
    public override void OnHitByBullet(HitByBulletEvent e)
    {
        targetX = X;
        targetY = Y;

        BulletState bullet = e.Bullet;
        double shooterDirection = (bullet.Direction + 180) % 360; 

        // Ngadep ke arah peluru ditembakin
        TurnRate = MaxTurnRate; 
        TurnTo(shooterDirection);
        Go();

        // double distance = DistanceTo(bullet.X,bullet.Y);
        TargetSpeed = MaxSpeed;
        SetForward(100);
        Go();
    }



    public override void OnHitWall(HitWallEvent e)
    {
        Back(50);
        TurnRight(180);
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
