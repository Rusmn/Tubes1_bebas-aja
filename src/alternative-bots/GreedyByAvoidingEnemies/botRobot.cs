using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

public class botRobot : Bot
{
    bool maju;
    public static void Main(string[] args)
    {
        new botRobot().Start();
    }

    public botRobot() : base(BotInfo.FromFile("botRobot.json")) { }

    public override void Run()
    {
        MaxSpeed  = 6;
        SetColors();
        maju = true;

        while (IsRunning)
        {
            // TurnGunRight(30);
            
            TurnGunRight(10);
            if (maju){
                SetForward(40000);
                SetTurnLeft(50);
                SetTurnRight(100);
            }
            // maju = true;

            // Go();
            // SetTurnRight(90);
            // Forward(200);
        }
    }

    public override void OnScannedBot(ScannedBotEvent e)
    {
        var distance = DistanceTo(e.X, e.Y);
        //1v1
        // if(EnemyCount == 1){
        //     TurnGunLeft(GunBearingTo(e.X, e.Y));
        //     Fire(1);
        // }

        // Slowing down kalo gaada musuh di jarak 500, let em fight sambil tembak kalo musuh lambat
        if (distance > 550) {
            MaxSpeed = 2;
            if(e.Speed < 3){
                TurnGunLeft(GunBearingTo(e.X, e.Y));
                Fire(1);
            } 
        }

        // Kalo ada di jarak 400, tembak ke dia, agak perlambat
        else if (distance < 450) {
            MaxSpeed = 4;
            if(e.Speed < 4){
                TurnGunLeft(GunBearingTo(e.X, e.Y));
                Fire(1);
            } 
        }
        else {
            // Warning musuh deket!
            if (distance <= 300){
                TurnGunLeft(GunBearingTo(e.X, e.Y));

                // Kalo deket banget, tembak pake 3
                if(distance < 200)  {
                    Fire(3);
                }
                // antara 200-300 tembak pake 2
                else {
                    Fire(2);
                }
                
                // kalo energi nya lebih dikit slowing down biar lebih akurat
                if(e.Energy < Energy){
                    MaxSpeed = 3;
                    // jarak dibawah 100, belok ngindar berdasar arah lawan, ga jauh jauh biar bisa lanjut nembak
                    if (distance < 100){
                        SetTurnRight(90 - BearingTo(e.X, e.Y));
                    }
                    // lanjut naikin kecepatan buat reposisi
                    MaxSpeed = 6;
                }
                // kalo energi lawan lebih gede
                else if (e.Energy > Energy ){
                    // cabutt, ngindar lebih jauh
                    MaxSpeed = 10;
                    SetTurnLeft(120 - BearingTo(e.X, e.Y));
                    // Go();
                }
            }
        }
    }


    private void SetColors()
    {
        BodyColor = Color.FromArgb(0xFA, 0x80, 0x00);
        TurretColor = Color.FromArgb(0xFF, 0xA0, 0x7A);
        RadarColor = Color.FromArgb(0xCD, 0x5C, 0x22);
        BulletColor = Color.FromArgb(0xEB, 0x00, 0xFB);
        ScanColor = Color.FromArgb(0xFD, 0xED, 0xEC);
        TracksColor = Color.FromArgb(0x64, 0x1E, 0x16);
        GunColor = Color.FromArgb(0xD3, 0x54, 0x00);
    }

    public void ReverseDirection()
    {
        if (maju)
        {
            SetBack(40000);
            maju = false;
        }
        else
        {
            SetForward(40000);
            maju = true;
        }
    }
    public override void OnHitWall(HitWallEvent e)
    {
        // Nabrak tembok oi
        MaxSpeed = 9;
        TurnLeft(10);
        ReverseDirection();
    }
    public override void OnHitBot(HitBotEvent e)
    {
        // Nabrak orang! cabutt
        MaxSpeed = 10;
        if (e.IsRammed)
        {
            ReverseDirection();
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e)
    {
        // Ketembakk!! ngebutt!
        MaxSpeed = 8;
        // Ngadep ke arah peluru ditembakin
        SetTurnLeft((e.Bullet.Direction + 180) % 360);
        Go();
    }
}
