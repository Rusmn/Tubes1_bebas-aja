using System;
using System.Drawing;
using System.Collections.Generic;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;

//heuristic : jarak musuh dan jarak ke dinding
//objektif : keberlangsungan hidup (karena berada pada zona aman), kekuatan tembakan (bonus) 

public class Position{
    public double X { get; set; }
    public double Y { get; set; }
    public Position(double x, double y){
        X = x;
        Y = y;
    }
}

public class FirstBot : Bot{
    private const double wallDist = 150; // wall distance -> jarak aman dari dinding
    private const double enemyDist = 300; //enemy distance -> jarak aman dari musuh
    
    private Position currentSafezone = null; //current Safe Zone
    private long cekZone = 0; //last safe zone check
    private bool needMove = true; //need to movesafe zone
    private bool movingForward = true;
    private bool stopOnScan = false; //Stop when see enemy
    private int gunDir = 1; //gun Direction
    private double enemyX = -1; //last enemy X
    private double enemyY = -1; //last enemy Y
    private long scanTime = -1; //last enemy seen
    
    static void Main(){
        new FirstBot().Start();
    }

    FirstBot() : base(BotInfo.FromFile("FirstBot.json")) { }

    public override void Run(){
        // Set warna bot
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0xFF);   // Putih
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); // Merah
        GunColor = Color.FromArgb(0x00, 0x00, 0x00);    // Hitam
        RadarColor = Color.FromArgb(0x64, 0xA7, 0xFF);  // Biru Muda
        BulletColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        ScanColor = Color.FromArgb(0x90, 0xEE, 0x90);   // Hijau Muda
        
        // Loop utama
        while (IsRunning){
            if (needMove || TurnNumber - cekZone > 50){
                currentSafezone = FindZone();
                cekZone = TurnNumber;
                needMove = false;

                GoToZone();
            }
            
            TurnGunRight(10 * gunDir);
            
            // Ubah arah putaran senjata secara berkala
            if (TurnNumber % 20 == 0){
                gunDir *= -1;
            }
            
            // Jika tidak melihat musuh untuk beberapa waktu, scan 360
            if (TurnNumber - scanTime > 10 && TurnNumber % 30 == 0){
                for (int i = 0; i < 36; i++){
                    TurnGunRight(10);
                }
            }
        }
    }
    private List<Position> GenPos(){ //generate Position
        List<Position> candidates = new List<Position>();
        
        for (double x = wallDist; x < ArenaWidth - wallDist; x += 100){
            for (double y = wallDist; y < ArenaHeight - wallDist; y += 100){
                candidates.Add(new Position(x, y));
            }
        }
        
        return candidates;
    }
    
    private bool IsSafe(Position pos){
        double distanceToWall = Math.Min(Math.Min(pos.X, ArenaWidth - pos.X), Math.Min(pos.Y, ArenaHeight - pos.Y));
        
        if (distanceToWall < wallDist){
            return false; // Terlalu dekat dengan dinding
        }
        
        if (scanTime > 0 && TurnNumber - scanTime < 30){
            double distanceToEnemy = Math.Sqrt(Math.Pow(pos.X - enemyX, 2) + Math.Pow(pos.Y - enemyY, 2));
            
            if (distanceToEnemy < enemyDist){
                return false; // Terlalu dekat dengan musuh
            }
        }

        return true; // Posisi aman
    }
    
    private double CalcScore(Position pos){ //calculate safety score (skor yang dimiliki pos)
        double score = 100; // Skor minimum/dasar

        double distanceToWall = Math.Min(Math.Min(pos.X, ArenaWidth - pos.X), Math.Min(pos.Y, ArenaHeight - pos.Y)
        );
        
        score += distanceToWall * 2;
        
        if (scanTime > 0 && TurnNumber - scanTime < 30){
            double distanceToEnemy = Math.Sqrt(Math.Pow(pos.X - enemyX, 2) + Math.Pow(pos.Y - enemyY, 2));
            
            score += distanceToEnemy * 0.5;
        }
        
        double distanceToCenter = Math.Sqrt(Math.Pow(pos.X - ArenaWidth/2, 2) + Math.Pow(pos.Y - ArenaHeight/2, 2));
        
        score += distanceToCenter * 0.5;
        
        double distanceFromCurrent = Math.Sqrt(Math.Pow(pos.X - X, 2) + Math.Pow(pos.Y - Y, 2));
        
        if (distanceFromCurrent > 300){
            score -= (distanceFromCurrent - 300) * 0.2;
        }
        
        return score;
    }
    
    private Position SelectBest(List<Position> candidates){ //memilih best pos
        Position bestPosition = null;
        double bestScore = double.MinValue;
        
        foreach (Position pos in candidates){
            if (IsSafe(pos)){
                double score = CalcScore(pos);
                
                if (score > bestScore){
                    bestScore = score;
                    bestPosition = pos;
                }
            }
        }
        
        if (bestPosition == null){
            bestPosition = FindCorner();
        }
        
        return bestPosition;
    }
    
    private Position FindZone(){ //mencari safe zone
        List<Position> candidates = GenPos();
        return SelectBest(candidates);
    }
    
    private Position FindCorner(){
        Position[] corners = new Position[4];
        corners[0] = new Position(wallDist, wallDist);
        corners[1] = new Position(ArenaWidth - wallDist, wallDist);
        corners[2] = new Position(wallDist, ArenaHeight - wallDist);
        corners[3] = new Position(ArenaWidth - wallDist, ArenaHeight - wallDist);
        
        if (scanTime > 0 && TurnNumber - scanTime < 30){
            Position bestCorner = corners[0];
            double maxDistance = 0;
            
            for (int i = 0; i < 4; i++){
                double distance = Math.Sqrt(Math.Pow(corners[i].X - enemyX, 2) + Math.Pow(corners[i].Y - enemyY, 2));
                
                if (distance > maxDistance){
                    maxDistance = distance;
                    bestCorner = corners[i];
                }
            }
            
            return bestCorner;
        }
        
        return corners[new Random().Next(4)];
    }
    
    private void GoToZone(){
        if (currentSafezone == null) return;
        
        stopOnScan = false;
        
        double bearing = BearingTo(currentSafezone.X, currentSafezone.Y);
        TurnRight(bearing);
        
        stopOnScan = true;
        
        double distance = DistanceTo(currentSafezone.X, currentSafezone.Y);
        Forward(distance);
        
        TurnGunRight(90);
    }
    
    public override void OnScannedBot(ScannedBotEvent e){
        enemyX = e.X;
        enemyY = e.Y;
        scanTime = TurnNumber;
        
        double distance = DistanceTo(e.X, e.Y);
        
        if (stopOnScan){
            Stop();
            Attack(distance);
            Rescan();
            Resume();
        }
        else{
            Attack(distance);
        }
        
        if (distance < enemyDist){
            needMove = true;
        }
    }

    private void Attack(double distance){ //perhatikan lagi batas energi yang baik.
        if (distance > 200 || Energy < 15)
            Fire(1);
        else if (distance > 50)
            Fire(2);
        else
            Fire(3);
    }
    
    public override void OnHitByBullet(HitByBulletEvent e){
        needMove = true;
        double bulletBearing = CalcBearing(e.Bullet.Direction);
        TurnRight(90 - bulletBearing);
        Forward(100);
    }
    
    public override void OnHitWall(HitWallEvent e){
        if (movingForward){
            SetBack(100);
            movingForward = false;
        } 
        else{
            SetForward(100);
            movingForward = true;
        }
        
        needMove = true;
    }
    
    public override void OnHitBot(HitBotEvent e){
        TurnGunRight(GunBearingTo(e.X, e.Y));
        Fire(3);
        
        Back(50);
        
        needMove = true;
    }

    public override void OnBulletHit(BulletHitEvent e){
        if (scanTime > 0 && TurnNumber - scanTime < 5){
            double gunBearing = GunBearingTo(enemyX, enemyY);
            TurnGunRight(gunBearing);
            Attack(DistanceTo(enemyX, enemyY));
        }
    }
}