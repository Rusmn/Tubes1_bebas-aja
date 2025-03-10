using System;
using System.Drawing;
using Robocode.TankRoyale.BotApi;
using Robocode.TankRoyale.BotApi.Events;
using System.Collections.Generic;

//heuristic : jarak musuh dan jarak ke dinding
//objektif : keberlangsungan hidup (karena berada pada zona aman), kekuatan tembakan (bonus) 

public class EnemyData{
    public int Id { get; set; } // Ubah ke int dari string
    public double X { get; set; }
    public double Y { get; set; }
    public double Energy { get; set; }
    public long LastSeenTime { get; set; }
}

public class Position{
    public double X { get; set; }
    public double Y { get; set; }
    
    public Position(double x, double y){
        X = x;
        Y = y;
    }
}

public class FirstBot : Bot{
    // Dictionary untuk melacak musuh - ubah key menjadi int
    private Dictionary<int, EnemyData> listEnemies;

    static void Main(){
        new FirstBot().Start();
    }

    FirstBot() : base(BotInfo.FromFile("FirstBot.json")) { }

    public override void Run(){
        //bot color
        BodyColor = Color.FromArgb(0xFF, 0xFF, 0xFF);   // Putih
        TurretColor = Color.FromArgb(0xFF, 0x00, 0x00); // Merah
        GunColor = Color.FromArgb(0x00, 0x00, 0x00); // Hitam
        RadarColor = Color.FromArgb(0x64, 0xA7, 0xFF);  // Biru Muda
        BulletColor = Color.FromArgb(0xFF, 0xFF, 0x64); // Kuning
        ScanColor = Color.FromArgb(0x90, 0xEE, 0x90);   // Hijau Muda

        listEnemies = new Dictionary<int, EnemyData>();

        int init = 1;

        // Main loop
        while (IsRunning){

            if(init == 1){
                TurnRight(90);
                TurnLeft(90);
                Position safeZoneInit = FindSafeZone();
                GoToSafeZone(safeZoneInit);
            }

            if (listEnemies.Count > 0){
                if (!IsInSafeZone()) {
                    Position safeZone = FindSafeZone();
                    GoToSafeZone(safeZone);
                } 
                else{
                    OnSafeZone();
                }
            }
        }
        init+=1;
    }

    public override void OnScannedBot(ScannedBotEvent e){
        // Cari Posisi Musuh berdasarkan koordinat
        double distance = DistanceTo(e.X, e.Y);
        
        // Simpan data musuh - ScannedBotId sekarang bertipe int
        listEnemies[e.ScannedBotId] = new EnemyData
        {
            Id = e.ScannedBotId,
            X = e.X,
            Y = e.Y,
            Energy = e.Energy,
            LastSeenTime = TurnNumber
        };
        
        // Serang musuh jika dalam jarak aman, evaluasi jarak
        if (IsInSafeZone() && distance > 100){
            AttEnemy(listEnemies[e.ScannedBotId]);
        }
    }

    public override void OnHitByBullet(HitByBulletEvent e){
        // Jika terkena peluru, pertimbangkan untuk mencari safe zone baru
        Position newSafeZone = FindSafeZone();
        GoToSafeZone(newSafeZone);
    }

    //Fungsi Kandidat
    private Position FindSafeZone(){
        List<Position> possiblePos = new List<Position>();

        double width = ArenaWidth;
        double height = ArenaHeight;

        double distanceFromWall = 100; //jarak minimum dari dinding, agar tidak menabrak dinding

        for(double x = distanceFromWall; x < width - distanceFromWall; x+=50){
            for(double y = distanceFromWall; y < height - distanceFromWall; y+=50){
                possiblePos.Add(new Position(x,y));
            }
        }

        return SelectPos(possiblePos);
    }

    //Fungsi Seleksi
    private Position SelectPos(List<Position> possiblePos){
        Position safePos = null;
        double maxSafeScore = -1;

        foreach(Position pos in possiblePos){

            double safeScore = countSafeScore(pos);

            if(safeScore > maxSafeScore){
                maxSafeScore = safeScore;
                safePos = pos;
            }
        }

        return safePos;
    }

    private double countSafeScore(Position pos){
        double score = 100; //perlu revisi tergantung bagaimana keberhasilan algoritma nantinya

        foreach (var enemyData in listEnemies.Values){

            double disToEnemy = DistanceTo(pos.X, pos.Y);

            score += disToEnemy*0.5; //cek lagi penting mana jauh dari musuh atau jauh dari tembok

            if(disToEnemy < 100){
                score -= 200;
            }
        }

        double disToWall = Math.Min(Math.Min(pos.X, ArenaWidth - pos.X), Math.Min(pos.Y, ArenaHeight - pos.Y));

        score += disToWall * 0.3;
        
        return score;
    }

    private void GoToSafeZone(Position safeZone){
        double bearingToTarget = BearingTo(safeZone.X, safeZone.Y);
        SetTurnRight(bearingToTarget);
        SetForward(DistanceTo(safeZone.X, safeZone.Y));
    }
    
    private void OnSafeZone(){
        TurnRight(90);
        TurnLeft(90);

        if(listEnemies.Count > 0){
            var nearEnemy = FindNearest();

            if(DistanceTo(nearEnemy.X, nearEnemy.Y) < 150){ //butuh revisi tergantung posibilitas kemenangan
                Position newSafeZone = FindSafeZone();
                GoToSafeZone(newSafeZone);
            }
            else{
                AttEnemy(nearEnemy);
            }
        }
    }
    
    // Mencari musuh terdekat
    private EnemyData FindNearest() {
        EnemyData nearest = null;
        double minDistance = Double.PositiveInfinity;
        
        foreach (var enemy in listEnemies.Values) {
            double distance = DistanceTo(enemy.X, enemy.Y);
            if (distance < minDistance) {
                minDistance = distance;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
    
    // Menyerang musuh
    private void AttEnemy(EnemyData enemy) { //skema menembaknya perlu diubah
        // Menghitung bearing ke musuh untuk gun
        double gunBearing = GunBearingTo(enemy.X, enemy.Y);
        
        // Memutar gun ke arah musuh
        SetTurnGunRight(gunBearing);
        
        double distance = DistanceTo(enemy.X, enemy.Y);
        double firePower = Math.Min(3, Math.Max(0.1, 400 / distance)); // Menyesuaikan kekuatan tembakan berdasarkan jarak, evaluasi nanti
        
        Fire(firePower);
    }
    
    // Memeriksa apakah bot berada di safe zone
    private bool IsInSafeZone(){
        // Periksa jarak dari dinding
        double distanceToWall = Math.Min(Math.Min(X, ArenaWidth - X), Math.Min(Y, ArenaHeight - Y));
        
        if (distanceToWall < 150){
            return false; // evaluasi nanti
        }
        
        // Periksa jarak dari semua musuh
        foreach (var enemy in listEnemies.Values) {
            double distanceToEnemy = DistanceTo(enemy.X, enemy.Y);
            if (distanceToEnemy < 150) {
                return false; // evaluasi nanti
            }
        }
        
        return true;
    }
    
    // Fungsi ToRadians dipertahankan karena mungkin diperlukan
    private double ToRadians(double degrees) {
        return degrees * Math.PI / 180.0;
    }
}