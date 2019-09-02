using System;

namespace Bechtle.A365.ConfigService.Projection.Metrics
{
    public class RabbitMetricsReporterOptions
    {
        public string AppId { get; set; }

        public bool Enabled { get; set; }

        public string Exchange { get; set; }

        public TimeSpan FlushInterval { get; set; }

        public string Hostname { get; set; }

        public string Password { get; set; }

        public int Port { get; set; }

        public string Topic { get; set; }

        public string Username { get; set; }
    }
}