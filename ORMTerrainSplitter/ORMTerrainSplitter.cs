using PaintDotNet;
using PaintDotNet.Effects;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS8625

namespace ORMTerrainSplitterEffect
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => "ManfredAabye";
        public string Copyright => "Copyright © 2025";
        public string DisplayName => "Terrain Splitter";
        public Version Version => new Version(1, 0, 0, 0);
        public Uri WebsiteUri => new Uri("https://github.com/ManfredAabye");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMTerrainSplitter : Effect
    {
        public ORMTerrainSplitter()
            : base("Terrain Splitter", 
                   (System.Drawing.Image?)null,
                   "ORM",
                   new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        public override EffectConfigDialog? CreateConfigDialog()
        {
            return new TerrainSplitterDialog();
        }

        public override void Render(EffectConfigToken? parameters, RenderArgs dstArgs, RenderArgs srcArgs, 
            Rectangle[] rois, int startIndex, int length)
        {
            // No visual effect - just copy source
            for (int i = startIndex; i < startIndex + length; i++)
            {
                Rectangle rect = rois[i];
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    if (IsCancelRequested) return;
                    for (int x = rect.Left; x < rect.Right; x++)
                    {
                        dstArgs.Surface[x, y] = srcArgs.Surface[x, y];
                    }
                }
            }
        }

        public static void ExportTiles(Surface source, int gridX, int gridY, int tileW, int tileH, string dir, string baseName)
        {
            using (Bitmap sourceBmp = source.CreateAliasedBitmap())
            {
                for (int y = 0; y < gridY; y++)
                {
                    for (int x = 0; x < gridX; x++)
                    {
                        Rectangle srcRect = new Rectangle(
                            x * tileW,
                            y * tileH,
                            tileW,
                            tileH);

                        using (Bitmap tile = new Bitmap(tileW, tileH, PixelFormat.Format24bppRgb))
                        {
                            using (Graphics g = Graphics.FromImage(tile))
                            {
                                g.Clear(Color.White);
                                g.DrawImage(
                                    sourceBmp,
                                    new Rectangle(0, 0, tileW, tileH),
                                    srcRect,
                                    GraphicsUnit.Pixel);
                            }

                            string filename = Path.Combine(dir, $"{baseName}_{x}_{y}.png");
                            tile.Save(filename, ImageFormat.Png);
                        }
                    }
                }
            }
        }
    }

    public class TerrainSplitterDialog : EffectConfigDialog
    {
        private NumericUpDown? gridXNumeric;
        private NumericUpDown? gridYNumeric;
        private NumericUpDown? tileWidthNumeric;
        private NumericUpDown? tileHeightNumeric;
        private Button? exportButton;
        private Button? closeButton;

        public TerrainSplitterDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Terrain Splitter";
            ClientSize = new Size(300, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            int labelWidth = 120;
            int controlWidth = 150;
            int x = 10;
            int y = 10;
            int rowHeight = 30;

            // Grid X
            Label lblGridX = new Label { Text = "Grid X (Spalten):", Location = new Point(x, y + 3), Width = labelWidth };
            gridXNumeric = new NumericUpDown { Location = new Point(x + labelWidth, y), Width = controlWidth, Minimum = 1, Maximum = 64, Value = 4 };
            Controls.Add(lblGridX);
            Controls.Add(gridXNumeric);
            y += rowHeight;

            // Grid Y
            Label lblGridY = new Label { Text = "Grid Y (Zeilen):", Location = new Point(x, y + 3), Width = labelWidth };
            gridYNumeric = new NumericUpDown { Location = new Point(x + labelWidth, y), Width = controlWidth, Minimum = 1, Maximum = 64, Value = 4 };
            Controls.Add(lblGridY);
            Controls.Add(gridYNumeric);
            y += rowHeight;

            // Tile Width
            Label lblTileW = new Label { Text = "Kachel Breite (px):", Location = new Point(x, y + 3), Width = labelWidth };
            tileWidthNumeric = new NumericUpDown { Location = new Point(x + labelWidth, y), Width = controlWidth, Minimum = 64, Maximum = 4096, Value = 256 };
            Controls.Add(lblTileW);
            Controls.Add(tileWidthNumeric);
            y += rowHeight;

            // Tile Height
            Label lblTileH = new Label { Text = "Kachel Höhe (px):", Location = new Point(x, y + 3), Width = labelWidth };
            tileHeightNumeric = new NumericUpDown { Location = new Point(x + labelWidth, y), Width = controlWidth, Minimum = 64, Maximum = 4096, Value = 256 };
            Controls.Add(lblTileH);
            Controls.Add(tileHeightNumeric);
            y += rowHeight + 10;

            // Buttons
            exportButton = new Button { Text = "Export", Location = new Point(x, y), Width = 130, Height = 50 };
            exportButton.Click += ExportButton_Click;
            Controls.Add(exportButton);

            closeButton = new Button { Text = "Beenden", Location = new Point(x + 140, y), Width = 130, Height = 50, DialogResult = DialogResult.Cancel };
            Controls.Add(closeButton);
            CancelButton = closeButton;
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            if (gridXNumeric == null || gridYNumeric == null || tileWidthNumeric == null || tileHeightNumeric == null)
                return;

            int gridX = (int)gridXNumeric.Value;
            int gridY = (int)gridYNumeric.Value;
            int tileW = (int)tileWidthNumeric.Value;
            int tileH = (int)tileHeightNumeric.Value;

            Surface? surface = EnvironmentParameters?.SourceSurface;
            if (surface == null) return;

            int requiredW = gridX * tileW;
            int requiredH = gridY * tileH;

            if (surface.Width != requiredW || surface.Height != requiredH)
            {
                MessageBox.Show(
                    $"Bildgröße stimmt nicht überein!\n\n" +
                    $"Benötigt: {requiredW}×{requiredH}\n" +
                    $"Aktuell: {surface.Width}×{surface.Height}",
                    "Validierung fehlgeschlagen",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Wähle Ordner und Dateinamen für Export";
                dlg.Filter = "PNG (*.png)|*.png";
                dlg.FileName = "terrain.png";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string baseName = Path.GetFileNameWithoutExtension(dlg.FileName);
                    string? dir = Path.GetDirectoryName(dlg.FileName);

                    if (!string.IsNullOrEmpty(dir))
                    {
                        try
                        {
                            ORMTerrainSplitter.ExportTiles(surface, gridX, gridY, tileW, tileH, dir, baseName);
                            MessageBox.Show(
                                $"{gridX * gridY} Dateien erfolgreich exportiert!\n\nOrdner: {dir}",
                                "Export abgeschlossen",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Fehler beim Export:\n{ex.Message}",
                                "Export fehlgeschlagen",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        protected override void InitialInitToken()
        {
            theEffectToken = null;
        }

        protected override void InitTokenFromDialog()
        {
        }

        protected override void InitDialogFromToken(EffectConfigToken? effectToken)
        {
        }
    }
}
