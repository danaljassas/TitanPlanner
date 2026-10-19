using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MissionPlanner.Utilities;
using Newtonsoft.Json;

namespace MissionPlanner.Controls
{
    public sealed class GoogleTerrainView : UserControl
    {
        private readonly WebView2 browser = new WebView2 { Dock = DockStyle.Fill };
        private readonly Label status = new Label { Dock = DockStyle.Bottom, Height = 28, AutoEllipsis = true };
        private readonly Timer telemetryTimer = new Timer { Interval = 200 };
        private readonly Map3D map;
        private readonly Action showNative;
        private bool ready;
        private bool initialized;

        public GoogleTerrainView(Map3D map, Action showNative)
        {
            this.map = map;
            this.showNative = showNative;
            BackColor = Color.FromArgb(44, 44, 44);
            ForeColor = Color.White;
            Controls.Add(browser);
            Controls.Add(status);
            Load += Initialize;
            telemetryTimer.Tick += (s, e) => SendTelemetry();
            VisibleChanged += (s, e) => telemetryTimer.Enabled = Visible && ready;
        }

        private async void Initialize(object sender, EventArgs e)
        {
            if (initialized)
                return;
            initialized = true;
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleTerrain");
                if (!File.Exists(Path.Combine(folder, "index.html")))
                    throw new FileNotFoundException("Google terrain viewer assets are missing. Rebuild StarboundPlanner.");
                string profile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "StarboundPlanner", "GoogleTerrainBrowser");
                var environment = await CoreWebView2Environment.CreateAsync(null, profile);
                if (IsDisposed)
                    return;
                await browser.EnsureCoreWebView2Async(environment);
                if (IsDisposed)
                    return;
                browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                browser.CoreWebView2.SetVirtualHostNameToFolderMapping("starbound-terrain.local", folder, CoreWebView2HostResourceAccessKind.DenyCors);
                browser.CoreWebView2.NavigationStarting += (s, args) =>
                {
                    if (!args.Uri.StartsWith("https://starbound-terrain.local/", StringComparison.OrdinalIgnoreCase))
                        args.Cancel = true;
                };
                browser.CoreWebView2.NewWindowRequested += (s, args) => args.Handled = true;
                browser.CoreWebView2.NavigationCompleted += (s, args) =>
                {
                    if (!args.IsSuccess)
                        status.Text = "Google 3D viewer page failed to load: " + args.WebErrorStatus;
                };
                browser.CoreWebView2.ProcessFailed += (s, args) =>
                {
                    ready = false;
                    telemetryTimer.Stop();
                    status.Text = "Google 3D browser stopped: " + args.ProcessFailedKind + ". Reopen the 3D viewer to retry.";
                };
                browser.CoreWebView2.WebMessageReceived += Receive;
                browser.Source = new Uri("https://starbound-terrain.local/index.html");
                status.Text = "Loading Google 3D terrain...";
            }
            catch (Exception ex)
            {
                status.Text = "Google 3D unavailable: " + ex.Message;
            }
        }

        private void Receive(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (!e.Source.StartsWith("https://starbound-terrain.local/", StringComparison.OrdinalIgnoreCase))
                return;
            string message = e.TryGetWebMessageAsString();
            if (message == "ready")
            {
                try
                {
                    string key = Environment.GetEnvironmentVariable("STARBOUND_GOOGLE_MAP_TILES_API_KEY");
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "StarboundPlanner", "GoogleMapsApiKey.bin");
                        if (!File.Exists(path))
                            throw new InvalidOperationException("Configure a Google Map Tiles API key locally, or use Native 3D.");
                        key = Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.CurrentUser));
                    }
                    browser.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "initialize", key }));
                }
                catch (Exception ex)
                {
                    status.Text = "Google 3D configuration error: " + ex.Message;
                }
            }
            else if (message == "native")
            {
                showNative();
            }
            else if (message == "tiles-ready")
            {
                ready = true;
                status.Text = "Google 3D terrain | drag to pan/orbit, scroll to zoom";
                telemetryTimer.Enabled = Visible;
                SendTelemetry();
            }
            else if (message == "tiles-error")
            {
                status.Text = "Google 3D terrain failed to load. Check network access, API restrictions, billing and coverage; Native 3D remains available.";
            }
            else if (message == "model-error")
            {
                status.Text = "Starbound drone model failed to load. Rebuild or reinstall the viewer assets.";
            }
        }

        private void SendTelemetry()
        {
            var cs = MainV2.comPort?.MAV?.cs;
            bool connected = MainV2.comPort?.BaseStream?.IsOpen == true || MainV2.comPort?.logreadmode == true;
            var position = connected && cs != null
                ? new PointLatLngAlt(cs.lat, cs.lng, cs.altasl)
                : map.LocationCenter;
            if (!connected)
            {
                var center = GCSViews.FlightData.instance?.gMapControl1?.Position;
                if (center.HasValue)
                    position = new PointLatLngAlt(center.Value.Lat, center.Value.Lng, 0);
            }
            if (position.Lat == 0 && position.Lng == 0)
                return;
            Send(new
            {
                type = "position",
                lat = position.Lat,
                lon = position.Lng,
                altitude = position.Alt,
                connected,
                heading = cs?.yaw ?? 0,
                pitch = cs?.pitch ?? 0,
                roll = cs?.roll ?? 0,
                quadrotor = MainV2.comPort?.MAV?.aptype == MAVLink.MAV_TYPE.QUADROTOR
            });
        }

        private void Send(object message)
        {
            if (ready && !IsDisposed && browser.CoreWebView2 != null)
                browser.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(message));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                telemetryTimer.Stop();
                telemetryTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
