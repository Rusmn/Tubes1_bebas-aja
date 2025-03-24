using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

#nullable enable

public class Position {
    public double X { get; set; }
    public double Y { get; set; }
    public Position(double x, double y) {
        X = x;
        Y = y;
    }
}

public class FirstBot : Bot {
    private ScannedBotEvent? lastScannedBot;
    private const double wallDist = 10; 
    private const double enemyDist = 100; 
    private Position? currentSafezone = null; 
    private long cekZone = 0; 
    private bool needMove = true; 
    private int gunDir = 1; 
    private double enemyX = -1; 
    private double enemyY = -1; 
    private long scanTime = -1; 
    private Random rand = new Random();
    
    static void Main() {
        new FirstBot().Start();
    }

    FirstBot() : base(BotInfo.FromFile("FirstBot.json")) { }

    public override void Run() {
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0xFF);   
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); 
        GunColor = Color.FromArgb(0x00, 0x00, 0x00);    
        RadarColor = Color.FromArgb(0x64, 0xA7, 0xFF);  
        BulletColor = Color.FromArgb(0x00, 0x00, 0x00); 
        ScanColor = Color.FromArgb(0x90, 0xEE, 0x90);   
        

        while (IsRunning) {
            if (needMove) {
                currentSafezone = FindZone();
                cekZone = TurnNumber;
                needMove = false;

                GoToZone();
            }
            
            TurnGunRight(10 * gunDir);
            
            if (TurnNumber % 11 == 0) { 
                gunDir *= -1;
            }
            
            if (TurnNumber - scanTime > 10) {
                for (int i = 0; i < 18; i++) {
                    TurnGunRight(20);
                }
            }
        }
    }

    private List<Position> GenPos() { 
        List<Position> candidates = new List<Position>();
        
        for (double x = wallDist; x < ArenaWidth - wallDist; x += 20) {
            for (double y = wallDist; y < ArenaHeight - wallDist; y += 20) {
                candidates.Add(new Position(x, y));
            }
        }
        
        return candidates;
    }
    
    private bool IsSafe(Position pos) {
        double distanceToWall = Math.Min(Math.Min(pos.X, ArenaWidth - pos.X), Math.Min(pos.Y, ArenaHeight - pos.Y));
        
        if (distanceToWall < wallDist) {
            return false; 
        }
        
        if (scanTime > 0 && TurnNumber - scanTime < 30) {
            double distanceToEnemy = Math.Sqrt(Math.Pow(pos.X - enemyX, 2) + Math.Pow(pos.Y - enemyY, 2));
            
            if (distanceToEnemy < enemyDist) {
                return false; 
            }
        }

        return true; 
    }
    
    private double CalcScore(Position pos) { 
        double score = 100; 

        double distanceToWall = Math.Min(Math.Min(pos.X, ArenaWidth - pos.X), Math.Min(pos.Y, ArenaHeight - pos.Y));
        score += distanceToWall * 2;
        
        if (scanTime > 0 && TurnNumber - scanTime < 30) {
            double distanceToEnemy = Math.Sqrt(Math.Pow(pos.X - enemyX, 2) + Math.Pow(pos.Y - enemyY, 2));
            score += distanceToEnemy * 0.5;
        }
        
        double distanceToCenter = Math.Sqrt(Math.Pow(pos.X - ArenaWidth/2, 2) + Math.Pow(pos.Y - ArenaHeight/2, 2));
        score += distanceToCenter * 0.5;
        
        double distanceFromCurrent = Math.Sqrt(Math.Pow(pos.X - X, 2) + Math.Pow(pos.Y - Y, 2));
        
        if (distanceFromCurrent > 250) {
            score -= (distanceFromCurrent - 300) * 0.2;
        }
        
        return score;
    }
    
    private Position? SelectBest(List<Position> candidates) { 
        Position? bestPosition = null;
        double bestScore = double.MinValue;
        
        foreach (Position pos in candidates) {
            if (IsSafe(pos)) {
                double score = CalcScore(pos);
                
                if (score > bestScore) {
                    bestScore = score;
                    bestPosition = pos;
                }
            }
        }

        return bestPosition;
    }
    
    private Position? FindZone() { 
        List<Position> candidates = GenPos();
        return SelectBest(candidates);
    }
        
    private void GoToZone() {
        if (currentSafezone == null) return;

        double bearing = BearingTo(currentSafezone.X, currentSafezone.Y);
        SetTurnRight(bearing);
        Go();
        
        double distance = DistanceTo(currentSafezone.X, currentSafezone.Y);
        
        while (distance > 0) {
            double moveDist = Math.Min(50, distance);
            SetForward(moveDist);
            SetTurnRight(rand.Next(-15, 15)); 
            Go();
            distance -= moveDist;
        }
    }
    
    public override void OnScannedBot(ScannedBotEvent e) {
        lastScannedBot = e;
        enemyX = e.X;
        enemyY = e.Y;
        scanTime = TurnNumber;

        double gunBearing = GunBearingTo(e.X, e.Y);
        SetTurnGunRight(gunBearing);
        Go();

        Attack(e);

        double distance = Math.Sqrt(Math.Pow(e.X - X, 2) + Math.Pow(e.Y - Y, 2));

        if (distance <= enemyDist) {
            needMove = true; 
        }
    }

    private void Attack(ScannedBotEvent e) {
        double bulletPower;
        double distance = Math.Sqrt(Math.Pow(e.X - X, 2) + Math.Pow(e.Y - Y, 2));

        if (e.Speed > 2) { 
            bulletPower = Math.Min(3, Energy / 5); 
        } else if (distance < 100) {
            bulletPower = 3;
        } else { 
            bulletPower = 2; 
        }

        double futureX = e.X + Math.Cos(e.Direction) * e.Speed * 10;
        double futureY = e.Y + Math.Sin(e.Direction) * e.Speed * 10;
        double gunBearing = GunBearingTo(futureX, futureY);

        SetTurnGunRight(gunBearing);
        Go();
        Fire(bulletPower);
    }
    
    public override void OnHitByBullet(HitByBulletEvent e) {
        double evasionAngle = rand.Next(30, 60); 
        SetTurnRight(evasionAngle);
        SetForward(100);
        needMove = true;
        Go();
    }

    public override void OnHitWall(HitWallEvent e) {
        SetBack(50);
        SetTurnRight(rand.Next(90, 180)); 
        needMove = true;
        Go();
    }
    
    public override void OnHitBot(HitBotEvent e) {
        if (e.IsRammed) {
            SetBack(rand.Next(50, 150));
            SetTurnRight(rand.Next(30, 90)); 
            Go();
        } 
        else {
            Fire(3);
            SetTurnRight(rand.Next(45, 90));
            SetForward(rand.Next(50, 150));
            Go();
        }
        needMove = true;
    }

    public override void OnBulletHit(BulletHitBotEvent e) {
        if (scanTime > 0 && TurnNumber - scanTime < 5) {
            double gunBearing = GunBearingTo(enemyX, enemyY);
            SetTurnGunRight(gunBearing);
            Go();
            Attack(lastScannedBot);
        }
    }
}