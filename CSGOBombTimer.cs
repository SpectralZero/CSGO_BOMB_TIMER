using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CSGOPngBombTimer
{
    public class BombForm : Form
    {
        private int countdownSeconds = 40;
        private float remainingTime;
        private bool isRunning = false;
        private bool highPrecisionMode = false;
        
        private Timer mainTimer;
        private Timer beepTimer;
        
        private PictureBox bombDisplay;
        private Bitmap bombImage;
        
        private bool isExploding = false;
        private string timerText = "40.0";
        
        // Timer display position on the bomb image
        private Point timerPosition = new Point(68, 47);
        private int backgroundWidth = 77;
        private int backgroundHeight = 20;
        private int fontSize = 14;
        private int backgroundOpacity = 0;
        
        // Sound players
        private SoundPlayer startSoundPlayer;
        private SoundPlayer beepSoundPlayer;
        private SoundPlayer explosionSoundPlayer;
        
        // Track if we've played the start sound
        private bool startSoundPlayed = false;

        // Animation
        private Timer shakeTimer;
        private Point originalPosition;
        private Random rnd = new Random();
        private int shakeIntensity = 0;

        // Settings
        public bool AlwaysOnTop { get; set; } = true;
        public bool ClickThrough { get; set; } = true;
        public bool ShowMilliseconds { get; set; } = true;
        
        public BombForm()
        {
            InitializeComponent();
            LoadBombImage();
            SetupForm();
            SetupTimers();
            SetupSounds();
            PositionTopRight();
            
            // Add double-click to hide
            this.DoubleClick += (s, e) => this.Hide();
            bombDisplay.DoubleClick += (s, e) => this.Hide();
            
            // Add right-click context menu
            CreateContextMenu();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form setup
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.TransparencyKey = Color.Black;
            
            // Bomb display
            bombDisplay = new PictureBox();
            bombDisplay.BackColor = Color.Transparent;
            bombDisplay.Paint += new PaintEventHandler(BombDisplay_Paint);
            bombDisplay.SizeMode = PictureBoxSizeMode.AutoSize;
            
            this.Controls.Add(bombDisplay);
            
            this.ResumeLayout(false);
        }

        private void CreateContextMenu()
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            
            contextMenu.Items.Add("Hide Bomb", null, (s, e) => this.Hide());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Stop Timer", null, (s, e) => this.StopCountdown());
            contextMenu.Items.Add("-");
            
            ToolStripMenuItem topMostItem = new ToolStripMenuItem("Always on Top");
            topMostItem.Checked = this.AlwaysOnTop;
            topMostItem.Click += (s, e) => {
                this.AlwaysOnTop = !this.AlwaysOnTop;
                this.TopMost = this.AlwaysOnTop;
                topMostItem.Checked = this.AlwaysOnTop;
            };
            contextMenu.Items.Add(topMostItem);

            ToolStripMenuItem clickThroughItem = new ToolStripMenuItem("Click Through");
            clickThroughItem.Checked = this.ClickThrough;
            clickThroughItem.Click += (s, e) => {
                this.ClickThrough = !this.ClickThrough;
                clickThroughItem.Checked = this.ClickThrough;
                if (this.ClickThrough)
                    SetWindowClickThrough(this.Handle);
                else
                    SetWindowNormal(this.Handle);
            };
            contextMenu.Items.Add(clickThroughItem);

            ToolStripMenuItem millisecondsItem = new ToolStripMenuItem("Show Milliseconds");
            millisecondsItem.Checked = this.ShowMilliseconds;
            millisecondsItem.Click += (s, e) => {
                this.ShowMilliseconds = !this.ShowMilliseconds;
                millisecondsItem.Checked = this.ShowMilliseconds;
            };
            contextMenu.Items.Add(millisecondsItem);
            
            this.ContextMenuStrip = contextMenu;
            bombDisplay.ContextMenuStrip = contextMenu;
        }
        
        private void LoadBombImage()
        {
            try
            {
                // Try multiple possible image locations
                string[] possiblePaths = {
                    "csgo_bomb.png",
                    "images/csgo_bomb.png",
                    "bomb.png",
                    "images/bomb.png"
                };
                
                string imagePath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        imagePath = path;
                        break;
                    }
                }
                
                if (imagePath != null)
                {
                    bombImage = new Bitmap(imagePath);
                    this.ClientSize = bombImage.Size;
                    bombDisplay.Size = bombImage.Size;
                }
                else
                {
                    CreatePlaceholderBomb();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading bomb image: " + ex.Message);
                CreatePlaceholderBomb();
            }
        }
        
        private void CreatePlaceholderBomb()
        {
            bombImage = new Bitmap(150, 200);
            using (Graphics g = Graphics.FromImage(bombImage))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                
                // Draw bomb body
                using (Brush bombBrush = new LinearGradientBrush(
                    new Point(25, 50), 
                    new Point(125, 110), 
                    Color.FromArgb(60, 60, 60), 
                    Color.FromArgb(100, 100, 100)))
                {
                    g.FillRectangle(bombBrush, 25, 50, 100, 60);
                }
                
                // Draw timer background
                g.FillRectangle(Brushes.Black, timerPosition.X, timerPosition.Y, backgroundWidth, backgroundHeight);
                
                // Draw wires
                using (Pen wirePen = new Pen(Color.Red, 2))
                {
                    g.DrawLine(wirePen, 30, 50, 30, 30);
                    g.DrawLine(wirePen, 50, 50, 50, 30);
                    g.DrawLine(wirePen, 70, 50, 70, 30);
                }
                
                using (Font font = new Font("Arial", 16, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("C4", font, textBrush, timerPosition.X + 15, timerPosition.Y + 2);
                }
            }
            
            this.ClientSize = bombImage.Size;
            bombDisplay.Size = bombImage.Size;
        }
        
        private void SetupForm()
        {
            originalPosition = this.Location;
            if (ClickThrough)
                SetWindowClickThrough(this.Handle);
        }
        
        private void SetupTimers()
        {
            mainTimer = new Timer();
            mainTimer.Interval = 50; // Higher precision for smooth animation
            mainTimer.Tick += (sender, e) => Tick();
            
            beepTimer = new Timer();
            beepTimer.Interval = 1000;
            beepTimer.Tick += (sender, e) => PlayBeepSound();

            shakeTimer = new Timer();
            shakeTimer.Interval = 50;
            shakeTimer.Tick += (sender, e) => UpdateShake();
        }
        
        private void SetupSounds()
        {
            // Initialize sound players
            startSoundPlayer = new SoundPlayer();
            beepSoundPlayer = new SoundPlayer();
            explosionSoundPlayer = new SoundPlayer();
            
            // Try to load external sound files
            try
            {
                string[] startPaths = { "start.wav", "sounds/start.wav", "audio/start.wav" };
                string[] beepPaths = { "beep.wav", "sounds/beep.wav", "audio/beep.wav" };
                string[] explosionPaths = { "explosion.wav", "sounds/explosion.wav", "audio/explosion.wav" };
                
                foreach (string path in startPaths)
                {
                    if (File.Exists(path))
                    {
                        startSoundPlayer.SoundLocation = path;
                        break;
                    }
                }
                
                foreach (string path in beepPaths)
                {
                    if (File.Exists(path))
                    {
                        beepSoundPlayer.SoundLocation = path;
                        break;
                    }
                }
                
                foreach (string path in explosionPaths)
                {
                    if (File.Exists(path))
                    {
                        explosionSoundPlayer.SoundLocation = path;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Sound setup: " + ex.Message);
            }
        }
        
        private void PositionTopRight()
        {
            Screen screen = Screen.PrimaryScreen;
            int x = screen.WorkingArea.Right - this.Width - 10;
            int y = screen.WorkingArea.Top + 10;
            this.Location = new Point(x, y);
            originalPosition = this.Location;
        }

        public void SetPosition(Point position)
        {
            this.Location = position;
            originalPosition = position;
        }
        
        public void StartCountdown(int seconds = 40)
        {
            if (isRunning) return;
            
            countdownSeconds = seconds;
            remainingTime = seconds;
            isRunning = true;
            isExploding = false;
            startSoundPlayed = false;
            highPrecisionMode = false;
            shakeIntensity = 0;
            
            UpdateTimerDisplay();
            
            this.Show();
            this.TopMost = this.AlwaysOnTop;
            
            mainTimer.Start();
            beepTimer.Stop();
            shakeTimer.Stop();
            
            bombDisplay.Invalidate();
            
            // Play start sound immediately
            PlayStartSound();
        }
        
        private void Tick()
        {
            float deltaTime = mainTimer.Interval / 1000.0f;
            remainingTime -= deltaTime;
            
            if (remainingTime <= 0)
            {
                remainingTime = 0;
                Explode();
                return;
            }
            
            // Switch to high precision mode when below 1 minute
            if (!highPrecisionMode && remainingTime < 60)
            {
                highPrecisionMode = true;
                mainTimer.Interval = 10; // Even higher precision for milliseconds
            }
            
            UpdateTimerDisplay();
            
            int remainingSeconds = (int)Math.Ceiling(remainingTime);
            
            // Start beeping in last 10 seconds
            if (remainingSeconds <= 10 && !beepTimer.Enabled)
            {
                beepTimer.Start();
            }
            
            // Stop beeping if we're above 10 seconds
            if (remainingSeconds > 10 && beepTimer.Enabled)
            {
                beepTimer.Stop();
            }
            
            // Start shaking in last 5 seconds
            if (remainingSeconds <= 5 && !shakeTimer.Enabled)
            {
                shakeTimer.Start();
                shakeIntensity = 2;
            }
            
            // Increase shake intensity in last 3 seconds
            if (remainingSeconds <= 3)
            {
                shakeIntensity = 4;
            }
            
            if (remainingSeconds <= 1)
            {
                shakeIntensity = 6;
            }
            
            // Faster beeping in last seconds
            if (remainingSeconds <= 5) beepTimer.Interval = 500;
            if (remainingSeconds <= 3) beepTimer.Interval = 250;
            if (remainingSeconds <= 1) beepTimer.Interval = 125;
            
            bombDisplay.Invalidate();
        }

        private void UpdateShake()
        {
            if (shakeIntensity > 0)
            {
                int shakeX = rnd.Next(-shakeIntensity, shakeIntensity + 1);
                int shakeY = rnd.Next(-shakeIntensity, shakeIntensity + 1);
                this.Location = new Point(originalPosition.X + shakeX, originalPosition.Y + shakeY);
            }
        }
        
        private void UpdateTimerDisplay()
        {
            if (remainingTime >= 3600) // 1 hour or more
            {
                int hours = (int)remainingTime / 3600;
                int minutes = ((int)remainingTime % 3600) / 60;
                int seconds = (int)remainingTime % 60;
                timerText = $"{hours:00}:{minutes:00}:{seconds:00}";
            }
            else if (remainingTime >= 60) // 1 minute or more but less than 1 hour
            {
                int minutes = (int)remainingTime / 60;
                int seconds = (int)remainingTime % 60;
                timerText = $"{minutes:00}:{seconds:00}";
            }
            else if (remainingTime >= 10 || !ShowMilliseconds) // 10 seconds or more, or milliseconds disabled
            {
                timerText = remainingTime.ToString("00.0");
            }
            else // Less than 10 seconds with milliseconds
            {
                timerText = remainingTime.ToString("0.00");
            }
        }
        
        private void PlayStartSound()
        {
            try
            {
                if (!startSoundPlayed)
                {
                    if (startSoundPlayer.SoundLocation != null && File.Exists(startSoundPlayer.SoundLocation))
                    {
                        startSoundPlayer.Play();
                    }
                    else
                    {
                        // Fallback start sound
                        Console.Beep(400, 200);
                        System.Threading.Thread.Sleep(50);
                        Console.Beep(600, 300);
                    }
                    startSoundPlayed = true;
                }
            }
            catch
            {
                Console.Beep(400, 200);
                startSoundPlayed = true;
            }
        }
        
        private void PlayBeepSound()
        {
            try
            {
                int remainingSeconds = (int)Math.Ceiling(remainingTime);
                
                if (remainingSeconds <= 10)
                {
                    if (beepSoundPlayer.SoundLocation != null && File.Exists(beepSoundPlayer.SoundLocation))
                    {
                        beepSoundPlayer.Play();
                    }
                    else
                    {
                        int frequency = remainingSeconds <= 5 ? 800 : 600;
                        int duration = remainingSeconds <= 5 ? 150 : 200;
                        Console.Beep(frequency, duration);
                    }
                }
            }
            catch
            {
                // Silent fallback
            }
        }
        
        private void Explode()
        {
            mainTimer.Stop();
            beepTimer.Stop();
            shakeTimer.Stop();
            isRunning = false;
            isExploding = true;
            
            // Reset position after shaking
            this.Location = originalPosition;
            
            timerText = "BOOM!";
            
            bombDisplay.Invalidate();
            
            PlayExplosionSound();
            ShowFullScreenExplosion();
            
            Timer hideTimer = new Timer();
            hideTimer.Interval = 3000;
            hideTimer.Tick += (sender, e) => {
                hideTimer.Stop();
                this.Hide();
                isExploding = false;
                timerText = countdownSeconds.ToString("00.0");
                startSoundPlayed = false;
                highPrecisionMode = false;
            };
            hideTimer.Start();
        }
        
        private void PlayExplosionSound()
        {
            try
            {
                if (explosionSoundPlayer.SoundLocation != null && File.Exists(explosionSoundPlayer.SoundLocation))
                {
                    explosionSoundPlayer.Play();
                }
                else
                {
                    // Multi-tone explosion sound
                    Console.Beep(100, 500);
                    System.Threading.Thread.Sleep(50);
                    Console.Beep(80, 400);
                    System.Threading.Thread.Sleep(50);
                    Console.Beep(60, 300);
                    System.Threading.Thread.Sleep(50);
                    Console.Beep(40, 600);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Explosion sound error: " + ex.Message);
                Console.Beep(100, 500);
            }
        }
        
        private void ShowFullScreenExplosion()
        {
            try
            {
                // Use simple invocation without separate thread
                this.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        ExplosionForm explosionForm = new ExplosionForm();
                        explosionForm.Show();
                        
                        // Auto-close after animation
                        Timer closeTimer = new Timer();
                        closeTimer.Interval = 2500; // 2.5 seconds
                        closeTimer.Tick += (s, e) =>
                        {
                            closeTimer.Stop();
                            explosionForm.Close();
                            explosionForm.Dispose();
                        };
                        closeTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Explosion error: " + ex.Message);
                        // Fallback beep
                        Console.Beep(200, 500);
                    }
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Explosion invocation error: " + ex.Message);
                Console.Beep(200, 500);
            }
        }
        
        public void StopCountdown()
        {
            mainTimer.Stop();
            beepTimer.Stop();
            shakeTimer.Stop();
            isRunning = false;
            this.Hide();
            this.Location = originalPosition; // Reset position
            timerText = countdownSeconds.ToString("00.0");
            bombDisplay.Invalidate();
            startSoundPlayed = false;
            highPrecisionMode = false;
            shakeIntensity = 0;
        }
        
        public void SetCustomTimeAndStart(int seconds)
        {
            countdownSeconds = seconds;
            StartCountdown(seconds);
        }

        public string GetCurrentTime()
        {
            return timerText;
        }

        public bool IsRunning()
        {
            return isRunning;
        }
        
        private void BombDisplay_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            if (bombImage != null)
            {
                g.DrawImage(bombImage, 0, 0);
            }
            
            DrawTimerOnBomb(g);
        }
        
        private void DrawTimerOnBomb(Graphics g)
        {
            int remainingSeconds = (int)Math.Ceiling(remainingTime);
            
            Color textColor = Color.Lime;
            if (remainingSeconds <= 10) textColor = Color.Yellow;
            if (remainingSeconds <= 5) textColor = Color.Red;
            if (isExploding) textColor = Color.Orange;
            
            // Add glow effect for low time
            if (remainingSeconds <= 5)
            {
                using (Font glowFont = new Font("Consolas", fontSize, FontStyle.Bold))
                using (Brush glowBrush = new SolidBrush(Color.FromArgb(100, textColor)))
                {
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                SizeF textSize = g.MeasureString(timerText, glowFont);
                                float textX = timerPosition.X + (backgroundWidth - textSize.Width) / 2 + i;
                                float textY = timerPosition.Y + (backgroundHeight - textSize.Height) / 2 + j;
                                g.DrawString(timerText, glowFont, glowBrush, textX, textY);
                            }
                        }
                    }
                }
            }
            
            using (Font timerFont = new Font("Consolas", fontSize, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(textColor))
            using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(backgroundOpacity, 0, 0, 0)))
            {
                SizeF textSize = g.MeasureString(timerText, timerFont);
                
                Rectangle textBg = new Rectangle(
                    timerPosition.X, 
                    timerPosition.Y, 
                    backgroundWidth, 
                    backgroundHeight
                );
                g.FillRectangle(backgroundBrush, textBg);
                
                float textX = timerPosition.X + (backgroundWidth - textSize.Width) / 2;
                float textY = timerPosition.Y + (backgroundHeight - textSize.Height) / 2;
                
                g.DrawString(timerText, timerFont, textBrush, textX, textY);
            }
        }
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int LWA_COLORKEY = 0x1;
        
        public void SetWindowClickThrough(IntPtr handle)
        {
            try
            {
                int extendedStyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
                SetLayeredWindowAttributes(handle, 0x00000000, 0, LWA_COLORKEY);
            }
            catch { }
        }

        public void SetWindowNormal(IntPtr handle)
        {
            try
            {
                int extendedStyle = GetWindowLong(handle, GWL_EXSTYLE);
                SetWindowLong(handle, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
            }
            catch { }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            mainTimer?.Stop();
            beepTimer?.Stop();
            shakeTimer?.Stop();
        }
    }
    
    public class ExplosionForm : Form
    {
        private Timer animationTimer;
        private int frame = 0;
        private const int totalFrames = 45;
        private List<Particle> particles = new List<Particle>();
        private Random rnd = new Random();
        private Bitmap explosionImage;
        private bool useImage = false;
        private PictureBox gifBox;

        public ExplosionForm()
        {
            InitializeComponent();
            
            // Try to load explosion image first
            if (File.Exists("explosion.gif") || File.Exists("explosion.png"))
            {
                try
                {
                    string imagePath = File.Exists("explosion.gif") ? "explosion.gif" : "explosion.png";
                    explosionImage = new Bitmap(imagePath);
                    useImage = true;
                    SetupGifAnimation();
                    return; // Exit early if using image
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Image load error: " + ex.Message);
                    useImage = false;
                }
            }
            
            // Fallback to particle explosion
            CreateParticles();
            SetupAnimation();
        }

        private void InitializeComponent()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = Color.Black; // Changed to black
            this.TransparencyKey = Color.Black; // Make black transparent
            this.ShowInTaskbar = false;
            this.Opacity = 0;
            this.DoubleBuffered = true;
        }

        private void SetupGifAnimation()
        {
            try
            {
                gifBox = new PictureBox();
                gifBox.Dock = DockStyle.Fill;
                gifBox.SizeMode = PictureBoxSizeMode.StretchImage; // Stretch to full screen
                gifBox.BackColor = Color.Transparent;
                
                if (File.Exists("explosion.gif"))
                {
                    // For GIF - use Image directly for animation
                    gifBox.Image = Image.FromFile("explosion.gif");
                }
                else if (File.Exists("explosion.png"))
                {
                    // For PNG - use Bitmap
                    gifBox.Image = explosionImage;
                }
                
                this.Controls.Add(gifBox);
                
                // Animation timer for fade effect
                animationTimer = new Timer();
                animationTimer.Interval = 30;
                animationTimer.Tick += (sender, e) => AnimateGifExplosion();
                animationTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("GIF setup error: " + ex.Message);
                // Fallback to particle explosion
                useImage = false;
                CreateParticles();
                SetupAnimation();
            }
        }

        private void AnimateGifExplosion()
        {
            frame++;
            
            // Fade in and out
            if (frame <= totalFrames / 3)
            {
                this.Opacity = (double)frame / (totalFrames / 3);
            }
            else if (frame <= totalFrames * 2 / 3)
            {
                this.Opacity = 1.0;
            }
            else
            {
                this.Opacity = 1.0 - (double)(frame - totalFrames * 2 / 3) / (totalFrames / 3);
            }

            if (frame >= totalFrames)
            {
                animationTimer.Stop();
                this.Close();
            }
        }

        private void CreateParticles()
        {
            int centerX = Screen.PrimaryScreen.Bounds.Width / 2;
            int centerY = Screen.PrimaryScreen.Bounds.Height / 2;

            // Create particles for explosion effect
            for (int i = 0; i < 80; i++) // Increased count for better effect
            {
                float angle = (float)(rnd.NextDouble() * Math.PI * 2);
                float speed = (float)(rnd.NextDouble() * 8 + 3);
                int life = rnd.Next(20, 50);
                
                particles.Add(new Particle
                {
                    X = centerX,
                    Y = centerY,
                    SpeedX = (float)(Math.Cos(angle) * speed),
                    SpeedY = (float)(Math.Sin(angle) * speed),
                    Life = life,
                    MaxLife = life,
                    Size = rnd.Next(3, 8),
                    Color = GetExplosionColor(rnd.Next(100))
                });
            }
        }

        private Color GetExplosionColor(int seed)
        {
            switch (seed % 4)
            {
                case 0: return Color.Orange;
                case 1: return Color.Yellow;
                case 2: return Color.Red;
                case 3: return Color.OrangeRed;
                default: return Color.Orange;
            }
        }

        private void SetupAnimation()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 30;
            animationTimer.Tick += (sender, e) => AnimateParticleExplosion();
            animationTimer.Start();
        }

        private void AnimateParticleExplosion()
        {
            frame++;
            
            // Update particles
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var particle = particles[i];
                particle.X += particle.SpeedX;
                particle.Y += particle.SpeedY;
                particle.SpeedX *= 0.97f; // Slow down gradually
                particle.SpeedY *= 0.97f;
                particle.Life--;
                
                if (particle.Life <= 0)
                {
                    particles.RemoveAt(i);
                }
            }

            // Fade effect
            if (frame <= totalFrames / 2)
            {
                this.Opacity = (double)frame / (totalFrames / 2);
            }
            else
            {
                this.Opacity = 1.0 - (double)(frame - totalFrames / 2) / (totalFrames / 2);
            }

            this.Invalidate();

            if (frame >= totalFrames)
            {
                animationTimer.Stop();
                this.Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Only paint for particle explosion
            if (!useImage)
            {
                base.OnPaint(e);
                DrawParticleExplosion(e.Graphics);
            }
        }

        private void DrawParticleExplosion(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;

            int centerX = this.Width / 2;
            int centerY = this.Height / 2;
            
            float progress = (float)frame / totalFrames;
            
            // Main explosion light
            if (progress < 0.8f)
            {
                float explosionSize = 100 + progress * 600;
                using (Brush brush = new SolidBrush(Color.FromArgb(150, Color.Orange)))
                {
                    g.FillEllipse(brush, centerX - explosionSize/2, centerY - explosionSize/2, explosionSize, explosionSize);
                }
                
                // Outer glow
                float glowSize = explosionSize * 1.4f;
                using (Brush brush = new SolidBrush(Color.FromArgb(80, Color.Yellow)))
                {
                    g.FillEllipse(brush, centerX - glowSize/2, centerY - glowSize/2, glowSize, glowSize);
                }
            }

            // Draw particles
            foreach (var particle in particles)
            {
                if (particle.Life > 0)
                {
                    float alpha = (float)particle.Life / particle.MaxLife;
                    int alphaByte = (int)(alpha * 255);
                    
                    using (Brush brush = new SolidBrush(Color.FromArgb(alphaByte, particle.Color)))
                    {
                        float size = particle.Size * alpha;
                        g.FillEllipse(brush, particle.X - size/2, particle.Y - size/2, size, size);
                    }
                }
            }

            // Shockwave rings
            for (int i = 0; i < 2; i++)
            {
                float ringProgress = progress - (i * 0.2f);
                if (ringProgress > 0 && ringProgress < 0.8f)
                {
                    float ringSize = 200 + ringProgress * 800;
                    int alpha = (int)(180 * (1 - ringProgress));
                    using (Pen ringPen = new Pen(Color.FromArgb(alpha, Color.White), 4))
                    {
                        g.DrawEllipse(ringPen, centerX - ringSize/2, centerY - ringSize/2, ringSize, ringSize);
                    }
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            animationTimer?.Stop();
            animationTimer?.Dispose();
            explosionImage?.Dispose();
            gifBox?.Image?.Dispose();
            gifBox?.Dispose();
        }
    }

    // Update the Particle class to include the new properties:
    public class Particle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float SpeedX { get; set; }
        public float SpeedY { get; set; }
        public int Life { get; set; }
        public int MaxLife { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
    }
        
    public class CustomTimeDialog : Form
    {
        private NumericUpDown hoursInput;
        private NumericUpDown minutesInput;
        private NumericUpDown secondsInput;
        private Button okButton;
        
        private CheckBox millisecondsCheckbox;
        
        public int SelectedTime { get; private set; }
        public bool ShowMilliseconds { get; private set; }
        
        public CustomTimeDialog()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Set Custom Time";
            this.Size = new Size(320, 220);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(10);
            
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 3;
            table.RowCount = 5;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            
            // Labels
            table.Controls.Add(new Label { Text = "Hours", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill }, 0, 0);
            table.Controls.Add(new Label { Text = "Minutes", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill }, 1, 0);
            table.Controls.Add(new Label { Text = "Seconds", TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill }, 2, 0);
            
            // Inputs
            hoursInput = new NumericUpDown();
            hoursInput.Minimum = 0;
            hoursInput.Maximum = 23;
            hoursInput.Value = 0;
            hoursInput.TextAlign = HorizontalAlignment.Center;
            hoursInput.Dock = DockStyle.Fill;
            table.Controls.Add(hoursInput, 0, 1);
            
            minutesInput = new NumericUpDown();
            minutesInput.Minimum = 0;
            minutesInput.Maximum = 59;
            minutesInput.Value = 0;
            minutesInput.TextAlign = HorizontalAlignment.Center;
            minutesInput.Dock = DockStyle.Fill;
            table.Controls.Add(minutesInput, 1, 1);
            
            secondsInput = new NumericUpDown();
            secondsInput.Minimum = 0;
            secondsInput.Maximum = 59;
            secondsInput.Value = 40;
            secondsInput.TextAlign = HorizontalAlignment.Center;
            secondsInput.Dock = DockStyle.Fill;
            table.Controls.Add(secondsInput, 2, 1);
            
            // Quick buttons
            var quickButtons = new[] { 10, 35, 60, 300, 600, 1800 };
            string[] quickLabels = { "10s", "35s", "1m", "5m", "10m", "30m" };
            
            for (int i = 0; i < quickButtons.Length; i++)
            {
                var button = new Button();
                button.Text = quickLabels[i];
                button.Tag = quickButtons[i];
                button.Click += QuickTimeButton_Click;
                button.Dock = DockStyle.Fill;
                table.Controls.Add(button, i % 3, 2 + i / 3);
            }
            
            // Milliseconds checkbox
            millisecondsCheckbox = new CheckBox();
            millisecondsCheckbox.Text = "Show milliseconds when < 10 seconds";
            millisecondsCheckbox.Checked = true;
            millisecondsCheckbox.Dock = DockStyle.Fill;
            table.Controls.Add(millisecondsCheckbox, 0, 4);
            table.SetColumnSpan(millisecondsCheckbox, 3);
            
            // Buttons
            okButton = new Button();
            okButton.Text = "Start Bomb";
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += (s, e) => {
                SelectedTime = (int)hoursInput.Value * 3600 + (int)minutesInput.Value * 60 + (int)secondsInput.Value;
                ShowMilliseconds = millisecondsCheckbox.Checked;
                if (SelectedTime < 1) SelectedTime = 1;
            };
            okButton.Dock = DockStyle.Fill;
            table.Controls.Add(okButton, 0, 5);
            
            
            
            this.Controls.Add(table);
            
            this.AcceptButton = okButton;
            
        }

        private void QuickTimeButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is int seconds)
            {
                hoursInput.Value = seconds / 3600;
                minutesInput.Value = (seconds % 3600) / 60;
                secondsInput.Value = seconds % 60;
            }
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            hoursInput.Select(0, hoursInput.Text.Length);
        }
    }

    public class SettingsForm : Form
    {
        private BombForm bombForm;
        private CheckBox topMostCheckbox;
        private CheckBox clickThroughCheckbox;
        private CheckBox millisecondsCheckbox;
        private Button positionButton;
        private Button saveButton;
       
        
        public SettingsForm(BombForm bombForm)
        {
            this.bombForm = bombForm;
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Bomb Timer Settings";
            this.Size = new Size(300, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(10);
            
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.RowCount = 6;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            
            topMostCheckbox = new CheckBox();
            topMostCheckbox.Text = "Always on Top";
            topMostCheckbox.Dock = DockStyle.Fill;
            table.Controls.Add(topMostCheckbox, 0, 0);
            
            clickThroughCheckbox = new CheckBox();
            clickThroughCheckbox.Text = "Click Through (transparent to mouse)";
            clickThroughCheckbox.Dock = DockStyle.Fill;
            table.Controls.Add(clickThroughCheckbox, 0, 1);
            
            millisecondsCheckbox = new CheckBox();
            millisecondsCheckbox.Text = "Show milliseconds when < 10 seconds";
            millisecondsCheckbox.Dock = DockStyle.Fill;
            table.Controls.Add(millisecondsCheckbox, 0, 2);
            
            positionButton = new Button();
            positionButton.Text = "Set Custom Position...";
            positionButton.Click += PositionButton_Click;
            positionButton.Dock = DockStyle.Fill;
            table.Controls.Add(positionButton, 0, 3);
            
            // Buttons
            saveButton = new Button();
            saveButton.Text = "Save Settings";
            saveButton.DialogResult = DialogResult.OK;
            saveButton.Click += SaveButton_Click;
            saveButton.Dock = DockStyle.Fill;
            table.Controls.Add(saveButton, 0, 5);
            
            this.Controls.Add(table);
        }

        private void LoadSettings()
        {
            topMostCheckbox.Checked = bombForm.AlwaysOnTop;
            clickThroughCheckbox.Checked = bombForm.ClickThrough;
            millisecondsCheckbox.Checked = bombForm.ShowMilliseconds;
        }

        private void PositionButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new PositionDialog(bombForm.Location))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    bombForm.SetPosition(dialog.SelectedPosition);
                }
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            bombForm.AlwaysOnTop = topMostCheckbox.Checked;
            bombForm.ClickThrough = clickThroughCheckbox.Checked;
            bombForm.ShowMilliseconds = millisecondsCheckbox.Checked;
            
            if (bombForm.AlwaysOnTop)
                bombForm.TopMost = true;
                
            if (bombForm.ClickThrough)
                bombForm.SetWindowClickThrough(bombForm.Handle);
            else
                bombForm.SetWindowNormal(bombForm.Handle);
                
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    public class PositionDialog : Form
    {
        private NumericUpDown xInput;
        private NumericUpDown yInput;
        private Button okButton;
        
        private Button screenPositionButton;
        
        public Point SelectedPosition { get; private set; }
        
        public PositionDialog(Point currentPosition)
        {
            SelectedPosition = currentPosition;
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Set Bomb Position";
            this.Size = new Size(300, 180);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Padding = new Padding(10);
            
            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.RowCount = 4;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            
            // X coordinate
            table.Controls.Add(new Label { Text = "X Position:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            xInput = new NumericUpDown();
            xInput.Minimum = 0;
            xInput.Maximum = Screen.PrimaryScreen.Bounds.Width;
            xInput.Value = SelectedPosition.X;
            xInput.Dock = DockStyle.Fill;
            table.Controls.Add(xInput, 1, 0);
            
            // Y coordinate
            table.Controls.Add(new Label { Text = "Y Position:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            yInput = new NumericUpDown();
            yInput.Minimum = 0;
            yInput.Maximum = Screen.PrimaryScreen.Bounds.Height;
            yInput.Value = SelectedPosition.Y;
            yInput.Dock = DockStyle.Fill;
            table.Controls.Add(yInput, 1, 1);
            
            // Screen position button
            screenPositionButton = new Button();
            screenPositionButton.Text = "Get from Mouse Position";
            screenPositionButton.Click += ScreenPositionButton_Click;
            screenPositionButton.Dock = DockStyle.Fill;
            table.Controls.Add(screenPositionButton, 0, 2);
            table.SetColumnSpan(screenPositionButton, 2);
            
            // Buttons
            okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Click += (s, e) => SelectedPosition = new Point((int)xInput.Value, (int)yInput.Value);
            okButton.Dock = DockStyle.Fill;
            table.Controls.Add(okButton, 0, 3);
            
            
            this.Controls.Add(table);
            
            this.AcceptButton = okButton;
            
        }

        private void ScreenPositionButton_Click(object sender, EventArgs e)
        {
            this.Hide();
            MessageBox.Show("Move your mouse to the desired position and click OK", "Set Position", 
                          MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            var timer = new Timer();
            timer.Interval = 100;
            timer.Tick += (s, args) => {
                xInput.Value = Cursor.Position.X;
                yInput.Value = Cursor.Position.Y;
            };
            timer.Start();
            
            this.Show();
            timer.Stop();
        }
    }
    
    public class TrayApplication : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private BombForm bombForm;
        
        public TrayApplication()
        {
            bombForm = new BombForm();
            InitializeTrayIcon();
        }
        
        private void InitializeTrayIcon()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            
            // Quick start options
           
            menu.Items.Add("💥 Start 15s Bomb ", null, (s, e) => StartBomb(15));
            menu.Items.Add("⏰ Start 30s Bomb", null, (s, e) => StartBomb(30));
            menu.Items.Add("🔥 Start 1m Bomb", null, (s, e) => StartBomb(60));
            menu.Items.Add("⏳ Start 5m Bomb", null, (s, e) => StartBomb(300));
            menu.Items.Add("💀 Start 10m Bomb", null, (s, e) => StartBomb(600));
            menu.Items.Add("☠️ Start 30m Bomb", null, (s, e) => StartBomb(1800));
            menu.Items.Add("🏴 Start 1h Bomb", null, (s, e) => StartBomb(3600));
            
            menu.Items.Add("-");
            
            // Custom time option
            menu.Items.Add("🎯 Set Custom Time...", null, (s, e) => SetCustomTime());
            
            menu.Items.Add("-");

            // Control options
            menu.Items.Add("👁️ Toggle Visibility", null, (s, e) => ToggleBombVisibility());
            menu.Items.Add("⚙️ Settings", null, (s, e) => ShowSettings());
            menu.Items.Add("🛑 Stop Bomb", null, (s, e) => StopBomb());
            menu.Items.Add("-");
            menu.Items.Add("❌ Quit", null, (s, e) => ExitApplication());
            
            trayIcon = new NotifyIcon();
            trayIcon.Icon = CreateTrayIcon();
            trayIcon.Text = "CS:GO PNG Bomb Timer - Double click to start!";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            
            trayIcon.DoubleClick += (s, e) => StartBomb(35);
            trayIcon.BalloonTipClicked += (s, e) => ToggleBombVisibility();
            
            trayIcon.ShowBalloonTip(3000, "CS:GO PNG Bomb Timer", 
                "Double-click for 35s bomb!\nRight-click for more options.", 
                ToolTipIcon.Info);
        }
        
        private void ToggleBombVisibility()
        {
            if (bombForm.Visible)
            {
                bombForm.Hide();
                trayIcon.ShowBalloonTip(2000, "Bomb Hidden", 
                    "Timer still running in background\nClick this balloon to show", 
                    ToolTipIcon.Info);
            }
            else
            {
                bombForm.Show();
                bombForm.TopMost = bombForm.AlwaysOnTop;
                trayIcon.ShowBalloonTip(2000, "Bomb Visible", 
                    "Timer window shown", ToolTipIcon.Info);
            }
        }

        private void ShowSettings()
        {
            using (SettingsForm settings = new SettingsForm(bombForm))
            {
                settings.ShowDialog();
            }
        }
        
        private Icon CreateTrayIcon()
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Bomb body
                using (Brush bombBrush = new LinearGradientBrush(
                    new Point(8, 8), 
                    new Point(24, 24), 
                    Color.FromArgb(80, 80, 80), 
                    Color.FromArgb(120, 120, 120)))
                {
                    g.FillRectangle(bombBrush, 8, 8, 16, 16);
                }

                // Red light
                g.FillEllipse(Brushes.Red, 22, 12, 4, 4);

                // Timer display
                g.FillRectangle(Brushes.Black, 10, 12, 8, 6);
                using (Font font = new Font("Arial", 5, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.Lime))
                {
                    string timeText = bombForm.IsRunning() ? bombForm.GetCurrentTime() : "C4";
                    if (timeText.Length > 3) timeText = timeText.Substring(0, 3);
                    g.DrawString(timeText, font, textBrush, 11, 13);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }
        
        private void StartBomb(int seconds)
        {
            bombForm.StartCountdown(seconds);
            UpdateTrayIcon();
            trayIcon.ShowBalloonTip(2000, "Bomb Planted", 
                $"{FormatTime(seconds)} countdown started!", 
                ToolTipIcon.Warning);
        }

        private string FormatTime(int seconds)
        {
            if (seconds >= 3600)
                return $"{seconds / 3600}h {seconds % 3600 / 60}m {seconds % 60}s";
            else if (seconds >= 60)
                return $"{seconds / 60}m {seconds % 60}s";
            else
                return $"{seconds}s";
        }
        
        private void SetCustomTime()
        {
            using (CustomTimeDialog dialog = new CustomTimeDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int seconds = dialog.SelectedTime;
                    bombForm.ShowMilliseconds = dialog.ShowMilliseconds;
                    bombForm.SetCustomTimeAndStart(seconds);
                    UpdateTrayIcon();
                    trayIcon.ShowBalloonTip(2000, "Bomb Planted", 
                        $"Custom {FormatTime(seconds)} countdown started!", 
                        ToolTipIcon.Warning);
                }
            }
        }
        
        private void StopBomb()
        {
            bombForm.StopCountdown();
            UpdateTrayIcon();
            trayIcon.ShowBalloonTip(2000, "Bomb Defused", 
                "Timer stopped", ToolTipIcon.Info);
        }

        private void UpdateTrayIcon()
        {
            trayIcon.Icon = CreateTrayIcon();
        }
        
        private void ExitApplication()
        {
            bombForm.StopCountdown();
            trayIcon.Visible = false;
            Application.Exit();
        }
    }
    
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) => {
                Console.WriteLine("Thread exception: " + e.Exception.Message);
                MessageBox.Show($"An error occurred: {e.Exception.Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                Console.WriteLine("Unhandled exception: " + e.ExceptionObject.ToString());
                MessageBox.Show($"A critical error occurred: {((Exception)e.ExceptionObject).Message}", "Error", 
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            
            Application.Run(new TrayApplication());
        }
    }
}