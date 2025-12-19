using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Direct2D1;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ORMForm
{
    [PluginSupportInfo(typeof(PluginSupportInfo))]
    public class ORMFormEffect : Effect
    {
        public ORMFormEffect()
            : base("ORM Form Exporter", (IBitmapSource?)null, "ORM", new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        public override EffectConfigDialog CreateConfigDialog()
        {
            return new ORMFormConfigDialog();
        }

        protected override void OnSetRenderInfo(EffectConfigToken? parameters, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            base.OnSetRenderInfo(parameters, dstArgs, srcArgs);
        }

        public override void Render(EffectConfigToken? parameters, RenderArgs dstArgs, RenderArgs srcArgs, Rectangle[] rois, int startIndex, int length)
        {
            // Kein Rendering nötig - nur Export
        }
    }

    public class ORMFormConfigDialog : EffectConfigDialog
    {
        private Button exportButton;
        private TextBox outputBox;
        private NumericUpDown toleranceInput;
        private Label statusLabel;

        public ORMFormConfigDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "ORM Form Exporter";
            this.ClientSize = new Size(600, 500);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblTolerance = new Label()
            {
                Text = "Zauberstab Toleranz (0-100%):",
                Location = new Point(20, 20),
                Width = 200
            };

            toleranceInput = new NumericUpDown()
            {
                Location = new Point(230, 18),
                Width = 100,
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                DecimalPlaces = 1
            };

            exportButton = new Button()
            {
                Text = "Auswahl als XAML exportieren",
                Location = new Point(20, 60),
                Width = 250,
                Height = 35
            };
            exportButton.Click += ExportButton_Click;

            statusLabel = new Label()
            {
                Text = "Bereit",
                Location = new Point(20, 105),
                Width = 560,
                Height = 20,
                ForeColor = Color.Blue
            };

            outputBox = new TextBox()
            {
                Location = new Point(20, 135),
                Size = new Size(560, 330),
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            this.Controls.AddRange(new Control[] { lblTolerance, toleranceInput, exportButton, statusLabel, outputBox });
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            try
            {
                statusLabel.Text = "Verarbeite Auswahl...";
                statusLabel.ForeColor = Color.Blue;
                Application.DoEvents();

                Surface src = this.EnvironmentParameters.SourceSurface;
                Bitmap sourceBitmap = src.CreateAliasedBitmap();

                if (sourceBitmap == null)
                {
                    MessageBox.Show("Kein Bild geladen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verwende Bildmitte als Startpunkt
                int seedX = sourceBitmap.Width / 2;
                int seedY = sourceBitmap.Height / 2;
                Color seedColor = sourceBitmap.GetPixel(seedX, seedY);
                double tolerance = (double)toleranceInput.Value;

                // Führe Magic Wand Selection aus
                bool[,] selectedArea = MagicWandSelection(sourceBitmap, new Point(seedX, seedY), tolerance);

                // Finde Kontur
                List<Point> contour = ExtractLargestContour(sourceBitmap, selectedArea);

                if (contour == null || contour.Count == 0)
                {
                    MessageBox.Show("Keine Kontur gefunden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    statusLabel.Text = "Keine Kontur gefunden";
                    statusLabel.ForeColor = Color.Red;
                    return;
                }

                // Vereinfache Kontur
                List<Point> simplified = SimplifyContour(contour, 2);

                // Konvertiere zu XAML
                string xaml = ConvertToXaml(simplified);
                outputBox.Text = xaml;

                // Speichern anbieten
                SaveFileDialog saveDialog = new SaveFileDialog()
                {
                    Filter = "XAML Dateien (*.xaml)|*.xaml|Alle Dateien (*.*)|*.*",
                    DefaultExt = "xaml",
                    FileName = "contour.xaml"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, xaml);
                    statusLabel.Text = $"Erfolgreich exportiert: {Path.GetFileName(saveDialog.FileName)} ({simplified.Count} Punkte)";
                    statusLabel.ForeColor = Color.Green;
                }

                sourceBitmap.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = $"Fehler: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private bool[,] MagicWandSelection(Bitmap bitmap, Point seed, double tolerance)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            bool[,] selected = new bool[width, height];
            bool[,] visited = new bool[width, height];

            Color seedColor = bitmap.GetPixel(seed.X, seed.Y);
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(seed);
            visited[seed.X, seed.Y] = true;

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                Color currentColor = bitmap.GetPixel(p.X, p.Y);

                if (ColorDistance(seedColor, currentColor) <= tolerance)
                {
                    selected[p.X, p.Y] = true;

                    // 4-Nachbarschaft
                    Point[] neighbors = new[]
                    {
                        new Point(p.X - 1, p.Y),
                        new Point(p.X + 1, p.Y),
                        new Point(p.X, p.Y - 1),
                        new Point(p.X, p.Y + 1)
                    };

                    foreach (Point n in neighbors)
                    {
                        if (n.X >= 0 && n.X < width && n.Y >= 0 && n.Y < height && !visited[n.X, n.Y])
                        {
                            visited[n.X, n.Y] = true;
                            queue.Enqueue(n);
                        }
                    }
                }
            }

            return selected;
        }

        private double ColorDistance(Color c1, Color c2)
        {
            double rDiff = c1.R - c2.R;
            double gDiff = c1.G - c2.G;
            double bDiff = c1.B - c2.B;
            double aDiff = c1.A - c2.A;
            return Math.Sqrt(rDiff * rDiff + gDiff * gDiff + bDiff * bDiff + aDiff * aDiff) / 510.0 * 100.0;
        }

        private List<Point> ExtractLargestContour(Bitmap bitmap, bool[,] selectedArea)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            bool[,] visited = new bool[width, height];
            List<List<Point>> allContours = new List<List<Point>>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!visited[x, y] && selectedArea[x, y])
                    {
                        bool isEdge = false;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    if (!selectedArea[nx, ny])
                                    {
                                        isEdge = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    isEdge = true;
                                    break;
                                }
                            }
                            if (isEdge) break;
                        }

                        if (isEdge)
                        {
                            List<Point> contour = TraceContour(selectedArea, visited, x, y, width, height);
                            if (contour.Count > 20)
                            {
                                allContours.Add(contour);
                            }
                        }
                    }
                }
            }

            if (allContours.Count == 0) return new List<Point>();

            // Wähle größte Kontur
            List<Point> largest = allContours[0];
            int maxArea = 0;

            foreach (var contour in allContours)
            {
                int minX = contour.Min(p => p.X);
                int maxX = contour.Max(p => p.X);
                int minY = contour.Min(p => p.Y);
                int maxY = contour.Max(p => p.Y);
                int area = (maxX - minX) * (maxY - minY);

                if (area > maxArea)
                {
                    maxArea = area;
                    largest = contour;
                }
            }

            return largest;
        }

        private List<Point> TraceContour(bool[,] selectedArea, bool[,] visited, int startX, int startY, int width, int height)
        {
            List<Point> contour = new List<Point>();
            int[,] directions = new int[,] {
                {1, 0}, {1, 1}, {0, 1}, {-1, 1},
                {-1, 0}, {-1, -1}, {0, -1}, {1, -1}
            };

            int x = startX;
            int y = startY;
            int dir = 0;

            do
            {
                contour.Add(new Point(x, y));
                visited[x, y] = true;

                bool found = false;
                for (int i = 0; i < 8; i++)
                {
                    int checkDir = (dir + i) % 8;
                    int nx = x + directions[checkDir, 0];
                    int ny = y + directions[checkDir, 1];

                    if (nx >= 0 && nx < width && ny >= 0 && ny < height && selectedArea[nx, ny])
                    {
                        x = nx;
                        y = ny;
                        dir = (checkDir + 6) % 8;
                        found = true;
                        break;
                    }
                }

                if (!found) break;

            } while (!(x == startX && y == startY) && contour.Count < 100000);

            return contour;
        }

        private List<Point> SimplifyContour(List<Point> points, double epsilon)
        {
            if (points.Count < 3) return points;

            double dmax = 0;
            int index = 0;

            for (int i = 1; i < points.Count - 1; i++)
            {
                double d = PerpendicularDistance(points[i], points[0], points[points.Count - 1]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon)
            {
                List<Point> rec1 = SimplifyContour(points.GetRange(0, index + 1), epsilon);
                List<Point> rec2 = SimplifyContour(points.GetRange(index, points.Count - index), epsilon);

                return rec1.Take(rec1.Count - 1).Concat(rec2).ToList();
            }
            else
            {
                return new List<Point> { points[0], points[points.Count - 1] };
            }
        }

        private double PerpendicularDistance(Point pt, Point lineStart, Point lineEnd)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            double mag = Math.Sqrt(dx * dx + dy * dy);
            if (mag > 0.0)
            {
                dx /= mag;
                dy /= mag;
            }

            double pvx = pt.X - lineStart.X;
            double pvy = pt.Y - lineStart.Y;

            double pvdot = dx * pvx + dy * pvy;

            double dsx = pvdot * dx;
            double dsy = pvdot * dy;

            double ax = pvx - dsx;
            double ay = pvy - dsy;

            return Math.Sqrt(ax * ax + ay * ay);
        }

        private string ConvertToXaml(List<Point> points)
        {
            if (points.Count == 0) return "";

            // Erstelle Geometry-String im Format "F1 M x,y L x,y ... Z"
            StringBuilder geometryData = new StringBuilder();
            geometryData.Append("F1 M ");
            geometryData.Append($"{points[0].X},{points[0].Y}");

            for (int i = 1; i < points.Count; i++)
            {
                geometryData.Append($"L {points[i].X},{points[i].Y}");
                if (i < points.Count - 1)
                {
                    geometryData.Append(" ");
                }
            }

            geometryData.Append(" Z");

            // Erstelle SimpleGeometryShape im Paint.NET Format
            StringBuilder xaml = new StringBuilder();
            xaml.AppendLine("<ps:SimpleGeometryShape xmlns=\"clr-namespace:PaintDotNet.UI.Media;assembly=PaintDotNet.Framework\"");
            xaml.AppendLine("                        xmlns:ps=\"clr-namespace:PaintDotNet.Shapes;assembly=PaintDotNet.Framework\"");
            xaml.AppendLine("                        DisplayName=\"Contour\"");
            xaml.Append("                        Geometry=\"");
            xaml.Append(geometryData.ToString());
            xaml.AppendLine("\" />");

            return xaml.ToString();
        }
    }

    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string? Author => "ORM Tools";
        public string? Copyright => "Copyright © 2025";
        public string? DisplayName => "ORM Form Exporter";
        public Version? Version => new Version(1, 0, 0, 0);
        public Uri? WebsiteUri => null;
    }
}
