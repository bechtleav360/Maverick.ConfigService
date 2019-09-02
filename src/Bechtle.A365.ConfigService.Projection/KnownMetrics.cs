using System;
using App.Metrics;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using App.Metrics.Timer;

namespace Bechtle.A365.ConfigService.Projection
{
    public static class KnownMetrics
    {
        /// <summary>
        ///     Context used for Contained Metrics.
        ///     Chosen to overlap with AspNetCore-Metrics and be aggregatable / viewable via the same names
        /// </summary>
        public static string ContextName = "Application.HttpRequests";

        public static readonly CounterOptions ActiveRequestCount = new CounterOptions
        {
            Context = ContextName,
            Name = "Active",
            MeasurementUnit = Unit.Custom("Active Requests")
        };

        public static readonly Func<double, ApdexOptions> Apdex = apdexTSeconds => new ApdexOptions
        {
            Context = ContextName,
            Name = "Apdex",
            ApdexTSeconds = apdexTSeconds
        };

        public static readonly MeterOptions EndpointErrorRequestRate = new MeterOptions
        {
            Context = ContextName,
            Name = "Error Rate Per Endpoint",
            MeasurementUnit = Unit.Requests
        };

        public static readonly GaugeOptions EndpointOneMinuteErrorPercentageRate = new GaugeOptions
        {
            Context = ContextName,
            Name = "One Minute Error Percentage Rate Per Endpoint",
            MeasurementUnit = Unit.Requests
        };

        public static readonly TimerOptions EndpointRequestTransactionDuration = new TimerOptions
        {
            Context = ContextName,
            Name = "Transactions Per Endpoint",
            MeasurementUnit = Unit.Requests
        };

        public static readonly MeterOptions ErrorRequestRate = new MeterOptions
        {
            Context = ContextName,
            Name = "Error Rate",
            MeasurementUnit = Unit.Requests
        };

        public static readonly GaugeOptions OneMinErrorPercentageRate = new GaugeOptions
        {
            Context = ContextName,
            Name = "One Minute Error Percentage Rate",
            MeasurementUnit = Unit.Requests
        };

        public static readonly HistogramOptions PostRequestSizeHistogram = new HistogramOptions
        {
            Context = ContextName,
            Name = "POST Size",
            MeasurementUnit = Unit.Bytes
        };

        public static readonly TimerOptions RequestTransactionDuration = new TimerOptions
        {
            Context = ContextName,
            Name = "Transactions",
            MeasurementUnit = Unit.Requests
        };

        public static readonly CounterOptions TotalErrorRequestCount = new CounterOptions
        {
            Context = ContextName,
            Name = "Errors",
            ResetOnReporting = true,
            MeasurementUnit = Unit.Errors
        };
    }
}