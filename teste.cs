using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace TimerPositionTester
{
    public class TestApp : Form
    {
        private Bitmap bombImage;
        private Point timerPosition = new Point(87, 0);
        private int backgroundWidth = 80;
        private int backgroundHeight = 20;
        private int fontSize = 10;
        private int backgroundOpacity = 200;
        
        private PictureBox previewBox;
        private Button applyButton;
        private Label codeLabel;
        
        public TestApp()
        {
            InitializeComponent();
            LoadBombImage();
            UpdatePreview();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Timer Position & Width Adjuster - TEST TOOL";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(600, 500);
            
            // Create main layout panel
            TableLayoutPanel mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 2;
            mainLayout.RowCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            
            // Preview area
            previewBox = new PictureBox();
            previewBox.Dock = DockStyle.Fill;
            previewBox.BackColor = Color.Gray;
            previewBox.BorderStyle = BorderStyle.FixedSingle;
            previewBox.Paint += (s, e) => DrawPreview(e.Graphics);
            mainLayout.Controls.Add(previewBox, 0, 0);
            
            // Controls panel
            Panel controlsPanel = CreateControlsPanel();
            mainLayout.Controls.Add(controlsPanel, 1, 0);
            
            // Code output panel
            Panel codePanel = CreateCodePanel();
            mainLayout.Controls.Add(codePanel, 0, 1);
            mainLayout.SetColumnSpan(codePanel, 2);
            
            this.Controls.Add(mainLayout);
        }
        
        private Panel CreateControlsPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            
            int yPos = 10;
            int labelWidth = 80;
            int inputWidth = 60;
            int spacing = 30; // FIXED: Added spacing variable
            
            // Title
            Label titleLabel = new Label();
            titleLabel.Text = "Timer Settings";
            titleLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            titleLabel.Location = new Point(10, yPos);
            titleLabel.Size = new Size(200, 25);
            panel.Controls.Add(titleLabel);
            yPos += 35;
            
            // X Position
            CreateControlGroup(panel, "X Position:", ref yPos, labelWidth, inputWidth, spacing,
                timerPosition.X, 0, 200, (v) => timerPosition.X = v);
            
            // Y Position  
            CreateControlGroup(panel, "Y Position:", ref yPos, labelWidth, inputWidth, spacing,
                timerPosition.Y, 0, 200, (v) => timerPosition.Y = v);
            
            // Background Width
            CreateControlGroup(panel, "Width:", ref yPos, labelWidth, inputWidth, spacing,
                backgroundWidth, 40, 150, (v) => backgroundWidth = v);
            
            // Background Height
            CreateControlGroup(panel, "Height:", ref yPos, labelWidth, inputWidth, spacing,
                backgroundHeight, 15, 50, (v) => backgroundHeight = v);
            
            // Font Size
            CreateControlGroup(panel, "Font Size:", ref yPos, labelWidth, inputWidth, spacing,
                fontSize, 6, 20, (v) => fontSize = v);
            
            // Background Opacity
            CreateControlGroup(panel, "Opacity:", ref yPos, labelWidth, inputWidth, spacing,
                backgroundOpacity, 0, 255, (v) => backgroundOpacity = v);
            
            // Apply Button
            applyButton = new Button();
            applyButton.Text = "Apply to Main Code";
            applyButton.Location = new Point(20, yPos + 20);
            applyButton.Size = new Size(150, 30);
            applyButton.BackColor = Color.LightGreen;
            applyButton.Click += (s, e) => ShowCode();
            panel.Controls.Add(applyButton);
            
            // Reset Button
            Button resetButton = new Button();
            resetButton.Text = "Reset to Default";
            resetButton.Location = new Point(180, yPos + 20);
            resetButton.Size = new Size(100, 30);
            resetButton.Click += (s, e) => ResetToDefault();
            panel.Controls.Add(resetButton);
            
            return panel;
        }
        
        private void CreateControlGroup(Panel parent, string label, ref int yPos, int labelWidth, int inputWidth, int spacing,
                                      int defaultValue, int min, int max, Action<int> updateAction)
        {
            // Label
            Label controlLabel = new Label();
            controlLabel.Text = label;
            controlLabel.Location = new Point(10, yPos);
            controlLabel.Size = new Size(labelWidth, 20);
            parent.Controls.Add(controlLabel);
            
            // Numeric input
            NumericUpDown input = new NumericUpDown();
            input.Location = new Point(labelWidth + 15, yPos - 3);
            input.Size = new Size(inputWidth, 20);
            input.Minimum = min;
            input.Maximum = max;
            input.Value = defaultValue;
            input.ValueChanged += (s, e) => {
                updateAction((int)input.Value);
                UpdatePreview();
                UpdateCodeDisplay();
            };
            parent.Controls.Add(input);
            
            // Trackbar for fine adjustment
            TrackBar trackbar = new TrackBar();
            trackbar.Location = new Point(labelWidth + inputWidth + 25, yPos - 3);
            trackbar.Size = new Size(120, 20);
            trackbar.Minimum = min;
            trackbar.Maximum = max;
            trackbar.Value = defaultValue;
            trackbar.ValueChanged += (s, e) => {
                input.Value = trackbar.Value;
                updateAction(trackbar.Value);
                UpdatePreview();
                UpdateCodeDisplay();
            };
            parent.Controls.Add(trackbar);
            
            // Sync input and trackbar
            input.ValueChanged += (s, e) => trackbar.Value = (int)input.Value;
            
            yPos += spacing;
        }
        
        private Panel CreateCodePanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.BorderStyle = BorderStyle.FixedSingle;
            
            // Title
            Label titleLabel = new Label();
            titleLabel.Text = "Code to Copy to Main Application:";
            titleLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            titleLabel.Location = new Point(10, 10);
            titleLabel.Size = new Size(300, 20);
            panel.Controls.Add(titleLabel);
            
            // Code display
            codeLabel = new Label();
            codeLabel.Location = new Point(10, 40);
            codeLabel.Size = new Size(750, 80);
            codeLabel.Font = new Font("Consolas", 9);
            codeLabel.Text = GetCurrentCode();
            panel.Controls.Add(codeLabel);
            
            // Copy button
            Button copyButton = new Button();
            copyButton.Text = "Copy Code to Clipboard";
            copyButton.Location = new Point(10, 130);
            copyButton.Size = new Size(150, 25);
            copyButton.Click += (s, e) => CopyCodeToClipboard();
            panel.Controls.Add(copyButton);
            
            return panel;
        }
        
        private void LoadBombImage()
        {
            try
            {
                if (File.Exists("csgo_bomb.png"))
                {
                    bombImage = new Bitmap("csgo_bomb.png");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bomb image: " + ex.Message);
            }
        }
        
        private void DrawPreview(Graphics g)
        {
            g.Clear(Color.Gray);
            
            if (bombImage != null)
            {
                // Center the bomb image in the preview
                int x = (previewBox.Width - bombImage.Width) / 2;
                int y = (previewBox.Height - bombImage.Height) / 2;
                g.DrawImage(bombImage, x, y);
                
                // Draw timer at adjusted position (relative to bomb image)
                DrawTimerOnBomb(g, x, y);
                
                // Draw crosshairs at timer position
                DrawCrosshairs(g, x + timerPosition.X, y + timerPosition.Y);
                
                // Draw info text
                DrawInfoText(g);
            }
            else
            {
                // Draw placeholder
                using (Font font = new Font("Arial", 14))
                using (Brush brush = new SolidBrush(Color.White))
                {
                    g.DrawString("No bomb image found.\nPlace 'csgo_bomb.png' in same folder.", 
                                font, brush, 20, 20);
                }
            }
        }
        
        private void DrawTimerOnBomb(Graphics g, int offsetX, int offsetY)
        {
            string timerText = "40.0";
            
            using (Font timerFont = new Font("Consolas", fontSize, FontStyle.Bold))
            using (Brush textBrush = new SolidBrush(Color.Lime))
            using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(backgroundOpacity, 0, 0, 0)))
            {
                SizeF textSize = g.MeasureString(timerText, timerFont);
                
                Rectangle textBg = new Rectangle(
                    offsetX + timerPosition.X, 
                    offsetY + timerPosition.Y, 
                    backgroundWidth, 
                    backgroundHeight
                );
                g.FillRectangle(backgroundBrush, textBg);
                
                // Center text in background
                float textX = offsetX + timerPosition.X + (backgroundWidth - textSize.Width) / 2;
                float textY = offsetY + timerPosition.Y + (backgroundHeight - textSize.Height) / 2;
                
                g.DrawString(timerText, timerFont, textBrush, textX, textY);
            }
        }
        
        private void DrawCrosshairs(Graphics g, int x, int y)
        {
            using (Pen redPen = new Pen(Color.Red, 1))
            {
                // Horizontal line
                g.DrawLine(redPen, x - 15, y, x + 15, y);
                // Vertical line
                g.DrawLine(redPen, x, y - 15, x, y + 15);
                // Circle
                g.DrawEllipse(redPen, x - 5, y - 5, 10, 10);
            }
        }
        
        private void DrawInfoText(Graphics g)
        {
            using (Font infoFont = new Font("Arial", 9))
            using (Brush infoBrush = new SolidBrush(Color.Yellow))
            {
                string info = $"Position: ({timerPosition.X}, {timerPosition.Y}) | " +
                             $"Size: {backgroundWidth}×{backgroundHeight} | " +
                             $"Font: {fontSize}pt | Opacity: {backgroundOpacity}";
                g.DrawString(info, infoFont, infoBrush, 10, 10);
            }
        }
        
        private void UpdatePreview()
        {
            previewBox.Invalidate();
        }
        
        private void UpdateCodeDisplay()
        {
            codeLabel.Text = GetCurrentCode();
        }
        
        private string GetCurrentCode()
        {
            return $@"// Add these variables to your BombForm class:

private Point timerPosition = new Point({timerPosition.X}, {timerPosition.Y});
private int backgroundWidth = {backgroundWidth};
private int backgroundHeight = {backgroundHeight};
private int fontSize = {fontSize};
private int backgroundOpacity = {backgroundOpacity};

// Replace your DrawTimerOnBomb method with:

private void DrawTimerOnBomb(Graphics g)
{{
    int remainingSeconds = (int)Math.Ceiling(remainingTime);
    
    Color textColor = Color.Lime;
    if (remainingSeconds <= 10) textColor = Color.Yellow;
    if (remainingSeconds <= 5) textColor = Color.Red;
    if (isExploding) textColor = Color.Orange;
    
    using (Font timerFont = new Font(""Consolas"", fontSize, FontStyle.Bold))
    using (Brush textBrush = new SolidBrush(textColor))
    using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(backgroundOpacity, 0, 0, 0)))
    {{
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
    }}
}}";
        }
        
        private void ShowCode()
        {
            UpdateCodeDisplay();
            MessageBox.Show("Code has been updated in the display below.\n\n" +
                          "Copy the code and replace the corresponding parts in your main application.", 
                          "Code Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void CopyCodeToClipboard()
        {
            Clipboard.SetText(GetCurrentCode());
            MessageBox.Show("Code copied to clipboard!\n\n" +
                          "Now paste it into your main CSGOPngBombTimer.cs file.", 
                          "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void ResetToDefault()
        {
            timerPosition = new Point(87, 0);
            backgroundWidth = 80;
            backgroundHeight = 20;
            fontSize = 10;
            backgroundOpacity = 200;
            
            UpdatePreview();
            UpdateCodeDisplay();
        }
        
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestApp());
        }
    }
}