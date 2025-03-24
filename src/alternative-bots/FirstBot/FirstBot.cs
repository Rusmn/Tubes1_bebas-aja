using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

#nullable enable

//heuristic : jarak musuh dan jarak ke dinding
//objektif : keberlangsungan hidup (karena berada pada zona aman), kekuatan tembakan (bonus) 

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
    private const double wallDist = 10; // wall distance -> jarak aman dari dinding
    private const double enemyDist = 100; //enemy distance -> jarak aman dari musuh
    
    private Position? currentSafezone = null; //current Safe Zone
    private long cekZone = 0; //last safe zone check
    private bool needMove = true; //need to move to safe zone
    private int gunDir = 1; //gun Direction
    private double enemyX = -1; //last enemy X
    private double enemyY = -1; //last enemy Y
    private long scanTime = -1; //last enemy seen
    private Random rand = new Random();
    
    static void Main() {
        new FirstBot().Start();
    }

    FirstBot() : base(BotInfo.FromFile("FirstBot.json")) { }

    public override void Run() {
        // Set warna bot
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0xFF);   // Putih
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); // Merah
        GunColor = Color.FromArgb(0x00, 0x00, 0x00);    // Hitam
        RadarColor = Color.FromArgb(0x64, 0xA7, 0xFF);  // Biru Muda
        BulletColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        ScanColor = Color.FromArgb(0x90, 0xEE, 0x90);   // Hijau Muda
        
        // Loop utama
        while (IsRunning) {
            if (needMove) {
                currentSafezone = FindZone();
                cekZone = TurnNumber;
                needMove = false;

                GoToZone();
            }
            
            TurnGunRight(10 * gunDir);
            
            //Ubah arah putaran senjata secara berkala
            if (TurnNumber % 11 == 0) { 
                gunDir *= -1;
            }
            
            // Jika tidak melihat musuh untuk beberapa waktu, scan 360
            if (TurnNumber - scanTime > 10) {
                for (int i = 0; i < 18; i++) {
                    TurnGunRight(20);
                }
            }
        }
    }

    private List<Position> GenPos() { //generate Position
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
            return false; // Terlalu dekat dengan dinding
        }
        
        if (scanTime > 0 && TurnNumber - scanTime < 30) {
            double distanceToEnemy = Math.Sqrt(Math.Pow(pos.X - enemyX, 2) + Math.Pow(pos.Y - enemyY, 2));
            
            if (distanceToEnemy < enemyDist) {
                return false; // Terlalu dekat dengan musuh
            }
        }

        return true; // Posisi aman
    }
    
    private double CalcScore(Position pos) { //calculate safety score (skor yang dimiliki pos)
        double score = 100; // Skor minimum/dasar

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
    
    private Position? SelectBest(List<Position> candidates) { //memilih best pos
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
    
    private Position? FindZone() { //mencari safe zone
        List<Position> candidates = GenPos();
        return SelectBest(candidates);
    }
        
    private void GoToZone() {
        if (currentSafezone == null) return;

        double bearing = BearingTo(currentSafezone.X, currentSafezone.Y);
        SetTurnRight(bearing);
        Go();
        
        double distance = DistanceTo(currentSafezone.X, currentSafezone.Y);
        
        // Zig-zag movement
        while (distance > 0) {
            double moveDist = Math.Min(50, distance);
            SetForward(moveDist);
            SetTurnRight(rand.Next(-15, 15)); // Random angle untuk menghindari peluru
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
            needMove = true; // Pindah jika musuh terlalu dekat
        }
    }

    private void Attack(ScannedBotEvent e) {
        double bulletPower;
        double distance = Math.Sqrt(Math.Pow(e.X - X, 2) + Math.Pow(e.Y - Y, 2));

        if (e.Speed > 2) { // Jika musuh bergerak cepat, kurangi kekuatan tembakan agar lebih akurat
            bulletPower = Math.Min(3, Energy / 5); 
        } else if (distance < 100) { // Musuh dekat, gunakan tembakan besar
            bulletPower = 3;
        } else { 
            bulletPower = 2; 
        }

        //predict
        double futureX = e.X + Math.Cos(e.Direction) * e.Speed * 10;
        double futureY = e.Y + Math.Sin(e.Direction) * e.Speed * 10;
        double gunBearing = GunBearingTo(futureX, futureY);

        SetTurnGunRight(gunBearing);
        Go();
        Fire(bulletPower);
    }
    
    public override void OnHitByBullet(HitByBulletEvent e) {
        //ngehindar dulu
        double evasionAngle = rand.Next(30, 60); // Sudut menghindar acak
        SetTurnRight(evasionAngle);
        SetForward(100);
        needMove = true;
        Go();
    }

    public override void OnHitWall(HitWallEvent e) {
        SetBack(50);
        SetTurnRight(rand.Next(90, 180)); // Putar secara acak agar tidak terjebak
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