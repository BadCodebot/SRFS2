using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using SRFS.Model;
using TrackUtility.Model;
using MoreLinq;
using System.ComponentModel;

namespace TrackUtility.Controls {
    /// <summary>
    /// Interaction logic for TrackControl.xaml
    /// </summary>
    public partial class TrackControl : UserControl {
        public TrackControl() {
            InitializeComponent();
        }

        private const int nHorizontalSquaresGoal = 200;
        private const int squareSize = 4;

        public void Open(FileSystem fs) {
            _data = new TrackStatusWindowModel(fs);
            DataContext = _data;

            // Should change this to get dpi or otherwise calculate it

            int clustersPerSquare = Math.Max(1, Configuration.Geometry.DataClustersPerTrack / nHorizontalSquaresGoal);

            int nDataSquares = (Configuration.Geometry.DataClustersPerTrack + clustersPerSquare - 1) / clustersPerSquare;
            int nParitySquares = (Configuration.Geometry.ParityClustersPerTrack + clustersPerSquare - 1) / clustersPerSquare;
            int nSquares = nDataSquares + nParitySquares;

            Bitmap = new WriteableBitmap(
                squareSize * nSquares + 1,
                squareSize * Configuration.Geometry.TrackCount + 1, 96, 96, PixelFormats.Bgr32, null);
            image.Source = Bitmap;

            Bitmap.Lock();
            Bitmap.FillRectangle(0, 0, squareSize * nSquares + 2, squareSize * Configuration.Geometry.TrackCount + 2, Colors.White);

            for (int r = 0; r < Configuration.Geometry.TrackCount; r++) {
                int c = 0;
                Track t = _data.GetTrack(r);
                foreach (var a in t.DataClusters.Batch(clustersPerSquare)) {
                    Color color;
                    if (a.Any(x => !_data.GetClusterState(x).IsSystem() && _data.GetClusterState(x).IsModified())) color = Colors.Red;
                    else if (a.Any(x => _data.GetClusterState(x).IsUsed())) color = Colors.LightGreen;
                    else if (a.Any(x => _data.GetClusterState(x).IsSystem())) color = Colors.Violet;
                    else color = Colors.Gray;

                    Bitmap.FillRectangle(c * squareSize + 1, r * squareSize + 1, c * squareSize + squareSize, r * squareSize + squareSize, color);
                    c++;
                }
                foreach (var a in t.ParityClusters.Batch(clustersPerSquare)) {
                    Color color;
                    if (a.All(x => !_data.GetClusterState(x).IsUnwritten())) color = Colors.Blue;
                    else color = Colors.Gray;

                    Bitmap.FillRectangle(c * squareSize + 1, r * squareSize + 1, c * squareSize + squareSize, r * squareSize + squareSize, color);
                    c++;
                }
            }
            Bitmap.Unlock();

        }

        private async void Button_Click(object sender, RoutedEventArgs e) {
            int clustersPerSquare = Math.Max(1, Configuration.Geometry.DataClustersPerTrack / nHorizontalSquaresGoal);

            int nDataSquares = (Configuration.Geometry.DataClustersPerTrack + clustersPerSquare - 1) / clustersPerSquare;
            int nParitySquares = (Configuration.Geometry.ParityClustersPerTrack + clustersPerSquare - 1) / clustersPerSquare;
            int nSquares = nDataSquares + nParitySquares;

            cts = new CancellationTokenSource();
            cancelButton.IsEnabled = true;
            calculateParityButton.IsEnabled = false;

            for (int i = 0; i < Configuration.Geometry.TrackCount && !cts.IsCancellationRequested; i++) {
                Track t = _data.GetTrack(i);
                if (!t.UpToDate) {
                    Track.UpdateParityStatus status = new Track.UpdateParityStatus();
                    void h(object s2, PropertyChangedEventArgs e2)
                    {
                        Dispatcher.Invoke(() => {
                            Color color;
                            int clusterFinished = status.Cluster;
                            int c = 0;
                            if (clusterFinished < Configuration.Geometry.DataClustersPerTrack) {
                                color = Colors.LightGreen;
                                if (clusterFinished % clustersPerSquare == clustersPerSquare - 1) {
                                    c = clusterFinished / clustersPerSquare;
                                } else if (clusterFinished == Configuration.Geometry.DataClustersPerTrack - 1) {
                                    c = nDataSquares - 1;
                                } else {
                                    return;
                                }
                            } else {
                                clusterFinished -= Configuration.Geometry.DataClustersPerTrack;
                                color = Colors.Blue;
                                if (clusterFinished % clustersPerSquare == clustersPerSquare - 1) {
                                    c = nDataSquares + clusterFinished / clustersPerSquare;
                                } else if (clusterFinished == Configuration.Geometry.ParityClustersPerTrack - 1) {
                                    c = nDataSquares + nParitySquares - 1;
                                } else {
                                    return;
                                }
                            }

                            Bitmap.Lock();
                            Bitmap.FillRectangle(
                                c * squareSize + 1, i * squareSize + 1,
                                c * squareSize + squareSize, i * squareSize + squareSize,
                                color);
                            Bitmap.AddDirtyRect(new Int32Rect(c * squareSize + 1, i * squareSize + 1, squareSize - 1, squareSize - 1));
                            Bitmap.Unlock();
                        });
                    }
                    status.PropertyChanged += h;
                    await t.UpdateParity(false, status, cts.Token);
                    if (cts.IsCancellationRequested) {

                        Bitmap.Lock();

                        int c = 0;
                        foreach (var a in t.DataClusters.Batch(clustersPerSquare)) {
                            Color color;
                            if (a.Any(x => !_data.GetClusterState(x).IsSystem() && _data.GetClusterState(x).IsModified())) color = Colors.Red;
                            else if (a.Any(x => _data.GetClusterState(x).IsUsed())) color = Colors.LightGreen;
                            else if (a.Any(x => _data.GetClusterState(x).IsSystem())) color = Colors.Violet;
                            else color = Colors.Gray;

                            Bitmap.FillRectangle(c * squareSize + 1, i * squareSize + 1, c * squareSize + squareSize, i * squareSize + squareSize, color);
                            c++;
                        }
                        foreach (var a in t.ParityClusters.Batch(clustersPerSquare)) {
                            Color color;
                            if (a.All(x => !_data.GetClusterState(x).IsUnwritten())) color = Colors.Blue;
                            else color = Colors.Gray;

                            Bitmap.FillRectangle(c * squareSize + 1, i * squareSize + 1, c * squareSize + squareSize, i * squareSize + squareSize, color);
                            c++;
                        }

                        Bitmap.AddDirtyRect(new Int32Rect(0, i * squareSize + 1, nSquares * squareSize + 1, squareSize - 1));
                        Bitmap.Unlock();
                    } else {
                        _data.ProtectedTrackCount++;
                    }
                    status.PropertyChanged -= h;
                }
            }

            calculateParityButton.IsEnabled = true;
            cancelButton.IsEnabled = false;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            cts?.Cancel();
        }

        private WriteableBitmap Bitmap;
        private CancellationTokenSource cts = null;
        private TrackStatusWindowModel _data;
    }
}
